# Module: Contracts

This document defines what a contract means in this system and explains the lifecycle, structure, validation, and execution of the PAM contract type.

Related: [Domain Model](../domain-model.md) | [Calculation Engine](./calculation-engine.md) | [Risk Factors](./risk-factors.md)

---

## Business View

### What Is a Contract?

In this system, a **contract** is a financial agreement between two parties where one lends a principal amount and the other promises to repay it at maturity, along with interest and any fees, according to a defined schedule.

### What Is PAM?

**PAM (Principal At Maturity)** is the simplest type of fixed-income contract:

- A principal amount is exchanged at an initial date.
- Interest accrues over the life of the contract.
- Interest may be paid periodically or capitalised (added to the principal).
- The full principal is repaid as a single bullet payment at maturity.

This models instruments like zero-coupon bonds, bullet loans, and term deposits.

### Why Does the Contract Lifecycle Matter?

The lifecycle determines when money flows:

1. **Initial exchange** — the lender provides the principal to the borrower.
2. **Interest payments** — periodic cash flows from the borrower to the lender.
3. **Rate resets** — for variable-rate contracts, the interest rate is updated.
4. **Maturity** — the borrower repays the full principal.

If any event is calculated incorrectly, the wrong amount changes hands.

### Risks If Contracts Fail

- Incorrect schedules produce billing errors
- Missing maturity events result in unpaid principal
- Wrong rate-reset logic misprices variable-rate products

---

## Technical View

### Contract Interface

```csharp
// src/ActusInsurance.Core/Models/IContractTerms.cs
public interface IContractTerms
{
    string ContractID { get; }
    string ContractType { get; }
    string Currency { get; }
    DateTime StatusDate { get; }
    DateTime MaturityDate { get; }
}
```

`IContractScheduler<TTerms>` (in `src/ActusInsurance.Core/Contracts/IContractScheduler.cs`) is the generic processing interface:

```csharp
public interface IContractScheduler<TTerms> where TTerms : IContractTerms
{
    List<ContractEvent> Schedule(DateTime to, TTerms terms);
    List<ContractEvent> Apply(List<ContractEvent> events, TTerms terms, RiskFactorModel riskFactors);
}
```

`PrincipalAtMaturity` in `src/ActusInsurance.Core.CPU/Contracts/PAM.cs` implements this contract as static methods (not as a class implementing the interface, but following the same method signature pattern).

### Contract Structure (PamContractTerms)

See [Domain Model — PamContractTerms](../domain-model.md) for the full field table.

Key required fields for scheduling:
- `StatusDate` — as-of date; events before this date are filtered out
- `InitialExchangeDate` — start of the contract
- `MaturityDate` — end of the contract

Optional cycles (omit to disable that event type):
- `CycleOfInterestPayment` — e.g. `"P1M"` for monthly interest payments
- `CycleOfRateReset` — e.g. `"P3M"` for quarterly rate resets
- `CycleOfFee` — e.g. `"P1Y"` for annual fees
- `CycleOfScalingIndex` — scaling index update cycle

### Contract Lifecycle

```
flowchart TD
    IED[IED Initial Exchange]
    IP[IP Interest Payments]
    IPCI[IPCI Interest Capitalisation]
    RR[RR or RRF Rate Reset]
    FP[FP Fee Payment]
    SC[SC Scaling Update]
    PRD[PRD Purchase optional]
    TD[TD Termination optional]
    MD[MD Maturity]
    IED --> IP
    IP --> RR
    RR --> IP
    IP --> IPCI
    IPCI --> IP
    IP --> FP
    FP --> IP
    IP --> SC
    SC --> IP
    IP --> MD
    IED --> PRD
    PRD --> IP
    IP --> TD
    TD --> MD
```

**Legend:**
- `IED` — always first event; sets notional principal and rate
- `IP` / `IPCI` — mutually exclusive per date; determined by `CapitalizationEndDate`
- `RR` / `RRF` — rate reset; `RRF` used for the first reset if `NextResetRate` is set
- `FP` — fee payment; only if `CycleOfFee` is set
- `SC` — scaling; only if `ScalingEffect` contains `"I"` or `"N"`
- `PRD` — purchase; only if `PurchaseDate` is set
- `TD` — termination; only if `TerminationDate` is set
- `MD` — always last event

### Scheduling Logic

The `PrincipalAtMaturity.Schedule(DateTime to, PamContractTerms model)` method:

1. Creates a `BusinessDayAdjuster` from `model.BusinessDayConvention` and `model.Calendar`.
2. Adds `IED` and `MD` events unconditionally.
3. If `PurchaseDate` is set, adds a `PRD` event.
4. If `CycleOfInterestPayment` or `CycleAnchorDateOfInterestPayment` is set:
   - Generates dates via `ScheduleFactory.CreateSchedule(...)`.
   - Dates on or before `CapitalizationEndDate` become `IPCI`; after it become `IP`.
5. If `CycleOfRateReset` is set:
   - The first event after `StatusDate` is `RRF` (if `NextResetRate` is set), otherwise `RR`.
   - All subsequent rate-reset dates become `RR`.
6. If `CycleOfFee` is set, generates `FP` events.
7. If `ScalingEffect` contains `"I"` or `"N"`, generates `SC` events.
8. If `TerminationDate` is set, adds `IP` + `TD` at termination and removes later events.
9. Filters: removes events before `StatusDate` and after `to`.
10. Sorts the list using `ContractEvent.CompareTo` (time then type priority).

### Event Sort Order

When two events share the same `Time`, they are ordered by type priority:

```
IED (0) < IP (1) < IPCI (2) < PRD (3) < TD (4) < RR (5) < RRF (6) < FP (7) < SC (8) < MD (9) < CD (10)
```

Source: `ContractEvent.GetEventTypePriority()` in `src/ActusInsurance.Core/Events/ContractEvent.cs`

### Apply Logic

`PrincipalAtMaturity.Apply(events, model, riskFactors)`:

1. Calls `InitializeStateSpace(model)` to build the initial `StateSpace`.
2. Iterates sorted events; calls `evt.Evaluate(ref states, model, riskFactors, timeAdjuster)` for each.
3. If `PurchaseDate` is set, removes non-AD events before `PurchaseDate` and IP events exactly at `PurchaseDate` from the result.

### State Initialisation

`InitializeStateSpace(model)` sets:

- `NotionalScalingMultiplier` = `model.NotionalScalingMultiplier`
- `InterestScalingMultiplier` = `model.InterestScalingMultiplier`
- `ContractPerformance` = `model.ContractPerformance`
- `StatusDate` = `model.StatusDate`

If `InitialExchangeDate > StatusDate` (future-start contract):
- `NotionalPrincipal = 0`
- `NominalInterestRate = 0`

Otherwise:
- `NotionalPrincipal = model.RoleSign * model.NotionalPrincipal`
- `NominalInterestRate = model.NominalInterestRate`

Accrued interest is initialised from `model.AccruedInterest` if non-zero, or calculated from the last IP schedule date before `StatusDate` using the contract's day-count convention. Fee accrued is initialised from `model.FeeAccrued` if non-zero.

### Validation

No explicit validation layer is present in the library (no exceptions thrown for missing dates, etc.). The contract terms are expected to be well-formed by the caller. Incorrect or missing terms will produce empty schedules or zero payoffs rather than exceptions.

`PamContractTerms.FromDictionary()` provides a dictionary-based factory with safe parsing helpers (`GetDouble`, `GetDateTime`, etc.) that return defaults on failure.

### Persistence

Unknown from repo. The library has no persistence layer. Files checked:
- `src/ActusInsurance.Core/Models/PamContractTerms.cs`
- `src/ActusInsurance.Core.CPU/Contracts/PAM.cs`
- `src/ActusInsurance.Core/ActusInsurance.Core.csproj`

### Evidence from Code

- `src/ActusInsurance.Core/Contracts/IContractScheduler.cs`
- `src/ActusInsurance.Core/Models/IContractTerms.cs`
- `src/ActusInsurance.Core/Models/PamContractTerms.cs`
- `src/ActusInsurance.Core.CPU/Contracts/PAM.cs`
- `src/ActusInsurance.Core/Events/ContractEvent.cs`
- `src/ActusInsurance.Core/Types/Enums.cs`

# Domain Model

This document defines the core concepts used throughout the system and shows how they relate to each other.

Related: [Architecture](./architecture.md) | [Modules — Contracts](./modules/contracts.md) | [Modules — Calculation Engine](./modules/calculation-engine.md)

---

## Business View

### Core Concepts

**Contract**
A legal agreement in which one party lends money (or a financial asset) and the other party promises to repay it on specific terms. In this system, every contract has a type. Currently only PAM (Principal At Maturity) is supported.

**Contract Terms**
The fixed parameters agreed at the start of a contract: who owes what, at what rate, for how long, with which payment dates.

**Event**
A specific moment in time when something financial happens on a contract — an interest payment, a maturity repayment, a rate reset, or a fee.

**Schedule**
The full ordered list of events for a contract, from today to maturity.

**State**
The financial position of a contract at any moment: outstanding principal, accrued interest, current nominal rate, and accrued fees.

**Risk Factor**
An external market observable — such as a benchmark interest rate or a price index — that may change the contract's behaviour (e.g. variable-rate resets).

### How They Relate

A **contract** is defined by its **terms**. Given those terms, the system generates a **schedule** of **events**. As each event is processed, a **state** is updated. Some events consult **risk factors** to determine their outcome (e.g. the new rate after a rate reset).

---

## Technical View

### Concept Map

```
flowchart TD
    CT[IContractTerms]
    PAM[PamContractTerms]
    CE[ContractEvent]
    SS[StateSpace]
    RF[RiskFactorModel]
    SF[ScheduleFactory]
    CT --> PAM
    PAM --> SF
    SF --> CE
    CE --> SS
    RF --> CE
```

**Legend:**
- `IContractTerms` — marker interface; every contract terms class implements it
- `PamContractTerms` — PAM-specific parameters
- `ScheduleFactory` — builds the date series for each cycle
- `ContractEvent` — holds `ScheduleTime`, `Time`, `Type`, `Payoff`, and post-event state snapshot
- `StateSpace` — mutable struct threaded through all event evaluations
- `RiskFactorModel` — rate/index lookup injected by the caller

### IContractTerms

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

Minimal interface. All contract types implement it. Allows generic code (e.g. `IContractScheduler<TTerms>`) to operate without knowing the specific contract type.

### PamContractTerms

Full parameter set for PAM. Groups:

| Group | Fields |
|---|---|
| Identification | `ContractID`, `ContractType` ("PAM"), `Currency`, `ContractRole` |
| Critical dates | `StatusDate`, `InitialExchangeDate`, `MaturityDate`, `PurchaseDate`, `TerminationDate`, `CapitalizationEndDate` |
| Principal and rates | `NotionalPrincipal`, `NominalInterestRate`, `AccruedInterest`, `PremiumDiscountAtIED`, `PriceAtPurchaseDate`, `PriceAtTerminationDate` |
| Rate reset | `RateSpread`, `RateMultiplier`, `NextResetRate`, `MarketObjectCodeOfRateReset`, `FixingPeriod`, `LifeCap`, `LifeFloor`, `PeriodCap`, `PeriodFloor` |
| Interest cycle | `CycleOfInterestPayment`, `CycleAnchorDateOfInterestPayment`, `CyclePointOfInterestPayment` |
| Rate reset cycle | `CycleOfRateReset`, `CycleAnchorDateOfRateReset`, `CyclePointOfRateReset` |
| Fee | `CycleOfFee`, `CycleAnchorDateOfFee`, `FeeBasis`, `FeeRate`, `FeeAccrued` |
| Scaling | `MarketObjectCodeOfScalingIndex`, `ScalingIndexAtContractDealDate`, `NotionalScalingMultiplier`, `InterestScalingMultiplier`, `CycleOfScalingIndex`, `ScalingEffect` |
| Conventions | `DayCountConvention`, `BusinessDayConvention`, `EndOfMonthConvention`, `Calendar` |
| Performance | `ContractPerformance` |
| Derived | `RoleSign` (computed from `ContractRole`: +1 or −1) |

Source: `src/ActusInsurance.Core/Models/PamContractTerms.cs`

### ContractEvent

```csharp
// src/ActusInsurance.Core/Events/ContractEvent.cs
public sealed class ContractEvent : IComparable<ContractEvent>
{
    public DateTime Time { get; set; }         // Adjusted event time (ordering)
    public DateTime ScheduleTime { get; set; } // Original scheduled time (calculations)
    public EventType Type { get; set; }
    public string Currency { get; set; }
    public double Payoff { get; set; }
    public double NotionalPrincipal { get; set; }
    public double NominalInterestRate { get; set; }
    public double AccruedInterest { get; set; }
    public double FeeAccrued { get; set; }
}
```

After `Evaluate()` is called, the four state snapshot fields (`NotionalPrincipal`, `NominalInterestRate`, `AccruedInterest`, `FeeAccrued`) reflect the contract state **after** this event.

### EventType Enum

| Value | Name | Description |
|---|---|---|
| `AD` | Analysis Date | Observation point, no cash flow |
| `IED` | Initial Exchange | Contract starts; principal exchanged |
| `MD` | Maturity | Contract ends; principal returned |
| `IP` | Interest Payment | Periodic interest paid |
| `IPCI` | Interest Capitalisation | Accrued interest added to principal |
| `PRD` | Purchase | Contract bought at market price |
| `TD` | Termination | Early termination; settlement at `PriceAtTerminationDate` |
| `RR` | Rate Reset | Variable rate updated from market data |
| `RRF` | Rate Reset Fixed | Rate set to `NextResetRate` (predetermined) |
| `FP` | Fee Payment | Periodic fee paid |
| `SC` | Scaling | Notional or interest scaling index updated |
| `CD` | Credit Default | Credit default event |

Source: `src/ActusInsurance.Core/Types/Enums.cs`

### StateSpace

```csharp
// src/ActusInsurance.Core/States/StateSpace.cs
public struct StateSpace
{
    public double NotionalPrincipal;
    public double NominalInterestRate;
    public double AccruedInterest;
    public double FeeAccrued;
    public double NotionalScalingMultiplier;
    public double InterestScalingMultiplier;
    public DateTime StatusDate;
    public string? ContractPerformance;
}
```

`StatusDate` is advanced to `ScheduleTime` (the original unshifted date) after each event. This preserves correct day-count periods when business-day shifting is in effect.

### ContractRole

The `ContractRole` enum and its sign determines the direction of cash flows:

| Role | Sign | Meaning |
|---|---|---|
| `RPA` | +1 | Receive principal, pay assets |
| `RPL` | −1 | Receive principal, pay liabilities |
| `BUY` | +1 | Buyer |
| `SEL` | −1 | Seller |
| `RFL` | +1 | Receive fixed leg |
| `PFL` | −1 | Pay fixed leg |
| `RF` | +1 | Receive floating |
| `PF` | −1 | Pay floating |

Source: `src/ActusInsurance.Core/Models/PamContractTerms.cs` (`RoleSign` property)

### RiskFactorModel

See [Modules — Risk Factors](./modules/risk-factors.md) for full detail.

The model holds:
- A constant-rate dictionary: `marketObjectCode → double`
- A time-series dictionary: `marketObjectCode → (DateTime → double)`

Lookup prefers constant rates over time-series rates, and falls back to the most recent past value if no exact time match exists.

### Cycle Strings

Cycles are encoded as strings following a simplified ISO 8601 period format with an optional stub suffix:

```
P<n>M   e.g. P1M  = every 1 month
P<n>Y   e.g. P1Y  = every 1 year
P<n>D   e.g. P7D  = every 7 days
<n>M    e.g. 3M   = every 3 months (legacy format)
<suffix>L<stub>   e.g. P1MLs = short stub, P1MLl = long stub
```

Parsing is in `src/ActusInsurance.Core/Util/CycleUtils.cs`.

### Evidence from Code

- `src/ActusInsurance.Core/Models/IContractTerms.cs`
- `src/ActusInsurance.Core/Models/PamContractTerms.cs`
- `src/ActusInsurance.Core/Events/ContractEvent.cs`
- `src/ActusInsurance.Core/States/StateSpace.cs`
- `src/ActusInsurance.Core/Types/Enums.cs`
- `src/ActusInsurance.Core/Types/ContractRole.cs`
- `src/ActusInsurance.Core/Externals/RiskFactorModel.cs`
- `src/ActusInsurance.Core/Util/CycleUtils.cs`

# Module: Calculation Engine

This document explains how the system computes cash payoffs and updates financial state for each contract event.

Related: [Contracts](./contracts.md) | [Risk Factors](./risk-factors.md) | [Domain Model](../domain-model.md)

---

## Business View

### What Does the Calculation Engine Do?

For each event in a contract's schedule, the calculation engine answers two questions:

1. **How much cash changes hands?** (the payoff)
2. **What is the new financial state of the contract?** (outstanding principal, accrued interest, current rate)

### Why Does Accuracy Matter?

Even small rounding or timing errors compound over multi-year contracts. A loan calculated with the wrong day-count convention can produce interest amounts that differ from what was contractually agreed. The engine uses well-defined, tested formulas to eliminate ambiguity.

### Who Cares?

- **Accountants and controllers** — correct accrual ensures the balance sheet reflects economic reality
- **Cash managers** — precise payoff amounts ensure the right payments are made on the right day
- **Risk teams** — state-space snapshots enable scenario analysis and stress testing

---

## Technical View

### Entry Point

```csharp
// src/ActusInsurance.Core/Events/ContractEvent.cs
public void Evaluate(
    ref StateSpace states,
    PamContractTerms model,
    RiskFactorModel riskFactors,
    BusinessDayAdjuster timeAdjuster)
```

Called for each event in the list produced by `PrincipalAtMaturity.Apply()`. Mutates `states` in place and writes the resulting values back to the event's own properties.

### Execution Steps (per event)

```
flowchart TD
    A[ComputePayoff]
    B[UpdateState]
    C[AccrueInterest if applicable]
    D[AdvanceStatusDate]
    E[Apply event type transition]
    F[Snapshot state to event fields]
    A --> B
    B --> C
    C --> D
    D --> E
    E --> F
```

1. **ComputePayoff** — determine cash flow (may be 0 for state-only events)
2. **AccrueInterest** — for IP, IPCI, RR, FP, SC events: add interest and fee accrual since last event
3. **AdvanceStatusDate** — set `states.StatusDate = ScheduleTime`
4. **Apply type transition** — mutate principal, rate, accrued values based on `EventType`
5. **Snapshot** — copy `states` fields to event properties (`NotionalPrincipal`, etc.)

### Payoff Formulas

All payoffs are multiplied by an FX rate (currently always 1.0 — see note below).

| Event | Formula |
|---|---|
| `IED` | `fxRate * RoleSign * (−1) * (NotionalPrincipal + PremiumDiscountAtIED)` |
| `MD` | `fxRate * NotionalScalingMultiplier * states.NotionalPrincipal` |
| `PRD` | `fxRate * RoleSign * (−1) * PriceAtPurchaseDate` |
| `TD` | `fxRate * RoleSign * PriceAtTerminationDate` |
| `IP` | `fxRate * InterestScalingMultiplier * (AccruedInterest + YF * NominalInterestRate * NotionalPrincipal)` |
| `IPCI` | `0.0` (no cash; interest is capitalised instead) |
| `RR` / `RRF` | `0.0` (no cash; rate is updated) |
| `FP` (basis A) | `fxRate * RoleSign * FeeRate` |
| `FP` (basis N) | `fxRate * (FeeAccrued + NotionalPrincipal * FeeRate * YF)` |
| `SC` | `0.0` (no cash; scaling multipliers updated) |

**YF** = year fraction computed using the contract's `DayCountConvention`.

> **Note on FX:** `GetFxRate()` always returns `1.0`. Multi-currency support is not yet implemented. The method is a placeholder.

### Interest Accrual Formula

Before an IP, IPCI, RR, FP, or SC event, the engine adds the accrued interest since the last event:

```
AccruedInterest += NominalInterestRate * NotionalPrincipal * YF(lastEventDate, thisEventDate)
```

Where `YF` uses `timeAdjuster.ShiftCalcTime()` to apply the correct date for CalcShift vs ShiftCalc conventions.

Fee accrual (when `FeeBasis == "N"`):

```
FeeAccrued += FeeRate * NotionalPrincipal * YF(lastEventDate, thisEventDate)
```

Source: `AccrueInterest()` in `src/ActusInsurance.Core/Events/ContractEvent.cs`

### State Transitions by Event Type

| Event | NotionalPrincipal | NominalInterestRate | AccruedInterest | FeeAccrued |
|---|---|---|---|---|
| `IED` | `RoleSign * NotionalPrincipal` | `model.NominalInterestRate` | unchanged | unchanged |
| `MD` | `0` | unchanged | `0` | unchanged |
| `PRD` | unchanged | unchanged | unchanged | unchanged |
| `TD` | `0` | unchanged | `0` | unchanged |
| `IP` | unchanged | unchanged | `0` | unchanged |
| `IPCI` | `+= AccruedInterest` | unchanged | `0` | unchanged |
| `RR` / `RRF` | unchanged | updated (see rate reset) | unchanged | unchanged |
| `FP` | unchanged | unchanged | unchanged | `0` |
| `SC` | unchanged | unchanged | unchanged | unchanged |

### Rate Reset Logic

Source: `UpdateRateReset()` in `src/ActusInsurance.Core/Events/ContractEvent.cs`

**RRF (fixed next rate):**
```
NominalInterestRate = model.NextResetRate
```

**RR (market-driven):**
```
marketRate = riskFactors.GetRate(MarketObjectCodeOfRateReset, eventTime)
newRate    = marketRate * RateMultiplier + RateSpread
deltaRate  = newRate - NominalInterestRate
deltaRate  = clamp(deltaRate, PeriodFloor, PeriodCap)
newRate    = NominalInterestRate + deltaRate
newRate    = clamp(newRate, LifeFloor, LifeCap)
NominalInterestRate = newRate
```

Caps and floors:
- `PeriodCap` / `PeriodFloor` — maximum single-period change in rate
- `LifeCap` / `LifeFloor` — absolute bounds on the rate over the contract's life

Default values: `PeriodCap = +∞`, `PeriodFloor = −∞`, `LifeCap = +∞`, `LifeFloor = −∞`

### Scaling Logic

Source: `UpdateScaling()` in `src/ActusInsurance.Core/Events/ContractEvent.cs`

```
scalingIndex  = riskFactors.GetRate(MarketObjectCodeOfScalingIndex, eventTime)
scalingFactor = scalingIndex / ScalingIndexAtContractDealDate

if ScalingEffect contains "N":
    NotionalScalingMultiplier = scalingFactor * model.NotionalScalingMultiplier

if ScalingEffect contains "I":
    InterestScalingMultiplier = scalingFactor * model.InterestScalingMultiplier
```

### Day Count Conventions

The year fraction `YF(start, end)` is computed by `DayCountCalculator` in `src/ActusInsurance.Core/Conventions/DayCount/DayCountCalculator.cs`.

The event uses `ConvertDayCountToString(model.DayCountConvention)` to map the enum to the string key understood by the calculator:

| Enum | String key | Algorithm |
|---|---|---|
| `A_AISDA` | `"AA"` | Actual/Actual ISDA |
| `A_360` | `"A360"` | Actual days / 360 |
| `A_365` | `"A365"` | Actual days / 365 |
| `E30_360ISDA` | `"30E360ISDA"` | 30E/360 ISDA |
| `E30_360` | `"30E360"` | 30E/360 |
| `B_252` | `"B252"` | Business days / 252 |
| `A_336` | `"A336"` | Actual days / 336 |

`DayCountCalculator.DayCountFraction()` normalises dates to full hours via `TimeAdjuster.ToFullHours()` before delegating to the convention implementation.

### Business Day and Calculation Date Handling

The engine always computes year fractions using `ShiftCalcTime()` (from `BusinessDayAdjuster`), not raw dates. This correctly implements:

- **CalcShift** (`CS*`) — calculate on original date, shift the payment date
- **ShiftCalc** (`SC*`) — shift both the payment date and the calculation date

```csharp
double yearFraction = ComputeYearFraction(
    timeAdjuster.ShiftCalcTime(states.StatusDate),
    timeAdjuster.ShiftCalcTime(ScheduleTime),
    model.DayCountConvention);
```

### Rounding

Results are **not rounded internally** by the calculation engine. The test suite rounds to 10 decimal places before comparison:

```csharp
// src/ActusInsurance.Tests.CPU/PamTests.cs
computedResults.ForEach(r => r.RoundTo(10));
expectedResults.ForEach(r => r.RoundTo(10));
```

Callers are responsible for applying any required display or storage rounding.

### Invariants

- `StatusDate` in `StateSpace` always equals `ScheduleTime` (the unshifted date) of the last processed event.
- `AccruedInterest` resets to `0` after `IP` and `TD` events.
- `NotionalPrincipal` in `StateSpace` is signed (`RoleSign * model.NotionalPrincipal`).
- `FeeAccrued` resets to `0` after `FP` events.
- Before `IED`, `NotionalPrincipal = 0` and `NominalInterestRate = 0`.

### Edge Cases

| Situation | Behaviour |
|---|---|
| `NominalInterestRate == 0` | `AccruedInterest` initialised to `0`; no interest accrual |
| `FeeRate == 0` | `FeeAccrued` initialised to `0`; no fee accrual |
| No `CycleOfInterestPayment` but `CapitalizationEndDate` set | Single `IPCI` event at `CapitalizationEndDate` |
| `CycleAnchorDateOfInterestPayment` before `IED` | Interest accrues from anchor to `IED` at `IED` event |
| Future-start contract (`InitialExchangeDate > StatusDate`) | Principal and rate initialised to `0` until `IED` fires |
| `PurchaseDate` set | Events before `PurchaseDate` are removed from output (except `AD` type) |

### Evidence from Code

- `src/ActusInsurance.Core/Events/ContractEvent.cs` — `Evaluate`, `ComputePayoff`, `UpdateState`, `AccrueInterest`, `UpdateRateReset`, `UpdateScaling`
- `src/ActusInsurance.Core.CPU/Contracts/PAM.cs` — `Apply`, `InitializeStateSpace`
- `src/ActusInsurance.Core/Conventions/DayCount/DayCountCalculator.cs`
- `src/ActusInsurance.Core/Conventions/BusinessDay/BusinessDayAdjuster.cs`
- `src/ActusInsurance.Core/States/StateSpace.cs`

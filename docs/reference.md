# Reference

This document provides data model tables, the public API surface, the folder map, and important types.

Related: [Architecture](./architecture.md) | [Domain Model](./domain-model.md) | [Developer Guide](./developer-guide.md)

---

## Folder Map

```
Actus-Insurance.Core/
├── docs/                                   # This documentation folder
│   ├── README.md
│   ├── overview.md
│   ├── value-proposition.md
│   ├── architecture.md
│   ├── domain-model.md
│   ├── testing.md
│   ├── operations.md
│   ├── developer-guide.md
│   ├── reference.md
│   └── modules/
│       ├── contracts.md
│       ├── calculation-engine.md
│       └── risk-factors.md
│
├── src/
│   ├── ActusInsurance.Core/                # NuGet: ActusInsurance.Core
│   │   ├── Contracts/
│   │   │   └── IContractScheduler.cs       # Generic scheduler interface
│   │   ├── Conventions/
│   │   │   ├── BusinessDay/
│   │   │   │   ├── BusinessDayAdjuster.cs
│   │   │   │   ├── BusinessDayConventions.cs
│   │   │   │   ├── IBusinessDayConvention.cs
│   │   │   │   ├── IShiftCalcConvention.cs
│   │   │   │   └── ShiftCalcConventions.cs
│   │   │   ├── ContractRoles/
│   │   │   │   └── ContractRoleConvention.cs
│   │   │   ├── DayCount/
│   │   │   │   ├── ActualActualISDA.cs
│   │   │   │   ├── ActualThreeSixty.cs
│   │   │   │   ├── ActualThreeSixtyFiveFixed.cs
│   │   │   │   ├── ActualThreeThirtySix.cs
│   │   │   │   ├── BusinessTwoFiftyTwo.cs
│   │   │   │   ├── DayCountCalculator.cs
│   │   │   │   ├── IDayCountConventionProvider.cs
│   │   │   │   ├── ThirtyEThreeSixty.cs
│   │   │   │   ├── ThirtyEThreeSixtyISDA.cs
│   │   │   │   └── TwentyEightThreeThirtySix.cs
│   │   │   └── EndOfMonth/
│   │   │       ├── EndOfMonth.cs
│   │   │       ├── EndOfMonthAdjuster.cs
│   │   │       ├── IEndOfMonthConvention.cs
│   │   │       └── SameDay.cs
│   │   ├── Events/
│   │   │   └── ContractEvent.cs            # Per-event payoff and state logic
│   │   ├── Exceptions/
│   │   │   └── AttributeConversionException.cs
│   │   ├── Externals/
│   │   │   └── RiskFactorModel.cs          # External rate/index data
│   │   ├── Models/
│   │   │   ├── IContractTerms.cs           # Marker interface for all contracts
│   │   │   └── PamContractTerms.cs         # PAM contract parameters
│   │   ├── States/
│   │   │   └── StateSpace.cs               # Mutable contract financial state
│   │   ├── Time/
│   │   │   ├── Calendar/
│   │   │   │   ├── BusinessDayCalendarProvider.cs
│   │   │   │   ├── MondayToFridayCalendar.cs
│   │   │   │   ├── MondayToFridayWithHolidaysCalendar.cs
│   │   │   │   └── NoHolidaysCalendar.cs
│   │   │   ├── CycleAdjuster.cs
│   │   │   ├── ICycleAdjusterProvider.cs
│   │   │   ├── PeriodCycleAdjuster.cs
│   │   │   ├── ScheduleFactory.cs          # Date sequence generation
│   │   │   ├── TimeAdjuster.cs
│   │   │   └── WeekdayCycleAdjuster.cs
│   │   ├── Types/
│   │   │   ├── BusinessDayConventionEnum.cs
│   │   │   ├── Calendar.cs
│   │   │   ├── ClearingHouse.cs
│   │   │   ├── ComplexTypes.cs
│   │   │   ├── ContractPerformance.cs
│   │   │   ├── ContractRole.cs
│   │   │   ├── ContractTypeEnum.cs
│   │   │   ├── CreditEventTypeCovered.cs
│   │   │   ├── CyclePointOfInterestPayment.cs
│   │   │   ├── CyclePointOfRateReset.cs
│   │   │   ├── DayCountConvention.cs
│   │   │   ├── DeliverySettlement.cs
│   │   │   ├── EndOfMonthConventionEnum.cs
│   │   │   ├── Enums.cs                    # EventType, FeeBasis
│   │   │   ├── GuaranteedExposure.cs
│   │   │   ├── InterestCalculationBase.cs
│   │   │   ├── OptionExerciseType.cs
│   │   │   ├── OptionType.cs
│   │   │   ├── PenaltyType.cs
│   │   │   ├── PrepaymentEffect.cs
│   │   │   ├── ReferenceRole.cs
│   │   │   ├── ReferenceType.cs
│   │   │   ├── ScalingEffect.cs
│   │   │   └── Seniority.cs
│   │   └── Util/
│   │       ├── CommonUtils.cs
│   │       ├── Constants.cs
│   │       ├── CycleUtils.cs               # Cycle string parser
│   │       └── StringUtils.cs
│   │
│   ├── ActusInsurance.Core.CPU/            # NuGet: ActusInsurance.Core.CPU
│   │   └── Contracts/
│   │       └── PAM.cs                      # PrincipalAtMaturity static class
│   │
│   └── ActusInsurance.Tests.CPU/           # Not packaged
│       ├── PamTests.cs
│       └── Resources/
│           └── actus-tests-pam.json
│
├── .github/workflows/
│   ├── preview.yml
│   └── release.yml
├── Directory.Build.props
└── Actus-Insurance.Core.slnx
```

---

## Public API

### ActusInsurance.Core.CPU.Contracts.PrincipalAtMaturity

The main entry point for PAM contract processing.

```csharp
namespace ActusInsurance.Core.CPU.Contracts;

public static class PrincipalAtMaturity
{
    // Generate the event schedule up to 'to' date
    public static List<ContractEvent> Schedule(DateTime to, PamContractTerms model);

    // Evaluate all events; returns the same list with Payoff and state fields populated
    public static List<ContractEvent> Apply(
        List<ContractEvent> events,
        PamContractTerms model,
        RiskFactorModel riskFactors);
}
```

### ActusInsurance.Core.Externals.RiskFactorModel

```csharp
namespace ActusInsurance.Core.Externals;

public sealed class RiskFactorModel
{
    // Add a rate that varies over time
    public void AddRate(string marketObjectCode, DateTime time, double value);

    // Add a constant rate (applies at all times, takes priority over time-series)
    public void AddConstantRate(string marketObjectCode, double value);

    // Get the rate for a given market object code at a given time
    public double GetRate(string marketObjectCode, DateTime time);
}
```

### ActusInsurance.Core.Contracts.IContractScheduler

```csharp
namespace ActusInsurance.Core.Contracts;

public interface IContractScheduler<TTerms> where TTerms : IContractTerms
{
    List<ContractEvent> Schedule(DateTime to, TTerms terms);
    List<ContractEvent> Apply(List<ContractEvent> events, TTerms terms, RiskFactorModel riskFactors);
}
```

### ActusInsurance.Core.Models.IContractTerms

```csharp
namespace ActusInsurance.Core.Models;

public interface IContractTerms
{
    string ContractID { get; }
    string ContractType { get; }
    string Currency { get; }
    DateTime StatusDate { get; }
    DateTime MaturityDate { get; }
}
```

### ActusInsurance.Core.Time.ScheduleFactory

```csharp
namespace ActusInsurance.Core.Time;

public static class ScheduleFactory
{
    public static IEnumerable<DateTime> CreateSchedule(
        DateTime startTime,
        DateTime endTime,
        string cycle,
        bool endOfMonthConvention,
        bool includeEndDate);

    public static IEnumerable<DateTime> CreateSchedule(
        DateTime startTime,
        DateTime endTime,
        string cycle,
        EndOfMonthConventionEnum endOfMonthConvention,
        bool addEndTime);

    public static IEnumerable<DateTime> CreateArraySchedule(
        DateTime[] startTimes,
        DateTime endTime,
        string[] cycles,
        EndOfMonthConventionEnum endOfMonthConvention);
}
```

---

## Data Models

### PamContractTerms — Field Reference

| Field | Type | Default | Description |
|---|---|---|---|
| `ContractID` | `string` | `""` | Unique contract identifier |
| `ContractType` | `string` | `"PAM"` | Always "PAM" for this class |
| `Currency` | `string` | `""` | ISO 4217 currency code |
| `ContractRole` | `ContractRole` | `RPA` | Determines sign of cash flows |
| `RoleSign` | `int` (computed) | `+1` | `+1` or `−1` based on role |
| `StatusDate` | `DateTime` | required | As-of date; events before this are filtered |
| `InitialExchangeDate` | `DateTime` | required | Contract start date |
| `MaturityDate` | `DateTime` | required | Contract end date |
| `PurchaseDate` | `DateTime?` | null | Optional purchase date |
| `TerminationDate` | `DateTime?` | null | Optional early termination date |
| `CapitalizationEndDate` | `DateTime?` | null | Interest capitalised until this date |
| `NotionalPrincipal` | `double` | 0.0 | Face value of the contract |
| `NominalInterestRate` | `double` | 0.0 | Annual interest rate (decimal, e.g. 0.05 = 5%) |
| `AccruedInterest` | `double` | 0.0 | Pre-existing accrued interest at StatusDate |
| `PremiumDiscountAtIED` | `double` | 0.0 | Added to principal at IED payoff |
| `PriceAtPurchaseDate` | `double` | 0.0 | PRD event payoff amount |
| `PriceAtTerminationDate` | `double` | 0.0 | TD event payoff amount |
| `RateSpread` | `double` | 0.0 | Added to market rate at reset |
| `RateMultiplier` | `double` | 1.0 | Multiplied by market rate at reset |
| `NextResetRate` | `double?` | null | Predetermined rate for first RRF event |
| `MarketObjectCodeOfRateReset` | `string?` | null | Key for rate reset risk factor |
| `FixingPeriod` | `string?` | null | Lag between fixing and reset date |
| `LifeCap` | `double` | +∞ | Maximum allowed rate over life |
| `LifeFloor` | `double` | −∞ | Minimum allowed rate over life |
| `PeriodCap` | `double` | +∞ | Maximum rate change per period |
| `PeriodFloor` | `double` | −∞ | Minimum rate change per period |
| `CycleOfInterestPayment` | `string?` | null | Period string, e.g. `"P1M"` |
| `CycleAnchorDateOfInterestPayment` | `DateTime?` | IED | First date in interest payment schedule |
| `CyclePointOfInterestPayment` | `string?` | null | Beginning or end of period |
| `CycleOfRateReset` | `string?` | null | Period string for rate reset events |
| `CycleAnchorDateOfRateReset` | `DateTime?` | IED | First date in rate reset schedule |
| `CyclePointOfRateReset` | `string?` | null | Beginning or end of period |
| `CycleOfFee` | `string?` | null | Period string for fee events |
| `CycleAnchorDateOfFee` | `DateTime?` | IED | First date in fee schedule |
| `FeeBasis` | `string?` | null | `"A"` (absolute) or `"N"` (notional-based) |
| `FeeRate` | `double` | 0.0 | Annual fee rate |
| `FeeAccrued` | `double` | 0.0 | Pre-existing accrued fee at StatusDate |
| `MarketObjectCodeOfScalingIndex` | `string?` | null | Key for scaling index risk factor |
| `ScalingIndexAtContractDealDate` | `double` | 0.0 | Index value at inception |
| `NotionalScalingMultiplier` | `double` | 1.0 | Current notional scaling multiplier |
| `InterestScalingMultiplier` | `double` | 1.0 | Current interest scaling multiplier |
| `CycleOfScalingIndex` | `string?` | null | Period string for scaling events |
| `ScalingEffect` | `string?` | null | `"N"`, `"I"`, or `"NI"` |
| `DayCountConvention` | `DayCountConvention` | `A_365` | Year fraction method |
| `BusinessDayConvention` | `BusinessDayConventionEnum` | `NOS` | Date shifting rule |
| `EndOfMonthConvention` | `bool` | false | Apply EOM adjustment to schedules |
| `Calendar` | `Calendar` | `NC` | Business day calendar |
| `ContractPerformance` | `string?` | null | Performance status label |

### StateSpace — Field Reference

| Field | Type | Default | Description |
|---|---|---|---|
| `NotionalPrincipal` | `double` | 0.0 | Signed outstanding principal |
| `NominalInterestRate` | `double` | 0.0 | Current annual interest rate |
| `AccruedInterest` | `double` | 0.0 | Interest accrued since last IP event |
| `FeeAccrued` | `double` | 0.0 | Fee accrued since last FP event |
| `NotionalScalingMultiplier` | `double` | 1.0 | Scaling factor for principal |
| `InterestScalingMultiplier` | `double` | 1.0 | Scaling factor for interest |
| `StatusDate` | `DateTime` | default | Date of last processed event |
| `ContractPerformance` | `string?` | null | Performance status |

### ContractEvent — Field Reference

| Field | Type | Description |
|---|---|---|
| `Time` | `DateTime` | Business-day-adjusted event time (ordering) |
| `ScheduleTime` | `DateTime` | Original scheduled time (calculations) |
| `Type` | `EventType` | Event category |
| `Currency` | `string` | ISO currency code |
| `Payoff` | `double` | Cash flow at this event |
| `NotionalPrincipal` | `double` | Outstanding principal after this event |
| `NominalInterestRate` | `double` | Rate after this event |
| `AccruedInterest` | `double` | Accrued interest after this event |
| `FeeAccrued` | `double` | Accrued fee after this event |

---

## Important Enums

### EventType

```
AD, IED, MD, IP, IPCI, PRD, TD, RR, RRF, FP, SC, CD
```

Full descriptions in [Domain Model — EventType Enum](./domain-model.md).

### DayCountConvention

```
A_AISDA, A_360, A_365, E30_360ISDA, E30_360, B_252, A_336
```

### BusinessDayConventionEnum

```
NOS, CSF, CSMF, CSP, CSMP, SCF, SCMF, SCP, SCMP, NO_ADJUST
```

### ContractRole

```
RPA (+1), RPL (-1), BUY (+1), SEL (-1), RFL (+1), PFL (-1), RF (+1), PF (-1)
```

### Calendar

```
NC (no holidays), MF (Mon-Fri), MFH (Mon-Fri with holidays)
```

---

## CLI Commands

There is no CLI tool in this repository. The library is used as a NuGet package.

```bash
# Build
dotnet build

# Test
dotnet test

# Pack
dotnet pack --configuration Release --output ./nupkgs
```

---

## Constants

```csharp
// src/ActusInsurance.Core/Util/Constants.cs
MAX_LIFETIME     = 50 years (18 250 days)
MAX_LIFETIME_STK = 10 years (3 650 days)
MAX_LIFETIME_UMP = 10 years (3 650 days)
```

---

## Evidence from Code

- `src/ActusInsurance.Core/Models/IContractTerms.cs`
- `src/ActusInsurance.Core/Models/PamContractTerms.cs`
- `src/ActusInsurance.Core/Events/ContractEvent.cs`
- `src/ActusInsurance.Core/States/StateSpace.cs`
- `src/ActusInsurance.Core/Externals/RiskFactorModel.cs`
- `src/ActusInsurance.Core/Contracts/IContractScheduler.cs`
- `src/ActusInsurance.Core/Time/ScheduleFactory.cs`
- `src/ActusInsurance.Core/Types/Enums.cs`
- `src/ActusInsurance.Core/Types/DayCountConvention.cs`
- `src/ActusInsurance.Core/Types/BusinessDayConventionEnum.cs`
- `src/ActusInsurance.Core/Types/ContractRole.cs`
- `src/ActusInsurance.Core/Types/Calendar.cs`
- `src/ActusInsurance.Core/Util/Constants.cs`

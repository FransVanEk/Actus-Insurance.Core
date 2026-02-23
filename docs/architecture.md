# Architecture

This document describes the package structure, internal data flow, and key design decisions of Actus-Insurance.Core.

Related: [Overview](./overview.md) | [Domain Model](./domain-model.md) | [Developer Guide](./developer-guide.md)

---

## Business View

### How Is the System Organised?

The library is split into two packages:

1. **ActusInsurance.Core** — the foundation. It holds all shared types, rules, and schedule-building logic.
2. **ActusInsurance.Core.CPU** — the calculation engine. It contains the actual contract implementations (currently PAM) and runs entirely on the CPU.

Think of the first package as the "grammar" of financial contracts and the second as the "calculator" that uses that grammar to produce results.

### Why Two Packages?

Separating concerns means:
- A team that only needs the shared types (e.g. to build a UI or reporting layer) can reference `ActusInsurance.Core` without pulling in calculation code.
- Future accelerated implementations (GPU, vectorised) can be added as additional packages alongside `ActusInsurance.Core.CPU` without touching the core.

---

## Technical View

### Repository Layout

```
Actus-Insurance.Core/
├── src/
│   ├── ActusInsurance.Core/              # Core library (NuGet: ActusInsurance.Core)
│   │   ├── Contracts/                    # IContractScheduler interface
│   │   ├── Conventions/
│   │   │   ├── BusinessDay/              # Business-day shifting logic
│   │   │   ├── ContractRoles/            # Role sign conventions
│   │   │   ├── DayCount/                 # Day-count fraction implementations
│   │   │   └── EndOfMonth/               # End-of-month adjustment
│   │   ├── Events/                       # ContractEvent — payoff and state logic
│   │   ├── Exceptions/                   # AttributeConversionException
│   │   ├── Externals/                    # RiskFactorModel
│   │   ├── Models/                       # IContractTerms, PamContractTerms
│   │   ├── States/                       # StateSpace struct
│   │   ├── Time/                         # ScheduleFactory, cycle adjusters, calendars
│   │   ├── Types/                        # Enums (EventType, DayCountConvention, …)
│   │   └── Util/                         # Constants, CycleUtils, StringUtils
│   │
│   ├── ActusInsurance.Core.CPU/          # CPU contract engine (NuGet: ActusInsurance.Core.CPU)
│   │   └── Contracts/
│   │       └── PAM.cs                    # PrincipalAtMaturity static class
│   │
│   └── ActusInsurance.Tests.CPU/         # NUnit test project (not packaged)
│       ├── PamTests.cs
│       └── Resources/
│           └── actus-tests-pam.json      # ACTUS reference test vectors
│
├── .github/workflows/
│   ├── preview.yml                       # Push-to-main preview publish
│   └── release.yml                       # Tag-triggered stable publish
├── Directory.Build.props                 # Shared NuGet metadata
└── Actus-Insurance.Core.slnx             # Solution file
```

### Package Dependency Graph

```
flowchart TD
    Tests[ActusInsurance.Tests.CPU]
    CPU[ActusInsurance.Core.CPU]
    Core[ActusInsurance.Core]
    Tests --> CPU
    CPU --> Core
```

`ActusInsurance.Core.CPU` depends on `ActusInsurance.Core`.
The test project depends on both.

### Data Flow

```
flowchart TD
    Terms[PamContractTerms]
    RF[RiskFactorModel]
    Sched[PrincipalAtMaturity.Schedule]
    Events[List of ContractEvent]
    Apply[PrincipalAtMaturity.Apply]
    State[StateSpace]
    Result[Evaluated ContractEvent list]
    Terms --> Sched
    Sched --> Events
    Events --> Apply
    Terms --> Apply
    RF --> Apply
    Apply --> State
    State --> Result
```

**Legend:**
- `PamContractTerms` — all contract parameters supplied by the caller
- `RiskFactorModel` — market rate data supplied by the caller
- `Schedule` — produces the ordered list of future events
- `Apply` — iterates events, mutates `StateSpace`, writes payoff/state to each event
- `Result` — the same event list, now populated with payoffs and post-event state values

### Key Design Decisions

#### 1. Static entry points

`PrincipalAtMaturity` exposes `Schedule` and `Apply` as `static` methods. This avoids the overhead of instantiating a class and makes the API surface minimal. There is no dependency injection required at the contract level.

#### 2. StateSpace as a struct

`StateSpace` is a `struct` (value type). This means passing it with `ref` avoids boxing and heap allocation in the evaluation loop. When a copy is needed, assignment suffices.

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

#### 3. Dual time stamps on ContractEvent

Every `ContractEvent` carries two date fields:

| Field | Purpose |
|---|---|
| `ScheduleTime` | The original scheduled date, used for day-count calculations |
| `Time` | The business-day-adjusted date, used for ordering and payment timing |

This separation correctly implements both CalcShift (`CS*`) and ShiftCalc (`SC*`) business-day convention families.

#### 4. Convention plug-in pattern

All conventions (day count, business day, end-of-month) implement a small interface (`IDayCountConventionProvider`, `IBusinessDayConvention`, etc.) and are resolved by string key in the calculator/adjuster constructors. Adding a new convention means adding an implementation class and a case in the switch.

### Conventions Supported

**Day Count Conventions** (`src/ActusInsurance.Core/Conventions/DayCount/`):

| Enum Value | Class | Description |
|---|---|---|
| `A_AISDA` | `ActualActualISDA` | Actual/Actual ISDA |
| `A_360` | `ActualThreeSixty` | Actual/360 |
| `A_365` | `ActualThreeSixtyFiveFixed` | Actual/365 Fixed |
| `E30_360ISDA` | `ThirtyEThreeSixtyISDA` | 30E/360 ISDA |
| `E30_360` | `ThirtyEThreeSixty` | 30E/360 |
| `B_252` | `BusinessTwoFiftyTwo` | Business/252 |
| `A_336` | `ActualThreeThirtySix` | Actual/336 |

**Business Day Conventions** (`src/ActusInsurance.Core/Conventions/BusinessDay/`):

| Code | Shift Order | Direction |
|---|---|---|
| `NOS` | No shift | — |
| `CSF` | Calc first, then shift | Following |
| `CSMF` | Calc first, then shift | Modified Following |
| `CSP` | Calc first, then shift | Preceding |
| `CSMP` | Calc first, then shift | Modified Preceding |
| `SCF` | Shift first, then calc | Following |
| `SCMF` | Shift first, then calc | Modified Following |
| `SCP` | Shift first, then calc | Preceding |
| `SCMP` | Shift first, then calc | Modified Preceding |

**Calendars** (`src/ActusInsurance.Core/Time/Calendar/`):

| Code | Class | Description |
|---|---|---|
| `NC` | `NoHolidaysCalendar` | Every day is a business day |
| `MF` | `MondayToFridayCalendar` | Mon–Fri are business days |
| `MFH` | `MondayToFridayWithHolidaysCalendar` | Mon–Fri minus supplied holidays |

### Evidence from Code

- `src/ActusInsurance.Core.CPU/ActusInsurance.Core.CPU.csproj`
- `src/ActusInsurance.Core/ActusInsurance.Core.csproj`
- `src/ActusInsurance.Core/Events/ContractEvent.cs`
- `src/ActusInsurance.Core/States/StateSpace.cs`
- `src/ActusInsurance.Core/Conventions/BusinessDay/BusinessDayAdjuster.cs`
- `src/ActusInsurance.Core/Conventions/DayCount/DayCountCalculator.cs`
- `src/ActusInsurance.Core/Types/BusinessDayConventionEnum.cs`
- `src/ActusInsurance.Core/Types/DayCountConvention.cs`

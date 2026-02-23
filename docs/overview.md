# System Overview

This document introduces the purpose, key workflows, main components, and boundaries of Actus-Insurance.Core.

Related: [Architecture](./architecture.md) | [Domain Model](./domain-model.md) | [Value Proposition](./value-proposition.md)

---

## Business View

### What Is This System?

Actus-Insurance.Core is a software library that calculates the future cash flows of financial contracts — specifically fixed-income instruments such as loans and bonds. It tells you, for a given contract, exactly when money moves, how much moves, and what the outstanding balance is at any point in time.

### Why Does It Exist?

Financial institutions — banks, insurers, asset managers — must regularly calculate what they owe and what they are owed under hundreds or thousands of contracts. These calculations depend on complex rules about interest, fees, rate resets, and calendar adjustments. Actus-Insurance.Core provides a single, tested, reusable engine for these calculations, so each system does not have to build its own.

### What Problem Does It Solve?

Without a shared library like this, every product team implements the same financial maths from scratch, leading to inconsistencies, errors, and expensive audits. Actus-Insurance.Core removes that duplication by encoding the ACTUS standard — an internationally agreed specification for financial contract behaviour.

### Key Workflows

1. **Schedule Generation** — Given a contract's terms (dates, rate, principal), the system generates an ordered list of events that will occur over the contract's lifetime.
2. **Event Evaluation** — For each event, the system computes the cash payoff and updates the contract's financial state.
3. **Risk Factor Integration** — Where interest rates or indices depend on market data, the system accepts external rate observations and applies them to the appropriate events.

### Who Cares About It?

- **Product teams** building loan management, insurance, or asset-liability systems
- **Risk managers** who need consistent cash-flow projections
- **Finance and actuarial teams** who require auditable, standard-compliant calculations
- **Developers** who want a pre-built, NuGet-distributed engine rather than coding financial maths themselves

---

## Technical View

### System Purpose

The library implements the [ACTUS Financial Research Foundation](https://www.actusfrf.org/) standard for financial contract modelling. It is a **.NET 9** class library distributed as two NuGet packages:

| Package | Description |
|---|---|
| `ActusInsurance.Core` | Core types, state space, schedule generation, conventions |
| `ActusInsurance.Core.CPU` | Contract implementations (PAM) that run on the CPU |

### Key Workflows (Technical)

```
flowchart TD
    A[Caller provides PamContractTerms]
    B[PrincipalAtMaturity.Schedule]
    C[ScheduleFactory generates dates]
    D[ContractEvent list sorted]
    E[PrincipalAtMaturity.Apply]
    F[ContractEvent.Evaluate loop]
    G[StateSpace mutated per event]
    H[Payoffs and states returned]
    A --> B
    B --> C
    C --> D
    D --> E
    E --> F
    F --> G
    G --> H
```

**Step 1 — Schedule:**
`PrincipalAtMaturity.Schedule(DateTime to, PamContractTerms model)` in `src/ActusInsurance.Core.CPU/Contracts/PAM.cs` builds the event list. For each enabled cycle (interest, rate-reset, fee, scaling) it calls `ScheduleFactory.CreateSchedule()` and converts dates to `ContractEvent` objects. Business-day adjustments are applied via `BusinessDayAdjuster`.

**Step 2 — Apply:**
`PrincipalAtMaturity.Apply(events, model, riskFactors)` initialises a `StateSpace` struct, sorts the events, and iterates. Each call to `evt.Evaluate(ref states, ...)` in `ContractEvent.cs` computes the payoff and mutates the state.

### Main Components

| Component | Location | Responsibility |
|---|---|---|
| `PrincipalAtMaturity` | `src/ActusInsurance.Core.CPU/Contracts/PAM.cs` | PAM contract scheduling and event application |
| `ContractEvent` | `src/ActusInsurance.Core/Events/ContractEvent.cs` | Per-event payoff computation and state transition |
| `StateSpace` | `src/ActusInsurance.Core/States/StateSpace.cs` | Mutable financial state between events |
| `PamContractTerms` | `src/ActusInsurance.Core/Models/PamContractTerms.cs` | All contract parameters |
| `RiskFactorModel` | `src/ActusInsurance.Core/Externals/RiskFactorModel.cs` | External rate/index lookup |
| `ScheduleFactory` | `src/ActusInsurance.Core/Time/ScheduleFactory.cs` | Date sequence generation from cycle strings |
| `BusinessDayAdjuster` | `src/ActusInsurance.Core/Conventions/BusinessDay/BusinessDayAdjuster.cs` | Event and calculation date shifting |
| `DayCountCalculator` | `src/ActusInsurance.Core/Conventions/DayCount/DayCountCalculator.cs` | Year-fraction computation |

### System Boundaries

The library is **stateless and in-process**. It has no database, no HTTP endpoints, and no external service dependencies. Callers supply all contract terms and risk-factor data; the library returns computed event lists. Persistence, API exposure, and UI are the responsibility of the host application.

### External Integrations

- None at the library level.
- The CI pipeline publishes NuGet packages to [nuget.org](https://www.nuget.org/) via `NUGET_API_KEY` (GitHub Actions secret).

### Evidence from Code

- `src/ActusInsurance.Core.CPU/Contracts/PAM.cs`
- `src/ActusInsurance.Core/Events/ContractEvent.cs`
- `src/ActusInsurance.Core/States/StateSpace.cs`
- `src/ActusInsurance.Core/Models/PamContractTerms.cs`
- `src/ActusInsurance.Core/Externals/RiskFactorModel.cs`
- `src/ActusInsurance.Core/Time/ScheduleFactory.cs`
- `.github/workflows/preview.yml`
- `.github/workflows/release.yml`

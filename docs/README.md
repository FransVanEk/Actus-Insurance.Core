# Actus-Insurance.Core Documentation

This folder contains the full documentation for **Actus-Insurance.Core** — a .NET library that models financial contracts using the [ACTUS](https://www.actusfrf.org/) standard.

## Document Index

| File | Description |
|---|---|
| [overview.md](./overview.md) | System purpose, key workflows, and main components |
| [value-proposition.md](./value-proposition.md) | Business value and strategic rationale |
| [architecture.md](./architecture.md) | Package structure, data flow, and design decisions |
| [domain-model.md](./domain-model.md) | Core concepts: contracts, events, state space, risk factors |
| [modules/contracts.md](./modules/contracts.md) | Contract types, lifecycle, and scheduling |
| [modules/calculation-engine.md](./modules/calculation-engine.md) | Event evaluation, payoff formulas, accrual logic |
| [modules/risk-factors.md](./modules/risk-factors.md) | External market data and rate look-up |
| [testing.md](./testing.md) | Test suite, coverage, and how to run tests |
| [operations.md](./operations.md) | CI/CD, NuGet publishing, configuration |
| [developer-guide.md](./developer-guide.md) | How to run locally, extend, and contribute |
| [reference.md](./reference.md) | Data models, public API, types, folder map |

## Quick Start

```bash
# Restore and build
dotnet restore
dotnet build

# Run tests
dotnet test
```

## Terminology Used Throughout These Docs

| Term | Meaning |
|---|---|
| **Contract** | A financial agreement (e.g. a loan) modelled according to the ACTUS standard |
| **PAM** | Principal-At-Maturity — the only contract type currently implemented |
| **Event** | A point-in-time action on a contract (interest payment, rate reset, etc.) |
| **State Space** | The mutable financial state of a contract between events |
| **Risk Factor** | External market data (interest rates, indices) supplied at run time |
| **Schedule** | The ordered list of events generated for a contract over its lifetime |
| **Day Count Convention** | The method used to compute a year fraction between two dates |
| **Business Day Convention** | The rule for shifting event dates that fall on weekends or holidays |

## Evidence from Code

- `src/ActusInsurance.Core/` — core types, conventions, state, schedule
- `src/ActusInsurance.Core.CPU/` — PAM contract implementation
- `src/ActusInsurance.Tests.CPU/` — NUnit test suite
- `Actus-Insurance.Core.slnx` — solution file
- `Directory.Build.props` — shared build properties

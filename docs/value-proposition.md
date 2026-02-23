# Value Proposition

This document explains why Actus-Insurance.Core exists, the problems it solves, and the value it delivers to both business stakeholders and engineering teams.

Related: [Overview](./overview.md) | [Architecture](./architecture.md)

---

## Business View

### The Problem

Every financial institution that holds loans, bonds, or structured products must answer three basic questions every day:

1. How much interest has accrued on each contract?
2. When will the next payment happen and for how much?
3. What is the outstanding principal and net present value?

In practice, these calculations are buried inside dozens of separate systems — core banking platforms, insurance ledgers, trading systems — each with slightly different rules, different calendar conventions, and different interpretations of what "interest" means. The result is:

- **Inconsistent numbers** across systems
- **Expensive reconciliation** cycles
- **Re-implementation risk** every time a new product is launched
- **Audit difficulty** because the calculation logic is not documented in a standard way

### The Solution

Actus-Insurance.Core encodes the **ACTUS standard** — a globally agreed specification for how financial contracts behave — into a single, tested, open-source .NET library. Any system that plugs in this library gets:

- **Consistent calculations** guaranteed by the ACTUS test suite
- **Standard terminology** shared between actuaries, risk managers, and developers
- **Reusable building blocks** so new contract types can be added without re-inventing accrual or scheduling logic

### Business Value

| Benefit | Description |
|---|---|
| **Faster product development** | Teams add a NuGet reference instead of writing financial maths from scratch |
| **Reduced operational risk** | A single, tested library eliminates calculation divergence between systems |
| **Regulatory alignment** | ACTUS is recognised by regulators and academics as a reference standard |
| **Auditability** | Every payoff is derived from documented formulas with a traceable test suite |

### Risks if This Library Fails

- Incorrect interest accrual leads to billing errors and customer disputes
- Wrong rate-reset values produce mispriced products and potential financial losses
- Schedule errors cause missed or double payments

### Who Cares

- **CFOs and finance directors** — correct P&L attribution and balance-sheet valuation
- **Risk managers** — accurate cash-flow projections for liquidity and ALM
- **Product managers** — faster time-to-market for new financial products
- **Compliance and audit** — standard, documentable calculation methodology
- **Engineering teams** — pre-built, tested engine they can trust

---

## Technical View

### Strategic Positioning

The library is deliberately **narrow in scope**:

- It models one contract type today (PAM — Principal At Maturity).
- It exposes a clean interface (`IContractScheduler<TTerms>`) so additional contract types can be added without changing the core.
- It ships as two NuGet packages so consumers can take only what they need.

### Design Choices That Deliver Value

| Choice | Rationale |
|---|---|
| Immutable contract terms (`PamContractTerms`) | Prevents accidental mutation during evaluation |
| `StateSpace` as a value type (`struct`) | Avoids heap allocations in tight loops; state copies are cheap |
| `IContractScheduler<TTerms>` interface | Decouples the calculation engine from any specific contract type |
| `RiskFactorModel` injected at call time | Keeps the engine pure and testable without mocking infrastructure |
| ACTUS JSON test vectors in `Resources/actus-tests-pam.json` | Validates against the canonical reference implementation |

### Extensibility Path

```
flowchart TD
    A[IContractTerms interface]
    B[New contract terms class]
    C[IContractScheduler implementation]
    D[New contract tests]
    A --> B
    B --> C
    C --> D
```

Adding a new contract type (e.g. ANN — Annuity) requires:

1. A new `IContractTerms` implementation in `src/ActusInsurance.Core/Models/`
2. A new `IContractScheduler<T>` implementation in `src/ActusInsurance.Core.CPU/Contracts/`
3. Corresponding test cases

No changes are needed to the core library.

### Evidence from Code

- `src/ActusInsurance.Core/Contracts/IContractScheduler.cs` — extension interface
- `src/ActusInsurance.Core/Models/IContractTerms.cs` — contract terms interface
- `src/ActusInsurance.Core.CPU/ActusInsurance.Core.CPU.csproj` — separate packagable project
- `src/ActusInsurance.Core/ActusInsurance.Core.csproj` — core package
- `Directory.Build.props` — shared metadata: `Authors`, `PackageTags`, `Company`

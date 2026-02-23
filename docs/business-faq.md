# Business FAQ

This document answers the most common questions that business stakeholders, managers, auditors, and partners ask when first encountering Actus-Insurance.Core. No technical background is required to read it.

Related: [Overview](./overview.md) | [Value Proposition](./value-proposition.md) | [Testing](./testing.md) | [Operations](./operations.md)

---

## 1. Fundamentals

### What does this system do?

Actus-Insurance.Core is a software library that calculates the future cash flows of financial contracts — for example, when interest payments are due, how much they are, and what the outstanding loan balance is at any date. It also projects how those figures change when market interest rates move.

Evidence from code:
- `docs/overview.md` — "What Is This System?" section
- `src/ActusInsurance.Core.CPU/Contracts/PAM.cs`

---

### Why does it exist?

Every financial institution that holds loans, bonds, or other fixed-income products must answer the same three questions every day: how much interest has accrued? When is the next payment and for how much? What is the outstanding balance? Without a shared library, each team writes its own version of these calculations — creating inconsistencies, costly reconciliation cycles, and audit difficulties. Actus-Insurance.Core provides one tested, standard-compliant engine that all systems can share.

Evidence from code:
- `docs/value-proposition.md` — "The Problem" section
- `docs/overview.md` — "Why Does It Exist?"

---

### What problem does it solve?

It removes the need for each product team to build financial mathematics from scratch. It also ensures that all systems produce the same numbers for the same contract, eliminating discrepancies between, for example, a core banking platform and a risk reporting system.

Evidence from code:
- `docs/value-proposition.md` — "The Solution" section

---

### Who is it for?

- **Product teams** building loan management, insurance, or asset-liability systems
- **Risk managers** who need consistent cash-flow projections
- **Finance and actuarial teams** who require auditable, standard-compliant calculations
- **Developers** who want a pre-built engine rather than coding financial maths themselves
- **Compliance and audit teams** who need a documentable calculation methodology

Evidence from code:
- `docs/overview.md` — "Who Cares About It?" section
- `docs/value-proposition.md` — "Who Cares" section

---

### What is a contract in this system?

A **contract** is a financial agreement in which one party lends a principal (a sum of money) and the other party promises to repay it at a future date, together with interest and any agreed fees. In this system, every contract has a type. Currently only one type is implemented: PAM (explained below). A contract is defined by its terms — the fixed parameters agreed at the start: who owes what, at what rate, for how long, and with which payment dates.

Evidence from code:
- `docs/domain-model.md` — "Core Concepts / Contract" section
- `src/ActusInsurance.Core/Models/IContractTerms.cs`
- `src/ActusInsurance.Core/Models/PamContractTerms.cs`

---

### What is PAM?

**PAM** stands for **Principal At Maturity**. It is the simplest type of fixed-income contract:

- A principal amount is exchanged on a start date.
- Interest accrues over the life of the contract.
- Interest may be paid periodically or added to the principal (capitalised).
- The full principal is repaid as a single lump sum at the end (maturity).

This covers instruments such as zero-coupon bonds, bullet loans, and term deposits. PAM is the only contract type currently implemented; others can be added without changing the core library.

Evidence from code:
- `docs/modules/contracts.md` — "What Is PAM?" section
- `src/ActusInsurance.Core.CPU/Contracts/PAM.cs`

---

### What is a calculation?

A **calculation** is the process of applying a financial formula to one event in a contract's timeline. For each event (for example, an interest payment date), the engine computes two things: how much cash changes hands (the payoff), and what the new financial state of the contract is (for example, the updated accrued interest balance). All formulas are derived from the ACTUS international standard.

Evidence from code:
- `docs/modules/calculation-engine.md` — "What Does the Calculation Engine Do?"
- `src/ActusInsurance.Core/Events/ContractEvent.cs`

---

### What is a risk factor?

A **risk factor** is a piece of external market data that can affect how a contract behaves. The most common example is a benchmark interest rate (such as EURIBOR or SOFR) used to update the interest rate on a variable-rate loan. A price index (such as a consumer price index) used to scale the outstanding principal on an inflation-linked bond is another example. Risk factors come from outside the contract — they are observed in the market, not fixed in the contract's terms.

Evidence from code:
- `docs/modules/risk-factors.md` — "What Are Risk Factors?" section
- `src/ActusInsurance.Core/Externals/RiskFactorModel.cs`

---

### What is a sink?

The term "sink" is not currently defined in this codebase. It does not appear in the source code or existing documentation. Files checked:
- `docs/overview.md`, `docs/architecture.md`, `docs/domain-model.md`
- `src/ActusInsurance.Core/` (full folder)

In the context of financial contract modelling, a "sink" sometimes refers to the destination of cash flows (e.g. the party receiving payment). If this term applies to this system, please refer to `ContractRole` in `docs/domain-model.md`, which defines the direction of cash flows (+1 or −1) for each party.

---

### What is a source?

The term "source" is not currently defined as a formal concept in this codebase. It does not appear as a defined term in the source code or existing documentation. Files checked:
- `docs/overview.md`, `docs/modules/risk-factors.md`
- `src/ActusInsurance.Core/Externals/RiskFactorModel.cs`

In practice, the caller (the application that uses this library) is responsible for sourcing market data and providing it to the library through the `RiskFactorModel` object. The library itself does not fetch data from any external service.

---

### What is an event?

An **event** is a specific moment in time when something financial happens on a contract — for example, an interest payment, a principal repayment at maturity, a rate reset, or a fee. The full list of event types is:

| Short name | What it means |
|---|---|
| IED | Initial Exchange — contract starts; principal is exchanged |
| IP | Interest Payment — periodic interest paid |
| IPCI | Interest Capitalisation — accrued interest added to principal instead of paid out |
| RR | Rate Reset — variable rate updated from market data |
| RRF | Rate Reset Fixed — rate set to a predetermined value |
| FP | Fee Payment — periodic fee paid |
| SC | Scaling — notional or interest scaling index updated |
| PRD | Purchase — contract bought at a market price |
| TD | Termination — early termination at a settlement price |
| MD | Maturity — contract ends; principal returned |
| AD | Analysis Date — observation point; no cash flow |
| CD | Credit Default — credit default event |

Evidence from code:
- `docs/domain-model.md` — "EventType Enum" section
- `src/ActusInsurance.Core/Types/Enums.cs`

---

## 2. How it Works (Plain Language)

### Can you explain what this system does step by step, without technical jargon?

Here is what happens from start to finish:

**Step 1 — You describe the contract.**
You provide the contract's terms: the principal amount, the start date, the end date, the interest rate, the currency, how often interest is paid, and any other relevant details (fees, rate-reset rules, etc.). This is like filling in a loan agreement form.

**Step 2 — The system builds a timeline.**
The system reads the terms and produces an ordered list of all the events that will occur over the life of the contract — every interest payment date, every rate reset date, every fee date, and the final maturity date. If a payment date falls on a weekend or public holiday, the system shifts it to the next (or previous) business day according to the rules you specified.

**Step 3 — You provide market data (if needed).**
For variable-rate contracts, you tell the system what the relevant benchmark interest rate was on each date it was fixed. This is optional — if the contract has a fixed rate, no market data is needed.

**Step 4 — The system calculates each event.**
Working through the timeline in order, the system applies the appropriate financial formula to each event. It computes how much cash changes hands and updates the running financial state of the contract (outstanding balance, accrued interest, current rate). Each event's result is stored alongside the event.

**Step 5 — You receive the results.**
The system returns the complete list of events, each with its date, type, cash amount, and the post-event financial state. You can use these results for payment processing, risk analysis, accounting entries, or regulatory reporting.

Evidence from code:
- `docs/overview.md` — "Key Workflows" section
- `docs/modules/contracts.md` — "Contract Lifecycle" section
- `docs/modules/calculation-engine.md` — "Execution Steps" section
- `src/ActusInsurance.Core.CPU/Contracts/PAM.cs`

---

### How does the system handle interest?

Interest accrues continuously between events. When an interest payment event arrives, the system calculates the exact interest owed since the last event using a **day count convention** — a standard rule for converting a calendar period into a fraction of a year. For example, the Actual/365 convention counts the exact number of days and divides by 365. The interest amount is then: outstanding principal × annual rate × year fraction.

Evidence from code:
- `docs/modules/calculation-engine.md` — "Interest Accrual Formula" section
- `src/ActusInsurance.Core/Events/ContractEvent.cs`
- `src/ActusInsurance.Core/Conventions/DayCount/DayCountCalculator.cs`

---

### How does the system handle variable interest rates?

On a rate-reset event, the system looks up the relevant benchmark rate from the market data you provided, applies any spread or multiplier defined in the contract terms, and updates the running interest rate. All subsequent interest events use the new rate until the next reset. Caps and floors (maximum and minimum rate changes per period, or over the contract's life) are enforced automatically.

Evidence from code:
- `docs/modules/calculation-engine.md` — "Rate Reset Logic" section
- `src/ActusInsurance.Core/Events/ContractEvent.cs` — `UpdateRateReset()`

---

### What happens at the end of the contract?

At the maturity date (MD event), the outstanding principal is returned in full. Accrued interest is reset to zero and the notional principal in the contract's state is set to zero, marking the contract as closed.

Evidence from code:
- `docs/modules/calculation-engine.md` — "State Transitions by Event Type" table
- `src/ActusInsurance.Core/Events/ContractEvent.cs`

---

## 3. Validation and Trust

### How can we verify that the results are correct?

The library validates its output against the official **ACTUS reference test vectors** — a published set of contract scenarios with pre-computed correct results from the ACTUS Financial Research Foundation (the international body that defines the standard). Passing these tests means the engine matches the canonical reference implementation, not just an internal expectation.

Evidence from code:
- `docs/testing.md` — "Why Testing Matters Here" section
- `src/ActusInsurance.Tests.CPU/Resources/actus-tests-pam.json`
- `src/ActusInsurance.Tests.CPU/PamTests.cs`

---

### How do we know calculations are accurate?

Three mechanisms work together:

1. **ACTUS test vectors** — the engine's output is compared field by field to published reference results for dozens of contract scenarios covering all event types.
2. **Numerical tolerance** — floating-point comparisons use a tolerance of `2 × 10⁻¹⁰` (0.0000000002), which is far tighter than any real-world monetary precision requirement.
3. **Automated CI** — every code change automatically runs the full test suite before any package is published. A failing test blocks the release.

Evidence from code:
- `src/ActusInsurance.Tests.CPU/PamTests.cs` — comparison logic
- `docs/testing.md` — "Comparison Logic" section
- `.github/workflows/preview.yml`, `.github/workflows/release.yml`

---

### What tests exist?

The test suite covers all PAM event types and a wide range of scenarios:

- All event types: IED, IP, IPCI, RR, RRF, FP, SC, MD, PRD, TD
- Variable-rate contracts with rate resets
- Contracts with fee schedules
- Contracts with scaling (notional and interest)
- Contracts with capitalisation periods
- All supported business-day adjustment conventions
- Various day-count conventions

Tests are driven by the ACTUS JSON reference file (`actus-tests-pam.json`). Each entry in that file is a self-contained scenario: contract terms, market data inputs, and expected event-by-event results.

Evidence from code:
- `docs/testing.md` — "What Does the Test Suite Cover?" section
- `src/ActusInsurance.Tests.CPU/PamTests.cs`
- `src/ActusInsurance.Tests.CPU/Resources/actus-tests-pam.json`

---

### How are ACTUS JSON test cases used?

The test runner reads the `actus-tests-pam.json` file. For each entry it:

1. Parses the contract terms from the `"terms"` block.
2. Builds a market-data object from the `"dataObserved"` block.
3. Calls the scheduling and calculation engine.
4. Compares every field of every event in the computed result to the expected result in the `"results"` block.

Any difference outside the numerical tolerance causes a test failure and a detailed error message showing exactly which event and which field differed.

Evidence from code:
- `docs/testing.md` — "Test Structure" and "Test Data Format" sections
- `src/ActusInsurance.Tests.CPU/PamTests.cs`

---

### What guarantees correctness?

- The ACTUS standard specifies exact payoff formulas — these are encoded directly in the engine.
- The reference test suite published by the ACTUS Foundation is the ground truth.
- The CI pipeline runs all tests on every commit; no package is published unless all tests pass.
- The library is stateless and deterministic: the same inputs always produce the same outputs.

Evidence from code:
- `docs/value-proposition.md` — "Consistent calculations guaranteed by the ACTUS test suite"
- `docs/operations.md` — CI/CD pipeline steps

---

### What prevents silent errors?

- **Automated tests** run on every code change. A silent regression in any covered formula will cause a test failure.
- **Test failure reporting** collects all differences within a test case and reports them together, so partial failures are not hidden.
- **Numerical tolerance** is strict enough (`2e-10`) to catch even sub-cent rounding errors in typical contract sizes.

Evidence from code:
- `docs/testing.md` — "Test Failure Reporting" section
- `src/ActusInsurance.Tests.CPU/PamTests.cs`

---

## 4. Risk and Safety

### What could go wrong?

| Risk | Description |
|---|---|
| Missing market data | If a rate-reset contract is processed without the required benchmark rate, the engine silently uses 0.0 as the rate. This produces incorrect payoffs. |
| Malformed contract terms | The library does not validate input. Incorrect or missing dates may produce empty schedules or zero payoffs without raising an error. |
| Division by zero in scaling | If `ScalingIndexAtContractDealDate` is zero and a scaling event fires, a division-by-zero error occurs. |
| Wrong currency | The library does not perform currency conversion. FX rates always return 1.0. Multi-currency setups require the caller to normalise currencies before calling the library. |
| Broken NuGet API key | If the publishing credential expires, new package versions cannot be released to users. |

Evidence from code:
- `docs/modules/risk-factors.md` — "Risks If Risk Factors Are Missing or Wrong" and "Safety Checks and Fallback"
- `docs/modules/contracts.md` — "Validation" section
- `docs/modules/calculation-engine.md` — "Note on FX"
- `docs/operations.md` — "Risks" section

---

### How is risk handled?

- **Rate caps and floors** — variable-rate contracts can specify a maximum and minimum interest rate per period and over the contract's life. The engine enforces these automatically.
- **Previous-value interpolation** — if no market rate exists for the exact reset date, the most recent available rate is used rather than defaulting to zero.
- **Deterministic engine** — the library is stateless. It cannot accumulate errors across runs.

Evidence from code:
- `docs/modules/calculation-engine.md` — "Rate Reset Logic" section
- `docs/modules/risk-factors.md` — "Rate Lookup Algorithm" section
- `src/ActusInsurance.Core/Events/ContractEvent.cs` — `UpdateRateReset()`

---

### How are incorrect inputs handled?

The library does not have a formal validation layer. Incorrect or missing inputs are handled as follows:

- Missing optional fields (cycles, dates) result in those event types being omitted from the schedule, which is correct behaviour.
- Missing required dates (StatusDate, MaturityDate, InitialExchangeDate) may produce an empty schedule.
- Non-parseable values in the dictionary-based factory (`PamContractTerms.FromDictionary()`) fall back to default values (0.0 for numbers, minimum DateTime for dates) without raising exceptions.

The caller is responsible for validating inputs before passing them to the library.

Evidence from code:
- `docs/modules/contracts.md` — "Validation" section
- `src/ActusInsurance.Core/Models/PamContractTerms.cs` — `GetDouble`, `GetDateTime` helpers

---

### What happens if data is missing?

| Missing data | Engine behaviour |
|---|---|
| No interest payment cycle | No interest payment events generated |
| No rate-reset cycle | Contract treated as fixed-rate |
| No fee cycle | No fee events generated |
| Market rate not found for exact date | Most recent available rate used |
| No market rate data at all | Rate defaults to 0.0 |
| Accrued interest not supplied | Calculated from last scheduled IP date before StatusDate |

Evidence from code:
- `docs/modules/contracts.md` — "Scheduling Logic" section
- `docs/modules/risk-factors.md` — "Rate Lookup Algorithm" section
- `src/ActusInsurance.Core.CPU/Contracts/PAM.cs`

---

### How are limits enforced?

- **Rate caps and floors** — the engine clamps rate changes to `PeriodCap`/`PeriodFloor` and the final rate to `LifeCap`/`LifeFloor`.
- **Contract lifetime constants** — the codebase defines maximum lifetimes (50 years for standard contracts, 10 years for stock and UMP types). These are documented in the code but not currently enforced by the scheduling logic; they serve as intended design boundaries.

Evidence from code:
- `docs/operations.md` — "Maximum Lifetimes" section
- `src/ActusInsurance.Core/Util/Constants.cs`
- `src/ActusInsurance.Core/Events/ContractEvent.cs` — `UpdateRateReset()`

---

## 5. Data Questions

### What inputs are required?

Every contract requires at minimum:

| Field | What it means |
|---|---|
| `ContractID` | A unique identifier for the contract |
| `StatusDate` | The as-of date (calculations start from here) |
| `InitialExchangeDate` | The date the principal is exchanged |
| `MaturityDate` | The date the contract ends |
| `NotionalPrincipal` | The face value (the amount lent) |
| `NominalInterestRate` | The annual interest rate (e.g. 0.05 for 5%) |
| `Currency` | The currency code (e.g. "EUR", "USD") |

Optional inputs enable additional features (fee schedules, rate resets, scaling, etc.). For variable-rate contracts, market data must also be supplied.

Evidence from code:
- `docs/reference.md` — "PamContractTerms — Field Reference" table
- `src/ActusInsurance.Core/Models/PamContractTerms.cs`

---

### Where does data come from?

The library does not fetch data from any source. All data — contract terms and market rates — must be supplied by the calling application. The calling application is responsible for reading contracts from its database, fetching benchmark rates from a market data provider, and constructing the input objects that the library expects.

Evidence from code:
- `docs/overview.md` — "System Boundaries" section
- `docs/modules/risk-factors.md` — "Who Supplies Risk Factors?" section

---

### Where does output go?

The library returns a list of events in memory. Each event contains:

- The event date and type
- The cash payoff (the amount that changes hands)
- The post-event financial state (outstanding principal, accrued interest, current rate, accrued fees)

The calling application is responsible for storing, displaying, or forwarding these results. The library has no database, no file output, and no network connections.

Evidence from code:
- `docs/overview.md` — "System Boundaries" section
- `docs/domain-model.md` — "ContractEvent" section
- `src/ActusInsurance.Core/Events/ContractEvent.cs`

---

### Can results be exported?

The library itself does not include export utilities. The library returns plain in-memory objects (a list of `ContractEvent` records). Export to CSV, Excel, a database, or any other format is the responsibility of the calling application. No export utilities are included in this library. Files checked:
- `src/ActusInsurance.Core/`, `src/ActusInsurance.Core.CPU/`

---

### Can results be audited?

Yes, and this is one of the library's key strengths. Every computed result is:

- **Derived from documented formulas** defined in the ACTUS standard.
- **Validated against published reference test vectors** that can be independently inspected in `src/ActusInsurance.Tests.CPU/Resources/actus-tests-pam.json`.
- **Fully reproducible** — the library is stateless and deterministic. Given the same inputs, it always produces the same outputs.

An auditor can inspect the test file to see what the engine is expected to produce for a given scenario, and can re-run the test suite to verify the engine still produces those results.

Evidence from code:
- `docs/testing.md` — "Why Testing Matters Here" section
- `src/ActusInsurance.Tests.CPU/Resources/actus-tests-pam.json`
- `docs/value-proposition.md` — "Auditability" row

---

## 6. Operational Questions

### How is it deployed?

The library is distributed as two **NuGet packages** — the standard packaging format for .NET software libraries. Teams add a package reference to their project and the library becomes available immediately. There is no server, no database, and no installation procedure.

| Package | Purpose |
|---|---|
| `ActusInsurance.Core` | Core types, schedules, conventions |
| `ActusInsurance.Core.CPU` | PAM contract calculation engine |

Publishing happens automatically via GitHub Actions when a new release is tagged. A preview version is also published on every commit to the main branch.

Evidence from code:
- `docs/operations.md` — "How Does the Software Reach Users?" section
- `.github/workflows/preview.yml`, `.github/workflows/release.yml`

---

### Can it scale?

Yes, with some caveats:

- The library processes one contract at a time in a single call. It is synchronous and in-process.
- Because there is no shared state between calls, the calling application can safely process thousands of contracts in parallel on multiple threads or servers.
- The engine is designed to be lightweight: it avoids memory allocations in the inner calculation loop and uses simple dictionary lookups for rate data.
- There is no built-in parallelism within the library itself.

Evidence from code:
- `docs/operations.md` — "Performance Considerations" section
- `src/ActusInsurance.Core/States/StateSpace.cs`

---

### What happens if it crashes?

Because the library runs inside the calling application's process, a crash in the library would surface as an unhandled exception in the host application. In practice, the library is designed to avoid exceptions by returning defaults rather than throwing errors for missing data. The most likely cause of an exception would be a programming error (such as passing a null reference) rather than a data issue.

The library has no persistent state, so a crash does not leave any data in an inconsistent state. The calling application can safely retry the same calculation.

Evidence from code:
- `docs/overview.md` — "System Boundaries" section
- `docs/modules/contracts.md` — "Validation" section

---

### Is there monitoring?

The library itself produces no logs or metrics. It is an in-process library with no runtime process of its own. The calling application is responsible for any monitoring, alerting, and logging around its use of the library.

CI build and test logs are available in the GitHub Actions run history for every code change.

Evidence from code:
- `docs/operations.md` — "Logging and Monitoring" section
- `.github/workflows/preview.yml`

---

## 7. Change and Maintenance

### Can logic change?

Yes. The calculation logic is ordinary code and can be changed. However, any change to a payoff formula or scheduling rule will be caught by the ACTUS reference test suite if it deviates from the standard. This acts as a safety net: accidental logic changes that break the standard will cause test failures in the CI pipeline before the change reaches users.

Evidence from code:
- `docs/testing.md` — "Risks If Tests Are Removed" section
- `.github/workflows/preview.yml` — tests run before every publish

---

### How safe are updates?

Updates go through the following safety gates:

1. A developer makes a code change and opens a pull request.
2. The CI pipeline automatically builds the project and runs all ACTUS reference tests.
3. If any test fails, the pipeline stops and no package is published.
4. Only after all tests pass can a new version be released.

The preview channel (publishing on every commit to `main`) and the stable release channel (publishing on a tagged release) both run through the same test gate.

Evidence from code:
- `docs/operations.md` — "CI/CD Pipelines" section
- `.github/workflows/preview.yml`, `.github/workflows/release.yml`

---

### How are new contract types added?

Adding a new contract type (for example, ANN — Annuity) requires three steps, none of which change existing code:

1. Define the new contract's terms in a new class.
2. Implement the scheduling and calculation logic in a new class.
3. Add test cases, ideally using ACTUS reference test vectors for that contract type.

The core library and the existing PAM implementation are not modified. The extensibility is built in through the `IContractTerms` and `IContractScheduler` interfaces.

Evidence from code:
- `docs/developer-guide.md` — "Adding a New Contract Type" section
- `docs/value-proposition.md` — "Extensibility Path" section
- `src/ActusInsurance.Core/Contracts/IContractScheduler.cs`

---

### How are new risk factors added?

No code change is required to support a new risk factor. The caller simply calls `AddRate` or `AddConstantRate` on the `RiskFactorModel` object with any identifier string it chooses. The contract terms must include the matching identifier so the engine knows which rate to look up on a rate-reset or scaling event.

Evidence from code:
- `docs/developer-guide.md` — "Adding a Risk Factor" section
- `src/ActusInsurance.Core/Externals/RiskFactorModel.cs`

---

## 8. Decision Maker Questions

### Why should a company trust this system?

1. **It implements an internationally recognised standard.** The ACTUS Financial Research Foundation defines the calculation rules. This is not a proprietary formula invented for this library — it is the same standard referenced by regulators and academics.
2. **It is validated against published reference test vectors.** The test results can be independently verified against the ACTUS Foundation's own published outputs.
3. **It is open source.** The complete calculation logic is visible and auditable by anyone.
4. **Automated testing blocks incorrect releases.** No package version reaches users unless the full test suite passes.

Evidence from code:
- `docs/value-proposition.md` — "The Solution" and "Business Value" sections
- `docs/testing.md` — "Why Testing Matters Here" section
- `src/ActusInsurance.Tests.CPU/Resources/actus-tests-pam.json`

---

### What business value does it provide?

| Benefit | Description |
|---|---|
| Faster product development | Teams add a NuGet reference instead of writing financial mathematics from scratch |
| Reduced operational risk | One tested library eliminates calculation divergence between systems |
| Regulatory alignment | ACTUS is recognised by regulators and academics as a reference standard |
| Auditability | Every payoff derives from documented formulas with a traceable test suite |
| Lower maintenance cost | A single library maintained in one place, not duplicated across teams |

Evidence from code:
- `docs/value-proposition.md` — "Business Value" table

---

### What risks does it reduce?

- **Incorrect billing** — consistent formulas prevent interest being calculated differently by different systems.
- **Missed payments** — a correct schedule ensures every payment event is generated on the right date.
- **Mispriced products** — correct rate-reset logic prevents variable-rate products from being valued with stale or wrong rates.
- **Audit failures** — documented, standard-compliant calculations reduce the risk of regulatory findings.

Evidence from code:
- `docs/value-proposition.md` — "Risks if This Library Fails" section
- `docs/modules/contracts.md` — "Risks If Contracts Fail" section

---

### What costs does it save?

- Development cost — teams do not build the same financial mathematics repeatedly.
- Reconciliation cost — inconsistencies between systems are eliminated at source rather than discovered and resolved after the fact.
- Audit cost — a standard methodology with documented test coverage is faster and cheaper to audit than bespoke implementations.

Evidence from code:
- `docs/value-proposition.md` — "The Problem" and "Business Value" sections

---

### What would happen without it?

Each team would implement the same financial calculations independently. This leads to:

- Different systems producing different interest amounts for the same contract
- Expensive manual reconciliation to find and explain discrepancies
- Higher risk of billing errors reaching customers
- Slower time to market for new products
- Increased audit and regulatory risk

Evidence from code:
- `docs/value-proposition.md` — "The Problem" section

---

## 9. Edge Case Questions

### What happens with extreme values — very large or very small principals?

The engine uses standard 64-bit floating-point arithmetic (double precision), which can represent values up to approximately 1.8 × 10³⁰⁸ with about 15–16 significant digits of precision. For realistic financial contract sizes (billions of currency units), this is sufficient. There is no explicit upper or lower limit on the principal amount in the library itself.

Evidence from code:
- `src/ActusInsurance.Core/States/StateSpace.cs` — `double NotionalPrincipal`
- `src/ActusInsurance.Core/Models/PamContractTerms.cs` — `double NotionalPrincipal`

---

### What happens with a zero interest rate?

If `NominalInterestRate` is zero, no interest accrues and all interest payment events produce a payoff of zero. The contract's principal is still exchanged at IED and returned at MD. This is valid behaviour for a zero-coupon contract with no stated rate.

Evidence from code:
- `docs/modules/calculation-engine.md` — "Edge Cases" table: "`NominalInterestRate == 0`"
- `src/ActusInsurance.Core/Events/ContractEvent.cs`

---

### What happens with a zero fee rate?

If `FeeRate` is zero, no fees accrue and all fee payment events produce a payoff of zero. The contract is effectively fee-free.

Evidence from code:
- `docs/modules/calculation-engine.md` — "Edge Cases" table: "`FeeRate == 0`"

---

### What happens with a contract that starts in the future?

If the `InitialExchangeDate` is later than the `StatusDate`, the contract has not yet started. The engine initialises the outstanding principal and rate to zero and generates no payoff until the IED event fires. This is correct behaviour for a forward-starting contract.

Evidence from code:
- `docs/modules/calculation-engine.md` — "Edge Cases" table: "Future-start contract"
- `src/ActusInsurance.Core.CPU/Contracts/PAM.cs` — `InitializeStateSpace()`

---

### What happens if a payment date falls on a weekend or holiday?

The library supports multiple **business day adjustment conventions** that shift payment dates to the next or previous business day. The convention is set per contract in the `BusinessDayConvention` field. If the `NOS` (no shift) convention is used, dates are not adjusted. The available calendars are:

- `NC` — every day is a business day (no adjustment)
- `MF` — Monday to Friday are business days
- `MFH` — Monday to Friday minus a supplied list of holidays

Evidence from code:
- `docs/architecture.md` — "Conventions Supported" section
- `src/ActusInsurance.Core/Conventions/BusinessDay/BusinessDayAdjuster.cs`
- `src/ActusInsurance.Core/Time/Calendar/`

---

### What happens with very long-dated contracts (beyond 50 years)?

The code defines a constant `MAX_LIFETIME = 50 years`, documented as the intended maximum contract lifetime. However, this constant is **not currently enforced** by the scheduling logic — contracts beyond 50 years will still be processed without an error. The constant serves as a documented design boundary. Callers relying on this limit should enforce it themselves.

Evidence from code:
- `docs/operations.md` — "Maximum Lifetimes" section
- `src/ActusInsurance.Core/Util/Constants.cs`

---

### What happens if the rate-reset market data is provided for the wrong dates?

If no rate exists for the exact reset date, the engine uses the most recent rate provided before that date (previous-value interpolation). If no rate exists at all before the reset date, the engine uses 0.0. This is a silent fallback — no warning or error is raised. Callers must ensure that rate data covers all reset dates in the contract's schedule.

Evidence from code:
- `docs/modules/risk-factors.md` — "Rate Lookup Algorithm" and "Safety Checks and Fallback" sections
- `src/ActusInsurance.Core/Externals/RiskFactorModel.cs`

---

### What happens with a contract terminated early?

If a `TerminationDate` is set, the engine generates an Interest Payment event (IP) and a Termination event (TD) on that date. All events scheduled after the termination date are removed. The TD payoff is `RoleSign × PriceAtTerminationDate`, which is a settlement amount agreed at the time of termination.

Evidence from code:
- `docs/modules/contracts.md` — "Scheduling Logic" section, step 8
- `docs/modules/calculation-engine.md` — "Payoff Formulas" table
- `src/ActusInsurance.Core.CPU/Contracts/PAM.cs`

---

### What happens if the scaling index at contract deal date is zero?

If `ScalingIndexAtContractDealDate` is zero and a scaling event fires, the engine performs a division by zero. This is a known gap: the library does not guard against this input. Callers must ensure this value is non-zero when scaling is used.

Evidence from code:
- `docs/modules/risk-factors.md` — "Safety Checks and Fallback" table
- `src/ActusInsurance.Core/Events/ContractEvent.cs` — `UpdateScaling()`

---

### What happens with currency mismatches?

The library does not perform currency conversion. The FX rate is always 1.0 (a placeholder). If a caller passes contracts in different currencies, all values will be treated as if they share the same currency. Multi-currency support must be implemented by the calling application. This is a documented limitation.

Evidence from code:
- `docs/modules/calculation-engine.md` — "Note on FX" section
- `src/ActusInsurance.Core/Events/ContractEvent.cs` — `GetFxRate()`

---

### Are there regulatory edge cases the system handles?

The library implements the **ACTUS standard**, which was designed with regulatory compliance in mind. Specific regulatory considerations:

- **Day count conventions** — all major conventions used in regulatory reporting (Actual/Actual ISDA, Actual/360, 30E/360, Business/252, etc.) are supported.
- **Business day conventions** — all major shifting rules are supported, including those required by ISDA documentation.
- **Rate caps and floors** — enforced automatically, as required by many consumer lending regulations.

For any regulation that requires a specific calculation methodology not covered by ACTUS PAM, a new contract type or convention would need to be added.

Evidence from code:
- `docs/architecture.md` — "Conventions Supported" section
- `src/ActusInsurance.Core/Conventions/DayCount/`
- `src/ActusInsurance.Core/Conventions/BusinessDay/`

---

### What happens if the same contract is processed twice?

Because the library is stateless, processing the same contract twice produces exactly the same result both times. There is no risk of double-counting or state corruption from repeated calls.

Evidence from code:
- `docs/overview.md` — "System Boundaries" section
- `src/ActusInsurance.Core.CPU/Contracts/PAM.cs`

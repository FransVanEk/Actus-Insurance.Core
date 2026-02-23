# Testing

This document describes the test suite, what it covers, how tests validate behaviour, and how to run them.

Related: [Developer Guide](./developer-guide.md) | [Calculation Engine](./modules/calculation-engine.md)

---

## Business View

### Why Testing Matters Here

Financial calculation libraries carry a high accuracy requirement. A single incorrect formula can produce wrong interest amounts across every contract that uses it. This library validates its output against the official **ACTUS reference test vectors** — a set of contract scenarios with known correct results published by the ACTUS Financial Research Foundation.

Passing these tests gives confidence that the engine's behaviour matches the standard, not just an internal expectation.

### What Does the Test Suite Cover?

- All event types for the PAM (Principal At Maturity) contract: IED, IP, IPCI, RR, RRF, FP, SC, MD, PRD, TD
- Variable-rate contracts with rate resets
- Contracts with fee schedules
- Contracts with scaling (notional and interest)
- Contracts with capitalisation periods
- Business-day adjustment conventions
- Various day-count conventions

### Risks If Tests Are Removed

Without these tests, a code change could silently alter interest calculations and no automated check would catch it until incorrect amounts were paid.

---

## Technical View

### Test Project

| Item | Value |
|---|---|
| Project | `src/ActusInsurance.Tests.CPU/ActusInsurance.Tests.CPU.csproj` |
| Framework | NUnit (`[TestFixture]`, `[TestCaseSource]`) |
| Target | .NET 9 |
| Test file | `src/ActusInsurance.Tests.CPU/PamTests.cs` |
| Data | `src/ActusInsurance.Tests.CPU/Resources/actus-tests-pam.json` |

### How to Run Tests

```bash
# From the repo root
dotnet test

# With detailed output
dotnet test --verbosity normal

# Release configuration (matches CI)
dotnet test --configuration Release --verbosity normal
```

### Test Structure

```
flowchart TD
    A[GetTestCases reads JSON file]
    B[Each JSON entry becomes a TestCaseData]
    C[TestPrincipalAtMaturity runs per case]
    D[Parse PamContractTerms from JSON terms]
    E[Build RiskFactorModel from dataObserved]
    F[Call PrincipalAtMaturity.Schedule]
    G[Call PrincipalAtMaturity.Apply]
    H[Compare results to expected JSON results]
    A --> B
    B --> C
    C --> D
    D --> E
    E --> F
    F --> G
    G --> H
```

**Legend:**
- `GetTestCases` is a `[TestCaseSource]` method that yields one `TestCaseData` per JSON test entry.
- Each test case is named `PAM_V2_Test_<testId>`.

### Test Data Format

The file `actus-tests-pam.json` contains a JSON object where each key is a test ID. Each entry has:

```json
{
  "<testId>": {
    "terms": { /* PamContractTerms fields */ },
    "dataObserved": {
      "<marketObjectCode>": {
        "data": [
          { "timestamp": "2015-01-02T00:00:00", "value": "0.02" }
        ]
      }
    },
    "results": [
      {
        "eventDate": "2015-01-02T00:00:00",
        "eventType": "IED",
        "payoff": -1000.0,
        "notionalPrincipal": 1000.0,
        "nominalInterestRate": 0.05,
        "accruedInterest": 0.0
      }
    ]
  }
}
```

### Comparison Logic

The test compares computed results to expected results field by field:

- **Doubles**: compared with tolerance `2e-10` (`Math.Abs(expected - computed) <= 2e-10`).
- **Dates**: parsed to `DateTime` and compared exactly.
- **Strings**: compared with `Equals`.
- `nominalInterestRate` is **skipped** from assertions (known inconsistency in the reference data handling).
- Both sides are rounded to 10 decimal places before comparison.

```csharp
// src/ActusInsurance.Tests.CPU/PamTests.cs
if (Math.Abs(expectedDouble - computedDouble) <= 2e-10)
    return true;
```

### What Is Validated

For each event in the computed schedule, the following fields are checked against the reference:

| Field | Description |
|---|---|
| `eventDate` / `time` | Event time matches expected date |
| `eventType` / `type` | Event type string matches |
| `payoff` | Cash flow amount matches |
| `notionalPrincipal` | Post-event outstanding principal matches |
| `accruedInterest` | Post-event accrued interest matches |
| `feeAccrued` | Post-event accrued fee matches |
| `currency` | Currency code matches |

### Test Failure Reporting

When failures occur, all differences for a test case are collected and reported together:

```
Test <id>: Found N difference(s):
Index 2: Key 'payoff': Expected 41.666... but got 41.666... (diff: 1.2e-13)
```

### Test Helpers

| Class | Purpose |
|---|---|
| `TestDataLoader` | Reads and parses the JSON test file using `System.Text.Json` |
| `TestData` | Holds parsed terms, observed data, and expected results |
| `ObservedDataSet` | Holds a market object code and its time-series data |
| `ResultSet` | Holds a dictionary of field → value for one result row; supports `RoundTo()` |

### Evidence from Code

- `src/ActusInsurance.Tests.CPU/PamTests.cs`
- `src/ActusInsurance.Tests.CPU/Resources/actus-tests-pam.json`
- `src/ActusInsurance.Tests.CPU/ActusInsurance.Tests.CPU.csproj`

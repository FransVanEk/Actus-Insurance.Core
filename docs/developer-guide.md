# Developer Guide

This document explains how to set up the project locally, add new contract types or risk factors, extend the calculation logic, and add tests.

Related: [Architecture](./architecture.md) | [Testing](./testing.md) | [Reference](./reference.md)

---

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9)
- Any IDE or editor (Visual Studio, Rider, VS Code with C# extension)
- Git

---

## Running Locally

### Clone and Build

```bash
git clone <repo-url>
cd Actus-Insurance.Core
dotnet restore
dotnet build
```

### Run Tests

```bash
dotnet test
# or with verbose output:
dotnet test --verbosity normal
```

### Run Tests for a Specific Project

```bash
dotnet test src/ActusInsurance.Tests.CPU/ActusInsurance.Tests.CPU.csproj
```

### Build NuGet Packages Locally

```bash
dotnet pack --configuration Release --output ./nupkgs
```

---

## Adding a New Contract Type

A contract type defines a new set of terms and a new scheduling/evaluation engine. The steps below use ANN (Annuity) as an example name.

### Step 1 — Define Contract Terms

Create a new class in `src/ActusInsurance.Core/Models/` implementing `IContractTerms`:

```csharp
// src/ActusInsurance.Core/Models/AnnContractTerms.cs
namespace ActusInsurance.Core.Models;

public sealed class AnnContractTerms : IContractTerms
{
    public string ContractID { get; set; } = string.Empty;
    public string ContractType => "ANN";
    public string Currency { get; set; } = string.Empty;
    public DateTime StatusDate { get; set; }
    public DateTime MaturityDate { get; set; }

    // Add ANN-specific fields here
    public double NotionalPrincipal { get; set; }
    public double NominalInterestRate { get; set; }
    // ...
}
```

### Step 2 — Implement the Contract Scheduler

Create the contract logic in `src/ActusInsurance.Core.CPU/Contracts/`:

```csharp
// src/ActusInsurance.Core.CPU/Contracts/ANN.cs
namespace ActusInsurance.Core.CPU.Contracts;

public static class Annuity
{
    public static List<ContractEvent> Schedule(DateTime to, AnnContractTerms model)
    {
        // Build event list using ScheduleFactory and business day adjuster
        var events = new List<ContractEvent>();
        // ...
        return events;
    }

    public static List<ContractEvent> Apply(
        List<ContractEvent> events,
        AnnContractTerms model,
        RiskFactorModel riskFactors)
    {
        // Initialise state space and evaluate each event
        // ...
        return events;
    }
}
```

Follow the same pattern as `PrincipalAtMaturity` in `src/ActusInsurance.Core.CPU/Contracts/PAM.cs`.

### Step 3 — Add Tests

Create a test class in `src/ActusInsurance.Tests.CPU/` and add a corresponding JSON test data file in `src/ActusInsurance.Tests.CPU/Resources/`.

See [Adding Tests](#adding-tests) below.

---

## Adding a Risk Factor

`RiskFactorModel` supports any market object code. No code change is required to add a new risk factor — callers simply call `AddRate` or `AddConstantRate` with the code they define.

If a contract type needs to look up a specific risk factor, it must store the `marketObjectCode` in its terms class and call `riskFactors.GetRate(marketObjectCode, time)` inside the event evaluation logic.

Example:

```csharp
// In contract terms
public string? MarketObjectCodeOfCreditSpread { get; set; }

// In event evaluation
if (!string.IsNullOrEmpty(model.MarketObjectCodeOfCreditSpread))
{
    double creditSpread = riskFactors.GetRate(model.MarketObjectCodeOfCreditSpread, Time);
    // use creditSpread
}
```

---

## Extending Calculation Logic

### Adding a New Event Type

1. Add the new value to the `EventType` enum in `src/ActusInsurance.Core/Types/Enums.cs`:

```csharp
public enum EventType
{
    // ... existing values ...
    MY_NEW_EVENT
}
```

2. Add a priority value in `ContractEvent.GetEventTypePriority()` in `src/ActusInsurance.Core/Events/ContractEvent.cs`.

3. Add payoff computation in `ComputePayoff()`:

```csharp
EventType.MY_NEW_EVENT => ComputeMyNewEventPayoff(states, model, riskFactors),
```

4. Add state transition in `UpdateState()`:

```csharp
case EventType.MY_NEW_EVENT:
    // mutate states as needed
    break;
```

5. In the contract's `Schedule()` method, generate events of this type from a cycle or at a specific date.

### Adding a New Day Count Convention

1. Create a class implementing `IDayCountConventionProvider` in `src/ActusInsurance.Core/Conventions/DayCount/`:

```csharp
public class MyNewConvention : IDayCountConventionProvider
{
    public double DayCountFraction(DateTime start, DateTime end)
    {
        // implement formula
    }
}
```

2. Add the enum value to `DayCountConvention` in `src/ActusInsurance.Core/Types/DayCountConvention.cs`.

3. Add the `case` in `DayCountCalculator`'s constructor (`src/ActusInsurance.Core/Conventions/DayCount/DayCountCalculator.cs`).

4. Add the string constant in `src/ActusInsurance.Core/Util/StringUtils.cs`.

5. Add the mapping in `ConvertDayCountToString()` in `ContractEvent.cs`.

6. Add the parsing case in `PamContractTerms.ParseDayCountConvention()`.

### Adding a New Business Day Convention

1. Implement `IBusinessDayConvention` in `src/ActusInsurance.Core/Conventions/BusinessDay/`.
2. Add the enum value to `BusinessDayConventionEnum`.
3. Add the case in `BusinessDayAdjuster`'s constructor.
4. Add the parsing case in `PamContractTerms.ParseBusinessDayConvention()`.

---

## Adding Tests

### Creating a New Test Class

Follow the pattern in `src/ActusInsurance.Tests.CPU/PamTests.cs`:

```csharp
[TestFixture]
public class MyContractTest
{
    [TestCaseSource(nameof(GetTestCases))]
    public void TestMyContract(string testId, TestData testData)
    {
        var terms = MyContractTerms.FromDictionary(testData.Terms);
        var riskFactors = new RiskFactorModel();
        // populate riskFactors from testData.DataObserved ...

        var schedule = MyContract.Schedule(terms.MaturityDate, terms);
        schedule = MyContract.Apply(schedule, terms, riskFactors);

        // compare schedule to testData.Results ...
    }

    public static IEnumerable<TestCaseData> GetTestCases()
    {
        // load from a JSON file in Resources/
        var tests = TestDataLoader.ReadTests("path/to/my-tests.json");
        foreach (var kvp in tests)
        {
            yield return new TestCaseData(kvp.Key, kvp.Value).SetName($"MY_Test_{kvp.Key}");
        }
    }
}
```

### Test Data File Format

See [Testing — Test Data Format](./testing.md) for the JSON schema.

### Numerical Tolerance

Use `2e-10` as the double comparison tolerance, consistent with the existing test suite:

```csharp
if (Math.Abs(expected - computed) <= 2e-10)
    // pass
```

---

## Project Structure Reference

See [Reference — Folder Map](./reference.md) for the complete folder tree.

---

## Common Issues

| Issue | Solution |
|---|---|
| `dotnet restore` fails | Ensure .NET 9 SDK is installed |
| Tests not found | Ensure the test project targets `net9.0` and references NUnit |
| `actus-tests-pam.json` missing | File must be present in `src/ActusInsurance.Tests.CPU/Resources/` and marked as `CopyToOutputDirectory` |
| Wrong payoff for rate-reset contract | Verify `MarketObjectCodeOfRateReset` matches a key in `RiskFactorModel` |
| All rates return 0 | Check that `AddRate` or `AddConstantRate` was called with the correct market object code |

## Evidence from Code

- `src/ActusInsurance.Core.CPU/Contracts/PAM.cs`
- `src/ActusInsurance.Core/Contracts/IContractScheduler.cs`
- `src/ActusInsurance.Core/Models/IContractTerms.cs`
- `src/ActusInsurance.Core/Events/ContractEvent.cs`
- `src/ActusInsurance.Core/Conventions/DayCount/DayCountCalculator.cs`
- `src/ActusInsurance.Core/Conventions/BusinessDay/BusinessDayAdjuster.cs`
- `src/ActusInsurance.Core/Types/Enums.cs`
- `src/ActusInsurance.Tests.CPU/PamTests.cs`

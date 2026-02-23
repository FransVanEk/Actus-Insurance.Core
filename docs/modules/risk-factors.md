# Module: Risk Factors

This document explains what risk factors are, how they are supplied, and how the engine uses them during contract evaluation.

Related: [Calculation Engine](./calculation-engine.md) | [Contracts](./contracts.md) | [Domain Model](../domain-model.md)

---

## Business View

### What Are Risk Factors?

A **risk factor** is a piece of external market data that affects how a contract behaves. Common examples:

- A **benchmark interest rate** (e.g. EURIBOR, SOFR) used to reset the interest rate on a variable-rate loan
- A **price index** (e.g. a consumer price index) used to scale the outstanding principal on an inflation-linked bond

Risk factors are called "external" because they come from outside the contract itself — they are observed in the market, not determined by the contract's terms.

### Why Do They Matter?

Without risk factors, all variable-rate contracts would be fixed. Any loan or bond whose interest rate changes over time must consult an external rate at each reset date. Providing incorrect or missing rate data produces incorrect cash-flow projections.

### Who Supplies Risk Factors?

The **caller** — the application or service that uses this library — is responsible for constructing a `RiskFactorModel` and populating it with the rates it needs. The library does not fetch rates from any external source.

### Risks If Risk Factors Are Missing or Wrong

- Missing rate → the engine returns `0.0` for that rate, which effectively zeroes out the variable component of the interest rate
- Wrong rate → incorrect payoff at each reset date; downstream position and valuation data is wrong

---

## Technical View

### RiskFactorModel

```csharp
// src/ActusInsurance.Core/Externals/RiskFactorModel.cs
public sealed class RiskFactorModel
{
    private readonly Dictionary<string, Dictionary<DateTime, double>> _rates = new();
    private readonly Dictionary<string, double> _constantRates = new();

    public void AddRate(string marketObjectCode, DateTime time, double value);
    public void AddConstantRate(string marketObjectCode, double value);
    public double GetRate(string marketObjectCode, DateTime time);
}
```

### Adding Rates

**Constant rate** — a single value that applies at all times:

```csharp
var rf = new RiskFactorModel();
rf.AddConstantRate("EURIBOR_3M", 0.0350);  // 3.50%
```

**Time-series rate** — a value that varies by date:

```csharp
rf.AddRate("EURIBOR_3M", new DateTime(2024, 1, 1), 0.0340);
rf.AddRate("EURIBOR_3M", new DateTime(2024, 4, 1), 0.0360);
```

### Rate Lookup Algorithm

`GetRate(marketObjectCode, time)`:

1. Check `_constantRates` — if found, return immediately.
2. Check `_rates[marketObjectCode]` for an exact date match.
3. If no exact match, find the most recent date `<= time` (a "previous-value" interpolation).
4. If no date at or before `time` exists, return `0.0`.

```
flowchart TD
    A[Look up constant rate]
    B[Found constant]
    C[Look up time series]
    D[Exact date match]
    E[Find most recent previous date]
    F[No prior date exists]
    G[Return value]
    H[Return 0.0]
    A --> B
    B --> G
    A --> C
    C --> D
    D --> G
    C --> E
    E --> G
    E --> F
    F --> H
```

**Legend:**
- Steps 1–2 are attempted in order.
- "Previous-value" means the last known rate before the query date is used.
- `0.0` is the hard-coded fallback when no data is available.

### How Risk Factors Are Applied

Risk factors are consulted in two places in `ContractEvent.cs`:

**Rate Reset (RR event):**
```csharp
double marketRate = riskFactors.GetRate(model.MarketObjectCodeOfRateReset, Time);
double newRate = marketRate * model.RateMultiplier + model.RateSpread;
```
The `MarketObjectCodeOfRateReset` field on the contract terms identifies which risk factor to look up.

**Scaling (SC event):**
```csharp
double scalingIndex = riskFactors.GetRate(model.MarketObjectCodeOfScalingIndex, Time);
double scalingFactor = scalingIndex / model.ScalingIndexAtContractDealDate;
```
The `MarketObjectCodeOfScalingIndex` field identifies the scaling index.

### Safety Checks and Fallback

| Situation | Behaviour |
|---|---|
| `MarketObjectCodeOfRateReset` is null or empty | Rate reset event fires but rate is not changed (no lookup performed) |
| Rate not found at exact date | Previous-value interpolation used |
| No rate data at all for the code | Returns `0.0` |
| `ScalingIndexAtContractDealDate == 0` | Division by zero in scaling factor — caller must ensure this is non-zero |

> **Note:** The library does not validate that required risk factors are present before processing. A missing rate silently returns `0.0`, which may produce unexpected but non-exceptional results.

### Limits

There is no enforced limit on the number of market object codes or time-series points that can be loaded into `RiskFactorModel`. Memory is bounded only by available heap.

### How Tests Supply Risk Factors

The test suite reads `dataObserved` from the ACTUS JSON test file:

```csharp
// src/ActusInsurance.Tests.CPU/PamTests.cs
foreach (var dataset in testData.DataObserved.Values)
{
    if (dataset.Data != null)
    {
        foreach (var kvp in dataset.Data)
        {
            riskFactors.AddRate(dataset.MarketObjectCode, kvp.Key, kvp.Value);
        }
    }
}
```

The JSON format used:

```json
"dataObserved": {
  "YCSWAPEUR": {
    "data": [
      { "timestamp": "2015-01-02T00:00:00", "value": "0.02" }
    ]
  }
}
```

### Evidence from Code

- `src/ActusInsurance.Core/Externals/RiskFactorModel.cs`
- `src/ActusInsurance.Core/Events/ContractEvent.cs` — `UpdateRateReset()`, `UpdateScaling()`
- `src/ActusInsurance.Core/Models/PamContractTerms.cs` — `MarketObjectCodeOfRateReset`, `MarketObjectCodeOfScalingIndex`
- `src/ActusInsurance.Tests.CPU/PamTests.cs` — test data loading
- `src/ActusInsurance.Tests.CPU/Resources/actus-tests-pam.json` — reference test data

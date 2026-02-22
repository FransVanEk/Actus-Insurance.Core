namespace ActusInsurance.Core.Externals;

public sealed class RiskFactorModel
{
    private readonly Dictionary<string, Dictionary<DateTime, double>> _rates = new();
    private readonly Dictionary<string, double> _constantRates = new();

    public void AddRate(string marketObjectCode, DateTime time, double value)
    {
        if (!_rates.ContainsKey(marketObjectCode))
        {
            _rates[marketObjectCode] = new Dictionary<DateTime, double>();
        }
        _rates[marketObjectCode][time] = value;
    }

    public void AddConstantRate(string marketObjectCode, double value)
    {
        _constantRates[marketObjectCode] = value;
    }

    public double GetRate(string marketObjectCode, DateTime time)
    {
        // Try constant rate first (most common case)
        if (_constantRates.TryGetValue(marketObjectCode, out double constantRate))
        {
            return constantRate;
        }

        // Try time-specific rate
        if (_rates.TryGetValue(marketObjectCode, out var timeSeriesData))
        {
            if (timeSeriesData.TryGetValue(time, out double value))
            {
                return value;
            }

            // Find closest previous rate
            DateTime? closestDate = null;
            foreach (var kvp in timeSeriesData)
            {
                if (kvp.Key <= time && (!closestDate.HasValue || kvp.Key > closestDate.Value))
                {
                    closestDate = kvp.Key;
                }
            }

            if (closestDate.HasValue)
            {
                return timeSeriesData[closestDate.Value];
            }
        }

        return 0.0;
    }
}

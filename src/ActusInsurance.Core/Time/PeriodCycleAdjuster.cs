using ActusInsurance.Core.Util;

namespace ActusInsurance.Core.Time;

public sealed class PeriodCycleAdjuster : ICycleAdjusterProvider
{
    private readonly TimeSpan period;
    private readonly char stub;

    public PeriodCycleAdjuster(string cycle)
    {
        this.period = CycleUtils.ParsePeriodAsTimeSpan(cycle);
        this.stub = CycleUtils.ParseStub(cycle);
    }

    public DateTime PlusCycle(DateTime time)
    {
        return time.Add(period);
    }

    public DateTime MinusCycle(DateTime time)
    {
        return time.Subtract(period);
    }
}
using ActusInsurance.Core.Util;

namespace ActusInsurance.Core.Time;

public class CycleAdjuster
{
    private readonly ICycleAdjusterProvider adjuster;

    public CycleAdjuster(string cycle)
    {
        if (CycleUtils.IsPeriod(cycle))
        {
            adjuster = new PeriodCycleAdjuster(cycle);
        }
        else
        {
            adjuster = new WeekdayCycleAdjuster(cycle);
        }
    }

    public DateTime PlusCycle(DateTime time)
    {
        return adjuster.PlusCycle(time);
    }

    public DateTime MinusCycle(DateTime time)
    {
        return adjuster.MinusCycle(time);
    }
}
namespace ActusInsurance.Core.Conventions.DayCount;

public class ActualThreeThirtySix : IDayCountConventionProvider
{
    public double DayCount(DateTime startTime, DateTime endTime)
    {
        return (endTime - startTime).TotalDays;
    }

    public double DayCountFraction(DateTime startTime, DateTime endTime)
    {
        return DayCount(startTime, endTime) / 336.0;
    }
}
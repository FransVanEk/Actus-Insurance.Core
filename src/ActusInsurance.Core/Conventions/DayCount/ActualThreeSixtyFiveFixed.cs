namespace ActusInsurance.Core.Conventions.DayCount;

public class ActualThreeSixtyFiveFixed : IDayCountConventionProvider
{
    public double DayCount(DateTime startTime, DateTime endTime)
    {
        return (endTime - startTime).TotalDays;
    }

    public double DayCountFraction(DateTime startTime, DateTime endTime)
    {
        return DayCount(startTime, endTime) / 365.0;
    }
}
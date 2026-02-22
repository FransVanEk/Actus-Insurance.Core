namespace ActusInsurance.Core.Conventions.DayCount;

public class ThirtyEThreeSixty : IDayCountConventionProvider
{
    public double DayCount(DateTime startTime, DateTime endTime)
    {
        double d1 = (startTime.Day == 31) ? 30.0 : startTime.Day;
        double d2 = (endTime.Day == 31) ? 30.0 : endTime.Day;

        double delD = d2 - d1;
        double delM = endTime.Month - startTime.Month;
        double delY = endTime.Year - startTime.Year;

        return (360.0 * delY + 30.0 * delM + delD);
    }

    public double DayCountFraction(DateTime startTime, DateTime endTime)
    {
        return DayCount(startTime, endTime) / 360.0;
    }
}
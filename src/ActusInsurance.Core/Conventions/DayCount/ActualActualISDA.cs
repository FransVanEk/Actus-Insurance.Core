namespace ActusInsurance.Core.Conventions.DayCount;

public sealed class ActualActualISDA : IDayCountConventionProvider
{
    public double DayCount(DateTime startTime, DateTime endTime)
    {
        return (endTime - startTime).TotalDays;
    }

    public double DayCountFraction(DateTime startTime, DateTime endTime)
    {
        int y1 = startTime.Year;
        int y2 = endTime.Year;

        if (y1 == y2)
        {
            double basis = DateTime.IsLeapYear(y1) ? 366.0 : 365.0;
            return (endTime - startTime).TotalDays / basis;
        }

        double firstBasis = DateTime.IsLeapYear(y1) ? 366.0 : 365.0;
        double secondBasis = DateTime.IsLeapYear(y2) ? 366.0 : 365.0;
        
        var startOfNextYear = new DateTime(y1 + 1, 1, 1, 0, 0, 0);
        var startOfEndYear = new DateTime(y2, 1, 1, 0, 0, 0);
        
        return ((startOfNextYear - startTime).TotalDays / firstBasis) +
               ((endTime - startOfEndYear).TotalDays / secondBasis) +
               (y2 - y1 - 1);
    }
}
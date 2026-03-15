namespace ActusInsurance.Core.Conventions.DayCount;

public class ThirtyEThreeSixtyISDA : IDayCountConventionProvider
{
    private DateTime maturityDate;

    public void SetMaturityDate(DateTime maturityDate)
    {
        this.maturityDate = maturityDate;
    }

    public double DayCount(DateTime startTime, DateTime endTime)
    {
        int d1 = startTime.Day;
        // Check if it's last day of month
        d1 = (d1 == DateTime.DaysInMonth(startTime.Year, startTime.Month)) ? 30 : d1;

        int d2 = endTime.Day;
        // Check if it's last day of month, but not if it's maturity date in February
        d2 = (!(endTime == maturityDate && endTime.Month == 2) && 
              d2 == DateTime.DaysInMonth(endTime.Year, endTime.Month)) ? 30 : d2;

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
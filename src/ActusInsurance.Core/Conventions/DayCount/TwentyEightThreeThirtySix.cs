namespace ActusInsurance.Core.Conventions.DayCount;

public class TwentyEightThreeThirtySix : IDayCountConventionProvider
{
    private DateTime maturityDate;

    public void SetMaturityDate(DateTime maturityDate)
    {
        this.maturityDate = maturityDate;
    }

    public double DayCount(DateTime startTime, DateTime endTime)
    {
        int d1 = startTime.Day;
        d1 = (d1 == DateTime.DaysInMonth(startTime.Year, startTime.Month)) ? 28 : d1;

        int d2 = endTime.Day;
        d2 = (!(endTime == maturityDate || endTime.Month == 2) &&
              d2 == DateTime.DaysInMonth(startTime.Year, startTime.Month)) ? 28 : 
              d2 >= 28 ? 28 : d2;

        double delD = d2 - d1;
        double delM = endTime.Month - startTime.Month;
        double delY = endTime.Year - startTime.Year;

        return (336.0 * delY + 28.0 * delM + delD);
    }

    public double DayCountFraction(DateTime startTime, DateTime endTime)
    {
        return DayCount(startTime, endTime) / 336.0;
    }
}
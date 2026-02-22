namespace ActusInsurance.Core.Conventions.EndOfMonth;

public class EndOfMonth : IEndOfMonthConvention
{
    public DateTime Shift(DateTime date)
    {
        int lastDayOfMonth = DateTime.DaysInMonth(date.Year, date.Month);
        return new DateTime(date.Year, date.Month, lastDayOfMonth, date.Hour, date.Minute, date.Second, date.Millisecond);
    }
}
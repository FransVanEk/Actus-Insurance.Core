namespace ActusInsurance.Core.Conventions.EndOfMonth;

public class SameDay : IEndOfMonthConvention
{
    public DateTime Shift(DateTime date)
    {
        return date;
    }
}
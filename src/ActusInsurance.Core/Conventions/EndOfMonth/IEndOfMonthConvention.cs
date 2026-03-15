namespace ActusInsurance.Core.Conventions.EndOfMonth;

public interface IEndOfMonthConvention
{
    DateTime Shift(DateTime date);
}
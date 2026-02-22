namespace ActusInsurance.Core.Conventions.BusinessDay;

public interface IBusinessDayConvention
{
    DateTime Shift(DateTime date);
}
namespace ActusInsurance.Core.Conventions.BusinessDay;

public interface IShiftCalcConvention
{
    DateTime Shift(DateTime time, IBusinessDayConvention businessDayConvention);
}
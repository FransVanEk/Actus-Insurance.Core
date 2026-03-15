namespace ActusInsurance.Core.Conventions.BusinessDay;

public sealed class ShiftCalc : IShiftCalcConvention
{
    public DateTime Shift(DateTime time, IBusinessDayConvention businessDayConvention)
    {
        return businessDayConvention.Shift(time);
    }
}

public sealed class CalcShift : IShiftCalcConvention
{
    public DateTime Shift(DateTime time, IBusinessDayConvention businessDayConvention)
    {
        // For CalcShift convention, calculations use the original time
        // The actual shifting happens separately for event timing
        return time;
    }
}

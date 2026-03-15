using ActusInsurance.Core.Time.Calendar;
using ActusInsurance.Core.Types;
using ActusInsurance.Core.Util;

namespace ActusInsurance.Core.Conventions.BusinessDay;

public sealed class BusinessDayAdjuster
{
    private IBusinessDayConvention bdConvention;
    private IShiftCalcConvention scConvention;

    public BusinessDayAdjuster(BusinessDayConventionEnum convention, BusinessDayCalendarProvider calendar)
    {
        if (CommonUtils.IsNull(convention) || convention.Equals(BusinessDayConventionEnum.NOS))
        {
            this.bdConvention = new Same();
            this.scConvention = new ShiftCalc();
        }
        else
        {
            switch (convention)
            {
                case BusinessDayConventionEnum.CSF:
                    scConvention = new CalcShift();
                    bdConvention = new Following(calendar);
                    break;
                case BusinessDayConventionEnum.CSMF:
                    scConvention = new CalcShift();
                    bdConvention = new ModifiedFollowing(calendar);
                    break;
                case BusinessDayConventionEnum.CSP:
                    scConvention = new CalcShift();
                    bdConvention = new Preceeding(calendar);
                    break;
                case BusinessDayConventionEnum.CSMP:
                    scConvention = new CalcShift();
                    bdConvention = new ModifiedPreceeding(calendar);
                    break;
                case BusinessDayConventionEnum.SCF:
                    scConvention = new ShiftCalc();
                    bdConvention = new Following(calendar);
                    break;
                case BusinessDayConventionEnum.SCMF:
                    scConvention = new ShiftCalc();
                    bdConvention = new ModifiedFollowing(calendar);
                    break;
                case BusinessDayConventionEnum.SCP:
                    scConvention = new ShiftCalc();
                    bdConvention = new Preceeding(calendar);
                    break;
                case BusinessDayConventionEnum.SCMP:
                    scConvention = new ShiftCalc();
                    bdConvention = new ModifiedPreceeding(calendar);
                    break;
                default:
                    throw new AttributeConversionException();
            }
        }
    }

    public DateTime ShiftEventTime(DateTime time)
    {
        return bdConvention.Shift(time);
    }

    public DateTime ShiftCalcTime(DateTime time)
    {
        return scConvention.Shift(time, bdConvention);
    }
}

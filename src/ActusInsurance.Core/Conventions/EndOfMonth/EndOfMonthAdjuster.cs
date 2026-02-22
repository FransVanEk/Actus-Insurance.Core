using ActusInsurance.Core.Types;
using ActusInsurance.Core.Util;

namespace ActusInsurance.Core.Conventions.EndOfMonth;

public sealed class EndOfMonthAdjuster
{
    private readonly IEndOfMonthConvention convention;

    public EndOfMonthAdjuster(EndOfMonthConventionEnum conventionEnum, DateTime refDate, string cycle)
    {
        if (CommonUtils.IsNull(conventionEnum))
        {
            throw new AttributeConversionException("EndOfMonthConventionEnum cannot be null");
        }

        switch (conventionEnum)
        {
            case EndOfMonthConventionEnum.EOM:
                // note, internally, units which are a multiple of "1M" are converted to "XM" why here we only have to check
                // for period-unit M when deciding whether or not to shift a date
                if (IsLastDayOfMonth(refDate) && CycleUtils.ParsePeriod(cycle).GetMonths() > 0)
                {
                    this.convention = new EndOfMonth();
                }
                else
                {
                    this.convention = new SameDay();
                }
                break;
            case EndOfMonthConventionEnum.SD:
                this.convention = new SameDay();
                break;
            default:
                throw new AttributeConversionException($"Unknown EndOfMonthConventionEnum: {conventionEnum}");
        }
    }

    public DateTime Shift(DateTime date)
    {
        return convention.Shift(date);
    }

    private static bool IsLastDayOfMonth(DateTime date)
    {
        int lastDayOfMonth = DateTime.DaysInMonth(date.Year, date.Month);
        return date.Day == lastDayOfMonth;
    }
}
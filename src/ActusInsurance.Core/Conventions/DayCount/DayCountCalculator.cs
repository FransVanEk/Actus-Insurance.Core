using ActusInsurance.Core.Time;
using ActusInsurance.Core.Time.Calendar;
using ActusInsurance.Core.Util;

namespace ActusInsurance.Core.Conventions.DayCount;

public class DayCountCalculator
{
    private readonly IDayCountConventionProvider convention;

    public DayCountCalculator(IDayCountConventionProvider convention)
    {
        this.convention = convention;
    }

    public DayCountCalculator(string conventionString, BusinessDayCalendarProvider calendar)
    {
        switch (conventionString)
        {
            case StringUtils.DayCountConvention_30E360:
                this.convention = new ThirtyEThreeSixty();
                break;
            case StringUtils.DayCountConvention_30E360ISDA:
                this.convention = new ThirtyEThreeSixtyISDA();
                break;
            case StringUtils.DayCountConvention_A360:
                this.convention = new ActualThreeSixty();
                break;
            case StringUtils.DayCountConvention_A365:
                this.convention = new ActualThreeSixtyFiveFixed();
                break;
            case StringUtils.DayCountConvention_AAISDA:
                this.convention = new ActualActualISDA();
                break;
            case StringUtils.DayCountConvention_B252:
                var businessConvention = new BusinessTwoFiftyTwo();
                businessConvention.SetCalendar(calendar);
                this.convention = businessConvention;
                break;
            case StringUtils.DayCountConvention_A336:
                this.convention = new ActualThreeThirtySix();
                break;
            case StringUtils.DayCountConvention_28336:
                this.convention = new TwentyEightThreeThirtySix();
                break;
            default:
                throw new ArgumentException($"Unknown day count convention: {conventionString}");
        }
    }

    public double DayCountFraction(DateTime startTime, DateTime endTime)
    {
        return convention.DayCountFraction(
            TimeAdjuster.ToFullHours(startTime), 
            TimeAdjuster.ToFullHours(endTime));
    }
}
using ActusInsurance.Core.Time.Calendar;

namespace ActusInsurance.Core.Conventions.DayCount;

public sealed class BusinessTwoFiftyTwo : IDayCountConventionProvider
{
    private BusinessDayCalendarProvider calendar;

    public void SetCalendar(BusinessDayCalendarProvider calendar)
    {
        this.calendar = calendar;
    }

    public double DayCount(DateTime startTime, DateTime endTime)
    {
        if (calendar == null)
            throw new InvalidOperationException("Calendar must be set before using BusinessTwoFiftyTwo day count convention");

        DateTime date = startTime;
        int daysCount = 0;
        int totalDays = (int)(endTime - startTime).TotalDays;

        for (int i = 0; i < totalDays; i++)
        {
            if (calendar.IsBusinessDay(date))
            {
                daysCount++;
            }
            date = date.AddDays(1);
        }
        return daysCount;
    }

    public double DayCountFraction(DateTime startTime, DateTime endTime)
    {
        return DayCount(startTime, endTime) / 252.0;
    }
}
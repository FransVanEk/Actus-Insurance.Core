namespace ActusInsurance.Core.Time.Calendar;

public class BusinessDayCalendarProvider
{
    public virtual bool IsBusinessDay(DateTime date)
    {
        return date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday;
    }
}
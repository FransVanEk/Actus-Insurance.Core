namespace ActusInsurance.Core.Time.Calendar;

public class NoHolidaysCalendar : BusinessDayCalendarProvider
{
    public override bool IsBusinessDay(DateTime date)
    {
        return true;
    }
}
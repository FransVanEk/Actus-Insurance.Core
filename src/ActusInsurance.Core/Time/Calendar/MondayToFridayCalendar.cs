namespace ActusInsurance.Core.Time.Calendar;

public class MondayToFridayCalendar : BusinessDayCalendarProvider
{
    public override bool IsBusinessDay(DateTime date)
    {
        return date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday;
    }
}
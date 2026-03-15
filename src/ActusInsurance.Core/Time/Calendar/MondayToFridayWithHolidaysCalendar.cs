namespace ActusInsurance.Core.Time.Calendar;

public class MondayToFridayWithHolidaysCalendar : BusinessDayCalendarProvider
{
    private readonly HashSet<DateTime> holidays;

    public MondayToFridayWithHolidaysCalendar(HashSet<DateTime> holidays)
    {
        this.holidays = holidays ?? new HashSet<DateTime>();
    }

    public override bool IsBusinessDay(DateTime dateTime)
    {
        var date = dateTime.Date; // Normalize to date only
        var dayOfWeek = (int)date.DayOfWeek; // 0 = Sunday, 1 = Monday, ..., 6 = Saturday
        
        // Check if it's Monday to Friday (1-5) and not a holiday
        return dayOfWeek >= 1 && dayOfWeek <= 5 && !holidays.Contains(date);
    }
}
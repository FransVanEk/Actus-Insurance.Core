using ActusInsurance.Core.Util;

namespace ActusInsurance.Core.Time;

public sealed class WeekdayCycleAdjuster : ICycleAdjusterProvider
{
    private readonly DayOfWeek weekday;
    private readonly int position;
    private readonly char stub;

    public WeekdayCycleAdjuster(string cycle)
    {
        this.weekday = CycleUtils.ParseWeekday(cycle);
        this.position = CycleUtils.ParsePosition(cycle);
        this.stub = CycleUtils.ParseStub(cycle);
    }

    public DateTime PlusCycle(DateTime time)
    {
        return GetDayOfWeekInMonth(time.AddMonths(1), position, weekday);
    }

    public DateTime MinusCycle(DateTime time)
    {
        return GetDayOfWeekInMonth(time.AddMonths(-1), position, weekday);
    }

    private static DateTime GetDayOfWeekInMonth(DateTime dateTime, int occurrence, DayOfWeek dayOfWeek)
    {
        var firstDayOfMonth = new DateTime(dateTime.Year, dateTime.Month, 1);
        var firstOccurrence = firstDayOfMonth;
        
        // Find the first occurrence of the day of week
        while (firstOccurrence.DayOfWeek != dayOfWeek)
        {
            firstOccurrence = firstOccurrence.AddDays(1);
        }
        
        // Add weeks to get to the nth occurrence
        var result = firstOccurrence.AddDays((occurrence - 1) * 7);
        
        // Check if the result is still in the same month
        if (result.Month != dateTime.Month)
        {
            // If we've gone past the month, get the last occurrence instead
            result = firstOccurrence.AddDays((occurrence - 2) * 7);
        }
        
        return new DateTime(result.Year, result.Month, result.Day, dateTime.Hour, dateTime.Minute, dateTime.Second);
    }
}
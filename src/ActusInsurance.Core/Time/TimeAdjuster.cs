namespace ActusInsurance.Core.Time;

public static class TimeAdjuster
{
    public static DateTime ToFullHours(DateTime dateTime)
    {
        if (dateTime.Minute < 30)
        {
            // Floor to current hour
            return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, 0, 0);
        }
        else
        {
            // Ceil to next hour
            return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, 0, 0).AddHours(1);
        }
    }
}


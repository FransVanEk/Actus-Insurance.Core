using ActusInsurance.Core.Time.Calendar;

namespace ActusInsurance.Core.Conventions.BusinessDay;

public class Same : IBusinessDayConvention
{
    public Same()
    {
    }

    public DateTime Shift(DateTime date)
    {
        return date;
    }
}

public sealed class Following : IBusinessDayConvention
{
    private readonly BusinessDayCalendarProvider calendar;

    public Following(BusinessDayCalendarProvider calendar)
    {
        this.calendar = calendar;
    }

    public DateTime Shift(DateTime date)
    {
        DateTime shiftedDate = date;
        while (!calendar.IsBusinessDay(shiftedDate))
        {
            shiftedDate = shiftedDate.AddDays(1);
        }
        return shiftedDate;
    }
}

public class ModifiedFollowing : IBusinessDayConvention
{
    private readonly BusinessDayCalendarProvider calendar;

    public ModifiedFollowing(BusinessDayCalendarProvider calendar)
    {
        this.calendar = calendar;
    }

    public DateTime Shift(DateTime date)
    {
        DateTime shiftedDate = date;
        while (!calendar.IsBusinessDay(shiftedDate))
        {
            shiftedDate = shiftedDate.AddDays(1);
        }
        if (shiftedDate.Month != date.Month)
        {
            shiftedDate = date;
            while (!calendar.IsBusinessDay(shiftedDate))
            {
                shiftedDate = shiftedDate.AddDays(-1);
            }
        }
        return shiftedDate;
    }
}

public sealed class Preceeding : IBusinessDayConvention
{
    private readonly BusinessDayCalendarProvider calendar;

    public Preceeding(BusinessDayCalendarProvider calendar)
    {
        this.calendar = calendar;
    }

    public DateTime Shift(DateTime date)
    {
        DateTime shiftedDate = date;
        while (!calendar.IsBusinessDay(shiftedDate))
        {
            shiftedDate = shiftedDate.AddDays(-1);
        }
        return shiftedDate;
    }
}

public class ModifiedPreceeding : IBusinessDayConvention
{
    private readonly BusinessDayCalendarProvider calendar;

    public ModifiedPreceeding(BusinessDayCalendarProvider calendar)
    {
        this.calendar = calendar;
    }

    public DateTime Shift(DateTime date)
    {
        DateTime shiftedDate = date;
        while (!calendar.IsBusinessDay(shiftedDate))
        {
            shiftedDate = shiftedDate.AddDays(-1);
        }
        if (shiftedDate.Month != date.Month)
        {
            shiftedDate = date;
            while (!calendar.IsBusinessDay(shiftedDate))
            {
                shiftedDate = shiftedDate.AddDays(1);
            }
        }
        return shiftedDate;
    }
}

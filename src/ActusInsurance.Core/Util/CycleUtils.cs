using System.Text.RegularExpressions;

namespace ActusInsurance.Core.Util;

public static class CycleUtils
{
    public static bool IsPeriod(string cycle)
    {
        return !string.IsNullOrEmpty(cycle) && cycle[0] == 'P';
    }

    public static Period ParsePeriod(string cycle, bool stub)
    {
        return ParsePeriod(cycle);
    }

    public static Period ParsePeriod(string cycle)
    {
        if (string.IsNullOrWhiteSpace(cycle))
            return new Period();

        try
        {
            // Split by 'L' to handle stub information
            string periodPart = cycle.Split('L')[0];
            
            // Parse ISO 8601 period format or custom format
            if (periodPart.StartsWith("P"))
            {
                return ParseIsoPeriod(periodPart);
            }
            else
            {
                // Handle custom format like "1M", "3M", "1Y", etc.
                var match = Regex.Match(periodPart.ToUpper(), @"^(\d+)([DMQHY])$");
                if (!match.Success)
                    throw new ArgumentException($"Invalid cycle format: {cycle}");

                int amount = int.Parse(match.Groups[1].Value);
                string unit = match.Groups[2].Value;

                return unit switch
                {
                    "D" => new Period { Days = amount },
                    "M" => new Period { Months = amount },
                    "Q" => new Period { Months = amount * 3 },
                    "H" => new Period { Months = amount * 6 },
                    "Y" => new Period { Years = amount },
                    _ => throw new ArgumentException($"Unknown period unit: {unit}")
                };
            }
        }
        catch (Exception e)
        {
            throw new AttributeConversionException("Failed to parse period from cycle", e);
        }
    }

    public static int ParsePosition(string cycle)
    {
        try
        {
            return int.Parse(cycle[0].ToString());
        }
        catch (Exception e)
        {
            throw new AttributeConversionException("Failed to parse position from cycle", e);
        }
    }

    public static DayOfWeek ParseWeekday(string cycle)
    {
        try
        {
            string weekdayPart = cycle.Split('L')[0].Substring(1);
            
            // Parse weekday abbreviation (e.g., "Mon", "Tue", etc.)
            return weekdayPart.ToUpper() switch
            {
                "MON" => DayOfWeek.Monday,
                "TUE" => DayOfWeek.Tuesday,
                "WED" => DayOfWeek.Wednesday,
                "THU" => DayOfWeek.Thursday,
                "FRI" => DayOfWeek.Friday,
                "SAT" => DayOfWeek.Saturday,
                "SUN" => DayOfWeek.Sunday,
                _ => throw new ArgumentException($"Unknown weekday: {weekdayPart}")
            };
        }
        catch (Exception e)
        {
            throw new AttributeConversionException("Failed to parse weekday from cycle", e);
        }
    }

    public static char ParseStub(string cycle)
    {
        try
        {
            string[] parts = cycle.Split('L');
            if (parts.Length < 2)
                return StringUtils.ShortStub; // Default to short stub
            
            char stub = parts[1][0];
            if (stub != StringUtils.LongStub && stub != StringUtils.ShortStub)
            {
                throw new AttributeConversionException("Invalid stub character");
            }
            return stub;
        }
        catch (Exception e)
        {
            throw new AttributeConversionException("Failed to parse stub from cycle", e);
        }
    }

    public static TimeSpan ParsePeriodAsTimeSpan(string cycle)
    {
        var period = ParsePeriod(cycle);
        // Approximate conversion to TimeSpan
        int totalDays = period.Days + (period.Months * 30) + (period.Years * 365);
        return TimeSpan.FromDays(totalDays);
    }

    private static Period ParseIsoPeriod(string isoPeriod)
    {
        // Basic ISO 8601 period parsing (simplified)
        var match = Regex.Match(isoPeriod, @"^P(?:(\d+)Y)?(?:(\d+)M)?(?:(\d+)D)?$");
        if (!match.Success)
            throw new ArgumentException($"Invalid ISO period format: {isoPeriod}");

        int years = match.Groups[1].Success ? int.Parse(match.Groups[1].Value) : 0;
        int months = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 0;
        int days = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : 0;

        return new Period { Years = years, Months = months, Days = days };
    }
}

public class Period
{
    public int Days { get; set; }
    public int Months { get; set; }
    public int Years { get; set; }

    public Period()
    {
        Days = 0;
        Months = 0;
        Years = 0;
    }

    public int GetMonths()
    {
        return Months + (Years * 12);
    }
}
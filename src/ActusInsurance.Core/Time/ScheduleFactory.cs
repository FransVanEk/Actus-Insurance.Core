using ActusInsurance.Core.Conventions.EndOfMonth;
using ActusInsurance.Core.Types;
using ActusInsurance.Core.Util;

namespace ActusInsurance.Core.Time;

public static class ScheduleFactory
{
    public static IEnumerable<DateTime> CreateSchedule(DateTime startTime,
                                                     DateTime endTime,
                                                     string cycle,
                                                     bool endOfMonthConvention,
                                                     bool includeEndDate)
    {
        return CreateSchedule(startTime, endTime, cycle, 
            endOfMonthConvention ? EndOfMonthConventionEnum.EOM : EndOfMonthConventionEnum.SD, 
            includeEndDate);
    }

    public static IEnumerable<DateTime> CreateSchedule(DateTime startTime,
                                                     DateTime endTime,
                                                     string cycle,
                                                     bool endOfMonthConvention)
    {
        return CreateSchedule(startTime, endTime, cycle, endOfMonthConvention, true);
    }

    public static IEnumerable<DateTime> CreateSchedule(DateTime startTime, 
                                                      DateTime endTime, 
                                                      string cycle, 
                                                      EndOfMonthConventionEnum endOfMonthConvention, 
                                                      bool addEndTime)
    {
        var timesSet = new HashSet<DateTime>();

        // if no cycle then only start (if specified) and end dates
        if (CommonUtils.IsNull(cycle))
        {
            timesSet.Add(startTime);
            
            // add or not additional time at endTime
            if (addEndTime)
            {
                timesSet.Add(endTime);
            }
            else
            {
                if (endTime.Equals(startTime)) 
                    timesSet.Remove(startTime);
            }
            return timesSet;
        }

        // parse stub
        char stub = CycleUtils.ParseStub(cycle);

        // parse end of month convention
        var shifter = new EndOfMonthAdjuster(endOfMonthConvention, startTime, cycle);

        // parse cycle
        var period = CycleUtils.ParsePeriod(cycle);

        // init helpers for schedule creation
        DateTime newTime = startTime;

        // create schedule based on end-of-month-convention
        int counter = 1;
        while (newTime < endTime)
        {
            timesSet.Add(newTime);
            
            // Calculate next time based on period
            DateTime nextTime;
            if (period.Years > 0)
            {
                nextTime = startTime.AddYears(period.Years * counter);
            }
            else if (period.Months > 0)
            {
                nextTime = startTime.AddMonths(period.Months * counter);
            }
            else
            {
                nextTime = startTime.AddDays(period.Days * counter);
            }
            
            newTime = shifter.Shift(nextTime);
            counter++;
        }

        // add (or not) additional time at endTime
        if (addEndTime)
        {
            timesSet.Add(endTime);
        }
        else
        {
            if (endTime.Equals(startTime)) 
                timesSet.Remove(startTime);
        }

        // now adjust for the last stub
        if (stub == StringUtils.LongStub && timesSet.Count > 2 && !endTime.Equals(newTime))
        {
            DateTime adjustTime;
            if (period.Years > 0)
            {
                adjustTime = startTime.AddYears(period.Years * (counter - 2));
            }
            else if (period.Months > 0)
            {
                adjustTime = startTime.AddMonths(period.Months * (counter - 2));
            }
            else
            {
                adjustTime = startTime.AddDays(period.Days * (counter - 2));
            }
            
            timesSet.Remove(shifter.Shift(adjustTime));
        }

        // return schedule
        return timesSet;
    }

    public static IEnumerable<DateTime> CreateArraySchedule(DateTime[] startTimes,
                                                           DateTime endTime, 
                                                           string[] cycles, 
                                                           EndOfMonthConventionEnum endOfMonthConvention)
    {
        var timesSet = new HashSet<DateTime>();

        // add schedules 1 to N-1
        for (int i = 0; i < startTimes.Length - 1; i++)
        {
            var scheduleSegment = CreateSchedule(startTimes[i], startTimes[i + 1], 
                (cycles == null) ? null : cycles[i], endOfMonthConvention, true);
            foreach (var time in scheduleSegment)
            {
                timesSet.Add(time);
            }
        }

        // add last schedule
        var lastScheduleSegment = CreateSchedule(startTimes[startTimes.Length - 1], endTime,
            (cycles == null) ? null : cycles[startTimes.Length - 1], endOfMonthConvention, true);
        foreach (var time in lastScheduleSegment)
        {
            timesSet.Add(time);
        }

        // return schedule
        return timesSet;
    }
}
namespace ActusInsurance.Core.Conventions.DayCount;

public interface IDayCountConventionProvider
{
    double DayCount(DateTime startTime, DateTime endTime);

    double DayCountFraction(DateTime startTime, DateTime endTime);
}
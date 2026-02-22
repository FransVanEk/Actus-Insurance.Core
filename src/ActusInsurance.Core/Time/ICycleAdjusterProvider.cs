namespace ActusInsurance.Core.Time;

public interface ICycleAdjusterProvider
{
    DateTime PlusCycle(DateTime time);

    DateTime MinusCycle(DateTime time);
}
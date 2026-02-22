namespace ActusInsurance.Core.States;

public struct StateSpace
{
    public double NotionalPrincipal;
    public double NominalInterestRate;
    public double AccruedInterest;
    public double FeeAccrued;
    public double NotionalScalingMultiplier;
    public double InterestScalingMultiplier;
    public DateTime StatusDate;
    public string? ContractPerformance;

    public StateSpace()
    {
        NotionalPrincipal = 0.0;
        NominalInterestRate = 0.0;
        AccruedInterest = 0.0;
        FeeAccrued = 0.0;
        NotionalScalingMultiplier = 1.0;
        InterestScalingMultiplier = 1.0;
        StatusDate = default;
        ContractPerformance = null;
    }

    public readonly StateSpace Copy()
    {
        return this; // Structs are copied by value
    }
}

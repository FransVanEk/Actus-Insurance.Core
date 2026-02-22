using ActusInsurance.Core.Types;
using ActusInsurance.Core.States;
using ActusInsurance.Core.Models;
using ActusInsurance.Core.Externals;
using ActusInsurance.Core.Conventions.DayCount;  // Shared DayCountCalculator from Actus.Core
using ActusInsurance.Core.Time.Calendar;

namespace ActusInsurance.Core.Events;

public sealed class ContractEvent : IComparable<ContractEvent>
{
    public DateTime Time { get; set; }  // Shifted event time (for event ordering/timing)
    public DateTime ScheduleTime { get; set; }  // Original scheduled time (for calculations with CalcShift convention)
    public EventType Type { get; set; }
    public string Currency { get; set; } = string.Empty;
    public double Payoff { get; set; }
    
    // State after this event
    public double NotionalPrincipal { get; set; }
    public double NominalInterestRate { get; set; }
    public double AccruedInterest { get; set; }
    public double FeeAccrued { get; set; }

    public int CompareTo(ContractEvent? other)
    {
        if (other == null) return 1;
        
        int timeCompare = Time.CompareTo(other.Time);
        if (timeCompare != 0) return timeCompare;
        
        // If times are equal, sort by event type priority
        // IED < IP < IPCI < PRD < TD < RR < RRF < FP < SC < MD < CD
        return GetEventTypePriority(Type).CompareTo(GetEventTypePriority(other.Type));
    }

    private static int GetEventTypePriority(EventType type)
    {
        return type switch
        {
            EventType.IED => 0,
            EventType.IP => 1,
            EventType.IPCI => 2,
            EventType.PRD => 3,
            EventType.TD => 4,
            EventType.RR => 5,
            EventType.RRF => 6,
            EventType.FP => 7,
            EventType.SC => 8,
            EventType.MD => 9,
            EventType.CD => 10,
            _ => 100
        };
    }

    public void Evaluate(ref StateSpace states, PamContractTerms model, RiskFactorModel riskFactors, ActusInsurance.Core.Conventions.BusinessDay.BusinessDayAdjuster timeAdjuster)
    {
        // Compute payoff based on event type
        Payoff = ComputePayoff(states, model, riskFactors, timeAdjuster);
        
        // Update state based on event type
        UpdateState(ref states, model, riskFactors, timeAdjuster);
        
        // Store state after event for reporting
        NotionalPrincipal = states.NotionalPrincipal;
        NominalInterestRate = states.NominalInterestRate;
        AccruedInterest = states.AccruedInterest;
        FeeAccrued = states.FeeAccrued;
    }

    private double ComputePayoff(StateSpace states, PamContractTerms model, RiskFactorModel riskFactors, ActusInsurance.Core.Conventions.BusinessDay.BusinessDayAdjuster timeAdjuster)
    {
        double fxRate = GetFxRate(riskFactors, model.Currency);

        return Type switch
        {
            EventType.IED => fxRate * model.RoleSign * (-1) * (model.NotionalPrincipal + model.PremiumDiscountAtIED),
            EventType.MD => fxRate * states.NotionalScalingMultiplier * states.NotionalPrincipal,
            EventType.PRD => fxRate * model.RoleSign * (-1) * model.PriceAtPurchaseDate,
            EventType.TD => ComputeTerminationPayoff(states, model, riskFactors),
            EventType.IP => ComputeInterestPayment(states, model, riskFactors, timeAdjuster),
            EventType.IPCI => 0.0,
            EventType.RR => 0.0,
            EventType.RRF => 0.0,
            EventType.FP => ComputeFeePayment(states, model, fxRate, timeAdjuster),
            EventType.SC => 0.0,
            _ => 0.0
        };
    }

    private void UpdateState(ref StateSpace states, PamContractTerms model, RiskFactorModel riskFactors, ActusInsurance.Core.Conventions.BusinessDay.BusinessDayAdjuster timeAdjuster)
    {
        // First, accrue interest from last event time to current event time for certain events
        if (Type == EventType.IP || Type == EventType.IPCI || Type == EventType.RR || 
            Type == EventType.FP || Type == EventType.SC)
        {
            AccrueInterest(ref states, model, timeAdjuster);
        }
        
        // Update StatusDate to ScheduleTime (original unshifted time) for consistency with v1
        states.StatusDate = ScheduleTime;

        switch (Type)
        {
            case EventType.IED:
                states.NotionalPrincipal = model.RoleSign * model.NotionalPrincipal;
                states.NominalInterestRate = model.NominalInterestRate;
                // IED does NOT reset AccruedInterest - it may have been initialized or accrued already
                // If cycle anchor date is before IED, accrue additional interest
                if (model.CycleAnchorDateOfInterestPayment.HasValue && 
                    model.CycleAnchorDateOfInterestPayment.Value < model.InitialExchangeDate)
                {
                    var calendar = new NoHolidaysCalendar();
                    var dayCounter = new DayCountCalculator(ConvertDayCountToString(model.DayCountConvention), calendar);
                    states.AccruedInterest += states.NotionalPrincipal * states.NominalInterestRate *
                        dayCounter.DayCountFraction(model.CycleAnchorDateOfInterestPayment.Value, ScheduleTime);
                }
                break;

            case EventType.MD:
                states.NotionalPrincipal = 0.0;
                states.AccruedInterest = 0.0;
                break;

            case EventType.PRD:
                // No state change for purchase in PAM
                break;

            case EventType.TD:
                states.NotionalPrincipal = 0.0;
                states.AccruedInterest = 0.0;
                break;

            case EventType.IP:
                states.AccruedInterest = 0.0;
                break;

            case EventType.IPCI:
                states.NotionalPrincipal += states.AccruedInterest;
                states.AccruedInterest = 0.0;
                break;

            case EventType.RR:
            case EventType.RRF:
                UpdateRateReset(ref states, model, riskFactors);
                break;

            case EventType.FP:
                states.FeeAccrued = 0.0;
                break;

            case EventType.SC:
                UpdateScaling(ref states, model, riskFactors);
                break;
        }
    }

    private double ComputeInterestPayment(StateSpace states, PamContractTerms model, RiskFactorModel riskFactors, ActusInsurance.Core.Conventions.BusinessDay.BusinessDayAdjuster timeAdjuster)
    {
        double fxRate = GetFxRate(riskFactors, model.Currency);
        // Use ScheduleTime (original unshifted time) for ShiftCalcTime
        // For CalcShift conventions (CSF, CSMF, etc.), this ensures calculations use unshifted dates
        // For ShiftCalc conventions (SCF, SCMF, etc.), ShiftCalcTime will apply the shift
        double yearFraction = ComputeYearFraction(timeAdjuster.ShiftCalcTime(states.StatusDate), timeAdjuster.ShiftCalcTime(ScheduleTime), model.DayCountConvention);
        
        return fxRate * states.InterestScalingMultiplier * 
               (states.AccruedInterest + yearFraction * states.NominalInterestRate * states.NotionalPrincipal);
    }

    private double ComputeTerminationPayoff(StateSpace states, PamContractTerms model, RiskFactorModel riskFactors)
    {
        double fxRate = GetFxRate(riskFactors, model.Currency);
        return fxRate * model.RoleSign * model.PriceAtTerminationDate;
    }

    private double ComputeFeePayment(StateSpace states, PamContractTerms model, double fxRate, ActusInsurance.Core.Conventions.BusinessDay.BusinessDayAdjuster timeAdjuster)
    {
        if (model.FeeBasis == "A")
        {
            return fxRate * model.RoleSign * model.FeeRate;
        }
        else // "N"
        {
            // Use ScheduleTime for ShiftCalcTime to respect CalcShift conventions
            double yearFraction = ComputeYearFraction(timeAdjuster.ShiftCalcTime(states.StatusDate), timeAdjuster.ShiftCalcTime(ScheduleTime), model.DayCountConvention);
            return fxRate * (states.FeeAccrued + states.NotionalPrincipal * model.FeeRate * yearFraction);
        }
    }

    private void UpdateRateReset(ref StateSpace states, PamContractTerms model, RiskFactorModel riskFactors)
    {
        if (Type == EventType.RRF && model.NextResetRate.HasValue)
        {
            states.NominalInterestRate = model.NextResetRate.Value;
        }
        else if (!string.IsNullOrEmpty(model.MarketObjectCodeOfRateReset))
        {
            double marketRate = riskFactors.GetRate(model.MarketObjectCodeOfRateReset, Time);
            // Formula: marketRate * rateMultiplier + rateSpread (from v1)
            double newRate = marketRate * model.RateMultiplier + model.RateSpread;
            double deltaRate = newRate - states.NominalInterestRate;
            
            // Apply period caps and floors to the delta
            deltaRate = Math.Min(Math.Max(deltaRate, model.PeriodFloor), model.PeriodCap);
            newRate = states.NominalInterestRate + deltaRate;
            
            // Apply life caps and floors to the final rate
            newRate = Math.Min(Math.Max(newRate, model.LifeFloor), model.LifeCap);
            
            states.NominalInterestRate = newRate;
        }
    }

    private void UpdateScaling(ref StateSpace states, PamContractTerms model, RiskFactorModel riskFactors)
    {
        if (!string.IsNullOrEmpty(model.MarketObjectCodeOfScalingIndex))
        {
            double scalingIndex = riskFactors.GetRate(model.MarketObjectCodeOfScalingIndex, Time);
            double scalingFactor = scalingIndex / model.ScalingIndexAtContractDealDate;

            if (model.ScalingEffect?.Contains("N") == true)
            {
                states.NotionalScalingMultiplier = scalingFactor * model.NotionalScalingMultiplier;
            }
            if (model.ScalingEffect?.Contains("I") == true)
            {
                states.InterestScalingMultiplier = scalingFactor * model.InterestScalingMultiplier;
            }
        }
    }

    private double ComputeYearFraction(DateTime start, DateTime end, DayCountConvention convention)
    {
        // For 100% accuracy match with v1, use the original DayCountCalculator
        var calendar = new NoHolidaysCalendar();
        var dayCounter = new DayCountCalculator(ConvertDayCountToString(convention), calendar);
        return dayCounter.DayCountFraction(start, end);
    }
    
    private string ConvertDayCountToString(DayCountConvention convention)
    {
        return convention switch
        {
            DayCountConvention.A_AISDA => "AA",
            DayCountConvention.A_360 => "A360",
            DayCountConvention.A_365 => "A365",
            DayCountConvention.E30_360ISDA => "30E360ISDA",
            DayCountConvention.E30_360 => "30E360",
            DayCountConvention.B_252 => "B252",
            DayCountConvention.A_336 => "A336",
            _ => "A365"
        };
    }

    private double Compute30E360(DateTime start, DateTime end)
    {
        int y1 = start.Year;
        int m1 = start.Month;
        int d1 = Math.Min(start.Day, 30);
        
        int y2 = end.Year;
        int m2 = end.Month;
        int d2 = Math.Min(end.Day, 30);
        
        return (360 * (y2 - y1) + 30 * (m2 - m1) + (d2 - d1)) / 360.0;
    }

    private void AccrueInterest(ref StateSpace states, PamContractTerms model, ActusInsurance.Core.Conventions.BusinessDay.BusinessDayAdjuster timeAdjuster)
    {
        if (states.StatusDate < ScheduleTime && states.NotionalPrincipal != 0)
        {
            var calendar = new NoHolidaysCalendar();
            var dayCounter = new DayCountCalculator(ConvertDayCountToString(model.DayCountConvention), calendar);
            // Use ScheduleTime for ShiftCalcTime to respect CalcShift conventions
            double timeFromLastEvent = dayCounter.DayCountFraction(timeAdjuster.ShiftCalcTime(states.StatusDate), timeAdjuster.ShiftCalcTime(ScheduleTime));
            
            // Accrue interest
            if (states.NominalInterestRate != 0)
            {
                states.AccruedInterest += states.NominalInterestRate * states.NotionalPrincipal * timeFromLastEvent;
            }
            
            // Accrue fees
            if (model.FeeRate != 0 && !string.IsNullOrEmpty(model.FeeBasis) && model.FeeBasis == "N")
            {
                states.FeeAccrued += model.FeeRate * states.NotionalPrincipal * timeFromLastEvent;
            }
        }
    }

    private double GetFxRate(RiskFactorModel riskFactors, string currency)
    {
        // For simplicity, assume settlement currency is same as contract currency
        // Real implementation would look up FX rate from risk factor model
        return 1.0;
    }
}

using ActusInsurance.Core.Models;
using ActusInsurance.Core.Events;
using ActusInsurance.Core.States;
using ActusInsurance.Core.Types;
using ActusInsurance.Core.Externals;
using ActusInsurance.Core.Time;

namespace ActusInsurance.Core.CPU.Contracts;

public static class PrincipalAtMaturity
{
    private static ActusInsurance.Core.Conventions.BusinessDay.BusinessDayAdjuster CreateBusinessDayAdjuster(BusinessDayConventionEnum convention, Calendar calendar)
    {
        ActusInsurance.Core.Time.Calendar.BusinessDayCalendarProvider cal = calendar switch
        {
            Calendar.MF => new ActusInsurance.Core.Time.Calendar.MondayToFridayCalendar(),
            Calendar.MFH => new ActusInsurance.Core.Time.Calendar.MondayToFridayWithHolidaysCalendar(new HashSet<DateTime>()),
            Calendar.NC => new ActusInsurance.Core.Time.Calendar.NoHolidaysCalendar(),
            _ => new ActusInsurance.Core.Time.Calendar.NoHolidaysCalendar()
        };
        return new ActusInsurance.Core.Conventions.BusinessDay.BusinessDayAdjuster(convention, cal);
    }

    private static List<DateTime> ApplyBusinessDayAdjustments(IEnumerable<DateTime> dates, ActusInsurance.Core.Conventions.BusinessDay.BusinessDayAdjuster adjuster)
    {
        return dates.Select(d => adjuster.ShiftEventTime(d)).ToList();
    }

    public static List<ContractEvent> Schedule(DateTime to, PamContractTerms model)
    {
        var events = new List<ContractEvent>(50);

        var adjuster = CreateBusinessDayAdjuster(model.BusinessDayConvention, model.Calendar);

        events.Add(new ContractEvent
        {
            ScheduleTime = model.InitialExchangeDate,
            Time = adjuster.ShiftEventTime(model.InitialExchangeDate),
            Type = EventType.IED,
            Currency = model.Currency
        });

        events.Add(new ContractEvent
        {
            ScheduleTime = model.MaturityDate,
            Time = adjuster.ShiftEventTime(model.MaturityDate),
            Type = EventType.MD,
            Currency = model.Currency
        });

        if (model.PurchaseDate.HasValue)
        {
            events.Add(new ContractEvent
            {
                ScheduleTime = model.PurchaseDate.Value,
                Time = adjuster.ShiftEventTime(model.PurchaseDate.Value),
                Type = EventType.PRD,
                Currency = model.Currency
            });
        }

        if (!string.IsNullOrEmpty(model.CycleOfInterestPayment) || model.CycleAnchorDateOfInterestPayment.HasValue)
        {
            var ipSchedule = ScheduleFactory.CreateSchedule(
                model.CycleAnchorDateOfInterestPayment ?? model.InitialExchangeDate,
                model.MaturityDate,
                model.CycleOfInterestPayment,
                model.EndOfMonthConvention,
                true);

            var ipDates = ipSchedule.ToList();

            EventType ipType = EventType.IP;

            if (model.CapitalizationEndDate.HasValue)
            {
                foreach (var date in ipDates)
                {
                    if (date <= model.CapitalizationEndDate.Value)
                    {
                        events.Add(new ContractEvent
                        {
                            ScheduleTime = date,
                            Time = adjuster.ShiftEventTime(date),
                            Type = EventType.IPCI,
                            Currency = model.Currency
                        });
                    }
                    else
                    {
                        events.Add(new ContractEvent
                        {
                            ScheduleTime = date,
                            Time = adjuster.ShiftEventTime(date),
                            Type = EventType.IP,
                            Currency = model.Currency
                        });
                    }
                }

                if (!ipDates.Contains(model.CapitalizationEndDate.Value))
                {
                    events.Add(new ContractEvent
                    {
                        ScheduleTime = model.CapitalizationEndDate.Value,
                        Time = adjuster.ShiftEventTime(model.CapitalizationEndDate.Value),
                        Type = EventType.IPCI,
                        Currency = model.Currency
                    });
                }
            }
            else
            {
                foreach (var date in ipDates)
                {
                    events.Add(new ContractEvent
                    {
                        ScheduleTime = date,
                        Time = adjuster.ShiftEventTime(date),
                        Type = EventType.IP,
                        Currency = model.Currency
                    });
                }
            }
        }
        else if (model.CapitalizationEndDate.HasValue)
        {
            events.Add(new ContractEvent
            {
                ScheduleTime = model.CapitalizationEndDate.Value,
                Time = adjuster.ShiftEventTime(model.CapitalizationEndDate.Value),
                Type = EventType.IPCI,
                Currency = model.Currency
            });
        }

        if (!string.IsNullOrEmpty(model.CycleOfRateReset))
        {
            var rrSchedule = ScheduleFactory.CreateSchedule(
                model.CycleAnchorDateOfRateReset ?? model.InitialExchangeDate,
                model.MaturityDate,
                model.CycleOfRateReset,
                model.EndOfMonthConvention,
                false);

            var rrDates = rrSchedule.ToList();

            bool firstAfterStatus = true;
            foreach (var date in rrDates)
            {
                var eventTime = adjuster.ShiftEventTime(date);
                if (firstAfterStatus && eventTime > model.StatusDate && model.NextResetRate.HasValue)
                {
                    events.Add(new ContractEvent
                    {
                        ScheduleTime = date,
                        Time = eventTime,
                        Type = EventType.RRF,
                        Currency = model.Currency
                    });
                    firstAfterStatus = false;
                }
                else
                {
                    events.Add(new ContractEvent
                    {
                        ScheduleTime = date,
                        Time = eventTime,
                        Type = EventType.RR,
                        Currency = model.Currency
                    });
                }
            }
        }

        if (!string.IsNullOrEmpty(model.CycleOfFee))
        {
            var fpSchedule = ScheduleFactory.CreateSchedule(
                model.CycleAnchorDateOfFee ?? model.InitialExchangeDate,
                model.MaturityDate,
                model.CycleOfFee,
                model.EndOfMonthConvention,
                true);

            var fpDates = fpSchedule.ToList();

            foreach (var date in fpDates)
            {
                events.Add(new ContractEvent
                {
                    ScheduleTime = date,
                    Time = adjuster.ShiftEventTime(date),
                    Type = EventType.FP,
                    Currency = model.Currency
                });
            }
        }

        if (!string.IsNullOrEmpty(model.ScalingEffect) &&
            (model.ScalingEffect.Contains("I") || model.ScalingEffect.Contains("N")))
        {
            var scSchedule = ScheduleFactory.CreateSchedule(
                model.CycleAnchorDateOfScalingIndex ?? model.InitialExchangeDate,
                model.MaturityDate,
                model.CycleOfScalingIndex,
                model.EndOfMonthConvention,
                false);

            var scDates = scSchedule.ToList();

            foreach (var date in scDates)
            {
                events.Add(new ContractEvent
                {
                    ScheduleTime = date,
                    Time = adjuster.ShiftEventTime(date),
                    Type = EventType.SC,
                    Currency = model.Currency
                });
            }
        }

        if (model.TerminationDate.HasValue)
        {
            events.Add(new ContractEvent
            {
                ScheduleTime = model.TerminationDate.Value,
                Time = adjuster.ShiftEventTime(model.TerminationDate.Value),
                Type = EventType.IP,
                Currency = model.Currency
            });

            events.Add(new ContractEvent
            {
                ScheduleTime = model.TerminationDate.Value,
                Time = adjuster.ShiftEventTime(model.TerminationDate.Value),
                Type = EventType.TD,
                Currency = model.Currency
            });

            events.RemoveAll(e => e.Time > model.TerminationDate.Value);
        }

        events.RemoveAll(e => e.Time < model.StatusDate);
        events.RemoveAll(e => e.Time > to);
        events.Sort();

        return events;
    }

    public static List<ContractEvent> Apply(List<ContractEvent> events, PamContractTerms model, RiskFactorModel riskFactors)
    {
        var states = InitializeStateSpace(model);

        var timeAdjuster = CreateBusinessDayAdjuster(model.BusinessDayConvention, model.Calendar);

        events.Sort();

        foreach (var evt in events)
        {
            evt.Evaluate(ref states, model, riskFactors, timeAdjuster);
        }

        if (model.PurchaseDate.HasValue)
        {
            events.RemoveAll(e => e.Type != EventType.AD && e.Time < model.PurchaseDate.Value);
            events.RemoveAll(e => e.Type == EventType.IP && e.Time == model.PurchaseDate.Value);
        }

        return events;
    }

    private static StateSpace InitializeStateSpace(PamContractTerms model)
    {
        var states = new StateSpace
        {
            NotionalScalingMultiplier = model.NotionalScalingMultiplier,
            InterestScalingMultiplier = model.InterestScalingMultiplier,
            ContractPerformance = model.ContractPerformance,
            StatusDate = model.StatusDate
        };

        if (model.InitialExchangeDate > model.StatusDate)
        {
            states.NotionalPrincipal = 0.0;
            states.NominalInterestRate = 0.0;
        }
        else
        {
            states.NotionalPrincipal = model.RoleSign * model.NotionalPrincipal;
            states.NominalInterestRate = model.NominalInterestRate;
        }

        if (model.NominalInterestRate == 0)
        {
            states.AccruedInterest = 0.0;
        }
        else if (model.AccruedInterest != 0)
        {
            states.AccruedInterest = model.AccruedInterest;
        }
        else if (!string.IsNullOrEmpty(model.CycleOfInterestPayment))
        {
            var ipSchedule = new List<DateTime>(ScheduleFactory.CreateSchedule(
                model.CycleAnchorDateOfInterestPayment ?? model.InitialExchangeDate,
                model.MaturityDate,
                model.CycleOfInterestPayment,
                model.EndOfMonthConvention,
                true));

            DateTime? lastIpDate = null;
            foreach (var date in ipSchedule)
            {
                if (date < states.StatusDate)
                {
                    lastIpDate = date;
                }
            }

            if (lastIpDate.HasValue)
            {
                TimeSpan diff = states.StatusDate - lastIpDate.Value;
                double yearFraction = model.DayCountConvention switch
                {
                    DayCountConvention.A_365 => diff.TotalDays / 365.0,
                    DayCountConvention.A_360 => diff.TotalDays / 360.0,
                    _ => diff.TotalDays / 365.0
                };

                states.AccruedInterest = yearFraction * states.NotionalPrincipal * states.NominalInterestRate;
            }
        }

        if (model.FeeRate == 0)
        {
            states.FeeAccrued = 0.0;
        }
        else if (model.FeeAccrued != 0)
        {
            states.FeeAccrued = model.FeeAccrued;
        }

        return states;
    }
}

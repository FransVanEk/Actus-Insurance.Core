using ActusInsurance.Core.Events;
using ActusInsurance.Core.Externals;
using ActusInsurance.Core.Models;

namespace ActusInsurance.Core.Contracts;

public interface IContractScheduler<TTerms> where TTerms : IContractTerms
{
    List<ContractEvent> Schedule(DateTime to, TTerms terms);

    List<ContractEvent> Apply(
        List<ContractEvent> events,
        TTerms              terms,
        RiskFactorModel     riskFactors);
}

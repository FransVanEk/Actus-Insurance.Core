namespace ActusInsurance.Core.Models;

public interface IContractTerms
{
    string ContractID { get; }

    string ContractType { get; }

    string Currency { get; }

    DateTime StatusDate { get; }

    DateTime MaturityDate { get; }
}

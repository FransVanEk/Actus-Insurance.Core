namespace ActusInsurance.Core.Types;

public enum EventType
{
    AD,   // Analysis Date
    IED,  // Initial Exchange
    MD,   // Maturity
    IP,   // Interest Payment
    IPCI, // Interest Payment Capitalization
    PRD,  // Purchase
    TD,   // Termination
    RR,   // Rate Reset
    RRF,  // Rate Reset Fixed
    FP,   // Fee Payment
    SC,   // Scaling
    CD    // Credit Default
}

public enum FeeBasis
{
    A,  // Absolute
    N   // Notional
}

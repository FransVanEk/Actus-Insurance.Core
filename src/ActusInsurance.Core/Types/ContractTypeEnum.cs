namespace ActusInsurance.Core.Types;

public enum ContractTypeEnum
{
    PAM,   // Principal at Maturity
    ANN,   // Annuity
    NAM,   // Negative Amortization
    LAM,   // Linear Amortization
    LAX,   // Linear Amortization with eXtension
    CLM,   // Call Money
    UMP,   // Undefined Maturity Profile
    CSH,   // Cash
    STK,   // Stock
    COM,   // Commodity
    SWAPS, // Plain Vanilla Interest Rate Swap
    SWPPV, // Plain Vanilla Interest Rate Swap
    FXOUT, // Foreign Exchange Outright
    CAPFL, // Caplet/Floorlet
    FUTUR, // Future
    OPTNS, // Option
    CEG,   // Credit Enhancement Guarantee
    CEC,   // Credit Enhancement Collateral
    BCS    // Basic Credit Support
}
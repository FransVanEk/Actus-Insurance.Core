namespace ActusInsurance.Core.Types;

public enum DayCountConvention
{
    A_AISDA,      // Actual/Actual ISDA
    A_360,        // Actual/360
    A_365,        // Actual/365 Fixed
    E30_360ISDA,  // 30E/360 ISDA
    E30_360,      // 30E/360
    B_252,        // Business/252
    A_336         // Actual/336
}
using System.Globalization;
using ActusInsurance.Core.Types;

namespace ActusInsurance.Core.Models;

public sealed class PamContractTerms : IContractTerms
{
    // Core identification
    public string ContractID { get; set; } = string.Empty;

    public string ContractType => "PAM";

    public string Currency { get; set; } = string.Empty; // Keep as string for ISO codes
    public ContractRole ContractRole { get; set; } = ContractRole.RPA;

    // Critical dates
    public DateTime StatusDate { get; set; }
    public DateTime InitialExchangeDate { get; set; }
    public DateTime MaturityDate { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public DateTime? TerminationDate { get; set; }
    public DateTime? CapitalizationEndDate { get; set; }

    // Principal and rates
    public double NotionalPrincipal { get; set; }
    public double NominalInterestRate { get; set; }
    public double AccruedInterest { get; set; }
    public double PremiumDiscountAtIED { get; set; }
    public double PriceAtPurchaseDate { get; set; }
    public double PriceAtTerminationDate { get; set; }

    // Rate reset parameters
    public double RateSpread { get; set; }
    public double RateMultiplier { get; set; } = 1.0;
    public double? NextResetRate { get; set; }
    public string? MarketObjectCodeOfRateReset { get; set; }
    public string? FixingPeriod { get; set; }
    public double LifeCap { get; set; } = double.PositiveInfinity;
    public double LifeFloor { get; set; } = double.NegativeInfinity;
    public double PeriodCap { get; set; } = double.PositiveInfinity;
    public double PeriodFloor { get; set; } = double.NegativeInfinity;

    // Interest payment cycle
    public string? CycleOfInterestPayment { get; set; }
    public DateTime? CycleAnchorDateOfInterestPayment { get; set; }
    public string? CyclePointOfInterestPayment { get; set; }

    // Rate reset cycle
    public string? CycleOfRateReset { get; set; }
    public DateTime? CycleAnchorDateOfRateReset { get; set; }
    public string? CyclePointOfRateReset { get; set; }

    // Fee parameters
    public string? CycleOfFee { get; set; }
    public DateTime? CycleAnchorDateOfFee { get; set; }
    public string? FeeBasis { get; set; }
    public double FeeRate { get; set; }
    public double FeeAccrued { get; set; }

    // Scaling parameters
    public string? MarketObjectCodeOfScalingIndex { get; set; }
    public double ScalingIndexAtContractDealDate { get; set; }
    public double NotionalScalingMultiplier { get; set; } = 1.0;
    public double InterestScalingMultiplier { get; set; } = 1.0;
    public string? CycleOfScalingIndex { get; set; }
    public DateTime? CycleAnchorDateOfScalingIndex { get; set; }
    public string? ScalingEffect { get; set; }

    // Conventions (using enums for type safety)
    public DayCountConvention DayCountConvention { get; set; } = DayCountConvention.A_365;
    public BusinessDayConventionEnum BusinessDayConvention { get; set; } = BusinessDayConventionEnum.NOS;
    public bool EndOfMonthConvention { get; set; }
    public Types.Calendar Calendar { get; set; } = Types.Calendar.NC;

    // Performance
    public string? ContractPerformance { get; set; }

    // Cached/computed values for performance
    private int _roleSign;
    private bool _roleSignComputed;

    public int RoleSign
    {
        get
        {
            if (!_roleSignComputed)
            {
                _roleSign = ContractRole switch
                {
                    Types.ContractRole.RPA => 1,
                    Types.ContractRole.RPL => -1,
                    Types.ContractRole.BUY => 1,
                    Types.ContractRole.SEL => -1,
                    Types.ContractRole.RFL => 1,
                    Types.ContractRole.PFL => -1,
                    Types.ContractRole.RF => 1,
                    Types.ContractRole.PF => -1,
                    _ => 1
                };
                _roleSignComputed = true;
            }
            return _roleSign;
        }
    }

    public static PamContractTerms FromDictionary(System.Collections.Generic.IDictionary<string, object> dict)
    {
        var terms = new PamContractTerms();

        // Use invariant culture for parsing to avoid culture-specific issues
        var culture = CultureInfo.InvariantCulture;

        terms.ContractID = GetString(dict, "contractID") ?? string.Empty;
        terms.Currency = GetString(dict, "currency") ?? string.Empty;
        terms.ContractRole = ParseContractRole(GetString(dict, "contractRole") ?? "RPA");

        terms.StatusDate = GetDateTime(dict, "statusDate");
        terms.InitialExchangeDate = GetDateTime(dict, "initialExchangeDate");
        terms.MaturityDate = GetDateTime(dict, "maturityDate");
        terms.PurchaseDate = GetNullableDateTime(dict, "purchaseDate");
        terms.TerminationDate = GetNullableDateTime(dict, "terminationDate");
        terms.CapitalizationEndDate = GetNullableDateTime(dict, "capitalizationEndDate");

        terms.NotionalPrincipal = GetDouble(dict, "notionalPrincipal");
        terms.NominalInterestRate = GetDouble(dict, "nominalInterestRate");
        terms.AccruedInterest = GetDouble(dict, "accruedInterest");
        terms.PremiumDiscountAtIED = GetDouble(dict, "premiumDiscountAtIED");
        terms.PriceAtPurchaseDate = GetDouble(dict, "priceAtPurchaseDate");
        terms.PriceAtTerminationDate = GetDouble(dict, "priceAtTerminationDate");

        terms.RateSpread = GetDouble(dict, "rateSpread");
        terms.RateMultiplier = GetDouble(dict, "rateMultiplier", 1.0);
        terms.NextResetRate = GetNullableDouble(dict, "nextResetRate");
        terms.MarketObjectCodeOfRateReset = GetString(dict, "marketObjectCodeOfRateReset");
        terms.FixingPeriod = GetString(dict, "fixingPeriod");
        terms.LifeCap = GetDouble(dict, "lifeCap", double.PositiveInfinity);
        terms.LifeFloor = GetDouble(dict, "lifeFloor", double.NegativeInfinity);
        terms.PeriodCap = GetDouble(dict, "periodCap", double.PositiveInfinity);
        terms.PeriodFloor = GetDouble(dict, "periodFloor", double.NegativeInfinity);

        terms.CycleOfInterestPayment = GetString(dict, "cycleOfInterestPayment");
        terms.CycleAnchorDateOfInterestPayment = GetNullableDateTime(dict, "cycleAnchorDateOfInterestPayment") ?? terms.InitialExchangeDate;
        terms.CyclePointOfInterestPayment = GetString(dict, "cyclePointOfInterestPayment");

        terms.CycleOfRateReset = GetString(dict, "cycleOfRateReset");
        terms.CycleAnchorDateOfRateReset = GetNullableDateTime(dict, "cycleAnchorDateOfRateReset") ?? terms.InitialExchangeDate;
        terms.CyclePointOfRateReset = GetString(dict, "cyclePointOfRateReset");

        terms.CycleOfFee = GetString(dict, "cycleOfFee");
        terms.CycleAnchorDateOfFee = GetNullableDateTime(dict, "cycleAnchorDateOfFee") ?? terms.InitialExchangeDate;
        terms.FeeBasis = GetString(dict, "feeBasis");
        terms.FeeRate = GetDouble(dict, "feeRate");
        terms.FeeAccrued = GetDouble(dict, "feeAccrued");

        terms.MarketObjectCodeOfScalingIndex = GetString(dict, "marketObjectCodeOfScalingIndex");
        terms.ScalingIndexAtContractDealDate = GetDouble(dict, "scalingIndexAtContractDealDate");
        terms.NotionalScalingMultiplier = GetDouble(dict, "notionalScalingMultiplier", 1.0);
        terms.InterestScalingMultiplier = GetDouble(dict, "interestScalingMultiplier", 1.0);
        terms.CycleOfScalingIndex = GetString(dict, "cycleOfScalingIndex");
        terms.CycleAnchorDateOfScalingIndex = GetNullableDateTime(dict, "cycleAnchorDateOfScalingIndex") ?? terms.InitialExchangeDate;
        terms.ScalingEffect = GetString(dict, "scalingEffect");

        terms.DayCountConvention = ParseDayCountConvention(GetString(dict, "dayCountConvention") ?? "A/365");
        terms.BusinessDayConvention = ParseBusinessDayConvention(GetString(dict, "businessDayConvention") ?? "NOS");
        terms.EndOfMonthConvention = GetBool(dict, "endOfMonthConvention");
        terms.Calendar = ParseCalendar(GetString(dict, "calendar") ?? "NC");

        terms.ContractPerformance = GetString(dict, "contractPerformance");

        return terms;
    }

    // Helper methods for dictionary access
    private static string? GetString(System.Collections.Generic.IDictionary<string, object> dict, string key)
    {
        if (dict.TryGetValue(key, out var value) && value != null)
        {
            return value.ToString();
        }
        return null;
    }

    private static DateTime GetDateTime(System.Collections.Generic.IDictionary<string, object> dict, string key)
    {
        if (dict.TryGetValue(key, out var value) && value != null)
        {
            if (value is DateTime dt) return dt;
            if (DateTime.TryParse(value.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                return parsed;
        }
        return default;
    }

    private static DateTime? GetNullableDateTime(System.Collections.Generic.IDictionary<string, object> dict, string key)
    {
        if (dict.TryGetValue(key, out var value) && value != null)
        {
            if (value is DateTime dt) return dt;
            if (DateTime.TryParse(value.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                return parsed;
        }
        return null;
    }

    private static double GetDouble(System.Collections.Generic.IDictionary<string, object> dict, string key, double defaultValue = 0.0)
    {
        if (dict.TryGetValue(key, out var value) && value != null)
        {
            if (value is double d) return d;
            if (value is int i) return i;
            if (value is long l) return l;
            if (value is decimal dec) return (double)dec;
            if (double.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
                return parsed;
        }
        return defaultValue;
    }

    private static double? GetNullableDouble(System.Collections.Generic.IDictionary<string, object> dict, string key)
    {
        if (dict.TryGetValue(key, out var value) && value != null)
        {
            if (value is double d) return d;
            if (value is int i) return i;
            if (value is long l) return l;
            if (value is decimal dec) return (double)dec;
            if (double.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
                return parsed;
        }
        return null;
    }

    private static bool GetBool(System.Collections.Generic.IDictionary<string, object> dict, string key, bool defaultValue = false)
    {
        if (dict.TryGetValue(key, out var value) && value != null)
        {
            if (value is bool b) return b;
            if (value.ToString()?.Equals("SD", StringComparison.OrdinalIgnoreCase) == true) return false;
            if (value.ToString()?.Equals("EOM", StringComparison.OrdinalIgnoreCase) == true) return true;
            if (bool.TryParse(value.ToString(), out var parsed))
                return parsed;
        }
        return defaultValue;
    }

    // Enum parsing helpers
    private static ContractRole ParseContractRole(string value)
    {
        return value?.ToUpperInvariant() switch
        {
            "RPA" => Types.ContractRole.RPA,
            "RPL" => Types.ContractRole.RPL,
            "BUY" => Types.ContractRole.BUY,
            "SEL" => Types.ContractRole.SEL,
            "RFL" => Types.ContractRole.RFL,
            "PFL" => Types.ContractRole.PFL,
            "RF" => Types.ContractRole.RF,
            "PF" => Types.ContractRole.PF,
            _ => Types.ContractRole.RPA
        };
    }

    private static DayCountConvention ParseDayCountConvention(string value)
    {
        return value?.ToUpperInvariant().Replace("/", "_") switch
        {
            "A_AISDA" or "AA" => DayCountConvention.A_AISDA,
            "A_360" or "A360" => DayCountConvention.A_360,
            "A_365" or "A365" => DayCountConvention.A_365,
            "30E_360ISDA" or "30E360ISDA" => DayCountConvention.E30_360ISDA,
            "30E_360" or "30E360" => DayCountConvention.E30_360,
            "B_252" or "B252" => DayCountConvention.B_252,
            "A_336" or "A336" => DayCountConvention.A_336,
            _ => DayCountConvention.A_365
        };
    }

    private static BusinessDayConventionEnum ParseBusinessDayConvention(string value)
    {
        return value?.ToUpperInvariant() switch
        {
            "NOS" => BusinessDayConventionEnum.NOS,
            "CSF" => BusinessDayConventionEnum.CSF,
            "CSMF" => BusinessDayConventionEnum.CSMF,
            "CSP" => BusinessDayConventionEnum.CSP,
            "CSMP" => BusinessDayConventionEnum.CSMP,
            "SCF" => BusinessDayConventionEnum.SCF,
            "SCMF" => BusinessDayConventionEnum.SCMF,
            "SCP" => BusinessDayConventionEnum.SCP,
            "SCMP" => BusinessDayConventionEnum.SCMP,
            _ => BusinessDayConventionEnum.NOS
        };
    }

    private static Types.Calendar ParseCalendar(string value)
    {
        return value?.ToUpperInvariant() switch
        {
            "MF" => Types.Calendar.MF,
            "MFH" => Types.Calendar.MFH,
            "NC" => Types.Calendar.NC,
            _ => Types.Calendar.NC
        };
    }
}

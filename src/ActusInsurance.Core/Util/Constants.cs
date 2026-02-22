namespace ActusInsurance.Core.Util;

public static class Constants
{
    public static readonly TimeSpan MAX_LIFETIME = TimeSpan.FromDays(365 * 50); // 50 years
    public static readonly TimeSpan MAX_LIFETIME_STK = TimeSpan.FromDays(365 * 10); // 10 years
    public static readonly TimeSpan MAX_LIFETIME_UMP = TimeSpan.FromDays(365 * 10); // 10 years
}
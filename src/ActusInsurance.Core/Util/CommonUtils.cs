namespace ActusInsurance.Core.Util;

public static class CommonUtils
{
    public static bool IsNull(object obj)
    {
        if (obj == null) return true;
        if (obj is string str) return string.IsNullOrEmpty(str);
        return false;
    }
}

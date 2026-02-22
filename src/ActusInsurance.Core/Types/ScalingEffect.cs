namespace ActusInsurance.Core.Types;

public enum ScalingEffect
{
    OOO, // No effect on interest or notional
    IOO, // Effect on interest only
    ONO, // Effect on notional only
    INO  // Effect on both interest and notional
}
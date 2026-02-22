namespace ActusInsurance.Core.Types;

public enum BusinessDayConventionEnum
{
    NOS,  // No Shift
    CSF,  // Calc Shift Following
    CSMF, // Calc Shift Modified Following
    CSP,  // Calc Shift Preceding
    CSMP, // Calc Shift Modified Preceding
    SCF,  // Shift Calc Following
    SCMF, // Shift Calc Modified Following
    SCP,  // Shift Calc Preceding
    SCMP,  // Shift Calc Modified Preceding
    NO_ADJUST
}
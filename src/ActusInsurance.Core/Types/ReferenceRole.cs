namespace ActusInsurance.Core.Types;

public enum ReferenceRole
{
    UDL,                 // Underlying
    FIL,                 // First Leg
    SEL,                 // Second Leg
    COVE,                // Covered Entity
    COVI,                // Covering Entity
    externalReferenceIndex // External Reference Index
}
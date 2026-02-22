using ActusInsurance.Core.Types;
using ActusInsurance.Core.Util;

namespace ActusInsurance.Core.Conventions.ContractRoles;

public static class ContractRoleConvention
{
    public static int RoleSign(ContractRole role)
    {
        if (CommonUtils.IsNull(role))
        {
            throw new AttributeConversionException("ContractRole cannot be null");
        }

        return role switch
        {
            ContractRole.RPA => 1,  // Real Position Asset - cash in-flow
            ContractRole.BUY => 1,  // Buy - cash in-flow  
            ContractRole.RFL => 1,  // Receive First Leg - cash in-flow
            ContractRole.RF => 1,   // Receive Fixed - cash in-flow
            ContractRole.RPL => -1, // Real Position Liability - cash out-flow
            ContractRole.SEL => -1, // Sell - cash out-flow
            ContractRole.PFL => -1, // Pay First Leg - cash out-flow
            ContractRole.PF => -1,  // Pay Fixed - cash out-flow
            _ => throw new AttributeConversionException($"Invalid ContractRole value: {role}")
        };
    }

    public static double RoleSign(string contractRole)
    {
        if (CommonUtils.IsNull(contractRole))
        {
            throw new AttributeConversionException("ContractRole string cannot be null");
        }

        if (!Enum.TryParse<ContractRole>(contractRole, true, out var role))
        {
            throw new AttributeConversionException($"Invalid ContractRole string: {contractRole}");
        }

        return RoleSign(role);
    }
}

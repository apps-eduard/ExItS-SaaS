using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Permissions;

public readonly record struct PosRoleAssignmentId(Guid Value)
{
    public static PosRoleAssignmentId New() => new(Guid.NewGuid());

    public static PosRoleAssignmentId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPosRoleAssignmentId,
                "Role assignment id cannot be an empty GUID.");
        }

        return new PosRoleAssignmentId(value);
    }
}

namespace ExItS.PinoyBusinessPOS.Domain.Permissions;

public enum PosRoleAssignmentStatus
{
    Active = 0,
    Revoked = 1
}

public static class PosRoleAssignmentStatusCodes
{
    public const string Active = "Active";
    public const string Revoked = "Revoked";

    public static string ToCode(PosRoleAssignmentStatus status) => status switch
    {
        PosRoleAssignmentStatus.Active => Active,
        PosRoleAssignmentStatus.Revoked => Revoked,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
    };

    public static bool TryParse(string? value, out PosRoleAssignmentStatus status)
    {
        status = default;
        if (string.Equals(value, Active, StringComparison.Ordinal))
        {
            status = PosRoleAssignmentStatus.Active;
            return true;
        }

        if (string.Equals(value, Revoked, StringComparison.Ordinal))
        {
            status = PosRoleAssignmentStatus.Revoked;
            return true;
        }

        return false;
    }
}

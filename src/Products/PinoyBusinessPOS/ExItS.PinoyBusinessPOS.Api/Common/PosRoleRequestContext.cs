using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Permissions;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Permissions;

namespace ExItS.PinoyBusinessPOS.Api.Common;

/// <summary>Per-request POS operational role resolved after commercial binding.</summary>
internal static class PosRoleRequestContext
{
    private static readonly AsyncLocal<PosRole?> Role = new();
    private static readonly AsyncLocal<bool> ActorPresent = new();
    private static readonly AsyncLocal<bool> Bypass = new();
    private static readonly AsyncLocal<bool> OrgManagement = new();
    private static readonly AsyncLocal<bool> OrgManagementOwner = new();

    public static PosRole? CurrentRole
    {
        get => Role.Value;
        set => Role.Value = value;
    }

    /// <summary>
    /// True when <c>X-Dev-Platform-User-Id</c> was present. When false, role enforcement is skipped
    /// so legacy org+commercial Dev/Testing callers (e.g. catalog reads without actor) keep working.
    /// When true, an active assignment (or Dev bootstrap Owner) is required.
    /// </summary>
    public static bool HasActorHeader
    {
        get => ActorPresent.Value;
        set => ActorPresent.Value = value;
    }

    /// <summary>When true, commercial checks run without role intersection (health/bootstrap paths).</summary>
    public static bool BypassRoleEnforcement
    {
        get => Bypass.Value;
        set => Bypass.Value = value;
    }

    /// <summary>
    /// Platform Organization Owner/Administrator management authority without a POS checkout role.
    /// </summary>
    public static bool OrganizationManagementAuthority
    {
        get => OrgManagement.Value;
        set => OrgManagement.Value = value;
    }

    public static bool OrganizationManagementIsExactOwner
    {
        get => OrgManagementOwner.Value;
        set => OrgManagementOwner.Value = value;
    }

    public static void Clear()
    {
        Role.Value = null;
        ActorPresent.Value = false;
        Bypass.Value = false;
        OrgManagement.Value = false;
        OrgManagementOwner.Value = false;
    }
}

internal static class PosRoleAuth
{
    public static bool TryAuthorizeRole(UtangCapability capability, out IResult? problem)
    {
        problem = null;
        if (PosRoleRequestContext.BypassRoleEnforcement)
        {
            return true;
        }

        // Legacy Dev/Testing paths that only send organization + commercial headers.
        if (!PosRoleRequestContext.HasActorHeader)
        {
            return true;
        }

        var role = PosRoleRequestContext.CurrentRole;
        if (role is not null)
        {
            if (!PosRoleMatrix.Allows(role.Value, capability))
            {
                problem = PosApiResults.Problem(
                    DomainErrorCodes.PosRoleDenied,
                    $"Role {PosRoleCodes.ToCode(role.Value)} is not permitted for this operation.",
                    StatusCodes.Status403Forbidden);
                PosAuthorizationDiagnostics.Record(
                    capability.ToString(),
                    PosRoleCodes.ToCode(role.Value),
                    "pos_role_denied",
                    false);
                return false;
            }

            return true;
        }

        if (PosRoleRequestContext.OrganizationManagementAuthority)
        {
            if (!PosRoleMatrix.AllowsOrganizationManagement(
                    PosRoleRequestContext.OrganizationManagementIsExactOwner,
                    capability))
            {
                problem = PosApiResults.Problem(
                    DomainErrorCodes.PosRoleDenied,
                    "Organization management authority does not permit this operation.",
                    StatusCodes.Status403Forbidden);
                PosAuthorizationDiagnostics.Record(
                    capability.ToString(),
                    "OrganizationManagement",
                    "organization_management_denied",
                    true);
                return false;
            }

            return true;
        }

        problem = PosApiResults.Problem(
            DomainErrorCodes.PosRoleRequired,
            "An active POS role assignment is required.",
            StatusCodes.Status403Forbidden);
        PosAuthorizationDiagnostics.Record(capability.ToString(), null, "pos_role_required", false);
        return false;
    }
}

/// <summary>Safe Development diagnostics for authorization denials (no tokens/passwords).</summary>
internal static class PosAuthorizationDiagnostics
{
    private static readonly AsyncLocal<string?> Last = new();

    public static void Record(string policy, string? role, string reason, bool orgManagement) =>
        Last.Value = $"policy={policy}; role={role ?? "none"}; orgManagement={orgManagement}; reason={reason}";

    public static string? ConsumeLast()
    {
        var value = Last.Value;
        Last.Value = null;
        return value;
    }
}

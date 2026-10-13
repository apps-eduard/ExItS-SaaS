using ExItS.Platform.Api.Common;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Api.Organizations;

internal static class StaffPasswordResetEndpoints
{
    public static IEndpointRouteBuilder MapStaffPasswordResetEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/platform/organizations/{organizationId:guid}/staff-password-reset-requests", async (
            Guid organizationId,
            ListOrganizationStaffPasswordResets list,
            PlatformMembershipAuthz membershipAuthz,
            CancellationToken ct) =>
        {
            var denied = await membershipAuthz.EnsureCanManageMembershipsAsync(
                PlatformAuditActions.PlatformAccessChecked,
                nameof(StaffPasswordResetRequest),
                organizationId.ToString("D"),
                organizationId,
                summary: "List staff password reset requests.",
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await list.ExecuteAsync(organizationId, ct).ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        app.MapPost("/api/v1/platform/organizations/{organizationId:guid}/staff-password-reset-requests/{requestId:guid}/approve", async (
            Guid organizationId,
            Guid requestId,
            DecideStaffPasswordReset decide,
            PlatformMembershipAuthz membershipAuthz,
            CancellationToken ct) =>
            await DecideAsync(organizationId, requestId, approve: true, decide, membershipAuthz, ct)
                .ConfigureAwait(false));

        app.MapPost("/api/v1/platform/organizations/{organizationId:guid}/staff-password-reset-requests/{requestId:guid}/deny", async (
            Guid organizationId,
            Guid requestId,
            DecideStaffPasswordReset decide,
            PlatformMembershipAuthz membershipAuthz,
            CancellationToken ct) =>
            await DecideAsync(organizationId, requestId, approve: false, decide, membershipAuthz, ct)
                .ConfigureAwait(false));

        return app;
    }

    private static async Task<IResult> DecideAsync(
        Guid organizationId,
        Guid requestId,
        bool approve,
        DecideStaffPasswordReset decide,
        PlatformMembershipAuthz membershipAuthz,
        CancellationToken ct)
    {
        var denied = await membershipAuthz.EnsureCanManageMembershipsAsync(
            approve
                ? PlatformAuditActions.StaffPasswordResetApproved
                : PlatformAuditActions.StaffPasswordResetDenied,
            nameof(StaffPasswordResetRequest),
            requestId.ToString("D"),
            organizationId,
            cancellationToken: ct).ConfigureAwait(false);
        if (denied is not null)
        {
            return denied;
        }

        var actorId = membershipAuthz.Inner.CurrentActor.PlatformUserId;
        if (actorId is null)
        {
            return PlatformApiResults.Problem(
                ApplicationErrorCodes.AccessTokenInvalid,
                "Authentication is required.",
                StatusCodes.Status401Unauthorized);
        }

        var result = await decide
            .ExecuteAsync(organizationId, requestId, actorId, approve, ct)
            .ConfigureAwait(false);
        return PlatformApiResults.FromResult(result, Results.Ok);
    }
}

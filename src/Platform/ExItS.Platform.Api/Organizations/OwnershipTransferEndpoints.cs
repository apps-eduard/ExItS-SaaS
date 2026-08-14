using ExItS.Platform.Api.Common;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Api.Organizations;

/// <summary>
/// Organization ownership transfer — Personal-identity Owner handoff.
/// Distinct from staff invitations (<see cref="InvitationEndpoints"/>).
/// </summary>
internal static class OwnershipTransferEndpoints
{
    public static IEndpointRouteBuilder MapOwnershipTransferEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/platform/organizations/{organizationId:guid}/ownership-transfer/resolve-target", async (
            Guid organizationId,
            ResolveOwnershipTransferTargetRequest body,
            ResolveOwnershipTransferTarget useCase,
            PlatformMembershipAuthz membershipAuthz,
            CancellationToken ct) =>
        {
            var denied = await membershipAuthz.EnsureCanManageMembershipsAsync(
                PlatformAuditActions.PlatformAccessChecked,
                nameof(OrganizationOwnershipTransfer),
                organizationId.ToString("D"),
                organizationId,
                summary: "Resolve ownership transfer target.",
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var authority = await membershipAuthz
                .ResolveActorMembershipAuthorityAsync(organizationId, ct)
                .ConfigureAwait(false);
            if (authority.ActorMembershipRole != OrganizationRole.OrganizationOwner)
            {
                return PlatformApiResults.Problem(
                    DomainErrorCodes.AuthorizationDenied,
                    "Only the current Organization Owner can resolve ownership transfer targets.",
                    StatusCodes.Status403Forbidden);
            }

            var result = await useCase.ExecuteAsync(body.Input ?? string.Empty, ct).ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
        });

        app.MapPost("/api/v1/platform/organizations/{organizationId:guid}/ownership-transfer/request", async (
            Guid organizationId,
            RequestOwnershipTransferRequest body,
            RequestOwnershipTransfer useCase,
            PlatformMembershipAuthz membershipAuthz,
            CancellationToken ct) =>
        {
            var denied = await membershipAuthz.EnsureCanManageMembershipsAsync(
                PlatformAuditActions.OwnershipTransferRequested,
                nameof(OrganizationOwnershipTransfer),
                organizationId.ToString("D"),
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

            var result = await useCase
                .ExecuteAsync(
                    PlatformOrganizationId.From(organizationId),
                    actorId,
                    body.TargetInput ?? string.Empty,
                    ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(
                result,
                dto => Results.Created(
                    $"/api/v1/platform/ownership-transfers/{dto.Id}",
                    dto));
        });

        app.MapGet("/api/v1/platform/organizations/{organizationId:guid}/ownership-transfer/pending", async (
            Guid organizationId,
            GetPendingOwnershipTransferForOrg useCase,
            PlatformMembershipAuthz membershipAuthz,
            CancellationToken ct) =>
        {
            var denied = await membershipAuthz.EnsureCanManageMembershipsAsync(
                PlatformAuditActions.PlatformAccessChecked,
                nameof(OrganizationOwnershipTransfer),
                organizationId.ToString("D"),
                organizationId,
                summary: "Get pending ownership transfer.",
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase
                .ExecuteAsync(PlatformOrganizationId.From(organizationId), ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
        });

        app.MapPost("/api/v1/platform/ownership-transfers/{transferId:guid}/cancel", async (
            Guid transferId,
            CancelOwnershipTransfer useCase,
            PlatformMembershipAuthz membershipAuthz,
            IOrganizationOwnershipTransferRepository transfers,
            CancellationToken ct) =>
        {
            var transfer = await transfers
                .GetByIdAsync(OrganizationOwnershipTransferId.From(transferId), ct)
                .ConfigureAwait(false);
            if (transfer is null)
            {
                return PlatformApiResults.Problem(
                    ApplicationErrorCodes.OwnershipTransferNotFound,
                    "Ownership transfer was not found.",
                    StatusCodes.Status404NotFound);
            }

            var denied = await membershipAuthz.EnsureCanManageMembershipsAsync(
                PlatformAuditActions.OwnershipTransferCancelled,
                nameof(OrganizationOwnershipTransfer),
                transferId.ToString("D"),
                transfer.OrganizationId.Value,
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

            var result = await useCase
                .ExecuteAsync(OrganizationOwnershipTransferId.From(transferId), actorId, ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
        });

        app.MapPost("/api/v1/platform/ownership-transfers/{transferId:guid}/accept", async (
            Guid transferId,
            AcceptOwnershipTransfer useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var actorId = authz.CurrentActor.PlatformUserId;
            if (actorId is null)
            {
                return PlatformApiResults.Problem(
                    ApplicationErrorCodes.AccessTokenInvalid,
                    "Authentication is required.",
                    StatusCodes.Status401Unauthorized);
            }

            var result = await useCase
                .ExecuteAsync(OrganizationOwnershipTransferId.From(transferId), actorId, ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
        });

        app.MapPost("/api/v1/platform/ownership-transfers/{transferId:guid}/decline", async (
            Guid transferId,
            DeclineOwnershipTransfer useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var actorId = authz.CurrentActor.PlatformUserId;
            if (actorId is null)
            {
                return PlatformApiResults.Problem(
                    ApplicationErrorCodes.AccessTokenInvalid,
                    "Authentication is required.",
                    StatusCodes.Status401Unauthorized);
            }

            var result = await useCase
                .ExecuteAsync(OrganizationOwnershipTransferId.From(transferId), actorId, ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
        });

        app.MapGet("/api/v1/platform/ownership-transfers/my-pending", async (
            ListPendingOwnershipTransfersForRecipient useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var actorId = authz.CurrentActor.PlatformUserId;
            if (actorId is null)
            {
                return PlatformApiResults.Problem(
                    ApplicationErrorCodes.AccessTokenInvalid,
                    "Authentication is required.",
                    StatusCodes.Status401Unauthorized);
            }

            var result = await useCase.ExecuteAsync(actorId, ct).ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
        });

        return app;
    }

    private sealed record ResolveOwnershipTransferTargetRequest(string? Input);
    private sealed record RequestOwnershipTransferRequest(string? TargetInput);
}

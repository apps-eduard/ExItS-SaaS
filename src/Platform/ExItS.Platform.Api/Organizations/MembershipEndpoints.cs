using ExItS.Platform.Api.Common;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Api.Organizations;

/// <summary>
/// Organization membership endpoints. Platform organization roles only — never product-local roles.
/// Development-stage: unauthenticated.
/// </summary>
internal static class MembershipEndpoints
{
    public static IEndpointRouteBuilder MapMembershipEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/platform/organizations/{organizationId:guid}/members", async (
            Guid organizationId,
            string? status,
            int? page,
            int? pageSize,
            MembershipQueryService queries,
            CancellationToken ct) =>
        {
            if (!TryParseMembershipStatus(status, out var parsed, out var error))
            {
                return error!;
            }

            var result = await queries
                .ListByOrganizationAsync(organizationId, parsed, page, pageSize, ct)
                .ConfigureAwait(false);
            return Results.Ok(result);
        });

        app.MapPost("/api/v1/platform/organizations/{organizationId:guid}/members", async (
            Guid organizationId,
            AddMemberRequest body,
            AddOrganizationMembership useCase,
            CancellationToken ct) =>
        {
            if (!TryParseRole(body.Role, out var role, out var error))
            {
                return error!;
            }

            try
            {
                var result = await useCase
                    .ExecuteAsync(
                        PlatformOrganizationId.From(organizationId),
                        PlatformUserId.From(body.UserId),
                        role,
                        ct)
                    .ConfigureAwait(false);
                return PlatformApiResults.FromResult(result, m => Results.Created(
                    $"/api/v1/platform/memberships/{m.Id.Value}",
                    MembershipQueryService.Map(m)));
            }
            catch (DomainException ex)
            {
                return PlatformApiResults.Problem(ex.ErrorCode, ex.Message, StatusCodes.Status400BadRequest);
            }
        });

        app.MapGet("/api/v1/platform/users/{userId:guid}/memberships", async (
            Guid userId,
            string? status,
            int? page,
            int? pageSize,
            MembershipQueryService queries,
            CancellationToken ct) =>
        {
            if (!TryParseMembershipStatus(status, out var parsed, out var error))
            {
                return error!;
            }

            var result = await queries.ListByUserAsync(userId, parsed, page, pageSize, ct).ConfigureAwait(false);
            return Results.Ok(result);
        });

        app.MapPut("/api/v1/platform/memberships/{membershipId:guid}/role", async (
            Guid membershipId,
            ChangeRoleRequest body,
            ChangeOrganizationRole useCase,
            CancellationToken ct) =>
        {
            if (!TryParseRole(body.Role, out var role, out var error))
            {
                return error!;
            }

            try
            {
                var result = await useCase
                    .ExecuteAsync(OrganizationMembershipId.From(membershipId), role, body.ActorReference, ct)
                    .ConfigureAwait(false);
                return PlatformApiResults.FromResult(result, m => Results.Ok(MembershipQueryService.Map(m)));
            }
            catch (DomainException ex)
            {
                return PlatformApiResults.Problem(ex.ErrorCode, ex.Message, StatusCodes.Status400BadRequest);
            }
        });

        app.MapPost("/api/v1/platform/memberships/{membershipId:guid}/suspend", async (
            Guid membershipId,
            MembershipLifecycleRequest? body,
            SuspendOrganizationMembership useCase,
            CancellationToken ct) =>
        {
            try
            {
                var result = await useCase
                    .ExecuteAsync(OrganizationMembershipId.From(membershipId), body?.Reason, body?.ActorReference, ct)
                    .ConfigureAwait(false);
                return PlatformApiResults.FromResult(result, m => Results.Ok(MembershipQueryService.Map(m)));
            }
            catch (DomainException ex)
            {
                return PlatformApiResults.Problem(ex.ErrorCode, ex.Message, StatusCodes.Status400BadRequest);
            }
        });

        app.MapPost("/api/v1/platform/memberships/{membershipId:guid}/reactivate", async (
            Guid membershipId,
            MembershipLifecycleRequest? body,
            ReactivateOrganizationMembership useCase,
            CancellationToken ct) =>
        {
            try
            {
                var result = await useCase
                    .ExecuteAsync(OrganizationMembershipId.From(membershipId), body?.ActorReference, ct)
                    .ConfigureAwait(false);
                return PlatformApiResults.FromResult(result, m => Results.Ok(MembershipQueryService.Map(m)));
            }
            catch (DomainException ex)
            {
                return PlatformApiResults.Problem(ex.ErrorCode, ex.Message, StatusCodes.Status400BadRequest);
            }
        });

        app.MapPost("/api/v1/platform/memberships/{membershipId:guid}/revoke", async (
            Guid membershipId,
            MembershipLifecycleRequest? body,
            RevokeOrganizationMembership useCase,
            CancellationToken ct) =>
        {
            try
            {
                var result = await useCase
                    .ExecuteAsync(OrganizationMembershipId.From(membershipId), body?.Reason, body?.ActorReference, ct)
                    .ConfigureAwait(false);
                return PlatformApiResults.FromResult(result, m => Results.Ok(MembershipQueryService.Map(m)));
            }
            catch (DomainException ex)
            {
                return PlatformApiResults.Problem(ex.ErrorCode, ex.Message, StatusCodes.Status400BadRequest);
            }
        });

        return app;
    }

    private static bool TryParseRole(string? role, out OrganizationRole parsed, out IResult? error)
    {
        parsed = default;
        error = null;
        if (string.IsNullOrWhiteSpace(role) || !Enum.TryParse(role, ignoreCase: true, out parsed))
        {
            error = PlatformApiResults.Problem(
                DomainErrorCodes.InvalidOrganizationRole,
                "Role must be OrganizationOwner, OrganizationAdministrator, or OrganizationMember.",
                StatusCodes.Status400BadRequest);
            return false;
        }

        return true;
    }

    private static bool TryParseMembershipStatus(string? status, out MembershipStatus? parsed, out IResult? error)
    {
        parsed = null;
        error = null;
        if (string.IsNullOrWhiteSpace(status))
        {
            return true;
        }

        if (!Enum.TryParse<MembershipStatus>(status, ignoreCase: true, out var value))
        {
            error = PlatformApiResults.Problem(
                DomainErrorCodes.InvalidMembershipStatusTransition,
                $"Unrecognized membership status '{status}'.",
                StatusCodes.Status400BadRequest);
            return false;
        }

        parsed = value;
        return true;
    }
}

internal sealed record AddMemberRequest(Guid UserId, string Role);
internal sealed record ChangeRoleRequest(string Role, string? ActorReference);
internal sealed record MembershipLifecycleRequest(string? Reason, string? ActorReference);

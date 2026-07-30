using ExItS.Platform.Api.Common;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Authorization;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Api.Organizations;

/// <summary>
/// Platform Organization lifecycle endpoints. Development-stage only: actor identity is
/// unauthenticated, but mutations enforce <see cref="PlatformPermission.ManageOrganizations"/> and
/// record audit trail entries.
/// </summary>
internal static class OrganizationEndpoints
{
    public static IEndpointRouteBuilder MapOrganizationEndpoints(this IEndpointRouteBuilder app)
    {
        var organizations = app.MapGroup("/api/v1/platform/organizations");

        organizations.MapGet("/", async (
            int? page,
            int? pageSize,
            OrganizationQueryService queries,
            CancellationToken ct) =>
        {
            var result = await queries.ListAsync(page, pageSize, ct).ConfigureAwait(false);
            return Results.Ok(result);
        });

        organizations.MapPost("/", async (
            CreateOrganizationRequest body,
            CreatePlatformOrganization useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManageOrganizations,
                PlatformAuditActions.OrganizationCreated,
                nameof(PlatformOrganization),
                body.Slug,
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase.ExecuteAsync(body.DisplayName, body.Slug, ct).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await authz.AuditSucceededAsync(
                    PlatformAuditActions.OrganizationCreated,
                    nameof(PlatformOrganization),
                    result.Value!.Id.Value.ToString("D"),
                    result.Value.Id.Value,
                    summary: $"Created organization {result.Value.DisplayName}.",
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(result, o => Results.Created(
                $"/api/v1/platform/organizations/{o.Id.Value}",
                MapOrganization(o)));
        });

        organizations.MapGet("/{organizationId:guid}", async (
            Guid organizationId,
            OrganizationQueryService queries,
            CancellationToken ct) =>
        {
            var organization = await queries.GetByIdAsync(organizationId, ct).ConfigureAwait(false);
            return organization is null
                ? PlatformApiResults.Problem(
                    ApplicationErrorCodes.OrganizationNotFound,
                    "Platform Organization was not found.",
                    StatusCodes.Status404NotFound)
                : Results.Ok(organization);
        });

        organizations.MapPost("/{organizationId:guid}/suspend", async (
            Guid organizationId,
            SuspendPlatformOrganization useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManageOrganizations,
                PlatformAuditActions.OrganizationSuspended,
                nameof(PlatformOrganization),
                organizationId.ToString("D"),
                organizationId,
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            try
            {
                var result = await useCase
                    .ExecuteAsync(PlatformOrganizationId.From(organizationId), ct)
                    .ConfigureAwait(false);
                if (result.IsSuccess)
                {
                    await authz.AuditSucceededAsync(
                        PlatformAuditActions.OrganizationSuspended,
                        nameof(PlatformOrganization),
                        organizationId.ToString("D"),
                        organizationId,
                        cancellationToken: ct).ConfigureAwait(false);
                }

                return PlatformApiResults.FromResult(result, o => Results.Ok(MapOrganization(o)));
            }
            catch (DomainException ex)
            {
                return PlatformApiResults.Problem(ex.ErrorCode, ex.Message, StatusCodes.Status400BadRequest);
            }
        });

        return app;
    }

    private static object MapOrganization(PlatformOrganization organization) => new
    {
        id = organization.Id.Value,
        displayName = organization.DisplayName,
        slug = organization.Slug,
        status = organization.Status.ToString(),
        createdAtUtc = organization.CreatedAtUtc,
        updatedAtUtc = organization.UpdatedAtUtc
    };
}

internal sealed record CreateOrganizationRequest(string DisplayName, string Slug);

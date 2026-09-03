using ExItS.Platform.Api.Common;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Api.Organizations;

/// <summary>
/// Area governance under the organization routes. Areas group branches for access,
/// navigation, and reporting; they never own stock, registers, shifts, or documents.
/// </summary>
internal static class OrganizationAreaEndpoints
{
    public static IEndpointRouteBuilder MapOrganizationAreaEndpoints(this IEndpointRouteBuilder app)
    {
        var root = app.MapGroup("/api/v1/platform/organizations/{organizationId:guid}");

        root.MapGet("/areas", async (
            Guid organizationId,
            ListOrganizationAreas useCase,
            PlatformOrganizationAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureCanViewOrganizationAsync(organizationId, ct).ConfigureAwait(false);
            if (denied is not null) return denied;

            return PlatformApiResults.FromResult(
                await useCase.ExecuteAsync(PlatformOrganizationId.From(organizationId), ct).ConfigureAwait(false),
                Results.Ok);
        });

        root.MapPost("/areas", async (
            Guid organizationId,
            AreaRequest body,
            CreateOrganizationArea useCase,
            PlatformOrganizationAuthz authz,
            CancellationToken ct) =>
        {
            var (denied, _) = await authz
                .EnsureCanEditOrganizationProfileAsync(organizationId, PlatformAuditActions.OrganizationAreaCreated, ct)
                .ConfigureAwait(false);
            if (denied is not null) return denied;

            var result = await useCase.ExecuteAsync(
                PlatformOrganizationId.From(organizationId),
                new CreateOrganizationAreaCommand(body.Name ?? string.Empty, body.Code),
                ct).ConfigureAwait(false);
            if (result.IsSuccess && result.Value is not null)
            {
                await WriteAreaAuditAsync(
                    authz,
                    PlatformAuditActions.OrganizationAreaCreated,
                    result.Value,
                    organizationId,
                    "Created",
                    ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(
                result,
                x => Results.Created($"/api/v1/platform/organizations/{organizationId}/areas/{x.Id}", x));
        });

        root.MapPut("/areas/{areaId:guid}", async (
            Guid organizationId,
            Guid areaId,
            AreaRequest body,
            UpdateOrganizationArea useCase,
            PlatformOrganizationAuthz authz,
            CancellationToken ct) =>
        {
            var (denied, _) = await authz
                .EnsureCanEditOrganizationProfileAsync(organizationId, PlatformAuditActions.OrganizationAreaUpdated, ct)
                .ConfigureAwait(false);
            if (denied is not null) return denied;

            var result = await useCase.ExecuteAsync(
                PlatformOrganizationId.From(organizationId),
                OrganizationAreaId.From(areaId),
                new UpdateOrganizationAreaCommand(body.Name ?? string.Empty, body.Code),
                ct).ConfigureAwait(false);
            if (result.IsSuccess && result.Value is not null)
            {
                await WriteAreaAuditAsync(
                    authz,
                    PlatformAuditActions.OrganizationAreaUpdated,
                    result.Value,
                    organizationId,
                    "Updated",
                    ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        root.MapPost("/areas/{areaId:guid}/archive", async (
            Guid organizationId,
            Guid areaId,
            ArchiveOrganizationArea useCase,
            PlatformOrganizationAuthz authz,
            CancellationToken ct) =>
        {
            var (denied, _) = await authz
                .EnsureCanEditOrganizationProfileAsync(organizationId, PlatformAuditActions.OrganizationAreaArchived, ct)
                .ConfigureAwait(false);
            if (denied is not null) return denied;

            var result = await useCase.ExecuteAsync(
                PlatformOrganizationId.From(organizationId),
                OrganizationAreaId.From(areaId),
                ct).ConfigureAwait(false);
            if (result.IsSuccess && result.Value is not null)
            {
                await WriteAreaAuditAsync(
                    authz,
                    PlatformAuditActions.OrganizationAreaArchived,
                    result.Value,
                    organizationId,
                    "Archived",
                    ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        // Assign, move, or unassign a branch. Grouping only: nothing operational moves with it.
        root.MapPut("/branches/{branchId:guid}/area", async (
            Guid organizationId,
            Guid branchId,
            SetBranchAreaRequest body,
            SetBranchArea useCase,
            PlatformOrganizationAuthz authz,
            CancellationToken ct) =>
        {
            var (denied, _) = await authz
                .EnsureCanEditOrganizationProfileAsync(organizationId, PlatformAuditActions.OrganizationBranchAreaChanged, ct)
                .ConfigureAwait(false);
            if (denied is not null) return denied;

            OrganizationAreaId? areaId;
            try
            {
                areaId = body.AreaId is Guid value ? OrganizationAreaId.From(value) : null;
            }
            catch (Domain.Common.DomainException ex)
            {
                return PlatformApiResults.Problem(ex.ErrorCode, ex.Message, StatusCodes.Status400BadRequest);
            }

            var result = await useCase.ExecuteAsync(
                PlatformOrganizationId.From(organizationId),
                OrganizationBranchId.From(branchId),
                areaId,
                ct).ConfigureAwait(false);
            if (result.IsSuccess && result.Value is not null)
            {
                await authz.Inner.AuditSucceededAsync(
                    PlatformAuditActions.OrganizationBranchAreaChanged,
                    nameof(OrganizationBranch),
                    result.Value.BranchId.ToString("D"),
                    organizationId,
                    summary: result.Value.AreaId is null
                        ? $"Removed branch {result.Value.Code} from its area."
                        : $"Placed branch {result.Value.Code} in area {result.Value.AreaName}.",
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        return app;
    }

    private static Task WriteAreaAuditAsync(
        PlatformOrganizationAuthz authz,
        string actionCode,
        OrganizationAreaDto area,
        Guid organizationId,
        string verb,
        CancellationToken cancellationToken) =>
        authz.Inner.AuditSucceededAsync(
            actionCode,
            nameof(OrganizationArea),
            area.Id.ToString("D"),
            organizationId,
            summary: $"{verb} area {area.Name}.",
            cancellationToken: cancellationToken);
}

internal sealed record AreaRequest(string? Name, string? Code);

internal sealed record SetBranchAreaRequest(Guid? AreaId);

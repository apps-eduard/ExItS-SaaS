using ExItS.Platform.Api.Common;
using ExItS.Platform.Application.Commercial;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Authorization;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Organizations;
using Microsoft.Extensions.Hosting;

namespace ExItS.Platform.Api.Organizations;

/// <summary>
/// Platform Organization lifecycle endpoints. Mutations enforce Platform
/// <see cref="PlatformPermission.ManageOrganizations"/> or trusted Organization Admin
/// self-service for permitted profile/branding fields only.
/// </summary>
internal static class OrganizationEndpoints
{
    public static IEndpointRouteBuilder MapOrganizationEndpoints(this IEndpointRouteBuilder app)
    {
        var organizations = app.MapGroup("/api/v1/platform/organizations");

        organizations.MapGet("/", async (
            int? page,
            int? pageSize,
            string? status,
            string? search,
            string? sortBy,
            bool? sortDesc,
            OrganizationQueryService queries,
            PlatformOrganizationAuthz orgAuthz,
            CancellationToken ct) =>
        {
            var denied = await orgAuthz.EnsureCanListOrganizationsAsync(ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            OrganizationStatus? parsedStatus = null;
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (!Enum.TryParse<OrganizationStatus>(status, ignoreCase: true, out var value))
                {
                    return PlatformApiResults.Problem(
                        DomainErrorCodes.InvalidOrganizationStatusTransition,
                        $"Unrecognized organization status '{status}'.",
                        StatusCodes.Status400BadRequest);
                }

                parsedStatus = value;
            }

            OrganizationListSortBy? parsedSort = null;
            if (!string.IsNullOrWhiteSpace(sortBy))
            {
                if (!Enum.TryParse<OrganizationListSortBy>(sortBy, ignoreCase: true, out var sortValue))
                {
                    return PlatformApiResults.Problem(
                        DomainErrorCodes.InvalidOrganizationProfile,
                        $"Unrecognized sort field '{sortBy}'.",
                        StatusCodes.Status400BadRequest);
                }

                parsedSort = sortValue;
            }

            var result = await queries
                .ListAsync(page, pageSize, parsedStatus, search, parsedSort, sortDesc, ct)
                .ConfigureAwait(false);
            return Results.Ok(result);
        });

        organizations.MapPost("/", async (
            CreateOrganizationRequest body,
            CreatePlatformOrganization useCase,
            PlatformAuthz authz,
            IHostEnvironment environment,
            CancellationToken ct) =>
        {
            if (!environment.IsEnvironment("Testing"))
            {
                return PlatformApiResults.Problem(
                    ApplicationErrorCodes.RuntimeOrganizationCreationDisabled,
                    "Platform runtime organization creation is disabled. Organizations are created through Start a Business.",
                    StatusCodes.Status403Forbidden);
            }

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
                OrganizationQueryService.Map(o)));
        });

        organizations.MapGet("/{organizationId:guid}", async (
            Guid organizationId,
            OrganizationQueryService queries,
            PlatformOrganizationAuthz orgAuthz,
            CancellationToken ct) =>
        {
            var denied = await orgAuthz.EnsureCanViewOrganizationAsync(organizationId, ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var organization = await queries.GetByIdAsync(organizationId, ct).ConfigureAwait(false);
            return organization is null
                ? PlatformApiResults.Problem(
                    ApplicationErrorCodes.OrganizationNotFound,
                    "Platform Organization was not found.",
                    StatusCodes.Status404NotFound)
                : Results.Ok(organization);
        });

        organizations.MapPut("/{organizationId:guid}", async (
            Guid organizationId,
            UpdateOrganizationRequest body,
            UpdateOrganizationPlatformFields platformUpdate,
            UpdateOrganizationProfile profileUpdate,
            PlatformOrganizationAuthz orgAuthz,
            CancellationToken ct) =>
        {
            var (denied, isPlatformManager) = await orgAuthz
                .EnsureCanEditOrganizationProfileAsync(
                    organizationId,
                    PlatformAuditActions.OrganizationUpdated,
                    ct)
                .ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            ApplicationResult<PlatformOrganization> result;
            if (isPlatformManager)
            {
                result = await platformUpdate
                    .ExecuteAsync(
                        PlatformOrganizationId.From(organizationId),
                        new UpdateOrganizationPlatformCommand(
                            body.DisplayName,
                            body.Slug,
                            body.LegalName,
                            body.ContactEmail,
                            body.ContactPhone,
                            body.AddressLine1,
                            body.AddressLine2,
                            body.City,
                            body.Region,
                            body.PostalCode,
                            body.CountryCode,
                            body.TimeZoneId,
                            body.Locale,
                            body.CurrencyCode,
                            body.ExpectedUpdatedAtUtc),
                        ct)
                    .ConfigureAwait(false);
            }
            else
            {
                // Org Admin cannot change slug or lifecycle; ignore slug if supplied.
                if (!string.IsNullOrWhiteSpace(body.Slug))
                {
                    return PlatformApiResults.Problem(
                        DomainErrorCodes.AuthorizationDenied,
                        "Organization administrators cannot change the organization slug.",
                        StatusCodes.Status403Forbidden);
                }

                result = await profileUpdate
                    .ExecuteAsync(
                        PlatformOrganizationId.From(organizationId),
                        new UpdateOrganizationProfileCommand(
                            body.DisplayName,
                            body.LegalName,
                            body.ContactEmail,
                            body.ContactPhone,
                            body.AddressLine1,
                            body.AddressLine2,
                            body.City,
                            body.Region,
                            body.PostalCode,
                            body.CountryCode,
                            body.TimeZoneId,
                            body.Locale,
                            body.CurrencyCode,
                            body.ExpectedUpdatedAtUtc),
                        requireActiveOrganization: true,
                        ct)
                    .ConfigureAwait(false);
            }

            if (result.IsSuccess)
            {
                await orgAuthz.Inner.AuditSucceededAsync(
                    PlatformAuditActions.OrganizationUpdated,
                    nameof(PlatformOrganization),
                    organizationId.ToString("D"),
                    organizationId,
                    summary: $"Updated organization {result.Value!.DisplayName}.",
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(result, o => Results.Ok(OrganizationQueryService.Map(o)));
        });

        organizations.MapPut("/{organizationId:guid}/branding", async (
            Guid organizationId,
            UpdateOrganizationBrandingRequest body,
            UpdateOrganizationBranding useCase,
            PlatformOrganizationAuthz orgAuthz,
            CancellationToken ct) =>
        {
            var (denied, isPlatformManager) = await orgAuthz
                .EnsureCanEditOrganizationProfileAsync(
                    organizationId,
                    PlatformAuditActions.OrganizationBrandingUpdated,
                    ct)
                .ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase
                .ExecuteAsync(
                    PlatformOrganizationId.From(organizationId),
                    new UpdateOrganizationBrandingCommand(
                        body.BrandDisplayName,
                        body.LogoUrl,
                        body.PrimaryColor,
                        body.AccentColor,
                        body.ExpectedUpdatedAtUtc),
                    requireActiveOrganization: !isPlatformManager,
                    ct)
                .ConfigureAwait(false);

            if (result.IsSuccess)
            {
                await orgAuthz.Inner.AuditSucceededAsync(
                    PlatformAuditActions.OrganizationBrandingUpdated,
                    nameof(PlatformOrganization),
                    organizationId.ToString("D"),
                    organizationId,
                    summary: "Updated organization branding.",
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(result, o => Results.Ok(OrganizationQueryService.Map(o)));
        });

        organizations.MapPost("/{organizationId:guid}/suspend", async (
            Guid organizationId,
            SuspendPlatformOrganization useCase,
            PlatformOrganizationAuthz orgAuthz,
            CancellationToken ct) =>
        {
            var denied = await orgAuthz
                .EnsureCanManageOrganizationLifecycleAsync(
                    organizationId,
                    PlatformAuditActions.OrganizationSuspended,
                    ct)
                .ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase
                .ExecuteAsync(PlatformOrganizationId.From(organizationId), ct)
                .ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await orgAuthz.Inner.AuditSucceededAsync(
                    PlatformAuditActions.OrganizationSuspended,
                    nameof(PlatformOrganization),
                    organizationId.ToString("D"),
                    organizationId,
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(result, o => Results.Ok(OrganizationQueryService.Map(o)));
        });

        organizations.MapPost("/{organizationId:guid}/reactivate", async (
            Guid organizationId,
            ReactivatePlatformOrganization useCase,
            PlatformOrganizationAuthz orgAuthz,
            CancellationToken ct) =>
        {
            var denied = await orgAuthz
                .EnsureCanManageOrganizationLifecycleAsync(
                    organizationId,
                    PlatformAuditActions.OrganizationReactivated,
                    ct)
                .ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase
                .ExecuteAsync(PlatformOrganizationId.From(organizationId), ct)
                .ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await orgAuthz.Inner.AuditSucceededAsync(
                    PlatformAuditActions.OrganizationReactivated,
                    nameof(PlatformOrganization),
                    organizationId.ToString("D"),
                    organizationId,
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(result, o => Results.Ok(OrganizationQueryService.Map(o)));
        });

        organizations.MapPost("/{organizationId:guid}/close", async (
            Guid organizationId,
            ClosePlatformOrganization useCase,
            PlatformOrganizationAuthz orgAuthz,
            CancellationToken ct) =>
        {
            var denied = await orgAuthz
                .EnsureCanManageOrganizationLifecycleAsync(
                    organizationId,
                    PlatformAuditActions.OrganizationClosed,
                    ct)
                .ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase
                .ExecuteAsync(PlatformOrganizationId.From(organizationId), ct)
                .ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await orgAuthz.Inner.AuditSucceededAsync(
                    PlatformAuditActions.OrganizationClosed,
                    nameof(PlatformOrganization),
                    organizationId.ToString("D"),
                    organizationId,
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(result, o => Results.Ok(OrganizationQueryService.Map(o)));
        });

        organizations.MapGet("/{organizationId:guid}/current-plan", async (
            Guid organizationId,
            string? productCode,
            OrganizationCurrentPlanQueryService queries,
            PlatformOrganizationAuthz orgAuthz,
            CancellationToken ct) =>
        {
            var denied = await orgAuthz.EnsureCanViewOrganizationAsync(organizationId, ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            try
            {
                var plan = await queries.GetCurrentPlanAsync(organizationId, productCode, ct).ConfigureAwait(false);
                return plan is null
                    ? PlatformApiResults.Problem(
                        ApplicationErrorCodes.OrganizationNotFound,
                        "Platform Organization was not found.",
                        StatusCodes.Status404NotFound)
                    : Results.Ok(plan);
            }
            catch (DomainException ex)
            {
                return PlatformApiResults.Problem(ex.ErrorCode, ex.Message, StatusCodes.Status400BadRequest);
            }
        });

        organizations.MapGet("/{organizationId:guid}/catalog", async (
            Guid organizationId,
            int? page,
            int? pageSize,
            string? search,
            GetOrganizationCatalogVisibility useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManageOrganizations,
                "platform.organization.catalog_viewed",
                nameof(PlatformOrganization),
                organizationId.ToString("D"),
                organizationId,
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase
                .ExecuteAsync(organizationId, page, pageSize, search, ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        organizations.MapGet("/{organizationId:guid}/business-type-entitlements", async (
            Guid organizationId,
            string? productCode,
            GetOrganizationBusinessTypeEntitlement useCase,
            PlatformOrganizationAuthz orgAuthz,
            CancellationToken ct) =>
        {
            var denied = await orgAuthz.EnsureCanViewOrganizationAsync(organizationId, ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase.ExecuteAsync(organizationId, productCode, ct).ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        organizations.MapPost("/{organizationId:guid}/business-type-activations", async (
            Guid organizationId,
            ActivateOrganizationBusinessTypeRequest body,
            ActivateOrganizationBusinessType useCase,
            PlatformOrganizationAuthz orgAuthz,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await orgAuthz
                .EnsureCanManageOrganizationCommercialAsync(
                    organizationId,
                    PlatformAuditActions.PlatformAccessChecked,
                    ct)
                .ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase
                .ExecuteAsync(organizationId, body.BusinessTypeId, body.ProductCode, ct)
                .ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await authz.AuditSucceededAsync(
                    PlatformAuditActions.PlatformAccessChecked,
                    "OrganizationBusinessTypeActivation",
                    $"{organizationId:D}:{body.BusinessTypeId:D}",
                    organizationId,
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(result, value => Results.Created(
                $"/api/v1/platform/organizations/{organizationId:D}/business-type-activations/{body.BusinessTypeId:D}",
                value));
        });

        organizations.MapDelete("/{organizationId:guid}/business-type-activations/{businessTypeId:guid}", async (
            Guid organizationId,
            Guid businessTypeId,
            DeactivateOrganizationBusinessType useCase,
            PlatformOrganizationAuthz orgAuthz,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await orgAuthz
                .EnsureCanManageOrganizationCommercialAsync(
                    organizationId,
                    PlatformAuditActions.PlatformAccessChecked,
                    ct)
                .ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase.ExecuteAsync(organizationId, businessTypeId, ct).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await authz.AuditSucceededAsync(
                    PlatformAuditActions.PlatformAccessChecked,
                    "OrganizationBusinessTypeActivation",
                    $"{organizationId:D}:{businessTypeId:D}",
                    organizationId,
                    cancellationToken: ct).ConfigureAwait(false);
                return Results.NoContent();
            }

            return PlatformApiResults.Problem(
                result.ErrorCode!,
                result.ErrorMessage!,
                PlatformApiResults.MapStatusCode(result.ErrorCode!));
        });

        return app;
    }
}

internal sealed record ActivateOrganizationBusinessTypeRequest(Guid BusinessTypeId, string? ProductCode = null);

internal sealed record CreateOrganizationRequest(string DisplayName, string Slug);

internal sealed record UpdateOrganizationRequest(
    string? DisplayName,
    string? Slug,
    string? LegalName,
    string? ContactEmail,
    string? ContactPhone,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? Region,
    string? PostalCode,
    string? CountryCode,
    string? TimeZoneId,
    string? Locale,
    string? CurrencyCode,
    DateTimeOffset? ExpectedUpdatedAtUtc);

internal sealed record UpdateOrganizationBrandingRequest(
    string? BrandDisplayName,
    string? LogoUrl,
    string? PrimaryColor,
    string? AccentColor,
    DateTimeOffset? ExpectedUpdatedAtUtc);

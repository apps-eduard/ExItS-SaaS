using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;

namespace ExItS.Platform.Application.Organizations;

public sealed record OrganizationProfileDto(
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
    string? CurrencyCode);

public sealed record OrganizationBrandingDto(
    string? BrandDisplayName,
    string? LogoUrl,
    string? PrimaryColor,
    string? AccentColor);

public sealed record PlatformOrganizationDto(
    Guid Id,
    string DisplayName,
    string Slug,
    string Status,
    OrganizationProfileDto Profile,
    OrganizationBrandingDto Branding,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed class OrganizationQueryService
{
    private readonly IPlatformOrganizationRepository _organizations;

    public OrganizationQueryService(IPlatformOrganizationRepository organizations)
    {
        _organizations = organizations;
    }

    public async Task<PlatformOrganizationDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var organization = await _organizations
            .GetByIdAsync(PlatformOrganizationId.From(id), cancellationToken)
            .ConfigureAwait(false);

        return organization is null ? null : Map(organization);
    }

    public async Task<PagedResult<PlatformOrganizationDto>> ListAsync(
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default) =>
        await ListAsync(page, pageSize, status: null, search: null, sortBy: null, sortDesc: null, productCode: null, cancellationToken)
            .ConfigureAwait(false);

    public async Task<PagedResult<PlatformOrganizationDto>> ListAsync(
        int? page,
        int? pageSize,
        OrganizationStatus? status,
        string? search,
        OrganizationListSortBy? sortBy,
        bool? sortDesc,
        CancellationToken cancellationToken = default) =>
        await ListAsync(page, pageSize, status, search, sortBy, sortDesc, productCode: null, cancellationToken)
            .ConfigureAwait(false);

    public async Task<PagedResult<PlatformOrganizationDto>> ListAsync(
        int? page,
        int? pageSize,
        OrganizationStatus? status,
        string? search,
        OrganizationListSortBy? sortBy,
        bool? sortDesc,
        ProductCode? productCode,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = CatalogPagination.Normalize(page, pageSize);
        var (items, totalCount) = await _organizations
            .ListAsync(
                status,
                search,
                sortBy ?? OrganizationListSortBy.DisplayName,
                sortDesc ?? false,
                skip,
                take,
                productCode,
                cancellationToken)
            .ConfigureAwait(false);
        var pageNumber = Math.Max(page ?? 1, 1);
        return new PagedResult<PlatformOrganizationDto>(
            items.Select(Map).ToList(),
            totalCount,
            pageNumber,
            take);
    }

    public static PlatformOrganizationDto Map(PlatformOrganization organization) =>
        new(
            organization.Id.Value,
            organization.DisplayName,
            organization.Slug,
            organization.Status.ToString(),
            new OrganizationProfileDto(
                organization.Profile.LegalName,
                organization.Profile.ContactEmail,
                organization.Profile.ContactPhone,
                organization.Profile.AddressLine1,
                organization.Profile.AddressLine2,
                organization.Profile.City,
                organization.Profile.Region,
                organization.Profile.PostalCode,
                organization.Profile.CountryCode,
                organization.Profile.TimeZoneId,
                organization.Profile.Locale,
                organization.Profile.CurrencyCode),
            new OrganizationBrandingDto(
                organization.Branding.BrandDisplayName,
                organization.Branding.LogoUrl,
                organization.Branding.PrimaryColor,
                organization.Branding.AccentColor),
            organization.CreatedAtUtc,
            organization.UpdatedAtUtc);
}

using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Integration.Pos;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Application.Organizations;

/// <summary>Platform Admin visibility of an Organization's POS catalog (read-only).</summary>
public sealed record OrganizationCatalogVisibilityDto(
    Guid OrganizationId,
    string OrganizationDisplayName,
    string OrganizationStatus,
    string? BusinessType,
    int ProductCount,
    IReadOnlyDictionary<string, int> SourceBreakdown,
    IReadOnlyList<OrganizationCatalogProductVisibilityDto> Products,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record OrganizationCatalogProductVisibilityDto(
    Guid ProductId,
    string Name,
    string? Sku,
    string? Barcode,
    Guid? CategoryId,
    string? CategoryName,
    decimal SellingPrice,
    bool IsTracked,
    decimal? OnHandQuantity,
    string Status,
    string SourceType,
    Guid? PlatformGlobalProductId,
    Guid? PlatformTemplateId,
    DateTimeOffset? CatalogImportedAt,
    string CatalogSource);

public sealed class GetOrganizationCatalogVisibility
{
    private readonly IPlatformOrganizationRepository _organizations;
    private readonly IPosOrganizationCatalogReadClient _posCatalog;

    public GetOrganizationCatalogVisibility(
        IPlatformOrganizationRepository organizations,
        IPosOrganizationCatalogReadClient posCatalog)
    {
        _organizations = organizations;
        _posCatalog = posCatalog;
    }

    public async Task<ApplicationResult<OrganizationCatalogVisibilityDto>> ExecuteAsync(
        Guid organizationId,
        int? page = null,
        int? pageSize = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var organization = await _organizations
            .GetByIdAsync(PlatformOrganizationId.From(organizationId), cancellationToken)
            .ConfigureAwait(false);
        if (organization is null)
        {
            return ApplicationResult<OrganizationCatalogVisibilityDto>.Failure(
                ApplicationErrorCodes.OrganizationNotFound,
                "Platform Organization was not found.");
        }

        var catalog = await _posCatalog
            .GetOrganizationCatalogAsync(organizationId, page, pageSize, search, cancellationToken)
            .ConfigureAwait(false);

        return ApplicationResult<OrganizationCatalogVisibilityDto>.Success(
            new OrganizationCatalogVisibilityDto(
                organization.Id.Value,
                organization.DisplayName,
                organization.Status.ToString(),
                BusinessType: null,
                catalog.ProductCount,
                catalog.SourceBreakdown,
                catalog.Products.Select(MapProduct).ToList(),
                catalog.Page,
                catalog.PageSize,
                catalog.TotalCount));
    }

    private static OrganizationCatalogProductVisibilityDto MapProduct(PosOrganizationCatalogProductDto p) =>
        new(
            p.ProductId,
            p.Name,
            p.Sku,
            p.Barcode,
            p.CategoryId,
            p.CategoryName,
            p.SellingPrice,
            p.IsTracked,
            p.OnHandQuantity,
            p.Status,
            p.SourceType,
            p.PlatformGlobalProductId,
            p.PlatformTemplateId,
            p.CatalogImportedAt,
            p.CatalogSource);
}

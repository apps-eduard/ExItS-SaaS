namespace ExItS.PinoyBusinessPOS.Application.Catalog;

/// <summary>Read-only organization catalog summary for Platform Admin support visibility.</summary>
public sealed record PlatformSupportOrganizationCatalogSummaryDto(
    Guid OrganizationId,
    int ProductCount,
    IReadOnlyDictionary<string, int> SourceBreakdown,
    IReadOnlyList<PlatformSupportOrganizationCatalogProductDto> Products,
    int Page,
    int PageSize,
    int TotalCount);

/// <summary>Single POS catalog product row with provenance for Platform support.</summary>
public sealed record PlatformSupportOrganizationCatalogProductDto(
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

/// <summary>Provenance codes exposed to Platform (prefer refs over raw CatalogSource).</summary>
public static class PlatformSupportCatalogProvenance
{
    public const string GlobalTemplate = "GlobalTemplate";
    public const string GlobalCatalog = "GlobalCatalog";
    public const string MerchantCreated = "MerchantCreated";

    public static string ResolveSourceType(Guid? platformTemplateId, Guid? platformGlobalProductId)
    {
        if (platformTemplateId is not null)
        {
            return GlobalTemplate;
        }

        if (platformGlobalProductId is not null)
        {
            return GlobalCatalog;
        }

        return MerchantCreated;
    }
}

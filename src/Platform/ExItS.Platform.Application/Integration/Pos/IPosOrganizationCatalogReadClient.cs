namespace ExItS.Platform.Application.Integration.Pos;

/// <summary>POS Product API connection for Platform support reads.</summary>
public sealed class PosProductApiOptions
{
    public const string SectionName = "PosProductApi";

    /// <summary>Base URL of the POS API (e.g. http://127.0.0.1:5290).</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Shared support API key sent as X-ExItS-Platform-Support-Key.</summary>
    public string SupportApiKey { get; set; } = string.Empty;
}

/// <summary>Read-only POS organization catalog client. No write methods.</summary>
public interface IPosOrganizationCatalogReadClient
{
    Task<PosOrganizationCatalogSummaryDto> GetOrganizationCatalogAsync(
        Guid organizationId,
        int? page = null,
        int? pageSize = null,
        string? search = null,
        CancellationToken cancellationToken = default);
}

public sealed record PosOrganizationCatalogSummaryDto(
    Guid OrganizationId,
    int ProductCount,
    IReadOnlyDictionary<string, int> SourceBreakdown,
    IReadOnlyList<PosOrganizationCatalogProductDto> Products,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record PosOrganizationCatalogProductDto(
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

/// <summary>Provenance mapping shared with POS (refs preferred over CatalogSource).</summary>
public static class OrganizationCatalogProvenance
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

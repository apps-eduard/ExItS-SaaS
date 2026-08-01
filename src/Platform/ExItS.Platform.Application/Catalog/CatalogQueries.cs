using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Products;

namespace ExItS.Platform.Application.Catalog;

public static class CatalogPagination
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public static (int Skip, int Take) Normalize(int? page, int? pageSize)
    {
        var take = Math.Clamp(pageSize ?? DefaultPageSize, 1, MaxPageSize);
        var pageNumber = Math.Max(page ?? 1, 1);
        return ((pageNumber - 1) * take, take);
    }
}

public sealed record ProductDto(
    Guid Id,
    string Code,
    string DisplayName,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record FeatureDefinitionDto(
    string ProductCode,
    string FeatureCode,
    string DisplayName,
    string ValueType,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record PlanDto(
    Guid Id,
    string ProductCode,
    string Code,
    string DisplayName,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record FeatureGrantDto(
    string FeatureCode,
    bool Enabled,
    int? NumericLimit);

public sealed record PlanVersionDto(
    Guid Id,
    Guid PlanId,
    string ProductCode,
    int VersionNumber,
    DateTimeOffset EffectiveFromUtc,
    DateTimeOffset? EffectiveToUtc,
    string BillingPeriod,
    bool TrialEligible,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    IReadOnlyList<FeatureGrantDto> Grants);

public sealed record TrialDefinitionDto(
    Guid Id,
    string ProductCode,
    Guid? PlanId,
    string DisplayName,
    long DurationTicks,
    string DurationIso,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    IReadOnlyList<FeatureGrantDto> FeatureGrants,
    IReadOnlyList<FeatureGrantDto> PostExpiryFeatureGrants);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);

public sealed class CatalogQueryService
{
    private readonly IProductRepository _products;
    private readonly IFeatureDefinitionRepository _features;
    private readonly IPlanRepository _plans;
    private readonly ITrialDefinitionRepository _trials;

    public CatalogQueryService(
        IProductRepository products,
        IFeatureDefinitionRepository features,
        IPlanRepository plans,
        ITrialDefinitionRepository trials)
    {
        _products = products;
        _features = features;
        _plans = plans;
        _trials = trials;
    }

    public Task<PagedResult<ProductDto>> ListProductsAsync(
        ProductStatus? status,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default) =>
        ListProductsAsync(status, page, pageSize, search: null, sortBy: null, sortDesc: null, cancellationToken);

    public async Task<PagedResult<ProductDto>> ListProductsAsync(
        ProductStatus? status,
        int? page,
        int? pageSize,
        string? search,
        CatalogListSortBy? sortBy,
        bool? sortDesc,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = CatalogPagination.Normalize(page, pageSize);
        var (items, totalCount) = await _products
            .ListAsync(
                status,
                search,
                sortBy ?? CatalogListSortBy.Code,
                sortDesc ?? false,
                skip,
                take,
                cancellationToken)
            .ConfigureAwait(false);
        var pageNumber = Math.Max(page ?? 1, 1);
        return new PagedResult<ProductDto>(
            items.Select(MapProduct).ToList(),
            totalCount,
            pageNumber,
            take);
    }

    public async Task<PagedResult<PlanDto>> ListPlansAsync(
        string? productCode,
        PlanStatus? status,
        int? page,
        int? pageSize,
        string? search,
        CatalogListSortBy? sortBy,
        bool? sortDesc,
        CancellationToken cancellationToken = default)
    {
        ProductCode? code = null;
        if (!string.IsNullOrWhiteSpace(productCode))
        {
            code = ProductCode.Create(productCode);
        }

        var (skip, take) = CatalogPagination.Normalize(page, pageSize);
        var (items, totalCount) = await _plans
            .ListAsync(
                code,
                status,
                search,
                sortBy ?? CatalogListSortBy.Code,
                sortDesc ?? false,
                skip,
                take,
                cancellationToken)
            .ConfigureAwait(false);
        return new PagedResult<PlanDto>(
            items.Select(MapPlan).ToList(),
            totalCount,
            Math.Max(page ?? 1, 1),
            take);
    }

    public async Task<ProductDto?> GetProductByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _products.GetByIdAsync(ProductId.From(id), cancellationToken).ConfigureAwait(false);
        return product is null ? null : MapProduct(product);
    }

    public async Task<ProductDto?> GetProductByCodeAsync(string productCode, CancellationToken cancellationToken = default)
    {
        var product = await _products
            .GetByCodeAsync(ProductCode.Create(productCode), cancellationToken)
            .ConfigureAwait(false);
        return product is null ? null : MapProduct(product);
    }

    public async Task<IReadOnlyList<FeatureDefinitionDto>> ListFeaturesByProductAsync(
        string productCode,
        CancellationToken cancellationToken = default)
    {
        var pc = ProductCode.Create(productCode);
        var features = await _features.ListByProductAsync(pc, cancellationToken).ConfigureAwait(false);
        return features.Select(MapFeature).ToList();
    }

    public async Task<IReadOnlyList<PlanDto>> ListPlansByProductAsync(
        string productCode,
        CancellationToken cancellationToken = default)
    {
        var pc = ProductCode.Create(productCode);
        var plans = await _plans.ListByProductAsync(pc, cancellationToken).ConfigureAwait(false);
        return plans.Select(MapPlan).ToList();
    }

    public async Task<PlanDto?> GetPlanByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var plan = await _plans.GetByIdAsync(PlanId.From(id), cancellationToken).ConfigureAwait(false);
        return plan is null ? null : MapPlan(plan);
    }

    public async Task<IReadOnlyList<PlanVersionDto>> ListPlanVersionsAsync(
        Guid planId,
        CancellationToken cancellationToken = default)
    {
        var versions = await _plans.ListVersionsAsync(PlanId.From(planId), cancellationToken).ConfigureAwait(false);
        return versions.Select(MapPlanVersion).ToList();
    }

    public async Task<PlanVersionDto?> GetPlanVersionByNumberAsync(
        Guid planId,
        int versionNumber,
        CancellationToken cancellationToken = default)
    {
        var version = await _plans
            .GetVersionByPlanAndNumberAsync(PlanId.From(planId), versionNumber, cancellationToken)
            .ConfigureAwait(false);

        return version is null ? null : MapPlanVersion(version);
    }

    public async Task<IReadOnlyList<TrialDefinitionDto>> ListTrialsByProductAsync(
        string productCode,
        CancellationToken cancellationToken = default)
    {
        var pc = ProductCode.Create(productCode);
        var trials = await _trials.ListByProductAsync(pc, cancellationToken).ConfigureAwait(false);
        return trials.Select(MapTrial).ToList();
    }

    private static ProductDto MapProduct(Product product) =>
        new(
            product.Id.Value,
            product.Code.Value,
            product.DisplayName,
            product.Status.ToString(),
            product.CreatedAtUtc,
            product.UpdatedAtUtc);

    private static FeatureDefinitionDto MapFeature(FeatureDefinition feature) =>
        new(
            feature.ProductCode.Value,
            feature.Code.Value,
            feature.DisplayName,
            feature.ValueType.ToString(),
            feature.Status.ToString(),
            feature.CreatedAtUtc,
            feature.UpdatedAtUtc);

    private static PlanDto MapPlan(Plan plan) =>
        new(
            plan.Id.Value,
            plan.ProductCode.Value,
            plan.Code.Value,
            plan.DisplayName,
            plan.Status.ToString(),
            plan.CreatedAtUtc,
            plan.UpdatedAtUtc);

    private static PlanVersionDto MapPlanVersion(PlanVersion version) =>
        new(
            version.Id.Value,
            version.PlanId.Value,
            version.ProductCode.Value,
            version.VersionNumber,
            version.EffectiveFromUtc,
            version.EffectiveToUtc,
            version.BillingPeriod.ToString(),
            version.TrialEligible,
            version.Status.ToString(),
            version.CreatedAtUtc,
            version.UpdatedAtUtc,
            version.Grants.Select(g => new FeatureGrantDto(g.FeatureCode.Value, g.Enabled, g.NumericLimit)).ToList());

    private static TrialDefinitionDto MapTrial(TrialDefinition trial) =>
        new(
            trial.Id.Value,
            trial.ProductCode.Value,
            trial.PlanId?.Value,
            trial.DisplayName,
            trial.Duration.Ticks,
            System.Xml.XmlConvert.ToString(trial.Duration),
            trial.Status.ToString(),
            trial.CreatedAtUtc,
            trial.UpdatedAtUtc,
            trial.FeatureGrants.Select(g => new FeatureGrantDto(g.FeatureCode.Value, g.Enabled, g.NumericLimit)).ToList(),
            trial.PostExpiryFeatureGrants.Select(g => new FeatureGrantDto(g.FeatureCode.Value, g.Enabled, g.NumericLimit)).ToList());
}

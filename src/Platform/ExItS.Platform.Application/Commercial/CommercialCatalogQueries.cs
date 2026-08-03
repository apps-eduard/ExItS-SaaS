using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Products;

namespace ExItS.Platform.Application.Commercial;

public sealed class CommercialCatalogQueryService
{
    private readonly EnsureMvpPosPlans _ensureMvpPosPlans;
    private readonly CatalogQueryService _catalog;

    public CommercialCatalogQueryService(EnsureMvpPosPlans ensureMvpPosPlans, CatalogQueryService catalog)
    {
        _ensureMvpPosPlans = ensureMvpPosPlans;
        _catalog = catalog;
    }

    public async Task<IReadOnlyList<PlanDto>> ListActivePlansAsync(
        string? productCode,
        CancellationToken cancellationToken = default)
    {
        await _ensureMvpPosPlans.ExecuteAsync(cancellationToken).ConfigureAwait(false);

        var resolvedProductCode = string.IsNullOrWhiteSpace(productCode)
            ? ProductCode.PinoyBusinessPos
            : productCode;

        try
        {
            var result = await _catalog
                .ListPlansAsync(
                    resolvedProductCode,
                    PlanStatus.Active,
                    page: 1,
                    pageSize: CatalogPagination.MaxPageSize,
                    search: null,
                    sortBy: CatalogListSortBy.SortOrder,
                    sortDesc: false,
                    cancellationToken)
                .ConfigureAwait(false);

            var items = result.Items;
            if (string.Equals(resolvedProductCode, ProductCode.PinoyBusinessPos, StringComparison.OrdinalIgnoreCase))
            {
                items = items
                    .Where(p => p.PlanKey is not null && MvpPosPlanCodes.All.Contains(p.PlanKey, StringComparer.Ordinal))
                    .ToList();
            }

            return items;
        }
        catch (DomainException ex)
        {
            throw new InvalidOperationException(ex.Message, ex);
        }
    }
}

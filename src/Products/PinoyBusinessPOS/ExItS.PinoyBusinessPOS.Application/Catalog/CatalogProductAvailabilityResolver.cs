using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Catalog;

public enum CatalogProductOfferingReason
{
    DefaultOrganizationStandard,
    ExplicitlyOffered,
    ExplicitlyNotOffered,
    BranchLocalOrigin,
    BranchLocalForeignBranch,
    BranchLocalOriginNotOffered
}

public sealed record CatalogProductOfferingResult(
    CatalogProductId ProductId,
    bool IsOffered,
    CatalogProductOfferingReason Reason);

/// <summary>Bulk-capable commercial offering resolver (scope + branch assortment).</summary>
public interface ICatalogProductAvailabilityResolver
{
    IReadOnlyList<CatalogProductOfferingResult> Resolve(
        PosBranchId actingBranchId,
        IReadOnlyList<CatalogProduct> products,
        IReadOnlyList<BranchProductAvailability> explicitRows);

    Task<IReadOnlyDictionary<Guid, CatalogProductOfferingResult>> ResolveForBranchAsync(
        Domain.Customers.PosOrganizationId organizationId,
        PosBranchId actingBranchId,
        IReadOnlyList<CatalogProduct> products,
        CancellationToken cancellationToken = default);
}

public sealed class CatalogProductAvailabilityResolver : ICatalogProductAvailabilityResolver
{
    private readonly IBranchProductAvailabilityRepository _availability;

    public CatalogProductAvailabilityResolver(IBranchProductAvailabilityRepository availability) =>
        _availability = availability;

    public IReadOnlyList<CatalogProductOfferingResult> Resolve(
        PosBranchId actingBranchId,
        IReadOnlyList<CatalogProduct> products,
        IReadOnlyList<BranchProductAvailability> explicitRows)
    {
        var byProduct = explicitRows.ToDictionary(r => r.ProductId.Value);
        var results = new List<CatalogProductOfferingResult>(products.Count);
        foreach (var product in products)
        {
            results.Add(ResolveOne(actingBranchId, product, byProduct.GetValueOrDefault(product.Id.Value)));
        }

        return results;
    }

    public async Task<IReadOnlyDictionary<Guid, CatalogProductOfferingResult>> ResolveForBranchAsync(
        Domain.Customers.PosOrganizationId organizationId,
        PosBranchId actingBranchId,
        IReadOnlyList<CatalogProduct> products,
        CancellationToken cancellationToken = default)
    {
        if (products.Count == 0)
        {
            return new Dictionary<Guid, CatalogProductOfferingResult>();
        }

        var rows = await _availability
            .ListByProductIdsAsync(organizationId, actingBranchId, products.Select(p => p.Id).ToList(), cancellationToken)
            .ConfigureAwait(false);
        return Resolve(actingBranchId, products, rows).ToDictionary(r => r.ProductId.Value);
    }

    public static CatalogProductOfferingResult ResolveOne(
        PosBranchId actingBranchId,
        CatalogProduct product,
        BranchProductAvailability? explicitRow)
    {
        if (product.Scope == CatalogProductScope.BranchLocal)
        {
            if (product.OriginBranchId is null
                || product.OriginBranchId.Value != actingBranchId.Value)
            {
                return new CatalogProductOfferingResult(
                    product.Id,
                    false,
                    CatalogProductOfferingReason.BranchLocalForeignBranch);
            }

            if (explicitRow is { IsOffered: false })
            {
                return new CatalogProductOfferingResult(
                    product.Id,
                    false,
                    CatalogProductOfferingReason.BranchLocalOriginNotOffered);
            }

            return new CatalogProductOfferingResult(
                product.Id,
                true,
                CatalogProductOfferingReason.BranchLocalOrigin);
        }

        // OrganizationStandard — sparse override
        if (explicitRow is null)
        {
            return new CatalogProductOfferingResult(
                product.Id,
                true,
                CatalogProductOfferingReason.DefaultOrganizationStandard);
        }

        return explicitRow.IsOffered
            ? new CatalogProductOfferingResult(
                product.Id,
                true,
                CatalogProductOfferingReason.ExplicitlyOffered)
            : new CatalogProductOfferingResult(
                product.Id,
                false,
                CatalogProductOfferingReason.ExplicitlyNotOffered);
    }
}

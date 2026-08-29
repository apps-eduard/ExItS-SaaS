using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Application.Returns;
using ExItS.PinoyBusinessPOS.Application.Sales;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.UnitTests.TestDoubles;

internal static class RepositoryStubExtensions
{
    public static async Task<IReadOnlyDictionary<Guid, decimal?>> ResolveLatestAcquisitionUnitCostsAsync(
        this IInventoryRepository inventory,
        PosOrganizationId organizationId,
        IReadOnlyCollection<CatalogProductId> productIds,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<Guid, decimal?>();
        foreach (var productId in productIds)
        {
            var cost = await inventory
                .GetLatestAcquisitionUnitCostAsync(organizationId, productId, cancellationToken)
                .ConfigureAwait(false);
            if (cost is not null)
            {
                result[productId.Value] = cost;
            }
        }

        return result;
    }

    public static Task<SaleCostPeriodAggregate> EmptySaleCostPeriodAggregate() =>
        Task.FromResult(new SaleCostPeriodAggregate(0, 0, 0, 0, 0m));

    public static Task<InventoryDocumentCostPeriodAggregate> EmptyInventoryDocumentCostPeriodAggregate() =>
        Task.FromResult(new InventoryDocumentCostPeriodAggregate(0m, 0, 0, 0, 0));

    public static Task<SaleReturnCogsPeriodAggregate> EmptySaleReturnCogsPeriodAggregate() =>
        Task.FromResult(new SaleReturnCogsPeriodAggregate(0m, false));

    public static Task<decimal> ZeroRefundsAsync() => Task.FromResult(0m);
}

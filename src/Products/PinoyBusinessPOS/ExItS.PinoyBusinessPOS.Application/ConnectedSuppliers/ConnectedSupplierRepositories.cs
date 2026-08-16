using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;

namespace ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;

public interface IConnectedSupplierRelationshipRepository
{
    Task<ConnectedSupplierRelationship?> GetAsync(ConnectedSupplierRelationshipId id, CancellationToken ct = default);
    Task<ConnectedSupplierRelationship?> FindOpenAsync(PosOrganizationId buyer, PosOrganizationId supplier, CancellationToken ct = default);
    Task<IReadOnlyList<ConnectedSupplierRelationship>> ListAsync(PosOrganizationId organizationId, bool supplierView, CancellationToken ct = default);
    Task AddAsync(ConnectedSupplierRelationship relationship, CancellationToken ct = default);
    Task UpdateAsync(ConnectedSupplierRelationship relationship, CancellationToken ct = default);
}

public interface ISupplierProductExposureRepository
{
    Task<SupplierProductExposure?> GetAsync(SupplierProductExposureId id, CancellationToken ct = default);
    Task<SupplierProductExposure?> GetByProductAsync(PosOrganizationId supplier, CatalogProductId productId, CancellationToken ct = default);
    Task<IReadOnlyList<SupplierProductExposure>> ListAsync(PosOrganizationId supplier, CancellationToken ct = default);
    Task<(IReadOnlyList<SupplierProductExposure> Items, int Total)> SearchAsync(PosOrganizationId supplier, string? query,
        string? category, int skip, int take, CancellationToken ct = default);
    Task AddAsync(SupplierProductExposure exposure, CancellationToken ct = default);
    Task UpdateAsync(SupplierProductExposure exposure, CancellationToken ct = default);
}

public interface IConnectedBuyerProductShareRepository
{
    Task<ConnectedBuyerProductShare?> GetAsync(ConnectedBuyerProductShareId id, CancellationToken ct = default);
    Task<ConnectedBuyerProductShare?> FindAsync(
        ConnectedSupplierRelationshipId relationshipId,
        CatalogProductId supplierProductId,
        CancellationToken ct = default);
    Task<IReadOnlyList<ConnectedBuyerProductShare>> ListAsync(
        ConnectedSupplierRelationshipId relationshipId,
        CancellationToken ct = default);
    Task<(IReadOnlyList<SupplierProductExposure> Exposures, IReadOnlyList<ConnectedBuyerProductShare> Shares, int Total)>
        SearchSharedCatalogAsync(
            ConnectedSupplierRelationshipId relationshipId,
            PosOrganizationId supplier,
            string? query,
            string? category,
            int skip,
            int take,
            CancellationToken ct = default);
    Task AddAsync(ConnectedBuyerProductShare share, CancellationToken ct = default);
    Task UpdateAsync(ConnectedBuyerProductShare share, CancellationToken ct = default);
}

public interface IBuyerSupplierProductLinkRepository
{
    Task<BuyerSupplierProductLink?> GetAsync(BuyerSupplierProductLinkId id, CancellationToken ct = default);
    Task<BuyerSupplierProductLink?> FindAsync(ConnectedSupplierRelationshipId relationshipId, CatalogProductId buyerProductId, CancellationToken ct = default);
    Task<IReadOnlyList<BuyerSupplierProductLink>> ListAsync(ConnectedSupplierRelationshipId relationshipId, PosOrganizationId buyer, CancellationToken ct = default);
    Task<IReadOnlyList<BuyerSupplierProductLink>> DeltaAsync(ConnectedSupplierRelationshipId relationshipId, PosOrganizationId buyer, long sinceVersion, CancellationToken ct = default);
    Task AddAsync(BuyerSupplierProductLink link, CancellationToken ct = default);
    Task UpdateAsync(BuyerSupplierProductLink link, CancellationToken ct = default);
}

public interface IConnectedPurchaseOrderRepository
{
    Task<ConnectedPurchaseOrder?> GetAsync(ConnectedPurchaseOrderId id, CancellationToken ct = default);
    Task<ConnectedPurchaseOrder?> GetByBuyerPurchaseOrderAsync(PurchaseOrderId id, CancellationToken ct = default);
    Task<IReadOnlyList<ConnectedPurchaseOrder>> ListIncomingAsync(PosOrganizationId supplier, CancellationToken ct = default);
    Task AddAsync(ConnectedPurchaseOrder order, CancellationToken ct = default);
    Task UpdateAsync(ConnectedPurchaseOrder order, CancellationToken ct = default);
}

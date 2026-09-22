using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Returns;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Application.Returns;

public interface IReturnBatchRepository
{
    Task<ReturnBatch?> GetByIdAsync(
        PosOrganizationId organizationId,
        ReturnBatchId returnBatchId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReturnBatch>> ListBySaleIdAsync(
        PosOrganizationId organizationId,
        SaleId saleId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Connected-PO returns visible to the requesting organization (buyer or seller side).
    /// </summary>
    Task<IReadOnlyList<ReturnBatch>> ListByPurchaseOrderIdAsync(
        PosOrganizationId organizationId,
        PurchaseOrderId purchaseOrderId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReturnBatch>> ListByConnectedPurchaseOrderIdAsync(
        PosOrganizationId organizationId,
        ConnectedPurchaseOrderId connectedPurchaseOrderId,
        CancellationToken cancellationToken = default);

    /// <summary>Seller-side inbox of connected-PO returns awaiting receipt or inspection.</summary>
    Task<IReadOnlyList<ReturnBatch>> ListOpenConnectedForSellerAsync(
        PosOrganizationId sellerOrganizationId,
        CancellationToken cancellationToken = default);

    Task<ReturnBatch> CreateAsync(
        PosOrganizationId organizationId,
        DateOnly businessDateUtc,
        Func<string, ReturnBatch> createBatch,
        Func<ReturnBatch, CancellationToken, Task>? afterCreated = null,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(ReturnBatch batch, CancellationToken cancellationToken = default);
}

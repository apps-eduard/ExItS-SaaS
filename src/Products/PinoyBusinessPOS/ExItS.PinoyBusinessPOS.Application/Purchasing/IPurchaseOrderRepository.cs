using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;

namespace ExItS.PinoyBusinessPOS.Application.Purchasing;

public sealed record PurchaseOrderFilter(
    PurchaseOrderStatus? Status = null,
    Guid? SupplierId = null,
    string? PoNumber = null,
    DateOnly? FromOrderDate = null,
    DateOnly? ToOrderDate = null);

public interface IPurchaseOrderRepository
{
    Task<PurchaseOrder?> GetByIdAsync(
        PosOrganizationId organizationId,
        PurchaseOrderId purchaseOrderId,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<PurchaseOrder> Items, int TotalCount)> ListAsync(
        PosOrganizationId organizationId,
        PurchaseOrderFilter filter,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task AddAsync(PurchaseOrder purchaseOrder, CancellationToken cancellationToken = default);

    Task UpdateAsync(PurchaseOrder purchaseOrder, CancellationToken cancellationToken = default);

    Task<PurchaseOrder> SubmitAsync(
        PosOrganizationId organizationId,
        PurchaseOrderId purchaseOrderId,
        DateOnly businessDateUtc,
        Func<string, PurchaseOrder> applySubmit,
        CancellationToken cancellationToken = default);

    Task<(PurchaseOrder PurchaseOrder, GoodsReceipt GoodsReceipt)> ReceiveAsync(
        PosOrganizationId organizationId,
        PurchaseOrderId purchaseOrderId,
        DateOnly businessDateUtc,
        Func<string, (PurchaseOrder UpdatedPo, GoodsReceipt Receipt)> applyReceive,
        Func<GoodsReceipt, PurchaseOrder, CancellationToken, Task>? afterReceiptCreated = null,
        CancellationToken cancellationToken = default);

    Task<GoodsReceipt?> GetGoodsReceiptByIdAsync(
        PosOrganizationId organizationId,
        GoodsReceiptId goodsReceiptId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GoodsReceipt>> ListGoodsReceiptsForPurchaseOrderAsync(
        PosOrganizationId organizationId,
        PurchaseOrderId purchaseOrderId,
        CancellationToken cancellationToken = default);
}

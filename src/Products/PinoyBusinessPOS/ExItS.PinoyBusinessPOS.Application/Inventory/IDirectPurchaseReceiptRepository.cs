using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Inventory;

public interface IDirectPurchaseReceiptRepository
{
    Task<DirectPurchaseReceipt?> GetByIdAsync(
        PosOrganizationId organizationId,
        DirectPurchaseReceiptId receiptId,
        CancellationToken cancellationToken = default);

    Task<DirectPurchaseReceipt?> FindByIdempotencyKeyAsync(
        PosOrganizationId organizationId,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<DirectPurchaseReceipt> Items, int TotalCount)> ListAsync(
        PosOrganizationId organizationId,
        DirectPurchaseReceiptFilter filter,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task AddAsync(DirectPurchaseReceipt receipt, CancellationToken cancellationToken = default);

    Task UpdateAsync(DirectPurchaseReceipt receipt, CancellationToken cancellationToken = default);

    Task<string> AllocateNextNumberAsync(
        PosOrganizationId organizationId,
        DateOnly businessDateUtc,
        CancellationToken cancellationToken = default);
}

public sealed record DirectPurchaseReceiptFilter(
    DateOnly? FromPurchaseDate = null,
    DateOnly? ToPurchaseDate = null,
    Guid? SupplierId = null,
    string? SourceSearch = null,
    string? ReferenceNumber = null);

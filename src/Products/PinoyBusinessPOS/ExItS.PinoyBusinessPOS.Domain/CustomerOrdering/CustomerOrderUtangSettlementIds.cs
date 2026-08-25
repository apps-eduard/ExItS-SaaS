using System.Security.Cryptography;
using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;

/// <summary>
/// Deterministic settlement identifiers for a completed Personal Utang customer order.
/// One customer order maps to exactly one Utang sale and one credit entry.
/// </summary>
public static class CustomerOrderUtangSettlementIds
{
    private static readonly Guid SaleNamespace = Guid.Parse("7c9e6679-7425-40de-944b-e07fc1f90ae7");
    private static readonly Guid CreditNamespace = Guid.Parse("6ba7b811-9dad-11d1-80b4-00c04fd430c8");

    public static SaleId SaleIdForOrder(CustomerOrderId orderId) =>
        SaleId.From(CreateDeterministicGuid(SaleNamespace, orderId.Value));

    public static CreditEntryId CreditEntryIdForOrder(CustomerOrderId orderId) =>
        CreditEntryId.From(CreateDeterministicGuid(CreditNamespace, orderId.Value));

    private static Guid CreateDeterministicGuid(Guid namespaceId, Guid name)
    {
        Span<byte> data = stackalloc byte[32];
        namespaceId.TryWriteBytes(data[..16]);
        name.TryWriteBytes(data[16..]);
        var hash = SHA256.HashData(data);
        return new Guid(hash.AsSpan(0, 16));
    }
}

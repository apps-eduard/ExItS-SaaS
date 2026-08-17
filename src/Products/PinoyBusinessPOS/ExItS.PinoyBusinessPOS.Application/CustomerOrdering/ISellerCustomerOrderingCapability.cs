namespace ExItS.PinoyBusinessPOS.Application.CustomerOrdering;

/// <summary>
/// Authoritative seller capability for customer storefront and place.
/// Fail-closed when the seller cannot accept customer orders.
/// </summary>
public sealed record SellerCustomerOrderingCapability(
    Guid OrganizationId,
    bool CanCustomerOrder,
    bool CanCustomerDelivery,
    string? OrganizationDisplayName = null);

public interface ISellerCustomerOrderingCapability
{
    Task<SellerCustomerOrderingCapability> ResolveAsync(
        Guid sellerOrganizationId,
        CancellationToken cancellationToken = default);
}

/// <summary>Test double / fallback that allows ordering and delivery.</summary>
public sealed class AllowAllSellerCustomerOrderingCapability : ISellerCustomerOrderingCapability
{
    public Task<SellerCustomerOrderingCapability> ResolveAsync(
        Guid sellerOrganizationId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new SellerCustomerOrderingCapability(
            sellerOrganizationId,
            CanCustomerOrder: true,
            CanCustomerDelivery: true));
}

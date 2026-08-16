namespace ExItS.PinoyBusinessPOS.Application.CustomerOrdering;

/// <summary>
/// Fulfillment branch snapshot for customer-order place/quote. Faked in unit tests;
/// production loads from Platform branches.
/// </summary>
public sealed record CustomerOrderBranchSnapshot(
    Guid BranchId,
    string Name,
    bool PickupEnabled,
    bool DeliveryEnabled,
    decimal? Latitude,
    decimal? Longitude,
    CustomerOrderBranchDeliveryPolicySnapshot? DeliveryPolicy);

public sealed record CustomerOrderBranchDeliveryPolicySnapshot(
    decimal MinimumOrderAmount,
    decimal BaseDeliveryFee,
    decimal IncludedDistanceKm,
    decimal AdditionalFeePerKm,
    decimal MaximumDeliveryDistanceKm,
    decimal? FreeDeliveryThreshold);

public interface ICustomerOrderBranchDirectory
{
    Task<CustomerOrderBranchSnapshot?> GetBranchAsync(
        Guid sellerOrganizationId,
        Guid branchId,
        CancellationToken cancellationToken = default);
}

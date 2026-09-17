namespace ExItS.PinoyBusinessPOS.Application.CustomerOrdering;

public sealed record CustomerOrderDeliveryServiceAreaSnapshot(
    Guid Id,
    string CityMunicipalityName,
    string? RegionOrProvinceName);

/// <summary>
/// Fulfillment branch snapshot for customer-order place/quote. Faked in unit tests;
/// production loads from Platform branches.
/// </summary>
public sealed record CustomerOrderBranchSnapshot(
    Guid BranchId,
    string Name,
    bool CustomerOrderingEnabled,
    bool PickupEnabled,
    bool DeliveryEnabled,
    bool CustomerOrderingOperational,
    bool PickupOperational,
    bool DeliveryOperational,
    bool OnlineOrdersPaused,
    string? StoreStatusMessage,
    decimal? Latitude,
    decimal? Longitude,
    CustomerOrderBranchDeliveryPolicySnapshot? DeliveryPolicy,
    bool IsPrimary = false,
    IReadOnlyList<CustomerOrderDeliveryServiceAreaSnapshot>? DeliveryServiceAreas = null,
    /// <summary>Setup complete for pickup (independent of store open-now / pause).</summary>
    bool PickupReady = false,
    /// <summary>Setup complete for delivery (independent of store open-now / pause).</summary>
    bool DeliveryReady = false);

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

    /// <summary>
    /// Loads a seller branch for connected-supplier commerce readiness.
    /// Default: same as <see cref="GetBranchAsync"/>. Implementations that serve connected
    /// buyers should fall back when the caller is not a seller-org member.
    /// </summary>
    Task<CustomerOrderBranchSnapshot?> GetCommerceBranchAsync(
        Guid sellerOrganizationId,
        Guid branchId,
        string? sellerPublicOrganizationId,
        CancellationToken cancellationToken = default) =>
        GetBranchAsync(sellerOrganizationId, branchId, cancellationToken);

    /// <summary>Active fulfillment branches for the seller organization (customer storefront).</summary>
    Task<IReadOnlyList<CustomerOrderBranchSnapshot>> ListBranchesAsync(
        Guid sellerOrganizationId,
        CancellationToken cancellationToken = default);
}

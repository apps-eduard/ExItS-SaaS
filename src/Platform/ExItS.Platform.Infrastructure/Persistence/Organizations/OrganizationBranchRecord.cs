namespace ExItS.Platform.Infrastructure.Persistence.Organizations;

internal sealed class OrganizationBranchRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? Region { get; set; }
    public string? PostalCode { get; set; }
    public string? CountryCode { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public bool PickupEnabled { get; set; }
    public bool DeliveryEnabled { get; set; }
    public bool CustomerOrderingEnabled { get; set; }
    public string? ContactPhone { get; set; }
    public string? TimeZoneId { get; set; }
    public bool OnlineOrdersPaused { get; set; }
    public string? OnlineOrdersPauseReason { get; set; }
    public bool IsPrimary { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset? SuspendedAtUtc { get; set; }
    public Guid? SuspendedByUserId { get; set; }
    public string? SuspensionReason { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

internal sealed class BranchDeliveryPolicyRecord
{
    public Guid BranchId { get; set; }
    public Guid OrganizationId { get; set; }
    public decimal MinimumOrderAmount { get; set; }
    public decimal BaseDeliveryFee { get; set; }
    public decimal IncludedDistanceKm { get; set; }
    public decimal AdditionalFeePerKm { get; set; }
    public decimal MaximumDeliveryDistanceKm { get; set; }
    public decimal? FreeDeliveryThreshold { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

internal sealed class BranchDeliveryServiceAreaRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid BranchId { get; set; }
    public string CountryCode { get; set; } = string.Empty;
    public string? RegionOrProvinceName { get; set; }
    public string CityMunicipalityName { get; set; } = string.Empty;
    public string NormalizedCityMunicipalityName { get; set; } = string.Empty;
    public string? ExternalAreaCode { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

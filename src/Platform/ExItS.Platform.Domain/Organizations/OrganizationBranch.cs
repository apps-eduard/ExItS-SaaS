using System.Text.RegularExpressions;
using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.Organizations;

public enum OrganizationBranchStatus
{
    Active,
    Inactive,
    Archived
}

/// <summary>
/// Authoritative physical operational location for an organization.
/// Delivery fee policy is stored separately on <see cref="BranchDeliveryPolicy"/>.
/// </summary>
public sealed class OrganizationBranch
{
    private static readonly Regex CodePattern = new("^[A-Z0-9][A-Z0-9-]{0,30}[A-Z0-9]$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public OrganizationBranchId Id { get; }
    public PlatformOrganizationId OrganizationId { get; }
    public string Code { get; }
    public string Name { get; private set; }
    public string? AddressLine1 { get; private set; }
    public string? AddressLine2 { get; private set; }
    public string? City { get; private set; }
    public string? Region { get; private set; }
    public string? PostalCode { get; private set; }
    public string? CountryCode { get; private set; }
    public decimal? Latitude { get; private set; }
    public decimal? Longitude { get; private set; }
    public bool PickupEnabled { get; private set; }
    public bool DeliveryEnabled { get; private set; }
    public bool CustomerOrderingEnabled { get; private set; }
    public string? ContactPhone { get; private set; }
    public string? TimeZoneId { get; private set; }
    public bool OnlineOrdersPaused { get; private set; }
    public OnlineOrdersPauseReason? PauseReason { get; private set; }
    public bool IsPrimary { get; }
    public OrganizationBranchStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public bool HasValidFulfillmentCoordinates =>
        Latitude is not null && Longitude is not null
        && IsValidLatitude(Latitude.Value) && IsValidLongitude(Longitude.Value);

    public bool HasCompleteStructuredAddress =>
        !string.IsNullOrWhiteSpace(AddressLine1)
        && !string.IsNullOrWhiteSpace(City)
        && !string.IsNullOrWhiteSpace(CountryCode);

    public bool CanOfferPickup =>
        Status == OrganizationBranchStatus.Active
        && CustomerOrderingEnabled
        && PickupEnabled;

    public bool CanOfferDeliveryLocation =>
        Status == OrganizationBranchStatus.Active
        && CustomerOrderingEnabled
        && DeliveryEnabled
        && HasValidFulfillmentCoordinates;

    private OrganizationBranch(
        OrganizationBranchId id,
        PlatformOrganizationId organizationId,
        string code,
        string name,
        string? addressLine1,
        string? addressLine2,
        string? city,
        string? region,
        string? postalCode,
        string? countryCode,
        decimal? latitude,
        decimal? longitude,
        bool pickupEnabled,
        bool deliveryEnabled,
        bool customerOrderingEnabled,
        string? contactPhone,
        string? timeZoneId,
        bool onlineOrdersPaused,
        OnlineOrdersPauseReason? onlineOrdersPauseReason,
        bool isPrimary,
        OrganizationBranchStatus status,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        Code = code;
        Name = name;
        AddressLine1 = addressLine1;
        AddressLine2 = addressLine2;
        City = city;
        Region = region;
        PostalCode = postalCode;
        CountryCode = countryCode;
        Latitude = latitude;
        Longitude = longitude;
        PickupEnabled = pickupEnabled;
        DeliveryEnabled = deliveryEnabled;
        CustomerOrderingEnabled = customerOrderingEnabled;
        ContactPhone = contactPhone;
        TimeZoneId = timeZoneId;
        OnlineOrdersPaused = onlineOrdersPaused;
        PauseReason = onlineOrdersPauseReason;
        IsPrimary = isPrimary;
        Status = status;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public static OrganizationBranch CreateMainBranch(PlatformOrganizationId organizationId, DateTimeOffset utcNow) =>
        CreateInternal(
            organizationId, "MAIN", "Main Branch",
            null, null, null, null, null, null,
            latitude: null, longitude: null,
            pickupEnabled: false, deliveryEnabled: false, customerOrderingEnabled: false,
            contactPhone: null, timeZoneId: null, onlineOrdersPaused: false, onlineOrdersPauseReason: null,
            isPrimary: true, OrganizationBranchStatus.Active, utcNow, id: null);

    public static OrganizationBranch Create(
        PlatformOrganizationId organizationId,
        string code,
        string name,
        DateTimeOffset utcNow,
        string? addressLine1 = null,
        string? addressLine2 = null,
        string? city = null,
        string? region = null,
        string? postalCode = null,
        string? countryCode = null,
        decimal? latitude = null,
        decimal? longitude = null,
        bool pickupEnabled = false,
        bool deliveryEnabled = false,
        bool customerOrderingEnabled = false,
        OrganizationBranchId? id = null) =>
        CreateInternal(
            organizationId, code, name,
            addressLine1, addressLine2, city, region, postalCode, countryCode,
            latitude, longitude, pickupEnabled, deliveryEnabled, customerOrderingEnabled,
            contactPhone: null, timeZoneId: null, onlineOrdersPaused: false, onlineOrdersPauseReason: null,
            isPrimary: false, OrganizationBranchStatus.Active, utcNow, id);

    internal static OrganizationBranch Rehydrate(
        OrganizationBranchId id,
        PlatformOrganizationId organizationId,
        string code,
        string name,
        string? addressLine1,
        string? addressLine2,
        string? city,
        string? region,
        string? postalCode,
        string? countryCode,
        bool isPrimary,
        OrganizationBranchStatus status,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        decimal? latitude = null,
        decimal? longitude = null,
        bool pickupEnabled = false,
        bool deliveryEnabled = false,
        bool customerOrderingEnabled = false,
        string? contactPhone = null,
        string? timeZoneId = null,
        bool onlineOrdersPaused = false,
        OnlineOrdersPauseReason? onlineOrdersPauseReason = null) =>
        new(
            id,
            organizationId,
            NormalizeCode(code),
            NormalizeName(name),
            NormalizeOptional(addressLine1, 200),
            NormalizeOptional(addressLine2, 200),
            NormalizeOptional(city, 100),
            NormalizeOptional(region, 100),
            NormalizeOptional(postalCode, 32),
            NormalizeCountryCode(countryCode),
            latitude,
            longitude,
            pickupEnabled,
            deliveryEnabled,
            customerOrderingEnabled,
            NormalizeContactPhone(contactPhone),
            NormalizeTimeZone(timeZoneId),
            onlineOrdersPaused,
            onlineOrdersPauseReason,
            isPrimary,
            status,
            createdAtUtc,
            updatedAtUtc);

    public void Rename(string name, DateTimeOffset utcNow)
    {
        EnsureMutable(utcNow);
        Name = NormalizeName(name);
        UpdatedAtUtc = utcNow;
    }

    public void UpdateAddress(
        string? addressLine1,
        string? addressLine2,
        string? city,
        string? region,
        string? postalCode,
        string? countryCode,
        DateTimeOffset utcNow)
    {
        EnsureMutable(utcNow);
        AddressLine1 = NormalizeOptional(addressLine1, 200);
        AddressLine2 = NormalizeOptional(addressLine2, 200);
        City = NormalizeOptional(city, 100);
        Region = NormalizeOptional(region, 100);
        PostalCode = NormalizeOptional(postalCode, 32);
        CountryCode = NormalizeCountryCode(countryCode);
        UpdatedAtUtc = utcNow;
    }

    public void UpdateCoordinates(decimal? latitude, decimal? longitude, DateTimeOffset utcNow)
    {
        EnsureMutable(utcNow);
        if (latitude is null && longitude is null)
        {
            if (DeliveryEnabled)
            {
                throw new DomainException(
                    DomainErrorCodes.OrganizationBranchDeliveryLocationRequired,
                    "Delivery requires valid latitude and longitude. Disable delivery before clearing coordinates.");
            }

            Latitude = null;
            Longitude = null;
            UpdatedAtUtc = utcNow;
            return;
        }

        if (latitude is null || longitude is null)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidOrganizationBranchCoordinates,
                "Latitude and longitude must both be provided.");
        }

        if (!IsValidLatitude(latitude.Value) || !IsValidLongitude(longitude.Value))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidOrganizationBranchCoordinates,
                "Latitude must be between -90 and 90; longitude must be between -180 and 180.");
        }

        Latitude = RoundCoordinate(latitude.Value);
        Longitude = RoundCoordinate(longitude.Value);
        UpdatedAtUtc = utcNow;
    }

    public void SetFulfillmentCapabilities(bool pickupEnabled, bool deliveryEnabled, DateTimeOffset utcNow)
    {
        EnsureMutable(utcNow);
        if (deliveryEnabled && !HasValidFulfillmentCoordinates)
        {
            throw new DomainException(
                DomainErrorCodes.OrganizationBranchDeliveryLocationRequired,
                "Enable delivery only after setting valid branch coordinates.");
        }

        PickupEnabled = pickupEnabled;
        DeliveryEnabled = deliveryEnabled;
        UpdatedAtUtc = utcNow;
    }

    public void SetCustomerOrderingEnabled(bool enabled, DateTimeOffset utcNow)
    {
        EnsureMutable(utcNow);
        CustomerOrderingEnabled = enabled;
        if (!enabled)
        {
            PickupEnabled = false;
            DeliveryEnabled = false;
        }

        UpdatedAtUtc = utcNow;
    }

    public void UpdateContactPhone(string? contactPhone, DateTimeOffset utcNow)
    {
        EnsureMutable(utcNow);
        ContactPhone = NormalizeContactPhone(contactPhone);
        UpdatedAtUtc = utcNow;
    }

    public void UpdateTimeZone(string? timeZoneId, DateTimeOffset utcNow)
    {
        EnsureMutable(utcNow);
        TimeZoneId = NormalizeTimeZone(timeZoneId);
        UpdatedAtUtc = utcNow;
    }

    public void SetOnlineOrdersPaused(bool paused, OnlineOrdersPauseReason? reason, DateTimeOffset utcNow)
    {
        EnsureMutable(utcNow);
        OnlineOrdersPaused = paused;
        PauseReason = paused ? reason ?? OnlineOrdersPauseReason.Other : null;
        UpdatedAtUtc = utcNow;
    }

    public void Activate(DateTimeOffset utcNow) => TransitionTo(OrganizationBranchStatus.Active, utcNow);
    public void Deactivate(DateTimeOffset utcNow) => TransitionTo(OrganizationBranchStatus.Inactive, utcNow);
    public void Archive(DateTimeOffset utcNow) => TransitionTo(OrganizationBranchStatus.Archived, utcNow);

    public void EnsureActive()
    {
        if (Status != OrganizationBranchStatus.Active)
        {
            throw new DomainException(DomainErrorCodes.OrganizationBranchNotActive, "Organization branch is not active.");
        }
    }

    public void EnsureUsableForNewFulfillment()
    {
        if (Status == OrganizationBranchStatus.Archived)
        {
            throw new DomainException(
                DomainErrorCodes.OrganizationBranchNotActive,
                "An archived branch cannot be used for new fulfillment.");
        }

        EnsureActive();
    }

    public static bool IsValidLatitude(decimal latitude) => latitude is >= -90m and <= 90m;
    public static bool IsValidLongitude(decimal longitude) => longitude is >= -180m and <= 180m;

    public static string NormalizeCode(string code)
    {
        var normalized = code?.Trim().ToUpperInvariant() ?? string.Empty;
        if (normalized.Length is < 2 or > 32 || !CodePattern.IsMatch(normalized))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidOrganizationBranchCode,
                "Branch code must be 2–32 uppercase alphanumeric characters or hyphens.");
        }

        return normalized;
    }

    private static OrganizationBranch CreateInternal(
        PlatformOrganizationId organizationId,
        string code,
        string name,
        string? addressLine1,
        string? addressLine2,
        string? city,
        string? region,
        string? postalCode,
        string? countryCode,
        decimal? latitude,
        decimal? longitude,
        bool pickupEnabled,
        bool deliveryEnabled,
        bool customerOrderingEnabled,
        string? contactPhone,
        string? timeZoneId,
        bool onlineOrdersPaused,
        OnlineOrdersPauseReason? onlineOrdersPauseReason,
        bool isPrimary,
        OrganizationBranchStatus status,
        DateTimeOffset utcNow,
        OrganizationBranchId? id)
    {
        ArgumentNullException.ThrowIfNull(organizationId);
        DomainTime.EnsureUtc(utcNow);

        decimal? lat = null;
        decimal? lng = null;
        if (latitude is not null || longitude is not null)
        {
            if (latitude is null || longitude is null
                || !IsValidLatitude(latitude.Value) || !IsValidLongitude(longitude.Value))
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidOrganizationBranchCoordinates,
                    "Latitude must be between -90 and 90; longitude must be between -180 and 180.");
            }

            lat = RoundCoordinate(latitude.Value);
            lng = RoundCoordinate(longitude.Value);
        }

        if (deliveryEnabled && (lat is null || lng is null))
        {
            throw new DomainException(
                DomainErrorCodes.OrganizationBranchDeliveryLocationRequired,
                "Enable delivery only after setting valid branch coordinates.");
        }

        return new(
            id ?? OrganizationBranchId.New(),
            organizationId,
            NormalizeCode(code),
            NormalizeName(name),
            NormalizeOptional(addressLine1, 200),
            NormalizeOptional(addressLine2, 200),
            NormalizeOptional(city, 100),
            NormalizeOptional(region, 100),
            NormalizeOptional(postalCode, 32),
            NormalizeCountryCode(countryCode),
            lat,
            lng,
            pickupEnabled,
            deliveryEnabled,
            customerOrderingEnabled,
            NormalizeContactPhone(contactPhone),
            NormalizeTimeZone(timeZoneId),
            onlineOrdersPaused,
            onlineOrdersPauseReason,
            isPrimary,
            status,
            utcNow,
            utcNow);
    }

    private void EnsureMutable(DateTimeOffset utcNow)
    {
        DomainTime.EnsureUtc(utcNow);
        if (Status == OrganizationBranchStatus.Archived)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidOrganizationBranchStatusTransition,
                "An archived branch cannot be changed.");
        }
    }

    private void TransitionTo(OrganizationBranchStatus target, DateTimeOffset utcNow)
    {
        EnsureMutable(utcNow);
        if (Status != target)
        {
            Status = target;
            UpdatedAtUtc = utcNow;
        }
    }

    private static string NormalizeName(string value) => DomainTime.NormalizeDisplayName(value);

    private static string? NormalizeOptional(string? value, int maximum) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim()[..Math.Min(value.Trim().Length, maximum)];

    private static string? NormalizeCountryCode(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim().ToUpperInvariant() is var normalized
              && normalized.Length == 2
              && normalized.All(char.IsLetter)
                ? normalized
                : throw new DomainException(
                    DomainErrorCodes.InvalidOrganizationBranchCode,
                    "Country code must be a two-letter ISO code.");

    private static string? NormalizeContactPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return null;
        }

        var trimmed = phone.Trim();
        if (trimmed.Length is < 7 or > 32)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidOrganizationProfile,
                "Contact phone format is invalid.");
        }

        return trimmed;
    }

    private static string? NormalizeTimeZone(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return null;
        }

        var trimmed = timeZoneId.Trim();
        if (!TimeZoneInfo.TryFindSystemTimeZoneById(trimmed, out _))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidOrganizationProfile,
                "Time zone is not recognized.");
        }

        return trimmed;
    }

    private static decimal RoundCoordinate(decimal value) =>
        Math.Round(value, 7, MidpointRounding.AwayFromZero);
}

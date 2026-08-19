using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Infrastructure.Persistence.Organizations;

namespace ExItS.Platform.Infrastructure.Persistence;

internal static class OrganizationBranchDeviceEntityMapper
{
    public static OrganizationBranch ToDomain(OrganizationBranchRecord record) =>
        OrganizationBranch.Rehydrate(
            OrganizationBranchId.From(record.Id),
            PlatformOrganizationId.From(record.OrganizationId),
            record.Code,
            record.Name,
            record.AddressLine1,
            record.AddressLine2,
            record.City,
            record.Region,
            record.PostalCode,
            record.CountryCode,
            record.IsPrimary,
            Enum.Parse<OrganizationBranchStatus>(record.Status),
            record.CreatedAtUtc,
            record.UpdatedAtUtc,
            record.Latitude,
            record.Longitude,
            record.PickupEnabled,
            record.DeliveryEnabled,
            record.CustomerOrderingEnabled,
            record.ContactPhone,
            record.TimeZoneId,
            record.OnlineOrdersPaused,
            string.IsNullOrWhiteSpace(record.OnlineOrdersPauseReason)
                ? null
                : Enum.Parse<OnlineOrdersPauseReason>(record.OnlineOrdersPauseReason),
            record.SuspendedAtUtc,
            record.SuspendedByUserId is Guid suspendedBy ? PlatformUserId.From(suspendedBy) : null,
            record.SuspensionReason);

    public static OrganizationBranchRecord ToRecord(OrganizationBranch branch) => new()
    {
        Id = branch.Id.Value,
        OrganizationId = branch.OrganizationId.Value,
        Code = branch.Code,
        Name = branch.Name,
        AddressLine1 = branch.AddressLine1,
        AddressLine2 = branch.AddressLine2,
        City = branch.City,
        Region = branch.Region,
        PostalCode = branch.PostalCode,
        CountryCode = branch.CountryCode,
        Latitude = branch.Latitude,
        Longitude = branch.Longitude,
        PickupEnabled = branch.PickupEnabled,
        DeliveryEnabled = branch.DeliveryEnabled,
        CustomerOrderingEnabled = branch.CustomerOrderingEnabled,
        ContactPhone = branch.ContactPhone,
        TimeZoneId = branch.TimeZoneId,
        OnlineOrdersPaused = branch.OnlineOrdersPaused,
        OnlineOrdersPauseReason = branch.PauseReason?.ToString(),
        IsPrimary = branch.IsPrimary,
        Status = branch.Status.ToString(),
        SuspendedAtUtc = branch.SuspendedAtUtc,
        SuspendedByUserId = branch.SuspendedByUserId?.Value,
        SuspensionReason = branch.SuspensionReason,
        CreatedAtUtc = branch.CreatedAtUtc,
        UpdatedAtUtc = branch.UpdatedAtUtc
    };

    public static void ApplyToRecord(OrganizationBranch branch, OrganizationBranchRecord record)
    {
        record.Name = branch.Name;
        record.AddressLine1 = branch.AddressLine1;
        record.AddressLine2 = branch.AddressLine2;
        record.City = branch.City;
        record.Region = branch.Region;
        record.PostalCode = branch.PostalCode;
        record.CountryCode = branch.CountryCode;
        record.Latitude = branch.Latitude;
        record.Longitude = branch.Longitude;
        record.PickupEnabled = branch.PickupEnabled;
        record.DeliveryEnabled = branch.DeliveryEnabled;
        record.CustomerOrderingEnabled = branch.CustomerOrderingEnabled;
        record.ContactPhone = branch.ContactPhone;
        record.TimeZoneId = branch.TimeZoneId;
        record.OnlineOrdersPaused = branch.OnlineOrdersPaused;
        record.OnlineOrdersPauseReason = branch.PauseReason?.ToString();
        record.Status = branch.Status.ToString();
        record.SuspendedAtUtc = branch.SuspendedAtUtc;
        record.SuspendedByUserId = branch.SuspendedByUserId?.Value;
        record.SuspensionReason = branch.SuspensionReason;
        record.UpdatedAtUtc = branch.UpdatedAtUtc;
    }

    public static BranchDeliveryPolicy ToDomain(BranchDeliveryPolicyRecord record) =>
        BranchDeliveryPolicy.Rehydrate(
            OrganizationBranchId.From(record.BranchId),
            PlatformOrganizationId.From(record.OrganizationId),
            record.MinimumOrderAmount,
            record.BaseDeliveryFee,
            record.IncludedDistanceKm,
            record.AdditionalFeePerKm,
            record.MaximumDeliveryDistanceKm,
            record.FreeDeliveryThreshold,
            record.CreatedAtUtc,
            record.UpdatedAtUtc);

    public static BranchDeliveryPolicyRecord ToRecord(BranchDeliveryPolicy policy) => new()
    {
        BranchId = policy.BranchId.Value,
        OrganizationId = policy.OrganizationId.Value,
        MinimumOrderAmount = policy.MinimumOrderAmount,
        BaseDeliveryFee = policy.BaseDeliveryFee,
        IncludedDistanceKm = policy.IncludedDistanceKm,
        AdditionalFeePerKm = policy.AdditionalFeePerKm,
        MaximumDeliveryDistanceKm = policy.MaximumDeliveryDistanceKm,
        FreeDeliveryThreshold = policy.FreeDeliveryThreshold,
        CreatedAtUtc = policy.CreatedAtUtc,
        UpdatedAtUtc = policy.UpdatedAtUtc
    };

    public static void ApplyToRecord(BranchDeliveryPolicy policy, BranchDeliveryPolicyRecord record)
    {
        record.MinimumOrderAmount = policy.MinimumOrderAmount;
        record.BaseDeliveryFee = policy.BaseDeliveryFee;
        record.IncludedDistanceKm = policy.IncludedDistanceKm;
        record.AdditionalFeePerKm = policy.AdditionalFeePerKm;
        record.MaximumDeliveryDistanceKm = policy.MaximumDeliveryDistanceKm;
        record.FreeDeliveryThreshold = policy.FreeDeliveryThreshold;
        record.UpdatedAtUtc = policy.UpdatedAtUtc;
    }

    public static PosDevice ToDomain(PosDeviceRecord record) =>
        PosDevice.Rehydrate(PosDeviceId.From(record.Id), PlatformOrganizationId.From(record.OrganizationId), OrganizationBranchId.From(record.BranchId),
            record.InstallationDeviceId, record.FriendlyName, record.Platform, record.Model, record.AppVersion, Enum.Parse<PosDeviceStatus>(record.Status),
            record.RegisteredAtUtc, record.LastSeenAtUtc, record.RevokedAtUtc, record.RevokedByUserId is null ? null : PlatformUserId.From(record.RevokedByUserId.Value));

    public static PosDeviceRecord ToRecord(PosDevice device) => new()
    {
        Id = device.Id.Value, OrganizationId = device.OrganizationId.Value, BranchId = device.BranchId.Value, InstallationDeviceId = device.InstallationDeviceId,
        FriendlyName = device.FriendlyName, Platform = device.Platform, Model = device.Model, AppVersion = device.AppVersion, Status = device.Status.ToString(),
        RegisteredAtUtc = device.RegisteredAtUtc, LastSeenAtUtc = device.LastSeenAtUtc, RevokedAtUtc = device.RevokedAtUtc, RevokedByUserId = device.RevokedByUserId?.Value
    };

    public static void ApplyToRecord(PosDevice device, PosDeviceRecord record)
    {
        record.FriendlyName = device.FriendlyName; record.Platform = device.Platform; record.Model = device.Model; record.AppVersion = device.AppVersion;
        record.Status = device.Status.ToString(); record.LastSeenAtUtc = device.LastSeenAtUtc; record.RevokedAtUtc = device.RevokedAtUtc; record.RevokedByUserId = device.RevokedByUserId?.Value;
    }
}

using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Infrastructure.Persistence.Organizations;

namespace ExItS.Platform.Infrastructure.Persistence;

internal static class OrganizationBranchDeviceEntityMapper
{
    public static OrganizationBranch ToDomain(OrganizationBranchRecord record) =>
        OrganizationBranch.Rehydrate(OrganizationBranchId.From(record.Id), PlatformOrganizationId.From(record.OrganizationId), record.Code, record.Name,
            record.AddressLine1, record.AddressLine2, record.City, record.Region, record.PostalCode, record.CountryCode, record.IsPrimary,
            Enum.Parse<OrganizationBranchStatus>(record.Status), record.CreatedAtUtc, record.UpdatedAtUtc);

    public static OrganizationBranchRecord ToRecord(OrganizationBranch branch) => new()
    {
        Id = branch.Id.Value, OrganizationId = branch.OrganizationId.Value, Code = branch.Code, Name = branch.Name,
        AddressLine1 = branch.AddressLine1, AddressLine2 = branch.AddressLine2, City = branch.City, Region = branch.Region,
        PostalCode = branch.PostalCode, CountryCode = branch.CountryCode, IsPrimary = branch.IsPrimary, Status = branch.Status.ToString(),
        CreatedAtUtc = branch.CreatedAtUtc, UpdatedAtUtc = branch.UpdatedAtUtc
    };

    public static void ApplyToRecord(OrganizationBranch branch, OrganizationBranchRecord record)
    {
        record.Name = branch.Name; record.AddressLine1 = branch.AddressLine1; record.AddressLine2 = branch.AddressLine2; record.City = branch.City;
        record.Region = branch.Region; record.PostalCode = branch.PostalCode; record.CountryCode = branch.CountryCode; record.Status = branch.Status.ToString();
        record.UpdatedAtUtc = branch.UpdatedAtUtc;
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

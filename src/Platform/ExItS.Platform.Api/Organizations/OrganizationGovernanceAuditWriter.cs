using ExItS.Platform.Api.Common;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Api.Organizations;

internal static class OrganizationGovernanceAuditWriter
{
    public static Task WriteBranchAsync(
        PlatformOrganizationAuthz authz,
        string actionCode,
        OrganizationBranchDto branch,
        Guid organizationId,
        string summary,
        CancellationToken cancellationToken) =>
        authz.Inner.AuditSucceededAsync(
            actionCode,
            nameof(OrganizationBranch),
            branch.Id.ToString("D"),
            organizationId,
            summary: summary,
            cancellationToken: cancellationToken);

    public static Task WriteDeviceAsync(
        PlatformOrganizationAuthz authz,
        string actionCode,
        PosDeviceDto device,
        Guid organizationId,
        string summary,
        CancellationToken cancellationToken) =>
        authz.Inner.AuditSucceededAsync(
            actionCode,
            nameof(PosDevice),
            device.Id.ToString("D"),
            organizationId,
            summary: summary,
            cancellationToken: cancellationToken);

    public static string BranchSummary(OrganizationBranchDto branch, string verb) =>
        $"{verb} branch {branch.Code} ({branch.Name}).";

    public static string DeviceSummary(PosDeviceDto device, string verb) =>
        $"{verb} device {device.FriendlyName} at branch {device.BranchId:D}.";

    public static string BranchConfigSummary(Guid branchId, string change) =>
        $"{change} for branch {branchId:D}.";

    public static string PauseSummary(Guid branchId, bool paused, string? reason)
    {
        var state = paused ? "Paused" : "Resumed";
        return string.IsNullOrWhiteSpace(reason)
            ? $"{state} online orders for branch {branchId:D}."
            : $"{state} online orders for branch {branchId:D}: {Sanitize(reason)}.";
    }

    private static string Sanitize(string value)
    {
        var trimmed = value.Trim();
        return trimmed.Length <= 200 ? trimmed : trimmed[..200];
    }
}

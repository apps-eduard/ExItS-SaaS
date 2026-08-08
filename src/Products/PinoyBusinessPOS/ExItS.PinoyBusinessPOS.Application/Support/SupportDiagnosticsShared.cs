using System.Text.RegularExpressions;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Offline;

namespace ExItS.PinoyBusinessPOS.Application.Support;

/// <summary>Safe public-id helpers for support diagnostics (not secrets).</summary>
public static class SupportDiagnosticsPublicIds
{
    private static readonly Regex PublicOrgIdInUsername = new(
        @"@(ORG\d{6})\b",
        RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static string? TryExtractPublicOrganizationId(string? usernameOrEmail)
    {
        if (string.IsNullOrWhiteSpace(usernameOrEmail))
        {
            return null;
        }

        var match = PublicOrgIdInUsername.Match(usernameOrEmail);
        return match.Success ? match.Groups[1].Value.ToUpperInvariant() : null;
    }
}

internal static class SupportDiagnosticsShared
{
    public static string ShortenDeviceId(string? deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return "(unavailable)";
        }

        return deviceId.Length <= 8 ? deviceId : $"{deviceId[..8]}…";
    }

    public static string DescribeGrantStatus(
        OfflineOperatingGrant? grant,
        bool isUnlockedThisProcess,
        DateTimeOffset utcNow)
    {
        if (grant is null)
        {
            return "None";
        }

        if (grant.IsExpired(utcNow))
        {
            return "Expired";
        }

        var scope = grant.IsPersonalScope ? "Personal" : "Organization";
        var unlock = isUnlockedThisProcess ? "unlocked" : "locked (PIN required)";
        return $"{scope} · valid · {unlock}";
    }

    public static int FailedCount(OfflineQueueCounts counts) =>
        counts.RetryableFailure + counts.PermanentFailure + counts.Conflict + counts.BlockedByAccess;

    public static int PendingCount(OfflineQueueCounts counts) =>
        counts.Pending + counts.Syncing;

    public static string ConnectionLabel(bool isConnected) =>
        isConnected ? "Online" : "Offline";
}

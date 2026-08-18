namespace ExItS.Platform.Domain.Organizations;

internal static class BranchEffectiveTimeZone
{
    public static bool TryResolve(string? timeZoneId, out TimeZoneInfo timeZone)
    {
        timeZone = null!;
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return false;
        }

        if (TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out timeZone))
        {
            return true;
        }

        if (OperatingSystem.IsWindows()
            && TimeZoneInfo.TryConvertIanaIdToWindowsId(timeZoneId, out var windowsId)
            && TimeZoneInfo.TryFindSystemTimeZoneById(windowsId, out timeZone))
        {
            return true;
        }

        if (!OperatingSystem.IsWindows()
            && TimeZoneInfo.TryConvertWindowsIdToIanaId(timeZoneId, out var ianaId)
            && TimeZoneInfo.TryFindSystemTimeZoneById(ianaId, out timeZone))
        {
            return true;
        }

        return false;
    }
}

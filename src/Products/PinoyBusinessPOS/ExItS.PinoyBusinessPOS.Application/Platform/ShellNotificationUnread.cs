using System.Globalization;

namespace ExItS.PinoyBusinessPOS.Application.Platform;

/// <summary>
/// Formats shell notification unread counts for badge display.
/// Authoritative unread values come from notification list APIs (<c>!IsRead</c>).
/// </summary>
public static class ShellNotificationUnread
{
    public const int CompactThreshold = 99;

    /// <summary>
    /// Returns null when there is nothing to show; otherwise a compact badge string (e.g. "7" or "99+").
    /// </summary>
    public static string? FormatBadge(int unreadCount)
    {
        if (unreadCount <= 0)
        {
            return null;
        }

        return unreadCount > CompactThreshold
            ? "99+"
            : unreadCount.ToString(CultureInfo.InvariantCulture);
    }
}

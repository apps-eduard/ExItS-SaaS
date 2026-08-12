namespace ExItS.PinoyBusinessPOS.Maui.Services;

/// <summary>
/// Signals the shell notification bell to reload unread counts after mark-read / accept / decline.
/// </summary>
public sealed class ShellNotificationUnreadState
{
    public event Func<Task>? Changed;

    public void NotifyChanged()
    {
        var handlers = Changed;
        if (handlers is null)
        {
            return;
        }

        foreach (var handler in handlers.GetInvocationList().Cast<Func<Task>>())
        {
            _ = SafeInvokeAsync(handler);
        }
    }

    private static async Task SafeInvokeAsync(Func<Task> handler)
    {
        try
        {
            await handler().ConfigureAwait(false);
        }
        catch
        {
            // Subscribers must not break unrelated UI flows.
        }
    }
}

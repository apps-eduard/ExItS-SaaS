namespace ExItS.PinoyBusinessPOS.Application.Offline;

/// <summary>
/// Process-scoped offline UX flags for one unlocked offline session.
/// Not durable and never grants authorization.
/// </summary>
public sealed class OfflineSessionUxState
{
    public bool OfflineWorkingWarningShown { get; private set; }

    public bool PendingOfflineWorkingWarning { get; private set; }

    public event Func<Task>? Changed;

    /// <summary>Call after a successful offline PIN unlock.</summary>
    public void NotifyOfflinePinUnlocked()
    {
        if (OfflineWorkingWarningShown)
        {
            return;
        }

        PendingOfflineWorkingWarning = true;
        _ = RaiseChangedAsync();
    }

    public void AcknowledgeOfflineWorkingWarning()
    {
        PendingOfflineWorkingWarning = false;
        OfflineWorkingWarningShown = true;
        _ = RaiseChangedAsync();
    }

    /// <summary>Reset when ending the offline unlock session (sign out, lock, or online restore).</summary>
    public void ResetSession()
    {
        OfflineWorkingWarningShown = false;
        PendingOfflineWorkingWarning = false;
        _ = RaiseChangedAsync();
    }

    private Task RaiseChangedAsync()
    {
        var handler = Changed;
        return handler is null ? Task.CompletedTask : handler();
    }
}

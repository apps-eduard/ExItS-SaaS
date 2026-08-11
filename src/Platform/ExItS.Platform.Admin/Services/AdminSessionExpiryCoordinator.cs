namespace ExItS.Platform.Admin.Services;

/// <summary>
/// Circuit-scoped signal when Platform API returns 401 for a session-backed call.
/// MainLayout redirects to logout once so the user is not left on a retry shell.
/// </summary>
public sealed class AdminSessionExpiryCoordinator
{
    private int _signaled;

    public event Action? SessionExpired;

    public void NotifyUnauthorized()
    {
        if (Interlocked.Exchange(ref _signaled, 1) != 0)
        {
            return;
        }

        SessionExpired?.Invoke();
    }
}

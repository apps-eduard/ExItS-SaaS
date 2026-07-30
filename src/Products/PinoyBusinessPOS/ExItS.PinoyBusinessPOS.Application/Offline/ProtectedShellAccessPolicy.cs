using ExItS.PinoyBusinessPOS.Application.Abstractions;

namespace ExItS.PinoyBusinessPOS.Application.Offline;

/// <summary>
/// Protected shell requires authenticated session, selected organization, HasPosAccess, and online connectivity.
/// Offline launch cannot unlock prior context (no offline authorization window in P7-WP01).
/// </summary>
public sealed class ProtectedShellAccessPolicy : IProtectedShellAccessPolicy, IDisposable
{
    private readonly ICurrentUserContext _currentUser;
    private readonly IConnectivityService _connectivity;
    private ConnectivityStatus _status = ConnectivityStatus.Unknown;
    private bool _statusKnown;

    public ProtectedShellAccessPolicy(ICurrentUserContext currentUser, IConnectivityService connectivity)
    {
        _currentUser = currentUser;
        _connectivity = connectivity;
        _connectivity.ConnectivityChanged += OnConnectivityChanged;
    }

    public bool CanEnterProtectedShell => HasValidatedSessionAccess && IsOnline;

    public bool RequiresReconnectToVerifyAccess
    {
        get
        {
            if (!_currentUser.IsAuthenticated)
            {
                return false;
            }

            // Authenticated but offline: must reconnect to verify access before protected routes.
            if (!IsOnline)
            {
                return true;
            }

            return false;
        }
    }

    private bool HasValidatedSessionAccess =>
        _currentUser.IsAuthenticated
        && _currentUser.Session?.OrganizationId is not null
        && _currentUser.HasPosAccess;

    private bool IsOnline => _statusKnown && _status == ConnectivityStatus.Online;

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        _status = await _connectivity.IsConnectedAsync(ct).ConfigureAwait(false)
            ? ConnectivityStatus.Online
            : ConnectivityStatus.Offline;
        _statusKnown = true;
    }

    private void OnConnectivityChanged(object? sender, ConnectivityStatus status)
    {
        _status = status;
        _statusKnown = true;
    }

    public void Dispose()
    {
        _connectivity.ConnectivityChanged -= OnConnectivityChanged;
    }
}

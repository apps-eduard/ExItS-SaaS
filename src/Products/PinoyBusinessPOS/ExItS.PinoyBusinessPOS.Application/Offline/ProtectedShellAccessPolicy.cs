using ExItS.PinoyBusinessPOS.Application.Abstractions;

namespace ExItS.PinoyBusinessPOS.Application.Offline;

/// <summary>
/// Protected shell: online validation required to unlock. Mid-session offline mutations are allowed
/// only after this process lifetime has validated access online (P7-WP03). Cold start / restart
/// while offline never unlocks from cache alone (no time-based entitlement grace).
/// </summary>
public sealed class ProtectedShellAccessPolicy : IProtectedShellAccessPolicy, IDisposable
{
    private readonly ICurrentUserContext _currentUser;
    private readonly IConnectivityService _connectivity;
    private ConnectivityStatus _status = ConnectivityStatus.Unknown;
    private bool _statusKnown;
    private bool _validatedOnlineThisProcess;
    private Guid? _validatedUserId;
    private Guid? _validatedOrganizationId;

    public ProtectedShellAccessPolicy(ICurrentUserContext currentUser, IConnectivityService connectivity)
    {
        _currentUser = currentUser;
        _connectivity = connectivity;
        _connectivity.ConnectivityChanged += OnConnectivityChanged;
    }

    public bool CanEnterProtectedShell =>
        HasValidatedSessionAccess && (IsOnline || HasContinuousValidatedSession);

    public bool RequiresReconnectToVerifyAccess
    {
        get
        {
            if (!_currentUser.IsAuthenticated)
            {
                return false;
            }

            // Authenticated but never validated online in this process (e.g. restart offline).
            if (!IsOnline && !HasContinuousValidatedSession)
            {
                return true;
            }

            return false;
        }
    }

    /// <summary>True when offline mutations may proceed for the active continuous session.</summary>
    public bool AllowsOfflineMutation => HasValidatedSessionAccess && HasContinuousValidatedSession;

    private bool HasContinuousValidatedSession =>
        _validatedOnlineThisProcess
        && _validatedUserId == _currentUser.Session?.UserId
        && _validatedOrganizationId == _currentUser.Session?.OrganizationId
        && HasValidatedSessionAccess;

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
        TryMarkValidatedOnline();
    }

    /// <summary>Clears process-lifetime validation (logout / user or organization switch).</summary>
    public void ClearProcessValidation()
    {
        _validatedOnlineThisProcess = false;
        _validatedUserId = null;
        _validatedOrganizationId = null;
    }

    private void OnConnectivityChanged(object? sender, ConnectivityStatus status)
    {
        _status = status;
        _statusKnown = true;
        TryMarkValidatedOnline();
    }

    private void TryMarkValidatedOnline()
    {
        if (!IsOnline || !HasValidatedSessionAccess)
        {
            return;
        }

        _validatedOnlineThisProcess = true;
        _validatedUserId = _currentUser.Session!.UserId;
        _validatedOrganizationId = _currentUser.Session.OrganizationId;
    }

    public void Dispose()
    {
        _connectivity.ConnectivityChanged -= OnConnectivityChanged;
    }
}

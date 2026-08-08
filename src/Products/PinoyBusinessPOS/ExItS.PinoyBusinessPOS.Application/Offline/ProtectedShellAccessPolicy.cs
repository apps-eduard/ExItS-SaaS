using ExItS.PinoyBusinessPOS.Application.Abstractions;

namespace ExItS.PinoyBusinessPOS.Application.Offline;

/// <summary>
/// Protected shell: online validation or PIN-unlocked offline grant unlocks the process session.
/// Mid-session / cold-start offline mutations are allowed only while that continuous session remains
/// active. Explicit server denial still fails closed; network unavailability alone does not.
/// </summary>
public sealed class ProtectedShellAccessPolicy : IProtectedShellAccessPolicy, IDisposable
{
    private readonly ICurrentUserContext _currentUser;
    private readonly IConnectivityService _connectivity;
    private ConnectivityStatus _status = ConnectivityStatus.Unknown;
    private bool _statusKnown;
    private bool _validatedThisProcess;
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

            // Authenticated but never validated in this process (online bind or offline PIN unlock).
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
        _validatedThisProcess
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
        _validatedThisProcess = false;
        _validatedUserId = null;
        _validatedOrganizationId = null;
    }

    /// <summary>
    /// Called after a successful online org bind / POS access grant. Marks this process as validated
    /// even when connectivity Initialize has not run yet (common right after Quick Login → Owner).
    /// </summary>
    public void NotifySessionAccessChanged()
    {
        if (!HasValidatedSessionAccess)
        {
            return;
        }

        // Bind just completed over the network — treat as online for this process lifetime.
        if (!_statusKnown || _status == ConnectivityStatus.Unknown)
        {
            _status = ConnectivityStatus.Online;
            _statusKnown = true;
        }

        _validatedThisProcess = true;
        _validatedUserId = _currentUser.Session!.UserId;
        _validatedOrganizationId = _currentUser.Session.OrganizationId;
    }

    public void NotifyOfflineUnlock(Guid userId, Guid organizationId)
    {
        if (!HasValidatedSessionAccess
            || _currentUser.Session?.UserId != userId
            || _currentUser.Session?.OrganizationId != organizationId)
        {
            return;
        }

        _validatedThisProcess = true;
        _validatedUserId = userId;
        _validatedOrganizationId = organizationId;
        if (!_statusKnown)
        {
            _status = ConnectivityStatus.Offline;
            _statusKnown = true;
        }
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

        _validatedThisProcess = true;
        _validatedUserId = _currentUser.Session!.UserId;
        _validatedOrganizationId = _currentUser.Session.OrganizationId;
    }

    public void Dispose()
    {
        _connectivity.ConnectivityChanged -= OnConnectivityChanged;
    }
}

namespace ExItS.PinoyBusinessPOS.Maui.Services;

/// <summary>
/// Lets pages request shell header "inner" mode (back button) without duplicating top bars.
/// Uses claim tokens so a disposing page cannot clear a newer page's back target
/// (Blazor may dispose the old @Body after the new one initializes).
/// </summary>
public sealed class StoreHeaderState
{
    private int _innerDepth;
    private int _claimSerial;
    private int _activeClaim;

    public bool ShowBack => _innerDepth > 0;

    public string? BackHref { get; private set; }

    public Func<Task>? BackHandler { get; private set; }

    public event Func<Task>? Changed;

    /// <summary>
    /// Page-level back: replaces any prior page claim (depth = 1).
    /// Returns a claim id that must be passed to <see cref="ReleaseBack"/>.
    /// </summary>
    public int SetBack(string? backHref = null, Func<Task>? backHandler = null)
    {
        var claim = ++_claimSerial;
        _activeClaim = claim;
        _innerDepth = 1;
        BackHref = string.IsNullOrWhiteSpace(backHref) ? null : backHref.Trim();
        BackHandler = backHandler;
        _ = NotifyAsync();
        return claim;
    }

    /// <summary>Clear page-level back only if <paramref name="claim"/> is still the active owner.</summary>
    public void ReleaseBack(int claim)
    {
        if (claim == 0 || claim != _activeClaim)
        {
            return;
        }

        _activeClaim = 0;
        _innerDepth = 0;
        BackHref = null;
        BackHandler = null;
        _ = NotifyAsync();
    }

    /// <summary>Enter nested inner mode (e.g. Payment). Call <see cref="ExitInner"/> when leaving.</summary>
    public void EnterInner(string? backHref = null, Func<Task>? backHandler = null)
    {
        _innerDepth++;
        if (!string.IsNullOrWhiteSpace(backHref))
        {
            BackHref = backHref.Trim();
        }

        if (backHandler is not null)
        {
            BackHandler = backHandler;
        }

        _ = NotifyAsync();
    }

    public void ExitInner()
    {
        if (_innerDepth > 0)
        {
            _innerDepth--;
        }

        if (_innerDepth == 0)
        {
            _activeClaim = 0;
            BackHref = null;
            BackHandler = null;
        }

        _ = NotifyAsync();
    }

    /// <summary>
    /// Hard clear (tests / rare forced reset). Prefer <see cref="ReleaseBack"/> from pages.
    /// Shells must not call this on LocationChanged — it races with the new page's SetBack.
    /// </summary>
    public void Reset()
    {
        _innerDepth = 0;
        _activeClaim = 0;
        BackHref = null;
        BackHandler = null;
        _ = NotifyAsync();
    }

    private Task NotifyAsync() =>
        Changed?.Invoke() ?? Task.CompletedTask;
}

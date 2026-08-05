namespace ExItS.PinoyBusinessPOS.Maui.Services;

/// <summary>
/// Lets pages request shell header "inner" mode (back button) without duplicating top bars.
/// </summary>
public sealed class StoreHeaderState
{
    private int _innerDepth;

    public bool ShowBack => _innerDepth > 0;

    public string? BackHref { get; private set; }

    public Func<Task>? BackHandler { get; private set; }

    public event Func<Task>? Changed;

    /// <summary>Enter inner mode (e.g. Payment). Call <see cref="ExitInner"/> when leaving.</summary>
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
            BackHref = null;
            BackHandler = null;
        }

        _ = NotifyAsync();
    }

    public void Reset()
    {
        _innerDepth = 0;
        BackHref = null;
        BackHandler = null;
        _ = NotifyAsync();
    }

    private Task NotifyAsync() =>
        Changed?.Invoke() ?? Task.CompletedTask;
}

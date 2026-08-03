namespace ExItS.Platform.Admin.Services;

/// <summary>
/// Prevents Admin list/detail pages from re-fetching when the layout re-renders
/// (e.g. <c>NavigationManager.LocationChanged</c> → layout <c>StateHasChanged</c>).
/// Re-entry with the same mode is skipped; mode changes (list ↔ detail, directory) load once.
/// </summary>
public sealed class AdminPageLoadGate
{
    private object? _mode;

    public bool ShouldLoad(object? mode)
    {
        if (Equals(_mode, mode))
        {
            return false;
        }

        _mode = mode;
        return true;
    }

    public void Reset() => _mode = null;
}

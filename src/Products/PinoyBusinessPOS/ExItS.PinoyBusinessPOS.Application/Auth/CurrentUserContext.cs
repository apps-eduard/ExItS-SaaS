using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Auth;

namespace ExItS.PinoyBusinessPOS.Application.Auth;

public sealed class CurrentUserContext : ICurrentUserContext
{
    public AuthSession? Session { get; private set; }
    public bool IsAuthenticated => Session is not null;
    public bool HasPosAccess => Session?.HasPosAccess == true;

    public event Func<Task>? Changed;

    public void Set(AuthSession? session)
    {
        Session = session;
        _ = RaiseChangedAsync();
    }

    public void Clear()
    {
        Session = null;
        _ = RaiseChangedAsync();
    }

    private async Task RaiseChangedAsync()
    {
        var handlers = Changed;
        if (handlers is null) return;
        foreach (var handler in handlers.GetInvocationList().Cast<Func<Task>>())
        {
            await handler().ConfigureAwait(false);
        }
    }
}

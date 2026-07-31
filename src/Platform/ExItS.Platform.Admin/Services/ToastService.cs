namespace ExItS.Platform.Admin.Services;

public enum ToastKind { Info, Success, Error }

public sealed record ToastMessage(Guid Id, string Text, ToastKind Kind);

/// <summary>
/// Scoped per-circuit toast bus used by the shell <c>ToastHost</c>.
/// Migrated P11-WP03 forms publish success through this service; remaining pages may still use inline toasts.
/// </summary>
public sealed class ToastService
{
    public event Action<ToastMessage>? Added;
    public event Action<Guid>? Removed;

    public void Show(string text, ToastKind kind = ToastKind.Info) =>
        Added?.Invoke(new ToastMessage(Guid.NewGuid(), text, kind));

    public void Dismiss(Guid id) => Removed?.Invoke(id);
}

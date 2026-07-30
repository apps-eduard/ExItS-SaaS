namespace ExItS.Platform.Admin.Services;

public enum ToastKind { Info, Success, Error }

public sealed record ToastMessage(Guid Id, string Text, ToastKind Kind);

/// <summary>
/// Scoped per-circuit toast bus. Existing pages keep their inline <c>.toast</c>/<c>.state.error</c>
/// success/error text (unchanged, to avoid touching P4-WP02/03 mutation logic); this service backs
/// the new <c>ToastHost</c> shell component for future pages that want transient notifications
/// without inline state.
/// </summary>
public sealed class ToastService
{
    public event Action<ToastMessage>? Added;
    public event Action<Guid>? Removed;

    public void Show(string text, ToastKind kind = ToastKind.Info) =>
        Added?.Invoke(new ToastMessage(Guid.NewGuid(), text, kind));

    public void Dismiss(Guid id) => Removed?.Invoke(id);
}

namespace ExItS.PinoyBusinessPOS.Application.Abstractions;

public sealed record DocumentHandoffResult(bool Initiated, string MessageKey);

/// <summary>
/// Device share/handoff for document text. Implementations may only claim that sharing was initiated —
/// never that print, save, or delivery succeeded.
/// </summary>
public interface IDocumentHandoffService
{
    Task<DocumentHandoffResult> ShareTextAsync(string title, string text, CancellationToken ct = default);
}

/// <summary>No-op handoff used by tests and non-UI hosts.</summary>
public sealed class NullDocumentHandoffService : IDocumentHandoffService
{
    public Task<DocumentHandoffResult> ShareTextAsync(string title, string text, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(new DocumentHandoffResult(false, "Handoff_Failed"));
    }
}

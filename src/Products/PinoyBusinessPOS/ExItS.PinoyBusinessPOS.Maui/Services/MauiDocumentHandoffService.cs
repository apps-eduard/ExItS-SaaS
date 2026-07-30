using ExItS.PinoyBusinessPOS.Application.Abstractions;
using Microsoft.Maui.ApplicationModel.DataTransfer;

namespace ExItS.PinoyBusinessPOS.Maui.Services;

/// <summary>
/// MAUI share-sheet handoff. Only reports that sharing was initiated — never print/save success.
/// </summary>
public sealed class MauiDocumentHandoffService : IDocumentHandoffService
{
    public async Task<DocumentHandoffResult> ShareTextAsync(string title, string text, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        try
        {
            await Share.Default.RequestAsync(new ShareTextRequest
            {
                Title = title,
                Text = text
            }).ConfigureAwait(false);

            return new DocumentHandoffResult(true, "Handoff_Initiated");
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return new DocumentHandoffResult(false, "Handoff_Failed");
        }
    }
}

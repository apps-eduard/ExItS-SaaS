using QRCoder;

namespace ExItS.PinoyBusinessPOS.Maui.Services;

/// <summary>
/// Renders QR images locally. Payload must remain a versioned public reference only
/// (never tokens, email, phone, UUID, roles, or balances).
/// </summary>
internal static class LocalQrCodeRenderer
{
    public static bool TryToPngDataUrl(string? payload, out string dataUrl, int pixelsPerModule = 6)
    {
        dataUrl = string.Empty;
        if (string.IsNullOrWhiteSpace(payload))
        {
            return false;
        }

        try
        {
            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
            var png = new PngByteQRCode(data);
            var bytes = png.GetGraphic(pixelsPerModule);
            if (bytes.Length == 0)
            {
                return false;
            }

            dataUrl = "data:image/png;base64," + Convert.ToBase64String(bytes);
            return dataUrl.StartsWith("data:image/png;base64,", StringComparison.Ordinal);
        }
        catch
        {
            dataUrl = string.Empty;
            return false;
        }
    }

    public static string ToPngDataUrl(string payload, int pixelsPerModule = 6)
    {
        if (!TryToPngDataUrl(payload, out var dataUrl, pixelsPerModule))
        {
            throw new InvalidOperationException("QR rendering failed.");
        }

        return dataUrl;
    }
}

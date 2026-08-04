using QRCoder;

namespace ExItS.PinoyBusinessPOS.Maui.Services;

/// <summary>
/// Renders QR images locally. Payload must remain a versioned public reference only
/// (never tokens, email, phone, UUID, roles, or balances).
/// </summary>
internal static class LocalQrCodeRenderer
{
    public static string ToPngDataUrl(string payload, int pixelsPerModule = 6)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);

        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(data);
        var bytes = png.GetGraphic(pixelsPerModule);
        return "data:image/png;base64," + Convert.ToBase64String(bytes);
    }
}

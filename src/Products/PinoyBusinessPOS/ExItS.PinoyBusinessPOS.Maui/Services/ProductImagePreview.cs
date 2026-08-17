namespace ExItS.PinoyBusinessPOS.Maui.Services;

public static class ProductImagePreview
{
    public static string ToDataUrl(byte[] bytes)
    {
        var mime = GuessMime(bytes);
        return $"data:{mime};base64,{Convert.ToBase64String(bytes)}";
    }

    private static string GuessMime(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
        {
            return "image/jpeg";
        }

        if (bytes.Length >= 8
            && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
        {
            return "image/png";
        }

        return "image/webp";
    }
}

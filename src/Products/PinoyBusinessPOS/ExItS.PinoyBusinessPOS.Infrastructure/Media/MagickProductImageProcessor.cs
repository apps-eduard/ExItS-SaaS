using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ImageMagick;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Media;

/// <summary>
/// Server-authoritative product-image pipeline using Magick.NET (Apache-2.0).
/// Validates magic bytes, rejects bombs/HEIC, AutoOrients, strips metadata, and writes WebP variants.
/// </summary>
public sealed class MagickProductImageProcessor : IProductImageProcessor
{
    public const int MaxUploadBytes = ProductImageUploadLimits.MaxBytes;
    public const int MaxDimension = 8000;
    public const int MaxPixels = 40_000_000;
    public const int ThumbMaxEdge = 200;
    public const int MediumMaxEdge = 800;
    public const uint ThumbQuality = 78;
    public const uint MediumQuality = 80;

    public ApplicationResult<ProcessedProductImage> Process(byte[] uploadBytes)
    {
        if (uploadBytes is null || uploadBytes.Length == 0)
        {
            return ApplicationResult<ProcessedProductImage>.Failure(
                DomainErrorCodes.InvalidProductImage,
                "An image file is required.");
        }

        if (uploadBytes.Length > MaxUploadBytes)
        {
            return ApplicationResult<ProcessedProductImage>.Failure(
                DomainErrorCodes.ProductImageTooLarge,
                "Image is too large. Use a file of 10 MB or less.");
        }

        if (!IsAcceptedMagic(uploadBytes))
        {
            return ApplicationResult<ProcessedProductImage>.Failure(
                DomainErrorCodes.ProductImageUnsupportedType,
                "Use a JPEG, PNG, or WebP image.");
        }

        try
        {
            ResourceLimits.Memory = 256UL * 1024UL * 1024UL;
            ResourceLimits.Area = (ulong)MaxPixels;

            using var image = new MagickImage();
            image.Read(uploadBytes);
            if (image.Width == 0 || image.Height == 0
                || image.Width > MaxDimension
                || image.Height > MaxDimension
                || (long)image.Width * image.Height > MaxPixels)
            {
                return ApplicationResult<ProcessedProductImage>.Failure(
                    DomainErrorCodes.InvalidProductImage,
                    "Image dimensions are not allowed.");
            }

            image.AutoOrient();
            image.Strip();
            image.Format = MagickFormat.WebP;

            var medium = EncodeVariant(image, MediumMaxEdge, MediumQuality);
            var thumb = EncodeVariant(image, ThumbMaxEdge, ThumbQuality);
            return ApplicationResult<ProcessedProductImage>.Success(
                new ProcessedProductImage(
                    thumb.Bytes,
                    thumb.Width,
                    thumb.Height,
                    medium.Bytes,
                    medium.Width,
                    medium.Height));
        }
        catch (MagickException)
        {
            return ApplicationResult<ProcessedProductImage>.Failure(
                DomainErrorCodes.InvalidProductImage,
                "The image could not be read.");
        }
    }

    private static (byte[] Bytes, int Width, int Height) EncodeVariant(MagickImage source, int maxEdge, uint quality)
    {
        using var clone = source.Clone();
        if (clone.Width > (uint)maxEdge || clone.Height > (uint)maxEdge)
        {
            clone.Resize(new MagickGeometry($"{maxEdge}x{maxEdge}>"));
        }

        clone.Quality = quality;
        clone.Format = MagickFormat.WebP;
        return (clone.ToByteArray(), (int)clone.Width, (int)clone.Height);
    }

    public static bool IsAcceptedMagic(ReadOnlySpan<byte> bytes)
    {
        if (LooksLikeHeic(bytes) || LooksLikeAvif(bytes))
        {
            return false;
        }

        if (bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
        {
            return true;
        }

        if (bytes.Length >= 8
            && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47
            && bytes[4] == 0x0D && bytes[5] == 0x0A && bytes[6] == 0x1A && bytes[7] == 0x0A)
        {
            return true;
        }

        if (bytes.Length >= 12
            && bytes[0] == (byte)'R' && bytes[1] == (byte)'I' && bytes[2] == (byte)'F' && bytes[3] == (byte)'F'
            && bytes[8] == (byte)'W' && bytes[9] == (byte)'E' && bytes[10] == (byte)'B' && bytes[11] == (byte)'P')
        {
            return true;
        }

        return false;
    }

    private static bool LooksLikeHeic(ReadOnlySpan<byte> bytes) =>
        bytes.Length >= 12
        && bytes[4] == (byte)'f' && bytes[5] == (byte)'t' && bytes[6] == (byte)'y' && bytes[7] == (byte)'p'
        && (HasBrand(bytes, "heic") || HasBrand(bytes, "heix") || HasBrand(bytes, "mif1") || HasBrand(bytes, "msf1"));

    private static bool LooksLikeAvif(ReadOnlySpan<byte> bytes) =>
        bytes.Length >= 12
        && bytes[4] == (byte)'f' && bytes[5] == (byte)'t' && bytes[6] == (byte)'y' && bytes[7] == (byte)'p'
        && HasBrand(bytes, "avif");

    private static bool HasBrand(ReadOnlySpan<byte> bytes, string brand) =>
        bytes.Length >= 12
        && bytes[8] == (byte)brand[0]
        && bytes[9] == (byte)brand[1]
        && bytes[10] == (byte)brand[2]
        && bytes[11] == (byte)brand[3];
}

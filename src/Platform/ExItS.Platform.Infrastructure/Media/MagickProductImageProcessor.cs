using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.GlobalCatalog;
using ExItS.Platform.Domain.Common;
using ImageMagick;

namespace ExItS.Platform.Infrastructure.Media;

/// <summary>
/// Server-authoritative Platform global-product image pipeline using Magick.NET (Apache-2.0).
/// Validates magic bytes, rejects bombs/HEIC, AutoOrients, strips metadata, and writes WebP variants.
/// </summary>
public sealed class MagickProductImageProcessor : IGlobalProductImageProcessor
{
    public const int MaxUploadBytes = GlobalProductImageUploadLimits.MaxBytes;
    public const int MaxDimension = 8000;
    public const int MaxPixels = 40_000_000;
    public const int ThumbMaxEdge = 200;
    public const int MediumMaxEdge = 800;
    public const uint ThumbQuality = 78;
    public const uint MediumQuality = 80;

    public ApplicationResult<ProcessedGlobalProductImage> Process(byte[] uploadBytes)
    {
        if (uploadBytes is null || uploadBytes.Length == 0)
        {
            return ApplicationResult<ProcessedGlobalProductImage>.Failure(
                DomainErrorCodes.InvalidGlobalProductImage,
                "An image file is required.");
        }

        if (uploadBytes.Length > MaxUploadBytes)
        {
            return ApplicationResult<ProcessedGlobalProductImage>.Failure(
                DomainErrorCodes.GlobalProductImageTooLarge,
                "Image is too large. Use a file of 10 MB or less.");
        }

        if (!IsAcceptedMagic(uploadBytes))
        {
            return ApplicationResult<ProcessedGlobalProductImage>.Failure(
                DomainErrorCodes.GlobalProductImageUnsupportedType,
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
                return ApplicationResult<ProcessedGlobalProductImage>.Failure(
                    DomainErrorCodes.InvalidGlobalProductImage,
                    "Image dimensions are not allowed.");
            }

            image.AutoOrient();
            image.Strip();
            image.Format = MagickFormat.WebP;

            var medium = EncodeVariant(image, MediumMaxEdge, MediumQuality);
            var thumb = EncodeVariant(image, ThumbMaxEdge, ThumbQuality);
            return ApplicationResult<ProcessedGlobalProductImage>.Success(
                new ProcessedGlobalProductImage(
                    thumb.Bytes,
                    thumb.Width,
                    thumb.Height,
                    medium.Bytes,
                    medium.Width,
                    medium.Height));
        }
        catch (MagickException)
        {
            return ApplicationResult<ProcessedGlobalProductImage>.Failure(
                DomainErrorCodes.InvalidGlobalProductImage,
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

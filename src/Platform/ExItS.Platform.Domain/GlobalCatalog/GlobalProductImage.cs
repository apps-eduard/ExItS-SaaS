using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.GlobalCatalog;

/// <summary>
/// One shared Platform global-product image. Binary variants live in object storage, not PostgreSQL.
/// Organizations reference this asset; they do not receive a copied file.
/// </summary>
public sealed class GlobalProductImage
{
    public const string WebpContentType = "image/webp";

    public Guid Id { get; }
    public GlobalProductId GlobalProductId { get; }
    /// <summary>Stable folder id. Never a user-supplied filename.</summary>
    public Guid StorageKey { get; }
    public int Version { get; }
    public int ThumbWidth { get; }
    public int ThumbHeight { get; }
    public int MediumWidth { get; }
    public int MediumHeight { get; }
    public string ContentType { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; }

    private GlobalProductImage(
        Guid id,
        GlobalProductId globalProductId,
        Guid storageKey,
        int version,
        int thumbWidth,
        int thumbHeight,
        int mediumWidth,
        int mediumHeight,
        string contentType,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        Id = id;
        GlobalProductId = globalProductId;
        StorageKey = storageKey;
        Version = version;
        ThumbWidth = thumbWidth;
        ThumbHeight = thumbHeight;
        MediumWidth = mediumWidth;
        MediumHeight = mediumHeight;
        ContentType = contentType;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public static GlobalProductImage Create(
        GlobalProductId globalProductId,
        Guid storageKey,
        int version,
        int thumbWidth,
        int thumbHeight,
        int mediumWidth,
        int mediumHeight,
        DateTimeOffset utcNow,
        Guid? id = null)
    {
        EnsureUtc(utcNow);
        EnsurePositive(thumbWidth, thumbHeight, mediumWidth, mediumHeight);
        if (storageKey == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidGlobalProductImage,
                "Image storage key cannot be empty.");
        }

        if (version < 1)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidGlobalProductImage,
                "Image version must be at least 1.");
        }

        return new(
            id ?? Guid.NewGuid(),
            globalProductId,
            storageKey,
            version,
            thumbWidth,
            thumbHeight,
            mediumWidth,
            mediumHeight,
            WebpContentType,
            utcNow,
            utcNow);
    }

    public GlobalProductImage Replace(
        int nextVersion,
        int thumbWidth,
        int thumbHeight,
        int mediumWidth,
        int mediumHeight,
        DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        EnsurePositive(thumbWidth, thumbHeight, mediumWidth, mediumHeight);
        if (nextVersion <= Version)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidGlobalProductImage,
                "Replacement image version must increase.");
        }

        return new(
            Id,
            GlobalProductId,
            StorageKey,
            nextVersion,
            thumbWidth,
            thumbHeight,
            mediumWidth,
            mediumHeight,
            WebpContentType,
            CreatedAtUtc,
            utcNow);
    }

    public static GlobalProductImage Rehydrate(
        Guid id,
        GlobalProductId globalProductId,
        Guid storageKey,
        int version,
        int thumbWidth,
        int thumbHeight,
        int mediumWidth,
        int mediumHeight,
        string contentType,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        if (id == Guid.Empty || storageKey == Guid.Empty || version < 1)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidGlobalProductImage,
                "Stored global product image metadata is invalid.");
        }

        return new(
            id,
            globalProductId,
            storageKey,
            version,
            thumbWidth,
            thumbHeight,
            mediumWidth,
            mediumHeight,
            string.IsNullOrWhiteSpace(contentType) ? WebpContentType : contentType.Trim(),
            createdAtUtc,
            updatedAtUtc);
    }

    private static void EnsurePositive(int thumbWidth, int thumbHeight, int mediumWidth, int mediumHeight)
    {
        if (thumbWidth <= 0 || thumbHeight <= 0 || mediumWidth <= 0 || mediumHeight <= 0)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidGlobalProductImage,
                "Image dimensions must be positive.");
        }
    }

    private static void EnsureUtc(DateTimeOffset utcNow)
    {
        if (utcNow.Offset != TimeSpan.Zero)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidGlobalProductImage,
                "Image timestamps must be UTC.");
        }
    }
}

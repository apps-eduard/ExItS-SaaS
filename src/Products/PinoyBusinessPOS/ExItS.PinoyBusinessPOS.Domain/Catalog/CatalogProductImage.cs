using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Domain.Catalog;

/// <summary>
/// One primary product image metadata row. Binary variants live in object storage, not PostgreSQL.
/// </summary>
public sealed class CatalogProductImage
{
    public const string WebpContentType = "image/webp";
    public const int StorageKeyLength = 36;

    public Guid Id { get; }
    public PosOrganizationId OrganizationId { get; }
    public CatalogProductId ProductId { get; }
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

    private CatalogProductImage(
        Guid id,
        PosOrganizationId organizationId,
        CatalogProductId productId,
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
        OrganizationId = organizationId;
        ProductId = productId;
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

    public static CatalogProductImage Create(
        PosOrganizationId organizationId,
        CatalogProductId productId,
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
                DomainErrorCodes.InvalidProductImage,
                "Image storage key cannot be empty.");
        }

        if (version < 1)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductImage,
                "Image version must be at least 1.");
        }

        return new(
            id ?? Guid.NewGuid(),
            organizationId,
            productId,
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

    public CatalogProductImage Replace(
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
                DomainErrorCodes.InvalidProductImage,
                "Replacement image version must increase.");
        }

        return new(
            Id,
            OrganizationId,
            ProductId,
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

    public static CatalogProductImage Rehydrate(
        Guid id,
        PosOrganizationId organizationId,
        CatalogProductId productId,
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
                DomainErrorCodes.InvalidProductImage,
                "Stored product image metadata is invalid.");
        }

        return new(
            id,
            organizationId,
            productId,
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
                DomainErrorCodes.InvalidProductImage,
                "Image dimensions must be positive.");
        }
    }

    private static void EnsureUtc(DateTimeOffset utcNow)
    {
        if (utcNow.Offset != TimeSpan.Zero)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductImage,
                "Image timestamps must be UTC.");
        }
    }
}

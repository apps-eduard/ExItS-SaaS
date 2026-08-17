using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Catalog;
using Microsoft.EntityFrameworkCore;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;

internal sealed class CatalogProductImageRepository : ICatalogProductImageRepository
{
    private readonly PosDbContext _db;

    public CatalogProductImageRepository(PosDbContext db) => _db = db;

    public async Task<CatalogProductImage?> GetByProductIdAsync(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.CatalogProductImages
            .FirstOrDefaultAsync(
                x => x.OrganizationId == organizationId.Value && x.ProductId == productId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : ToDomain(record);
    }

    public async Task<IReadOnlyList<CatalogProductImage>> ListByProductIdsAsync(
        PosOrganizationId organizationId,
        IReadOnlyList<CatalogProductId> productIds,
        CancellationToken cancellationToken = default)
    {
        if (productIds.Count == 0)
        {
            return [];
        }

        var ids = productIds.Select(p => p.Value).Distinct().ToList();
        var records = await _db.CatalogProductImages.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId.Value && ids.Contains(x.ProductId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(ToDomain).ToList();
    }

    public async Task AddAsync(CatalogProductImage image, CancellationToken cancellationToken = default)
    {
        _db.CatalogProductImages.Add(ToRecord(image));
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(CatalogProductImage image, CancellationToken cancellationToken = default)
    {
        var record = await _db.CatalogProductImages
            .FirstAsync(
                x => x.Id == image.Id && x.OrganizationId == image.OrganizationId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        record.StorageKey = image.StorageKey;
        record.Version = image.Version;
        record.ThumbWidth = image.ThumbWidth;
        record.ThumbHeight = image.ThumbHeight;
        record.MediumWidth = image.MediumWidth;
        record.MediumHeight = image.MediumHeight;
        record.ContentType = image.ContentType;
        record.UpdatedAtUtc = image.UpdatedAtUtc;
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(CatalogProductImage image, CancellationToken cancellationToken = default)
    {
        var record = await _db.CatalogProductImages
            .FirstOrDefaultAsync(
                x => x.Id == image.Id && x.OrganizationId == image.OrganizationId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            return;
        }

        _db.CatalogProductImages.Remove(record);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static CatalogProductImage ToDomain(CatalogProductImageRecord record) =>
        CatalogProductImage.Rehydrate(
            record.Id,
            PosOrganizationId.From(record.OrganizationId),
            CatalogProductId.From(record.ProductId),
            record.StorageKey,
            record.Version,
            record.ThumbWidth,
            record.ThumbHeight,
            record.MediumWidth,
            record.MediumHeight,
            record.ContentType,
            record.CreatedAtUtc,
            record.UpdatedAtUtc);

    private static CatalogProductImageRecord ToRecord(CatalogProductImage image) =>
        new()
        {
            Id = image.Id,
            OrganizationId = image.OrganizationId.Value,
            ProductId = image.ProductId.Value,
            StorageKey = image.StorageKey,
            Version = image.Version,
            ThumbWidth = image.ThumbWidth,
            ThumbHeight = image.ThumbHeight,
            MediumWidth = image.MediumWidth,
            MediumHeight = image.MediumHeight,
            ContentType = image.ContentType,
            CreatedAtUtc = image.CreatedAtUtc,
            UpdatedAtUtc = image.UpdatedAtUtc
        };
}

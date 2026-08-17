using ExItS.Platform.Application.GlobalCatalog;
using ExItS.Platform.Domain.GlobalCatalog;
using ExItS.Platform.Infrastructure.Persistence.GlobalCatalog;
using Microsoft.EntityFrameworkCore;

namespace ExItS.Platform.Infrastructure.Persistence.Repositories;

internal sealed class GlobalProductImageRepository : IGlobalProductImageRepository
{
    private readonly PlatformDbContext _db;

    public GlobalProductImageRepository(PlatformDbContext db) => _db = db;

    public async Task<GlobalProductImage?> GetByProductIdAsync(
        GlobalProductId productId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.GlobalProductImages
            .FirstOrDefaultAsync(x => x.GlobalProductId == productId.Value, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : ToDomain(record);
    }

    public async Task<IReadOnlyList<GlobalProductImage>> ListByProductIdsAsync(
        IReadOnlyList<GlobalProductId> productIds,
        CancellationToken cancellationToken = default)
    {
        if (productIds.Count == 0)
        {
            return [];
        }

        var ids = productIds.Select(p => p.Value).Distinct().ToList();
        var records = await _db.GlobalProductImages.AsNoTracking()
            .Where(x => ids.Contains(x.GlobalProductId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(ToDomain).ToList();
    }

    public async Task AddAsync(GlobalProductImage image, CancellationToken cancellationToken = default)
    {
        _db.GlobalProductImages.Add(ToRecord(image));
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(GlobalProductImage image, CancellationToken cancellationToken = default)
    {
        var record = await _db.GlobalProductImages
            .FirstAsync(x => x.Id == image.Id, cancellationToken)
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

    public async Task DeleteAsync(GlobalProductImage image, CancellationToken cancellationToken = default)
    {
        var record = await _db.GlobalProductImages
            .FirstOrDefaultAsync(x => x.Id == image.Id, cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            return;
        }

        _db.GlobalProductImages.Remove(record);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static GlobalProductImage ToDomain(GlobalProductImageRecord record) =>
        GlobalProductImage.Rehydrate(
            record.Id,
            GlobalProductId.From(record.GlobalProductId),
            record.StorageKey,
            record.Version,
            record.ThumbWidth,
            record.ThumbHeight,
            record.MediumWidth,
            record.MediumHeight,
            record.ContentType,
            record.CreatedAtUtc,
            record.UpdatedAtUtc);

    private static GlobalProductImageRecord ToRecord(GlobalProductImage image) =>
        new()
        {
            Id = image.Id,
            GlobalProductId = image.GlobalProductId.Value,
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

using ExItS.Platform.Application.Identity;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Infrastructure.Persistence.Identity;
using Microsoft.EntityFrameworkCore;

namespace ExItS.Platform.Infrastructure.Persistence.Repositories;

internal sealed class AccountProfileRepository(PlatformDbContext db) : IAccountProfileRepository
{
    public async Task<AccountProfile?> GetByIdAsync(AccountProfileId id, CancellationToken cancellationToken = default)
    {
        var record = await db.AccountProfiles.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id.Value, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : ToDomain(record);
    }

    public async Task<AccountProfile?> GetByUserAndClassAsync(
        PlatformUserId userIdentityId,
        AccountClass accountClass,
        CancellationToken cancellationToken = default)
    {
        var className = accountClass.ToString();
        var record = await db.AccountProfiles.AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.UserIdentityId == userIdentityId.Value && x.AccountClass == className,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : ToDomain(record);
    }

    public async Task<IReadOnlyList<AccountProfile>> ListByUserAsync(
        PlatformUserId userIdentityId,
        CancellationToken cancellationToken = default)
    {
        var records = await db.AccountProfiles.AsNoTracking()
            .Where(x => x.UserIdentityId == userIdentityId.Value)
            .OrderBy(x => x.AccountClass)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(ToDomain).ToList();
    }

    public Task AddAsync(AccountProfile profile, CancellationToken cancellationToken = default)
    {
        db.AccountProfiles.Add(ToRecord(profile));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(AccountProfile profile, CancellationToken cancellationToken = default)
    {
        var record = await db.AccountProfiles
            .FirstOrDefaultAsync(x => x.Id == profile.Id.Value, cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            return;
        }

        record.Status = profile.Status;
        record.UpdatedAtUtc = profile.UpdatedAtUtc;
    }

    private static AccountProfile ToDomain(AccountProfileRecord record) =>
        AccountProfile.Rehydrate(
            AccountProfileId.From(record.Id),
            PlatformUserId.From(record.UserIdentityId),
            Enum.Parse<AccountClass>(record.AccountClass, ignoreCase: true),
            record.Status,
            record.CreatedAtUtc,
            record.UpdatedAtUtc);

    private static AccountProfileRecord ToRecord(AccountProfile profile) =>
        new()
        {
            Id = profile.Id.Value,
            UserIdentityId = profile.UserIdentityId.Value,
            AccountClass = profile.AccountClass.ToString(),
            Status = profile.Status,
            CreatedAtUtc = profile.CreatedAtUtc,
            UpdatedAtUtc = profile.UpdatedAtUtc
        };
}

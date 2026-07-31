using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Infrastructure.Persistence.Identity;
using Microsoft.EntityFrameworkCore;

namespace ExItS.Platform.Infrastructure.Persistence.Repositories;

internal sealed class PlatformExternalLoginRepository : IPlatformExternalLoginRepository
{
    private readonly PlatformDbContext _db;

    public PlatformExternalLoginRepository(PlatformDbContext db) => _db = db;

    public async Task<PlatformExternalLogin?> FindByProviderSubjectAsync(
        string provider,
        string providerSubject,
        CancellationToken cancellationToken = default)
    {
        var normalizedProvider = provider.Trim().ToLowerInvariant();
        var subject = providerSubject.Trim();
        var record = await _db.PlatformExternalLogins.AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Provider == normalizedProvider && x.ProviderSubject == subject,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : ToDomain(record);
    }

    public Task AddAsync(PlatformExternalLogin login, CancellationToken cancellationToken = default)
    {
        _db.PlatformExternalLogins.Add(ToRecord(login));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(PlatformExternalLogin login, CancellationToken cancellationToken = default)
    {
        var record = await _db.PlatformExternalLogins
            .FirstOrDefaultAsync(x => x.Id == login.Id.Value, cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            throw new PersistenceConflictException(
                ApplicationErrorCodes.ExternalAuthFailed,
                "External login was not found.");
        }

        record.ProviderEmail = login.ProviderEmail;
        record.UpdatedAtUtc = login.UpdatedAtUtc;
    }

    private static PlatformExternalLogin ToDomain(PlatformExternalLoginRecord record) =>
        PlatformExternalLogin.Rehydrate(
            PlatformExternalLoginId.From(record.Id),
            PlatformUserId.From(record.UserId),
            record.Provider,
            record.ProviderSubject,
            record.ProviderEmail,
            record.CreatedAtUtc,
            record.UpdatedAtUtc);

    private static PlatformExternalLoginRecord ToRecord(PlatformExternalLogin login) =>
        new()
        {
            Id = login.Id.Value,
            UserId = login.UserId.Value,
            Provider = login.Provider,
            ProviderSubject = login.ProviderSubject,
            ProviderEmail = login.ProviderEmail,
            CreatedAtUtc = login.CreatedAtUtc,
            UpdatedAtUtc = login.UpdatedAtUtc
        };
}

using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Infrastructure.Persistence.Organizations;
using Microsoft.EntityFrameworkCore;

namespace ExItS.Platform.Infrastructure.Persistence.Repositories;

internal sealed class PosDeviceRegistrationTokenRepository : IPosDeviceRegistrationTokenRepository
{
    private readonly PlatformDbContext _db;

    public PosDeviceRegistrationTokenRepository(PlatformDbContext db) => _db = db;

    public async Task<PosDeviceRegistrationToken?> GetByIdAsync(
        PosDeviceRegistrationTokenId id,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.PosDeviceRegistrationTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id.Value, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : PosDeviceRegistrationTokenEntityMapper.ToDomain(record);
    }

    public async Task<PosDeviceRegistrationToken?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.PosDeviceRegistrationTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : PosDeviceRegistrationTokenEntityMapper.ToDomain(record);
    }

    public Task AddAsync(PosDeviceRegistrationToken token, CancellationToken cancellationToken = default)
    {
        _db.PosDeviceRegistrationTokens.Add(PosDeviceRegistrationTokenEntityMapper.ToRecord(token));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(PosDeviceRegistrationToken token, CancellationToken cancellationToken = default)
    {
        var record = await _db.PosDeviceRegistrationTokens
            .FirstOrDefaultAsync(x => x.Id == token.Id.Value, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("POS device registration token was not found.");
        PosDeviceRegistrationTokenEntityMapper.ApplyToRecord(token, record);
    }
}

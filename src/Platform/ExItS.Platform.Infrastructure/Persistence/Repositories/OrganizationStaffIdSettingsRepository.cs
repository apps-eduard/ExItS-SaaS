using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Infrastructure.Persistence.Organizations;
using Microsoft.EntityFrameworkCore;

namespace ExItS.Platform.Infrastructure.Persistence.Repositories;

internal sealed class OrganizationStaffIdSettingsRepository : IOrganizationStaffIdSettingsRepository
{
    private readonly PlatformDbContext _db;

    public OrganizationStaffIdSettingsRepository(PlatformDbContext db) => _db = db;

    public async Task<OrganizationStaffIdSettings?> GetAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.OrganizationStaffIdSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.OrganizationId == organizationId.Value, cancellationToken)
            .ConfigureAwait(false);
        return record is null
            ? null
            : OrganizationStaffIdSettings.Rehydrate(
                organizationId,
                record.Prefix,
                record.NextNumber,
                record.PadDigits,
                record.UpdatedAtUtc);
    }

    public Task AddAsync(OrganizationStaffIdSettings settings, CancellationToken cancellationToken = default)
    {
        _db.OrganizationStaffIdSettings.Add(ToRecord(settings));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(OrganizationStaffIdSettings settings, CancellationToken cancellationToken = default)
    {
        var record = await _db.OrganizationStaffIdSettings
            .FirstAsync(s => s.OrganizationId == settings.OrganizationId.Value, cancellationToken)
            .ConfigureAwait(false);
        record.Prefix = settings.Prefix;
        record.NextNumber = settings.NextNumber;
        record.PadDigits = settings.PadDigits;
        record.UpdatedAtUtc = settings.UpdatedAtUtc;
    }

    private static OrganizationStaffIdSettingsRecord ToRecord(OrganizationStaffIdSettings settings) =>
        new()
        {
            OrganizationId = settings.OrganizationId.Value,
            Prefix = settings.Prefix,
            NextNumber = settings.NextNumber,
            PadDigits = settings.PadDigits,
            UpdatedAtUtc = settings.UpdatedAtUtc,
        };
}

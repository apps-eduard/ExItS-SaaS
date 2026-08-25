using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Onboarding;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Onboarding;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Onboarding;
using Microsoft.EntityFrameworkCore;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;

internal sealed class OrganizationOnboardingProgressRepository : IOrganizationOnboardingProgressRepository
{
    private readonly PosDbContext _db;

    public OrganizationOnboardingProgressRepository(PosDbContext db) => _db = db;

    public async Task<OrganizationOnboardingProgress?> GetByOrganizationIdAsync(
        PosOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.OrganizationOnboardingProgressRows.AsNoTracking()
            .FirstOrDefaultAsync(r => r.OrganizationId == organizationId.Value, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : OrganizationOnboardingProgressEntityMapper.ToDomain(record);
    }

    public Task AddAsync(OrganizationOnboardingProgress progress, CancellationToken cancellationToken = default)
    {
        _db.OrganizationOnboardingProgressRows.Add(OrganizationOnboardingProgressEntityMapper.ToRecord(progress));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(
        OrganizationOnboardingProgress progress,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.OrganizationOnboardingProgressRows
            .FirstOrDefaultAsync(r => r.OrganizationId == progress.OrganizationId.Value, cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            throw new PersistenceConflictException(
                ApplicationErrorCodes.OnboardingProgressConcurrencyConflict,
                "Organization onboarding progress was not found for update.");
        }

        OrganizationOnboardingProgressEntityMapper.ApplyToRecord(progress, record);
    }
}

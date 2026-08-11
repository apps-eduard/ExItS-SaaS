using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.GlobalCatalog;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Infrastructure.Persistence.Organizations;
using Microsoft.EntityFrameworkCore;

namespace ExItS.Platform.Infrastructure.Persistence.Repositories;

internal sealed class OrganizationBusinessTypeActivationRepository
    : IOrganizationBusinessTypeActivationRepository
{
    private readonly PlatformDbContext _db;

    public OrganizationBusinessTypeActivationRepository(PlatformDbContext db) => _db = db;

    public async Task<IReadOnlyList<OrganizationBusinessTypeActivation>> ListByOrganizationAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var records = await _db.OrganizationBusinessTypeActivations
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId.Value)
            .OrderBy(x => x.BusinessTypeId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return records.Select(ToDomain).ToList();
    }

    public async Task<OrganizationBusinessTypeActivation?> GetAsync(
        PlatformOrganizationId organizationId,
        BusinessTypeId businessTypeId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.OrganizationBusinessTypeActivations
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.OrganizationId == organizationId.Value && x.BusinessTypeId == businessTypeId.Value,
                cancellationToken)
            .ConfigureAwait(false);

        return record is null ? null : ToDomain(record);
    }

    public async Task AddAsync(
        OrganizationBusinessTypeActivation activation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activation);

        var exists = await _db.OrganizationBusinessTypeActivations
            .AnyAsync(
                x => x.OrganizationId == activation.OrganizationId.Value
                     && x.BusinessTypeId == activation.BusinessTypeId.Value,
                cancellationToken)
            .ConfigureAwait(false);

        if (exists)
        {
            throw new DomainException(
                DomainErrorCodes.DuplicateBusinessTypeActivation,
                $"Duplicate business type activation '{activation.BusinessTypeId}'.");
        }

        _db.OrganizationBusinessTypeActivations.Add(new OrganizationBusinessTypeActivationRecord
        {
            OrganizationId = activation.OrganizationId.Value,
            BusinessTypeId = activation.BusinessTypeId.Value,
            ActivatedAtUtc = activation.ActivatedAtUtc
        });
    }

    public async Task RemoveAsync(
        PlatformOrganizationId organizationId,
        BusinessTypeId businessTypeId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.OrganizationBusinessTypeActivations
            .FirstOrDefaultAsync(
                x => x.OrganizationId == organizationId.Value && x.BusinessTypeId == businessTypeId.Value,
                cancellationToken)
            .ConfigureAwait(false);

        if (record is not null)
        {
            _db.OrganizationBusinessTypeActivations.Remove(record);
        }
    }

    private static OrganizationBusinessTypeActivation ToDomain(OrganizationBusinessTypeActivationRecord record) =>
        OrganizationBusinessTypeActivation.Rehydrate(
            PlatformOrganizationId.From(record.OrganizationId),
            BusinessTypeId.From(record.BusinessTypeId),
            record.ActivatedAtUtc);
}

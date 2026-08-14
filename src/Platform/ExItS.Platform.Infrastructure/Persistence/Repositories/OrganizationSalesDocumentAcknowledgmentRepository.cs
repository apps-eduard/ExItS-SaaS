using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using Microsoft.EntityFrameworkCore;

namespace ExItS.Platform.Infrastructure.Persistence.Repositories;

internal sealed class OrganizationSalesDocumentAcknowledgmentRepository(PlatformDbContext db)
    : IOrganizationSalesDocumentAcknowledgmentRepository
{
    public async Task<OrganizationSalesDocumentAcknowledgment?> FindAsync(
        PlatformOrganizationId organizationId,
        PlatformUserId userId,
        string version,
        CancellationToken cancellationToken = default)
    {
        var record = await db.OrganizationSalesDocumentAcknowledgments
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.OrganizationId == organizationId.Value
                     && x.UserId == userId.Value
                     && x.Version == version,
                cancellationToken)
            .ConfigureAwait(false);

        return record is null
            ? null
            : OrganizationSalesDocumentAcknowledgment.Rehydrate(
                record.Id,
                PlatformOrganizationId.From(record.OrganizationId),
                PlatformUserId.From(record.UserId),
                record.Version,
                record.AcknowledgedAtUtc,
                record.ContentKey);
    }

    public Task AddAsync(
        OrganizationSalesDocumentAcknowledgment acknowledgment,
        CancellationToken cancellationToken = default)
    {
        db.OrganizationSalesDocumentAcknowledgments.Add(new OrganizationSalesDocumentAcknowledgmentRecord
        {
            Id = acknowledgment.Id,
            OrganizationId = acknowledgment.OrganizationId.Value,
            UserId = acknowledgment.UserId.Value,
            Version = acknowledgment.Version,
            AcknowledgedAtUtc = acknowledgment.AcknowledgedAtUtc,
            ContentKey = acknowledgment.ContentKey
        });
        return Task.CompletedTask;
    }
}

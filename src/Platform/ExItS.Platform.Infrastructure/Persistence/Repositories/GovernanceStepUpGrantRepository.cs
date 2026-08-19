using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Governance;
using ExItS.Platform.Domain.Governance;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Infrastructure.Persistence.Governance;
using Microsoft.EntityFrameworkCore;

namespace ExItS.Platform.Infrastructure.Persistence.Repositories;

internal sealed class GovernanceStepUpGrantRepository : IGovernanceStepUpGrantRepository
{
    private readonly PlatformDbContext _db;

    public GovernanceStepUpGrantRepository(PlatformDbContext db) => _db = db;

    public async Task<GovernanceStepUpGrant?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.GovernanceStepUpGrants
            .FirstOrDefaultAsync(g => g.TokenHash == tokenHash, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : ToDomain(record);
    }

    public Task AddAsync(GovernanceStepUpGrant grant, CancellationToken cancellationToken = default)
    {
        _db.GovernanceStepUpGrants.Add(ToRecord(grant));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(GovernanceStepUpGrant grant, CancellationToken cancellationToken = default)
    {
        var record = await _db.GovernanceStepUpGrants
            .FirstOrDefaultAsync(g => g.Id == grant.Id.Value, cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            throw new PersistenceConflictException(
                ApplicationErrorCodes.GovernanceStepUpInvalid,
                "Governance step-up grant was not found.");
        }

        record.ConsumedAtUtc = grant.ConsumedAtUtc;
        record.ExpiresAtUtc = grant.ExpiresAtUtc;
    }

    private static GovernanceStepUpGrant ToDomain(GovernanceStepUpGrantRecord record) =>
        GovernanceStepUpGrant.Rehydrate(
            GovernanceStepUpGrantId.From(record.Id),
            PlatformUserId.From(record.UserId),
            PlatformOrganizationId.From(record.OrganizationId),
            record.ActionCode,
            record.TargetType,
            record.TargetId,
            record.TokenHash,
            record.CreatedAtUtc,
            record.ExpiresAtUtc,
            record.ConsumedAtUtc);

    private static GovernanceStepUpGrantRecord ToRecord(GovernanceStepUpGrant grant) => new()
    {
        Id = grant.Id.Value,
        UserId = grant.UserId.Value,
        OrganizationId = grant.OrganizationId.Value,
        ActionCode = grant.ActionCode,
        TargetType = grant.TargetType,
        TargetId = grant.TargetId,
        TokenHash = grant.TokenHash,
        CreatedAtUtc = grant.CreatedAtUtc,
        ExpiresAtUtc = grant.ExpiresAtUtc,
        ConsumedAtUtc = grant.ConsumedAtUtc
    };
}

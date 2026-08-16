using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Infrastructure.Persistence.Organizations;
using Microsoft.EntityFrameworkCore;

namespace ExItS.Platform.Infrastructure.Persistence.Repositories;

internal sealed class BranchDeliveryPolicyRepository(PlatformDbContext db) : IBranchDeliveryPolicyRepository
{
    public async Task<BranchDeliveryPolicy?> GetByBranchIdAsync(OrganizationBranchId branchId, CancellationToken cancellationToken = default)
    {
        var record = await db.BranchDeliveryPolicies.AsNoTracking()
            .FirstOrDefaultAsync(x => x.BranchId == branchId.Value, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : OrganizationBranchDeviceEntityMapper.ToDomain(record);
    }

    public async Task<IReadOnlyList<BranchDeliveryPolicy>> ListByOrganizationAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var records = await db.BranchDeliveryPolicies.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId.Value)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(OrganizationBranchDeviceEntityMapper.ToDomain).ToList();
    }

    public Task AddAsync(BranchDeliveryPolicy policy, CancellationToken cancellationToken = default)
    {
        db.BranchDeliveryPolicies.Add(OrganizationBranchDeviceEntityMapper.ToRecord(policy));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(BranchDeliveryPolicy policy, CancellationToken cancellationToken = default)
    {
        var record = await db.BranchDeliveryPolicies
            .FirstOrDefaultAsync(x => x.BranchId == policy.BranchId.Value, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Branch delivery policy was not found.");
        OrganizationBranchDeviceEntityMapper.ApplyToRecord(policy, record);
    }
}

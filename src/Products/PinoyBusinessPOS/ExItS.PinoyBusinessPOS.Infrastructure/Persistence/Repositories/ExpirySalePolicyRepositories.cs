using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Inventory;
using Microsoft.EntityFrameworkCore;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;

internal sealed class OrganizationExpirySalePolicyRepository : IOrganizationExpirySalePolicyRepository
{
    private readonly PosDbContext _db;

    public OrganizationExpirySalePolicyRepository(PosDbContext db) => _db = db;

    public async Task<OrganizationExpirySalePolicySetting?> GetAsync(
        PosOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.OrganizationExpirySalePolicySettings
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.OrganizationId == organizationId.Value, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : ToDomain(record);
    }

    public async Task UpsertAsync(
        OrganizationExpirySalePolicySetting setting,
        CancellationToken cancellationToken = default)
    {
        var record = _db.OrganizationExpirySalePolicySettings.Local.FirstOrDefault(r =>
                r.OrganizationId == setting.OrganizationId.Value)
            ?? await _db.OrganizationExpirySalePolicySettings
                .FirstOrDefaultAsync(r => r.OrganizationId == setting.OrganizationId.Value, cancellationToken)
                .ConfigureAwait(false);

        if (record is null)
        {
            _db.OrganizationExpirySalePolicySettings.Add(ToRecord(setting));
            return;
        }

        record.StopSellingDaysBeforeExpiry = setting.StopSellingDaysBeforeExpiry;
        record.UpdatedAtUtc = setting.UpdatedAtUtc;
        record.UpdatedBy = setting.UpdatedBy;
    }

    private static OrganizationExpirySalePolicySetting ToDomain(OrganizationExpirySalePolicySettingRecord record) =>
        OrganizationExpirySalePolicySetting.Rehydrate(
            PosOrganizationId.From(record.OrganizationId),
            record.StopSellingDaysBeforeExpiry,
            record.UpdatedAtUtc,
            record.UpdatedBy);

    private static OrganizationExpirySalePolicySettingRecord ToRecord(OrganizationExpirySalePolicySetting setting) =>
        new()
        {
            OrganizationId = setting.OrganizationId.Value,
            StopSellingDaysBeforeExpiry = setting.StopSellingDaysBeforeExpiry,
            UpdatedAtUtc = setting.UpdatedAtUtc,
            UpdatedBy = setting.UpdatedBy
        };
}

internal sealed class OrganizationCategoryExpirySalePolicyRepository : IOrganizationCategoryExpirySalePolicyRepository
{
    private readonly PosDbContext _db;

    public OrganizationCategoryExpirySalePolicyRepository(PosDbContext db) => _db = db;

    public async Task<OrganizationCategoryExpirySalePolicy?> GetAsync(
        PosOrganizationId organizationId,
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.OrganizationCategoryExpirySalePolicies
            .AsNoTracking()
            .FirstOrDefaultAsync(
                r => r.OrganizationId == organizationId.Value && r.CategoryId == categoryId,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : ToDomain(record);
    }

    public async Task<IReadOnlyList<OrganizationCategoryExpirySalePolicy>> ListByOrganizationAsync(
        PosOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var records = await _db.OrganizationCategoryExpirySalePolicies
            .AsNoTracking()
            .Where(r => r.OrganizationId == organizationId.Value)
            .OrderBy(r => r.CategoryId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(ToDomain).ToList();
    }

    public async Task UpsertAsync(
        OrganizationCategoryExpirySalePolicy policy,
        CancellationToken cancellationToken = default)
    {
        var record = _db.OrganizationCategoryExpirySalePolicies.Local.FirstOrDefault(r =>
                r.OrganizationId == policy.OrganizationId.Value
                && r.CategoryId == policy.CategoryId)
            ?? await _db.OrganizationCategoryExpirySalePolicies
                .FirstOrDefaultAsync(
                    r => r.OrganizationId == policy.OrganizationId.Value
                        && r.CategoryId == policy.CategoryId,
                    cancellationToken)
                .ConfigureAwait(false);

        if (record is null)
        {
            _db.OrganizationCategoryExpirySalePolicies.Add(ToRecord(policy));
            return;
        }

        record.StopSellingDaysBeforeExpiry = policy.StopSellingDaysBeforeExpiry;
        record.UpdatedAtUtc = policy.UpdatedAtUtc;
        record.UpdatedBy = policy.UpdatedBy;
    }

    public async Task DeleteAsync(
        PosOrganizationId organizationId,
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.OrganizationCategoryExpirySalePolicies
            .FirstOrDefaultAsync(
                r => r.OrganizationId == organizationId.Value && r.CategoryId == categoryId,
                cancellationToken)
            .ConfigureAwait(false);
        if (record is not null)
        {
            _db.OrganizationCategoryExpirySalePolicies.Remove(record);
        }
    }

    private static OrganizationCategoryExpirySalePolicy ToDomain(OrganizationCategoryExpirySalePolicyRecord record) =>
        OrganizationCategoryExpirySalePolicy.Rehydrate(
            PosOrganizationId.From(record.OrganizationId),
            record.CategoryId,
            record.StopSellingDaysBeforeExpiry,
            record.UpdatedAtUtc,
            record.UpdatedBy);

    private static OrganizationCategoryExpirySalePolicyRecord ToRecord(OrganizationCategoryExpirySalePolicy policy) =>
        new()
        {
            OrganizationId = policy.OrganizationId.Value,
            CategoryId = policy.CategoryId,
            StopSellingDaysBeforeExpiry = policy.StopSellingDaysBeforeExpiry,
            UpdatedAtUtc = policy.UpdatedAtUtc,
            UpdatedBy = policy.UpdatedBy
        };
}

internal sealed class BranchExpirySalePolicyRepository : IBranchExpirySalePolicyRepository
{
    private readonly PosDbContext _db;

    public BranchExpirySalePolicyRepository(PosDbContext db) => _db = db;

    public async Task<BranchExpirySalePolicySetting?> GetAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.BranchExpirySalePolicySettings
            .AsNoTracking()
            .FirstOrDefaultAsync(
                r => r.OrganizationId == organizationId.Value && r.BranchId == branchId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : ToDomain(record);
    }

    public async Task UpsertAsync(
        BranchExpirySalePolicySetting setting,
        CancellationToken cancellationToken = default)
    {
        var record = _db.BranchExpirySalePolicySettings.Local.FirstOrDefault(r =>
                r.OrganizationId == setting.OrganizationId.Value
                && r.BranchId == setting.BranchId.Value)
            ?? await _db.BranchExpirySalePolicySettings
                .FirstOrDefaultAsync(
                    r => r.OrganizationId == setting.OrganizationId.Value
                        && r.BranchId == setting.BranchId.Value,
                    cancellationToken)
                .ConfigureAwait(false);

        if (record is null)
        {
            _db.BranchExpirySalePolicySettings.Add(ToRecord(setting));
            return;
        }

        record.StopSellingDaysBeforeExpiry = setting.StopSellingDaysBeforeExpiry;
        record.UpdatedAtUtc = setting.UpdatedAtUtc;
        record.UpdatedBy = setting.UpdatedBy;
    }

    public async Task DeleteAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.BranchExpirySalePolicySettings
            .FirstOrDefaultAsync(
                r => r.OrganizationId == organizationId.Value && r.BranchId == branchId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        if (record is not null)
        {
            _db.BranchExpirySalePolicySettings.Remove(record);
        }
    }

    private static BranchExpirySalePolicySetting ToDomain(BranchExpirySalePolicySettingRecord record) =>
        BranchExpirySalePolicySetting.Rehydrate(
            PosOrganizationId.From(record.OrganizationId),
            PosBranchId.From(record.BranchId),
            record.StopSellingDaysBeforeExpiry,
            record.UpdatedAtUtc,
            record.UpdatedBy);

    private static BranchExpirySalePolicySettingRecord ToRecord(BranchExpirySalePolicySetting setting) =>
        new()
        {
            OrganizationId = setting.OrganizationId.Value,
            BranchId = setting.BranchId.Value,
            StopSellingDaysBeforeExpiry = setting.StopSellingDaysBeforeExpiry,
            UpdatedAtUtc = setting.UpdatedAtUtc,
            UpdatedBy = setting.UpdatedBy
        };
}

internal sealed class BranchCategoryExpirySalePolicyRepository : IBranchCategoryExpirySalePolicyRepository
{
    private readonly PosDbContext _db;

    public BranchCategoryExpirySalePolicyRepository(PosDbContext db) => _db = db;

    public async Task<BranchCategoryExpirySalePolicy?> GetAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.BranchCategoryExpirySalePolicies
            .AsNoTracking()
            .FirstOrDefaultAsync(
                r => r.OrganizationId == organizationId.Value
                    && r.BranchId == branchId.Value
                    && r.CategoryId == categoryId,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : ToDomain(record);
    }

    public async Task<IReadOnlyList<BranchCategoryExpirySalePolicy>> ListByBranchAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        CancellationToken cancellationToken = default)
    {
        var records = await _db.BranchCategoryExpirySalePolicies
            .AsNoTracking()
            .Where(r => r.OrganizationId == organizationId.Value && r.BranchId == branchId.Value)
            .OrderBy(r => r.CategoryId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(ToDomain).ToList();
    }

    public async Task UpsertAsync(
        BranchCategoryExpirySalePolicy policy,
        CancellationToken cancellationToken = default)
    {
        var record = _db.BranchCategoryExpirySalePolicies.Local.FirstOrDefault(r =>
                r.OrganizationId == policy.OrganizationId.Value
                && r.BranchId == policy.BranchId.Value
                && r.CategoryId == policy.CategoryId)
            ?? await _db.BranchCategoryExpirySalePolicies
                .FirstOrDefaultAsync(
                    r => r.OrganizationId == policy.OrganizationId.Value
                        && r.BranchId == policy.BranchId.Value
                        && r.CategoryId == policy.CategoryId,
                    cancellationToken)
                .ConfigureAwait(false);

        if (record is null)
        {
            _db.BranchCategoryExpirySalePolicies.Add(ToRecord(policy));
            return;
        }

        record.StopSellingDaysBeforeExpiry = policy.StopSellingDaysBeforeExpiry;
        record.UpdatedAtUtc = policy.UpdatedAtUtc;
        record.UpdatedBy = policy.UpdatedBy;
    }

    public async Task DeleteAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.BranchCategoryExpirySalePolicies
            .FirstOrDefaultAsync(
                r => r.OrganizationId == organizationId.Value
                    && r.BranchId == branchId.Value
                    && r.CategoryId == categoryId,
                cancellationToken)
            .ConfigureAwait(false);
        if (record is not null)
        {
            _db.BranchCategoryExpirySalePolicies.Remove(record);
        }
    }

    private static BranchCategoryExpirySalePolicy ToDomain(BranchCategoryExpirySalePolicyRecord record) =>
        BranchCategoryExpirySalePolicy.Rehydrate(
            PosOrganizationId.From(record.OrganizationId),
            PosBranchId.From(record.BranchId),
            record.CategoryId,
            record.StopSellingDaysBeforeExpiry,
            record.UpdatedAtUtc,
            record.UpdatedBy);

    private static BranchCategoryExpirySalePolicyRecord ToRecord(BranchCategoryExpirySalePolicy policy) =>
        new()
        {
            OrganizationId = policy.OrganizationId.Value,
            BranchId = policy.BranchId.Value,
            CategoryId = policy.CategoryId,
            StopSellingDaysBeforeExpiry = policy.StopSellingDaysBeforeExpiry,
            UpdatedAtUtc = policy.UpdatedAtUtc,
            UpdatedBy = policy.UpdatedBy
        };
}

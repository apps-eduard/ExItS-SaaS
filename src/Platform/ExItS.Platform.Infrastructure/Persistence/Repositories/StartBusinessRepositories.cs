using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Personal;
using ExItS.Platform.Infrastructure.Persistence.Organizations;
using Microsoft.EntityFrameworkCore;

namespace ExItS.Platform.Infrastructure.Persistence.Repositories;

internal sealed class BusinessCreditOpeningBalanceRepository(PlatformDbContext db)
    : IBusinessCreditOpeningBalanceRepository
{
    public async Task<BusinessCreditOpeningBalance?> GetByIdAsync(
        BusinessCreditOpeningBalanceId id,
        CancellationToken cancellationToken = default)
    {
        var record = await db.BusinessCreditOpeningBalances.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id.Value, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : ToDomain(record);
    }

    public async Task<IReadOnlyList<BusinessCreditOpeningBalance>> ListByOrganizationAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var records = await db.BusinessCreditOpeningBalances.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId.Value)
            .OrderByDescending(x => x.ImportedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(ToDomain).ToList();
    }

    public Task AddAsync(BusinessCreditOpeningBalance balance, CancellationToken cancellationToken = default)
    {
        db.BusinessCreditOpeningBalances.Add(ToRecord(balance));
        return Task.CompletedTask;
    }

    private static BusinessCreditOpeningBalance ToDomain(BusinessCreditOpeningBalanceRecord record) =>
        BusinessCreditOpeningBalance.Rehydrate(
            BusinessCreditOpeningBalanceId.From(record.Id),
            PlatformOrganizationId.From(record.OrganizationId),
            CreditCustomerId.From(record.CreditCustomerId),
            BusinessCustomerId.From(record.BusinessCustomerId),
            record.Amount,
            record.CurrencyCode,
            record.EffectiveDateUtc,
            Enum.Parse<PersonalUtangMigrationSourceType>(record.SourceType, ignoreCase: true),
            record.SourceRecordId,
            PersonalUtangMigrationBatchId.From(record.MigrationBatchId),
            PlatformUserId.From(record.ImportedByUserId),
            record.ImportedAtUtc,
            record.DestinationProduct);

    private static BusinessCreditOpeningBalanceRecord ToRecord(BusinessCreditOpeningBalance balance) =>
        new()
        {
            Id = balance.Id.Value,
            OrganizationId = balance.OrganizationId.Value,
            CreditCustomerId = balance.CreditCustomerId.Value,
            BusinessCustomerId = balance.BusinessCustomerId.Value,
            Amount = balance.Amount,
            CurrencyCode = balance.CurrencyCode,
            EffectiveDateUtc = balance.EffectiveDateUtc,
            SourceType = balance.SourceType.ToString(),
            SourceRecordId = balance.SourceRecordId,
            MigrationBatchId = balance.MigrationBatchId.Value,
            ImportedByUserId = balance.ImportedByUserId.Value,
            ImportedAtUtc = balance.ImportedAtUtc,
            DestinationProduct = balance.DestinationProduct
        };
}

internal sealed class ProductLocalRoleGrantRepository(PlatformDbContext db) : IProductLocalRoleGrantRepository
{
    public async Task<ProductLocalRoleGrant?> GetByIdAsync(
        ProductLocalRoleGrantId id,
        CancellationToken cancellationToken = default)
    {
        var record = await db.ProductLocalRoleGrants.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id.Value, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : ToDomain(record);
    }

    public async Task<ProductLocalRoleGrant?> FindAsync(
        PlatformOrganizationId organizationId,
        PlatformUserId userIdentityId,
        string productCode,
        string roleCode,
        CancellationToken cancellationToken = default)
    {
        var product = productCode.Trim().ToLowerInvariant();
        var role = roleCode.Trim();
        var record = await db.ProductLocalRoleGrants.AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.OrganizationId == organizationId.Value
                     && x.UserIdentityId == userIdentityId.Value
                     && x.ProductCode == product
                     && x.RoleCode == role
                     && x.Status == nameof(ProductLocalRoleGrantStatus.Active),
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : ToDomain(record);
    }

    public async Task<ProductLocalRoleGrant?> FindActiveByUserOrganizationProductAsync(
        PlatformOrganizationId organizationId,
        PlatformUserId userIdentityId,
        string productCode,
        CancellationToken cancellationToken = default)
    {
        var product = productCode.Trim().ToLowerInvariant();
        var record = await db.ProductLocalRoleGrants.AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.OrganizationId == organizationId.Value
                     && x.UserIdentityId == userIdentityId.Value
                     && x.ProductCode == product
                     && x.Status == nameof(ProductLocalRoleGrantStatus.Active),
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : ToDomain(record);
    }

    public async Task<IReadOnlyList<ProductLocalRoleGrant>> ListByOrganizationAsync(
        PlatformOrganizationId organizationId,
        ProductLocalRoleGrantStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = db.ProductLocalRoleGrants.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId.Value);
        if (status is not null)
        {
            var statusName = status.Value.ToString();
            query = query.Where(x => x.Status == statusName);
        }

        var records = await query
            .OrderBy(x => x.ProductCode)
            .ThenBy(x => x.UserIdentityId)
            .ThenByDescending(x => x.GrantedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(ToDomain).ToList();
    }

    public async Task<IReadOnlyList<ProductLocalRoleGrant>> ListActiveByUserOrganizationAsync(
        PlatformOrganizationId organizationId,
        PlatformUserId userIdentityId,
        CancellationToken cancellationToken = default)
    {
        var records = await db.ProductLocalRoleGrants.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId.Value
                        && x.UserIdentityId == userIdentityId.Value
                        && x.Status == nameof(ProductLocalRoleGrantStatus.Active))
            .OrderBy(x => x.ProductCode)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(ToDomain).ToList();
    }

    public Task AddAsync(ProductLocalRoleGrant grant, CancellationToken cancellationToken = default)
    {
        db.ProductLocalRoleGrants.Add(ToRecord(grant));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(ProductLocalRoleGrant grant, CancellationToken cancellationToken = default)
    {
        var record = await db.ProductLocalRoleGrants
            .FirstOrDefaultAsync(x => x.Id == grant.Id.Value, cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            return;
        }

        record.RoleCode = grant.RoleCode;
        record.Status = grant.Status.ToString();
        record.RevokedAtUtc = grant.RevokedAtUtc;
        record.RevokedByUserIdentityId = grant.RevokedByUserIdentityId?.Value;
        record.Reason = grant.Reason;
        record.Source = grant.Source;
    }

    private static ProductLocalRoleGrant ToDomain(ProductLocalRoleGrantRecord record) =>
        ProductLocalRoleGrant.Rehydrate(
            ProductLocalRoleGrantId.From(record.Id),
            PlatformOrganizationId.From(record.OrganizationId),
            PlatformUserId.From(record.UserIdentityId),
            record.ProductCode,
            record.RoleCode,
            Enum.TryParse<ProductLocalRoleGrantStatus>(record.Status, ignoreCase: true, out var status)
                ? status
                : ProductLocalRoleGrantStatus.Active,
            record.GrantedAtUtc,
            PlatformUserId.From(record.GrantedByUserIdentityId),
            record.Source,
            record.RevokedAtUtc,
            record.RevokedByUserIdentityId is Guid revokedBy
                ? PlatformUserId.From(revokedBy)
                : null,
            record.Reason);

    private static ProductLocalRoleGrantRecord ToRecord(ProductLocalRoleGrant grant) =>
        new()
        {
            Id = grant.Id.Value,
            OrganizationId = grant.OrganizationId.Value,
            UserIdentityId = grant.UserIdentityId.Value,
            ProductCode = grant.ProductCode.ToLowerInvariant(),
            RoleCode = grant.RoleCode,
            Status = grant.Status.ToString(),
            GrantedAtUtc = grant.GrantedAtUtc,
            GrantedByUserIdentityId = grant.GrantedByUserIdentityId.Value,
            Source = grant.Source,
            RevokedAtUtc = grant.RevokedAtUtc,
            RevokedByUserIdentityId = grant.RevokedByUserIdentityId?.Value,
            Reason = grant.Reason
        };
}

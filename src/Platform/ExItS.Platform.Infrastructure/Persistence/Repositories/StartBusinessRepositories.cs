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
                     && x.RoleCode == role,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : ToDomain(record);
    }

    public Task AddAsync(ProductLocalRoleGrant grant, CancellationToken cancellationToken = default)
    {
        db.ProductLocalRoleGrants.Add(ToRecord(grant));
        return Task.CompletedTask;
    }

    private static ProductLocalRoleGrant ToDomain(ProductLocalRoleGrantRecord record) =>
        ProductLocalRoleGrant.Rehydrate(
            ProductLocalRoleGrantId.From(record.Id),
            PlatformOrganizationId.From(record.OrganizationId),
            PlatformUserId.From(record.UserIdentityId),
            record.ProductCode,
            record.RoleCode,
            record.GrantedAtUtc,
            PlatformUserId.From(record.GrantedByUserIdentityId),
            record.Source);

    private static ProductLocalRoleGrantRecord ToRecord(ProductLocalRoleGrant grant) =>
        new()
        {
            Id = grant.Id.Value,
            OrganizationId = grant.OrganizationId.Value,
            UserIdentityId = grant.UserIdentityId.Value,
            ProductCode = grant.ProductCode.ToLowerInvariant(),
            RoleCode = grant.RoleCode,
            GrantedAtUtc = grant.GrantedAtUtc,
            GrantedByUserIdentityId = grant.GrantedByUserIdentityId.Value,
            Source = grant.Source
        };
}

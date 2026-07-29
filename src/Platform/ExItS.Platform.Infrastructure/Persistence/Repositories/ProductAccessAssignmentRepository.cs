using ExItS.Platform.Application.Access;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.Infrastructure.Persistence.Access;
using Microsoft.EntityFrameworkCore;

namespace ExItS.Platform.Infrastructure.Persistence.Repositories;

internal sealed class ProductAccessAssignmentRepository : IProductAccessAssignmentRepository
{
    private readonly PlatformDbContext _db;

    public ProductAccessAssignmentRepository(PlatformDbContext db) => _db = db;

    public async Task<ProductAccessAssignment?> GetByIdAsync(
        ProductAccessAssignmentId id,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.ProductAccessAssignments.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id.Value, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : IdentityAccessEntityMapper.ToAssignmentDomain(record);
    }

    public async Task<ProductAccessAssignment?> FindActiveByUserOrganizationProductAsync(
        PlatformUserId userId,
        PlatformOrganizationId organizationId,
        ProductCode productCode,
        CancellationToken cancellationToken = default)
    {
        var active = nameof(ProductAccessStatus.Active);
        var record = await _db.ProductAccessAssignments.AsNoTracking()
            .FirstOrDefaultAsync(
                a => a.UserId == userId.Value
                     && a.OrganizationId == organizationId.Value
                     && a.ProductCode == productCode.Value
                     && a.Status == active,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : IdentityAccessEntityMapper.ToAssignmentDomain(record);
    }

    public async Task<(IReadOnlyList<ProductAccessAssignment> Items, int TotalCount)> ListByOrganizationAsync(
        PlatformOrganizationId organizationId,
        ProductAccessStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _db.ProductAccessAssignments.AsNoTracking()
            .Where(a => a.OrganizationId == organizationId.Value);
        if (status is not null)
        {
            var statusName = status.Value.ToString();
            query = query.Where(a => a.Status == statusName);
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query.OrderByDescending(a => a.GrantedAtUtc).Skip(skip).Take(take)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return (records.Select(IdentityAccessEntityMapper.ToAssignmentDomain).ToList(), total);
    }

    public async Task<(IReadOnlyList<ProductAccessAssignment> Items, int TotalCount)> ListByUserAsync(
        PlatformUserId userId,
        ProductAccessStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _db.ProductAccessAssignments.AsNoTracking()
            .Where(a => a.UserId == userId.Value);
        if (status is not null)
        {
            var statusName = status.Value.ToString();
            query = query.Where(a => a.Status == statusName);
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query.OrderByDescending(a => a.GrantedAtUtc).Skip(skip).Take(take)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return (records.Select(IdentityAccessEntityMapper.ToAssignmentDomain).ToList(), total);
    }

    public async Task<(IReadOnlyList<ProductAccessAssignment> Items, int TotalCount)> ListByProductAsync(
        ProductCode productCode,
        ProductAccessStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _db.ProductAccessAssignments.AsNoTracking()
            .Where(a => a.ProductCode == productCode.Value);
        if (status is not null)
        {
            var statusName = status.Value.ToString();
            query = query.Where(a => a.Status == statusName);
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query.OrderByDescending(a => a.GrantedAtUtc).Skip(skip).Take(take)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return (records.Select(IdentityAccessEntityMapper.ToAssignmentDomain).ToList(), total);
    }

    public async Task<IReadOnlyList<ProductAccessAssignment>> ListActiveByMembershipAsync(
        OrganizationMembershipId membershipId,
        CancellationToken cancellationToken = default)
    {
        var active = nameof(ProductAccessStatus.Active);
        var records = await _db.ProductAccessAssignments.AsNoTracking()
            .Where(a => a.MembershipId == membershipId.Value && a.Status == active)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(IdentityAccessEntityMapper.ToAssignmentDomain).ToList();
    }

    public Task AddAsync(ProductAccessAssignment assignment, CancellationToken cancellationToken = default)
    {
        _db.ProductAccessAssignments.Add(IdentityAccessEntityMapper.ToAssignmentRecord(assignment));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(ProductAccessAssignment assignment, CancellationToken cancellationToken = default)
    {
        var record = await _db.ProductAccessAssignments
            .FirstOrDefaultAsync(a => a.Id == assignment.Id.Value, cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            throw new PersistenceConflictException(
                ApplicationErrorCodes.ProductAccessNotFound,
                "Product access assignment was not found.");
        }

        IdentityAccessEntityMapper.ApplyToAssignmentRecord(assignment, record);
    }
}

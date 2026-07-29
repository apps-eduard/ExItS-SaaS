using ExItS.Platform.Application.Access;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;

namespace ExItS.Platform.UnitTests.Support;

internal sealed class InMemoryProductAccessAssignmentRepository : IProductAccessAssignmentRepository
{
    private readonly Dictionary<Guid, ProductAccessAssignment> _byId = new();

    public int AddCount { get; private set; }
    public int UpdateCount { get; private set; }

    public Task<ProductAccessAssignment?> GetByIdAsync(
        ProductAccessAssignmentId id,
        CancellationToken cancellationToken = default)
    {
        _byId.TryGetValue(id.Value, out var assignment);
        return Task.FromResult(assignment);
    }

    public Task<ProductAccessAssignment?> FindActiveByUserOrganizationProductAsync(
        PlatformUserId userId,
        PlatformOrganizationId organizationId,
        ProductCode productCode,
        CancellationToken cancellationToken = default)
    {
        var match = _byId.Values.FirstOrDefault(a =>
            a.UserId == userId
            && a.OrganizationId == organizationId
            && a.ProductCode == productCode
            && a.Status == ProductAccessStatus.Active);
        return Task.FromResult(match);
    }

    public Task<(IReadOnlyList<ProductAccessAssignment> Items, int TotalCount)> ListByOrganizationAsync(
        PlatformOrganizationId organizationId,
        ProductAccessStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _byId.Values.Where(a => a.OrganizationId == organizationId);
        if (status is not null)
        {
            query = query.Where(a => a.Status == status);
        }

        var ordered = query.OrderByDescending(a => a.GrantedAtUtc).ToList();
        return Task.FromResult<(IReadOnlyList<ProductAccessAssignment>, int)>(
            (ordered.Skip(skip).Take(take).ToList(), ordered.Count));
    }

    public Task<(IReadOnlyList<ProductAccessAssignment> Items, int TotalCount)> ListByUserAsync(
        PlatformUserId userId,
        ProductAccessStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _byId.Values.Where(a => a.UserId == userId);
        if (status is not null)
        {
            query = query.Where(a => a.Status == status);
        }

        var ordered = query.OrderByDescending(a => a.GrantedAtUtc).ToList();
        return Task.FromResult<(IReadOnlyList<ProductAccessAssignment>, int)>(
            (ordered.Skip(skip).Take(take).ToList(), ordered.Count));
    }

    public Task<(IReadOnlyList<ProductAccessAssignment> Items, int TotalCount)> ListByProductAsync(
        ProductCode productCode,
        ProductAccessStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _byId.Values.Where(a => a.ProductCode == productCode);
        if (status is not null)
        {
            query = query.Where(a => a.Status == status);
        }

        var ordered = query.OrderByDescending(a => a.GrantedAtUtc).ToList();
        return Task.FromResult<(IReadOnlyList<ProductAccessAssignment>, int)>(
            (ordered.Skip(skip).Take(take).ToList(), ordered.Count));
    }

    public Task<IReadOnlyList<ProductAccessAssignment>> ListActiveByMembershipAsync(
        OrganizationMembershipId membershipId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ProductAccessAssignment> list = _byId.Values
            .Where(a => a.MembershipId == membershipId && a.Status == ProductAccessStatus.Active)
            .ToList();
        return Task.FromResult(list);
    }

    public Task AddAsync(ProductAccessAssignment assignment, CancellationToken cancellationToken = default)
    {
        _byId[assignment.Id.Value] = assignment;
        AddCount++;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(ProductAccessAssignment assignment, CancellationToken cancellationToken = default)
    {
        _byId[assignment.Id.Value] = assignment;
        UpdateCount++;
        return Task.CompletedTask;
    }
}

using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using Microsoft.AspNetCore.Http;

namespace ExItS.PinoyBusinessPOS.IntegrationTests.Support;

/// <summary>
/// Simulates Platform caller-access-filtered branch lists for MB2-02C-H1 security proofs.
/// </summary>
internal sealed class H1ProofBranchDirectoryOptions
{
    public Guid PrimaryBranchId { get; set; } = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public Dictionary<Guid, HashSet<Guid>> OrganizationBranches { get; } = new();

    public Dictionary<Guid, HashSet<Guid>> ActorAccessibleBranches { get; } = new();

    public HashSet<(Guid OrgId, Guid BranchId)> InactiveBranches { get; } = new();

    public void RegisterOrganization(Guid orgId, params Guid[] branchIds) =>
        OrganizationBranches[orgId] = branchIds.ToHashSet();

    public void RestrictActor(Guid actorId, params Guid[] branchIds) =>
        ActorAccessibleBranches[actorId] = branchIds.ToHashSet();

    public void SetInactive(Guid orgId, Guid branchId) => InactiveBranches.Add((orgId, branchId));
}

internal sealed class H1ProofOrganizationBranchDirectory(
    IHttpContextAccessor httpContextAccessor,
    H1ProofBranchDirectoryOptions options) : IOrganizationBranchDirectory
{
    public Task<bool> ExistsInOrganizationAsync(
        Guid organizationId,
        Guid branchId,
        CancellationToken cancellationToken = default)
    {
        if (branchId == Guid.Empty)
        {
            return Task.FromResult(false);
        }

        var names = GetAccessibleNames(organizationId, [branchId]);
        return Task.FromResult(names.ContainsKey(branchId));
    }

    public Task<IReadOnlyDictionary<Guid, string>> GetNamesAsync(
        Guid organizationId,
        IReadOnlyCollection<Guid> branchIds,
        CancellationToken cancellationToken = default)
    {
        var wanted = branchIds.Where(id => id != Guid.Empty).Distinct().ToList();
        if (wanted.Count == 0)
        {
            return Task.FromResult<IReadOnlyDictionary<Guid, string>>(new Dictionary<Guid, string>());
        }

        return Task.FromResult(GetAccessibleNames(organizationId, wanted));
    }

    public Task<bool> IsActiveInOrganizationAsync(
        Guid organizationId,
        Guid branchId,
        CancellationToken cancellationToken = default)
    {
        if (branchId == Guid.Empty)
        {
            return Task.FromResult(false);
        }

        if (options.InactiveBranches.Contains((organizationId, branchId)))
        {
            return Task.FromResult(false);
        }

        var names = GetAccessibleNames(organizationId, [branchId]);
        return Task.FromResult(names.ContainsKey(branchId));
    }

    public Task<Guid?> GetPrimaryBranchIdAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<Guid?>(options.PrimaryBranchId);

    private IReadOnlyDictionary<Guid, string> GetAccessibleNames(
        Guid organizationId,
        IReadOnlyCollection<Guid> branchIds)
    {
        if (!options.OrganizationBranches.TryGetValue(organizationId, out var orgBranches))
        {
            return new Dictionary<Guid, string>();
        }

        var actorId = ResolveActorId();
        var accessible = ResolveAccessibleBranches(actorId, orgBranches);
        return branchIds
            .Where(id => accessible.Contains(id))
            .ToDictionary(id => id, _ => "Branch");
    }

    private HashSet<Guid> ResolveAccessibleBranches(Guid actorId, HashSet<Guid> orgBranches)
    {
        if (options.ActorAccessibleBranches.TryGetValue(actorId, out var restricted))
        {
            return orgBranches.Where(restricted.Contains).ToHashSet();
        }

        return orgBranches;
    }

    private Guid ResolveActorId()
    {
        var request = httpContextAccessor.HttpContext?.Request;
        if (request is not null
            && request.Headers.TryGetValue(PosOrganizationHeaders.ActorHeaderName, out var values)
            && Guid.TryParse(values.FirstOrDefault(), out var actorId))
        {
            return actorId;
        }

        return Guid.Empty;
    }
}

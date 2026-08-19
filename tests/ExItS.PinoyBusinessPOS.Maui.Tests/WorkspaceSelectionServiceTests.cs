using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Auth;

namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class WorkspaceSelectionServiceTests
{
    private static readonly Guid OrgA = Guid.NewGuid();
    private static readonly Guid OrgB = Guid.NewGuid();
    private static readonly Guid MainA = Guid.NewGuid();
    private static readonly Guid BranchA2 = Guid.NewGuid();
    private static readonly Guid MainB = Guid.NewGuid();

    [Fact]
    public async Task ResolveRoutingPlan_single_org_single_branch_auto_selects()
    {
        var sut = CreateSut([
            Org(OrgA, "Kizy Store", MainA)
        ]);

        var plan = await sut.ResolveRoutingPlanAsync(Guid.NewGuid());

        Assert.Equal(WorkspaceRoutingOutcome.AutoSelect, plan.Outcome);
        Assert.Equal(OrgA, plan.AutoOrganizationId);
        Assert.Equal(MainA, plan.AutoBranchId);
    }

    [Fact]
    public async Task ResolveRoutingPlan_single_org_multiple_branches_shows_chooser()
    {
        var sut = CreateSut([
            Org(OrgA, "Kizy Store", MainA, BranchA2)
        ]);

        var plan = await sut.ResolveRoutingPlanAsync(Guid.NewGuid());

        Assert.Equal(WorkspaceRoutingOutcome.ShowChooser, plan.Outcome);
    }

    [Fact]
    public async Task ResolveRoutingPlan_multiple_orgs_shows_chooser()
    {
        var sut = CreateSut([
            Org(OrgA, "Kizy Store", MainA),
            Org(OrgB, "Kizy Mech", MainB)
        ]);

        var plan = await sut.ResolveRoutingPlanAsync(Guid.NewGuid());

        Assert.Equal(WorkspaceRoutingOutcome.ShowChooser, plan.Outcome);
    }

    [Fact]
    public async Task ResolveRoutingPlan_no_orgs_routes_personal_home()
    {
        var sut = CreateSut([]);

        var plan = await sut.ResolveRoutingPlanAsync(Guid.NewGuid());

        Assert.Equal(WorkspaceRoutingOutcome.PersonalHome, plan.Outcome);
    }

    [Fact]
    public async Task ListWorkspaces_skips_orgs_without_active_branches()
    {
        var sut = new WorkspaceSelectionService(
            new FakeAccess([
                new EligibleOrganization(OrgA, "Empty Org", Guid.NewGuid(), "Active", true, "allowed")
            ]),
            new FakeBranchResolver(new Dictionary<Guid, IReadOnlyList<AccessibleWorkspaceBranch>>()));

        var workspaces = await sut.ListWorkspacesAsync(Guid.NewGuid());

        Assert.Empty(workspaces);
        var plan = await sut.ResolveRoutingPlanAsync(Guid.NewGuid());
        Assert.Equal(WorkspaceRoutingOutcome.NoAccessibleBranch, plan.Outcome);
    }

    private static WorkspaceSelectionService CreateSut(IReadOnlyList<(EligibleOrganization Org, Guid[] Branches)> orgs)
    {
        var access = new FakeAccess(orgs.Select(o => o.Org).ToList());
        var branchMap = orgs.ToDictionary(
            o => o.Org.OrganizationId,
            o => (IReadOnlyList<AccessibleWorkspaceBranch>)o.Branches
                .Select((id, index) => new AccessibleWorkspaceBranch(
                    id,
                    index == 0 ? "Main Branch" : $"Branch {index + 1:00}",
                    index == 0 ? "Primary branch" : "Active",
                    index == 0,
                    true))
                .ToList());
        return new WorkspaceSelectionService(access, new FakeBranchResolver(branchMap));
    }

    private static (EligibleOrganization Org, Guid[] Branches) Org(Guid orgId, string name, params Guid[] branches) =>
        (new EligibleOrganization(orgId, name, Guid.NewGuid(), "Active", true, "allowed", "OrganizationOwner"), branches);

    private sealed class FakeAccess(IReadOnlyList<EligibleOrganization> orgs) : IProductAccessResolver
    {
        public Task<AuthResult> EvaluateAsync(Guid userId, Guid organizationId, CancellationToken ct = default) =>
            Task.FromResult(new AuthResult(true, AuthFailureReason.None));

        public Task<IReadOnlyList<EligibleOrganization>> ListEligibleOrganizationsAsync(
            Guid userId,
            CancellationToken ct = default) =>
            Task.FromResult(orgs);
    }

    private sealed class FakeBranchResolver(IReadOnlyDictionary<Guid, IReadOnlyList<AccessibleWorkspaceBranch>> branches)
        : IAccessibleBranchResolver
    {
        public Task<IReadOnlyList<AccessibleWorkspaceBranch>> ListAccessibleBranchesAsync(
            Guid organizationId,
            EligibleOrganization organization,
            CancellationToken ct = default) =>
            Task.FromResult(branches.TryGetValue(organizationId, out var list) ? list : []);
    }
}

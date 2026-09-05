using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Expenses;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Expenses;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.UnitTests.Expenses;

public sealed class ExpenseScopeAuthorityTests
{
    private static readonly Guid OrgId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid BranchA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid BranchB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid BranchUnauthorized = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    [Fact]
    public async Task One_branch_manager_defaults_to_that_branch_and_cannot_use_organization_scopes()
    {
        var authority = CreateAuthority(
            new AuthorizedBranchScope(
                false,
                [new AuthorizedBranchGrouping(BranchA, "Store A", null, null)]));

        var options = await authority.GetOptionsAsync(OrgId);
        Assert.False(options.CanViewOrganization);
        Assert.False(options.CanCreateOrganizationWide);
        Assert.False(options.CanViewAllBranches);
        Assert.False(options.CanViewAllExpenses);
        Assert.Single(options.Branches);
        Assert.Equal(BranchA, options.Branches[0].BranchId);

        var view = await authority.ResolveViewCriteriaAsync(OrgId, scope: null, branchId: null, preferredBranchId: null);
        Assert.True(view.IsSuccess);
        Assert.Equal(ExpenseBranchScopeKind.SingleBranch, view.Value!.Kind);
        Assert.Equal(BranchA, view.Value.BranchId);

        var organization = await authority.ResolveViewCriteriaAsync(OrgId, "organization", null, null);
        Assert.False(organization.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.ExpenseScopeInvalid, organization.ErrorCode);

        var allExpenses = await authority.ResolveViewCriteriaAsync(OrgId, "allExpenses", null, null);
        Assert.False(allExpenses.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.ExpenseScopeInvalid, allExpenses.ErrorCode);

        var unauthorized = await authority.ResolveViewCriteriaAsync(OrgId, "branch", BranchUnauthorized, null);
        Assert.False(unauthorized.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.ExpenseBranchForbidden, unauthorized.ErrorCode);

        var createOrgWide = await authority.ResolveCreateBranchAsync(OrgId, null);
        Assert.False(createOrgWide.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.ExpenseBranchRequired, createOrgWide.ErrorCode);

        var createOwn = await authority.ResolveCreateBranchAsync(OrgId, BranchA);
        Assert.True(createOwn.IsSuccess);
        Assert.Equal(BranchA, createOwn.Value!.Value);

        var createOther = await authority.ResolveCreateBranchAsync(OrgId, BranchUnauthorized);
        Assert.False(createOther.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.ExpenseBranchForbidden, createOther.ErrorCode);
    }

    [Fact]
    public async Task Multi_branch_manager_can_use_allBranches()
    {
        var authority = CreateAuthority(
            new AuthorizedBranchScope(
                false,
                [
                    new AuthorizedBranchGrouping(BranchA, "Store A", null, null),
                    new AuthorizedBranchGrouping(BranchB, "Store B", null, null)
                ]));

        var options = await authority.GetOptionsAsync(OrgId);
        Assert.True(options.CanViewAllBranches);
        Assert.False(options.CanViewOrganization);
        Assert.False(options.CanViewAllExpenses);

        var allBranches = await authority.ResolveViewCriteriaAsync(OrgId, "allBranches", null, null);
        Assert.True(allBranches.IsSuccess);
        Assert.Equal(ExpenseBranchScopeKind.AllAuthorizedBranches, allBranches.Value!.Kind);
        Assert.Equal([BranchA, BranchB], allBranches.Value.AuthorizedBranchIds);
    }

    [Fact]
    public async Task Owner_org_wide_can_use_organization_allExpenses_and_create_null()
    {
        var authority = CreateAuthority(
            new AuthorizedBranchScope(
                true,
                [
                    new AuthorizedBranchGrouping(BranchA, "Store A", null, null),
                    new AuthorizedBranchGrouping(BranchB, "Store B", null, null)
                ]),
            existsInOrg: id => id == BranchA || id == BranchB || id == BranchUnauthorized);

        var options = await authority.GetOptionsAsync(OrgId);
        Assert.True(options.CanViewOrganization);
        Assert.True(options.CanCreateOrganizationWide);
        Assert.True(options.CanViewAllBranches);
        Assert.True(options.CanViewAllExpenses);

        var organization = await authority.ResolveViewCriteriaAsync(OrgId, "organization", null, null);
        Assert.True(organization.IsSuccess);
        Assert.Equal(ExpenseBranchScopeKind.OrganizationWide, organization.Value!.Kind);

        var allExpenses = await authority.ResolveViewCriteriaAsync(OrgId, "allExpenses", null, null);
        Assert.True(allExpenses.IsSuccess);
        Assert.Equal(ExpenseBranchScopeKind.AllExpenses, allExpenses.Value!.Kind);

        var createNull = await authority.ResolveCreateBranchAsync(OrgId, null);
        Assert.True(createNull.IsSuccess);
        Assert.Null(createNull.Value);

        var createViaExists = await authority.ResolveCreateBranchAsync(OrgId, BranchUnauthorized);
        Assert.True(createViaExists.IsSuccess);
        Assert.Equal(BranchUnauthorized, createViaExists.Value!.Value);

        var defaultView = await authority.ResolveViewCriteriaAsync(OrgId, null, null, preferredBranchId: null);
        Assert.True(defaultView.IsSuccess);
        Assert.Equal(ExpenseBranchScopeKind.OrganizationWide, defaultView.Value!.Kind);
    }

    [Fact]
    public void Null_historical_expense_requires_organization_wide_access()
    {
        var orgWide = new AuthorizedBranchScope(true, []);
        var branchOnly = new AuthorizedBranchScope(
            false,
            [new AuthorizedBranchGrouping(BranchA, "Store A", null, null)]);

        var orgExpense = Expense.Record(
            PosOrganizationId.From(OrgId),
            "EXP-20260905-000001",
            ExpenseCategoryId.New(),
            ExpensePaymentMethod.Cash,
            10m,
            "Historical org expense",
            new DateOnly(2026, 9, 5),
            Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
            DateTimeOffset.Parse("2026-09-05T08:00:00Z"),
            branchId: null);

        var branchExpense = Expense.Record(
            PosOrganizationId.From(OrgId),
            "EXP-20260905-000002",
            ExpenseCategoryId.New(),
            ExpensePaymentMethod.Cash,
            20m,
            "Branch expense",
            new DateOnly(2026, 9, 5),
            Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
            DateTimeOffset.Parse("2026-09-05T08:00:00Z"),
            branchId: PosBranchId.From(BranchA));

        Assert.True(ExpenseScopeAuthority.CanAccessExpense(orgExpense, orgWide));
        Assert.False(ExpenseScopeAuthority.CanAccessExpense(orgExpense, branchOnly));
        Assert.True(ExpenseScopeAuthority.CanAccessExpense(branchExpense, branchOnly));
        Assert.False(ExpenseScopeAuthority.CanAccessExpense(
            branchExpense,
            new AuthorizedBranchScope(
                false,
                [new AuthorizedBranchGrouping(BranchB, "Store B", null, null)])));
    }

    private static ExpenseScopeAuthority CreateAuthority(
        AuthorizedBranchScope scope,
        Func<Guid, bool>? existsInOrg = null) =>
        new(new FakeBranchAccess(scope), new FakeBranches(existsInOrg ?? (_ => false)));

    private sealed class FakeBranchAccess(AuthorizedBranchScope scope) : IAuthorizedBranchGroupingDirectory
    {
        public Task<AuthorizedBranchScope> ListAuthorizedAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(scope);
    }

    private sealed class FakeBranches(Func<Guid, bool> exists) : IOrganizationBranchDirectory
    {
        public Task<bool> ExistsInOrganizationAsync(
            Guid organizationId,
            Guid branchId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(exists(branchId));

        public Task<IReadOnlyDictionary<Guid, string>> GetNamesAsync(
            Guid organizationId,
            IReadOnlyCollection<Guid> branchIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, string>>(
                branchIds.ToDictionary(id => id, id => "Branch"));
    }
}

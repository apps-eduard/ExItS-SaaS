using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Expenses;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Expenses;

/// <summary>
/// Resolves expense view/create branch scope from Platform-authorized branch access.
/// Null expense <see cref="Expense.BranchId"/> is organization-wide; non-null is branch-attributed.
/// </summary>
public sealed class ExpenseScopeAuthority
{
    private readonly IAuthorizedBranchGroupingDirectory _branchAccess;
    private readonly IOrganizationBranchDirectory _branches;

    public ExpenseScopeAuthority(
        IAuthorizedBranchGroupingDirectory branchAccess,
        IOrganizationBranchDirectory branches)
    {
        _branchAccess = branchAccess;
        _branches = branches;
    }

    public async Task<PosExpenseScopeOptionsDto> GetOptionsAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        var scope = await _branchAccess
            .ListAuthorizedAsync(organizationId, cancellationToken)
            .ConfigureAwait(false);

        var branches = scope.Branches
            .Select(b => new PosExpenseScopeBranchDto(b.BranchId, b.BranchName))
            .ToList();

        return new PosExpenseScopeOptionsDto(
            CanViewOrganization: scope.IsOrganizationWide,
            CanCreateOrganizationWide: scope.IsOrganizationWide,
            CanViewAllBranches: branches.Count > 1,
            CanViewAllExpenses: scope.IsOrganizationWide,
            Branches: branches);
    }

    public async Task<ApplicationResult<ExpenseBranchScopeCriteria>> ResolveViewCriteriaAsync(
        Guid organizationId,
        string? scope,
        Guid? branchId,
        Guid? preferredBranchId,
        CancellationToken cancellationToken = default)
    {
        var authorized = await _branchAccess
            .ListAuthorizedAsync(organizationId, cancellationToken)
            .ConfigureAwait(false);
        var authorizedIds = authorized.Branches.Select(b => b.BranchId).ToList();
        var authorizedSet = authorizedIds.ToHashSet();

        if (string.IsNullOrWhiteSpace(scope))
        {
            if (preferredBranchId is Guid preferred && authorizedSet.Contains(preferred))
            {
                return ApplicationResult<ExpenseBranchScopeCriteria>.Success(
                    new ExpenseBranchScopeCriteria(
                        ExpenseBranchScopeKind.SingleBranch,
                        preferred,
                        authorizedIds));
            }

            if (authorizedIds.Count == 1)
            {
                return ApplicationResult<ExpenseBranchScopeCriteria>.Success(
                    new ExpenseBranchScopeCriteria(
                        ExpenseBranchScopeKind.SingleBranch,
                        authorizedIds[0],
                        authorizedIds));
            }

            if (authorized.IsOrganizationWide)
            {
                return ApplicationResult<ExpenseBranchScopeCriteria>.Success(
                    new ExpenseBranchScopeCriteria(ExpenseBranchScopeKind.OrganizationWide));
            }

            if (authorizedIds.Count > 0)
            {
                return ApplicationResult<ExpenseBranchScopeCriteria>.Success(
                    new ExpenseBranchScopeCriteria(
                        ExpenseBranchScopeKind.SingleBranch,
                        authorizedIds[0],
                        authorizedIds));
            }

            return ApplicationResult<ExpenseBranchScopeCriteria>.Failure(
                ApplicationErrorCodes.ExpenseScopeInvalid,
                "No authorized expense branch scope is available.");
        }

        var kind = scope.Trim();
        if (string.Equals(kind, "branch", StringComparison.OrdinalIgnoreCase))
        {
            var target = branchId ?? preferredBranchId;
            if (target is null || target.Value == Guid.Empty)
            {
                return ApplicationResult<ExpenseBranchScopeCriteria>.Failure(
                    ApplicationErrorCodes.ExpenseScopeInvalid,
                    "scope=branch requires a branchId.");
            }

            if (!authorizedSet.Contains(target.Value))
            {
                return ApplicationResult<ExpenseBranchScopeCriteria>.Failure(
                    ApplicationErrorCodes.ExpenseBranchForbidden,
                    "Branch is not authorized for expense access.");
            }

            return ApplicationResult<ExpenseBranchScopeCriteria>.Success(
                new ExpenseBranchScopeCriteria(
                    ExpenseBranchScopeKind.SingleBranch,
                    target.Value,
                    authorizedIds));
        }

        if (string.Equals(kind, "allBranches", StringComparison.OrdinalIgnoreCase))
        {
            return ApplicationResult<ExpenseBranchScopeCriteria>.Success(
                new ExpenseBranchScopeCriteria(
                    ExpenseBranchScopeKind.AllAuthorizedBranches,
                    AuthorizedBranchIds: authorizedIds));
        }

        if (string.Equals(kind, "organization", StringComparison.OrdinalIgnoreCase))
        {
            if (!authorized.IsOrganizationWide)
            {
                return ApplicationResult<ExpenseBranchScopeCriteria>.Failure(
                    ApplicationErrorCodes.ExpenseScopeInvalid,
                    "Organization-wide expense scope requires organization-wide access.");
            }

            return ApplicationResult<ExpenseBranchScopeCriteria>.Success(
                new ExpenseBranchScopeCriteria(ExpenseBranchScopeKind.OrganizationWide));
        }

        if (string.Equals(kind, "allExpenses", StringComparison.OrdinalIgnoreCase))
        {
            if (!authorized.IsOrganizationWide)
            {
                return ApplicationResult<ExpenseBranchScopeCriteria>.Failure(
                    ApplicationErrorCodes.ExpenseScopeInvalid,
                    "All-expenses scope requires organization-wide access.");
            }

            return ApplicationResult<ExpenseBranchScopeCriteria>.Success(
                new ExpenseBranchScopeCriteria(
                    ExpenseBranchScopeKind.AllExpenses,
                    AuthorizedBranchIds: authorizedIds));
        }

        return ApplicationResult<ExpenseBranchScopeCriteria>.Failure(
            ApplicationErrorCodes.ExpenseScopeInvalid,
            $"Unrecognized expense scope '{scope}'.");
    }

    public async Task<ApplicationResult<PosBranchId?>> ResolveCreateBranchAsync(
        Guid organizationId,
        Guid? branchId,
        CancellationToken cancellationToken = default)
    {
        var authorized = await _branchAccess
            .ListAuthorizedAsync(organizationId, cancellationToken)
            .ConfigureAwait(false);
        var authorizedSet = authorized.Branches.Select(b => b.BranchId).ToHashSet();

        if (branchId is null)
        {
            if (!authorized.IsOrganizationWide)
            {
                return ApplicationResult<PosBranchId?>.Failure(
                    ApplicationErrorCodes.ExpenseBranchRequired,
                    "A branch is required when recording expenses without organization-wide access.");
            }

            return ApplicationResult<PosBranchId?>.Success(null);
        }

        if (branchId.Value == Guid.Empty)
        {
            return ApplicationResult<PosBranchId?>.Failure(
                ApplicationErrorCodes.ExpenseBranchForbidden,
                "BranchId cannot be an empty GUID.");
        }

        if (authorizedSet.Contains(branchId.Value))
        {
            return ApplicationResult<PosBranchId?>.Success(PosBranchId.From(branchId.Value));
        }

        if (authorized.IsOrganizationWide)
        {
            var exists = await _branches
                .ExistsInOrganizationAsync(organizationId, branchId.Value, cancellationToken)
                .ConfigureAwait(false);
            if (exists)
            {
                return ApplicationResult<PosBranchId?>.Success(PosBranchId.From(branchId.Value));
            }
        }

        return ApplicationResult<PosBranchId?>.Failure(
            ApplicationErrorCodes.ExpenseBranchForbidden,
            "Branch is not authorized for expense recording.");
    }

    public static bool CanAccessExpense(Expense expense, AuthorizedBranchScope scope)
    {
        if (expense.BranchId is null)
        {
            return scope.IsOrganizationWide;
        }

        var branchValue = expense.BranchId.Value;
        return scope.Branches.Any(b => b.BranchId == branchValue);
    }

    public async Task<AuthorizedBranchScope> GetAuthorizedScopeAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        await _branchAccess.ListAuthorizedAsync(organizationId, cancellationToken).ConfigureAwait(false);
}

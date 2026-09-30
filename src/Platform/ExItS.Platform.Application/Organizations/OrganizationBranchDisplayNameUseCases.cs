using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Application.Organizations;

/// <summary>
/// Safe projection for branch labels on operational history (transfers, stock requests, etc.).
/// Does not expand workspace or operational branch access — display only.
/// </summary>
public sealed record OrganizationBranchDisplayNameDto(
    Guid BranchId,
    string DisplayName,
    string Status);

public sealed record ResolveOrganizationBranchDisplayNamesRequest(IReadOnlyList<Guid> BranchIds);

/// <summary>
/// Org-scoped batch branch display-name resolution for POS/internal operational UI.
/// Authorization must be active org membership (or Platform view) — not ManageBranches.
/// Returns names for all requested branches that belong to the organization, regardless of
/// the caller's staff branch-access assignments.
/// </summary>
public sealed class ResolveOrganizationBranchDisplayNames
{
    public const int MaxBranchIds = 100;
    public const string DisplayNameNotAvailable = "Unknown branch";

    private readonly IOrganizationBranchRepository _branches;

    public ResolveOrganizationBranchDisplayNames(IOrganizationBranchRepository branches)
    {
        _branches = branches;
    }

    public async Task<ApplicationResult<IReadOnlyList<OrganizationBranchDisplayNameDto>>> ExecuteAsync(
        Guid organizationId,
        ResolveOrganizationBranchDisplayNamesRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var distinct = (request.BranchIds ?? Array.Empty<Guid>())
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        if (distinct.Count > MaxBranchIds)
        {
            return ApplicationResult<IReadOnlyList<OrganizationBranchDisplayNameDto>>.Failure(
                ApplicationErrorCodes.DomainViolation,
                $"At most {MaxBranchIds} branch ids may be resolved per request.");
        }

        if (distinct.Count == 0)
        {
            return ApplicationResult<IReadOnlyList<OrganizationBranchDisplayNameDto>>.Success(
                Array.Empty<OrganizationBranchDisplayNameDto>());
        }

        var orgId = PlatformOrganizationId.From(organizationId);
        var all = await _branches.ListByOrganizationAsync(orgId, cancellationToken).ConfigureAwait(false);
        var byId = all.ToDictionary(b => b.Id.Value);

        var results = new List<OrganizationBranchDisplayNameDto>(distinct.Count);
        foreach (var branchId in distinct)
        {
            if (!byId.TryGetValue(branchId, out var branch))
            {
                results.Add(new OrganizationBranchDisplayNameDto(
                    branchId,
                    DisplayNameNotAvailable,
                    "NotAvailable"));
                continue;
            }

            var displayName = string.IsNullOrWhiteSpace(branch.Name)
                ? (string.IsNullOrWhiteSpace(branch.Code) ? DisplayNameNotAvailable : branch.Code.Trim())
                : branch.Name.Trim();
            results.Add(new OrganizationBranchDisplayNameDto(
                branchId,
                displayName,
                branch.Status.ToString()));
        }

        return ApplicationResult<IReadOnlyList<OrganizationBranchDisplayNameDto>>.Success(results);
    }
}

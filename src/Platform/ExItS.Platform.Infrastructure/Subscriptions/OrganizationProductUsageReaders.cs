using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Application.Subscriptions;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;

namespace ExItS.Platform.Infrastructure.Subscriptions;

public sealed class MembershipStaffUsageReader
{
    private readonly IOrganizationMembershipRepository _memberships;

    public MembershipStaffUsageReader(IOrganizationMembershipRepository memberships)
    {
        _memberships = memberships;
    }

    public async Task<int> GetActiveStaffCountAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var (_, staffTotal) = await _memberships
            .ListByOrganizationAsync(organizationId, MembershipStatus.Active, skip: 0, take: 1, cancellationToken)
            .ConfigureAwait(false);
        return staffTotal;
    }
}

public sealed class UnresolvedProductBranchUsageReader
{
    public const string UnavailableReason =
        "Platform does not yet receive active branch counts from Pinoy Business POS across the product boundary; pass activeBranchCount query override for Local Validation or wire product-owned usage later.";

    public Task<OrganizationProductUsageSnapshot> GetUsageAsync(
        PlatformOrganizationId organizationId,
        ProductCode productCode,
        CancellationToken cancellationToken = default)
    {
        _ = organizationId;
        _ = productCode;
        _ = cancellationToken;
        return Task.FromResult(new OrganizationProductUsageSnapshot(
            ActiveStaffCount: 0,
            ActiveBranchCount: null,
            BranchCountAvailable: false,
            BranchCountUnavailableReason: UnavailableReason));
    }
}

public sealed class CompositeOrganizationProductUsageReader : IOrganizationProductUsageReader
{
    private readonly MembershipStaffUsageReader _staff;
    private readonly UnresolvedProductBranchUsageReader _branches;

    public CompositeOrganizationProductUsageReader(
        MembershipStaffUsageReader staff,
        UnresolvedProductBranchUsageReader branches)
    {
        _staff = staff;
        _branches = branches;
    }

    public async Task<OrganizationProductUsageSnapshot> GetUsageAsync(
        PlatformOrganizationId organizationId,
        ProductCode productCode,
        CancellationToken cancellationToken = default)
    {
        var staffCount = await _staff
            .GetActiveStaffCountAsync(organizationId, cancellationToken)
            .ConfigureAwait(false);
        var branchSnapshot = await _branches
            .GetUsageAsync(organizationId, productCode, cancellationToken)
            .ConfigureAwait(false);
        return branchSnapshot with { ActiveStaffCount = staffCount };
    }
}

using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Inventory;

public interface IOrganizationExpirySalePolicyRepository
{
    Task<OrganizationExpirySalePolicySetting?> GetAsync(
        PosOrganizationId organizationId,
        CancellationToken cancellationToken = default);

    Task UpsertAsync(
        OrganizationExpirySalePolicySetting setting,
        CancellationToken cancellationToken = default);
}

public interface IOrganizationCategoryExpirySalePolicyRepository
{
    Task<OrganizationCategoryExpirySalePolicy?> GetAsync(
        PosOrganizationId organizationId,
        Guid categoryId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrganizationCategoryExpirySalePolicy>> ListByOrganizationAsync(
        PosOrganizationId organizationId,
        CancellationToken cancellationToken = default);

    Task UpsertAsync(
        OrganizationCategoryExpirySalePolicy policy,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        PosOrganizationId organizationId,
        Guid categoryId,
        CancellationToken cancellationToken = default);
}

public interface IBranchExpirySalePolicyRepository
{
    Task<BranchExpirySalePolicySetting?> GetAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        CancellationToken cancellationToken = default);

    Task UpsertAsync(
        BranchExpirySalePolicySetting setting,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        CancellationToken cancellationToken = default);
}

public interface IBranchCategoryExpirySalePolicyRepository
{
    Task<BranchCategoryExpirySalePolicy?> GetAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        Guid categoryId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BranchCategoryExpirySalePolicy>> ListByBranchAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        CancellationToken cancellationToken = default);

    Task UpsertAsync(
        BranchCategoryExpirySalePolicy policy,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        Guid categoryId,
        CancellationToken cancellationToken = default);
}

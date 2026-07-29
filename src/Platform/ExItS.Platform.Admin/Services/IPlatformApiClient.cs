using ExItS.Platform.Admin.Models;

namespace ExItS.Platform.Admin.Services;

public interface IPlatformApiClient
{
    Task<ApiCallResult<PortfolioSummaryDto>> GetPortfolioSummaryAsync(CancellationToken ct = default);
    Task<ApiCallResult<PagedResult<ProductDto>>> GetProductsAsync(int page = 1, int pageSize = 20, string? status = null, CancellationToken ct = default);
    Task<ApiCallResult<ProductDto>> GetProductAsync(Guid id, CancellationToken ct = default);
    Task<ApiCallResult<ProductOverviewDto>> GetProductOverviewAsync(string productCode, CancellationToken ct = default);
    Task<ApiCallResult<PagedResult<OrganizationDto>>> GetOrganizationsAsync(int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<ApiCallResult<OrganizationDto>> GetOrganizationAsync(Guid id, CancellationToken ct = default);
    Task<ApiCallResult<OrganizationCommercialSummaryDto>> GetOrganizationCommercialSummaryAsync(Guid id, CancellationToken ct = default);
    Task<ApiCallResult<PagedResult<SubscriptionDto>>> GetSubscriptionsAsync(string? status = null, string? productCode = null, int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<ApiCallResult<SubscriptionDto>> GetSubscriptionAsync(Guid id, CancellationToken ct = default);
    Task<ApiCallResult<PagedResult<PaymentDto>>> GetPaymentsAsync(string? status = null, string? productCode = null, Guid? organizationId = null, string? method = null, int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<ApiCallResult<PaymentDto>> GetPaymentAsync(Guid id, CancellationToken ct = default);
    Task<ApiCallResult<PagedResult<EntitlementLatestSummaryDto>>> GetLatestEntitlementsAsync(int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<ApiCallResult<PagedResult<EntitlementSnapshotDto>>> GetEntitlementHistoryAsync(Guid organizationId, string productCode, CancellationToken ct = default);
    Task<ApiCallResult<EntitlementSnapshotDto>> GetLatestEntitlementAsync(Guid organizationId, string productCode, CancellationToken ct = default);
    Task<ApiCallResult<EntitlementSnapshotDto>> GetEntitlementAsync(Guid id, CancellationToken ct = default);
    Task<ApiCallResult<PagedResult<FeatureOverrideDto>>> GetFeatureOverridesAsync(Guid organizationId, string productCode, CancellationToken ct = default);

    Task<ApiCallResult<PagedResult<PlatformUserDto>>> GetUsersAsync(int page = 1, int pageSize = 20, string? status = null, string? search = null, CancellationToken ct = default);
    Task<ApiCallResult<PlatformUserDto>> GetUserAsync(Guid id, CancellationToken ct = default);
    Task<ApiCallResult<PlatformUserDto>> CreateUserAsync(CreatePlatformUserRequest request, CancellationToken ct = default);
    Task<ApiCallResult<PlatformUserDto>> UpdateUserAsync(Guid id, UpdatePlatformUserRequest request, CancellationToken ct = default);
    Task<ApiCallResult<PlatformUserDto>> SuspendUserAsync(Guid id, string? reason = null, CancellationToken ct = default);
    Task<ApiCallResult<PlatformUserDto>> ReactivateUserAsync(Guid id, CancellationToken ct = default);
    Task<ApiCallResult<PlatformUserDto>> DisableUserAsync(Guid id, CancellationToken ct = default);

    Task<ApiCallResult<PagedResult<OrganizationMembershipDto>>> GetOrganizationMembersAsync(Guid organizationId, int page = 1, int pageSize = 20, string? status = null, CancellationToken ct = default);
    Task<ApiCallResult<PagedResult<OrganizationMembershipDto>>> GetUserMembershipsAsync(Guid userId, int page = 1, int pageSize = 20, string? status = null, CancellationToken ct = default);
    Task<ApiCallResult<OrganizationMembershipDto>> AddOrganizationMemberAsync(Guid organizationId, AddMemberRequest request, CancellationToken ct = default);
    Task<ApiCallResult<OrganizationMembershipDto>> ChangeMembershipRoleAsync(Guid membershipId, ChangeRoleRequest request, CancellationToken ct = default);
    Task<ApiCallResult<OrganizationMembershipDto>> SuspendMembershipAsync(Guid membershipId, MembershipLifecycleRequest request, CancellationToken ct = default);
    Task<ApiCallResult<OrganizationMembershipDto>> ReactivateMembershipAsync(Guid membershipId, MembershipLifecycleRequest request, CancellationToken ct = default);
    Task<ApiCallResult<OrganizationMembershipDto>> RevokeMembershipAsync(Guid membershipId, MembershipLifecycleRequest request, CancellationToken ct = default);

    Task<ApiCallResult<PagedResult<ProductAccessAssignmentDto>>> GetOrganizationProductAccessAsync(Guid organizationId, int page = 1, int pageSize = 20, string? status = null, CancellationToken ct = default);
    Task<ApiCallResult<PagedResult<ProductAccessAssignmentDto>>> GetUserProductAccessAsync(Guid userId, int page = 1, int pageSize = 20, string? status = null, CancellationToken ct = default);
    Task<ApiCallResult<ProductAccessAssignmentDto>> GrantProductAccessAsync(Guid organizationId, GrantProductAccessRequest request, CancellationToken ct = default);
    Task<ApiCallResult<ProductAccessAssignmentDto>> RevokeProductAccessAsync(Guid assignmentId, RevokeProductAccessRequest request, CancellationToken ct = default);
    Task<ApiCallResult<EffectiveProductAccessResultDto>> EvaluateAccessAsync(Guid userId, Guid organizationId, string productCode, CancellationToken ct = default);
}

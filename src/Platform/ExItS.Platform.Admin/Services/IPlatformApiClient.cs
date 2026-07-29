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
}

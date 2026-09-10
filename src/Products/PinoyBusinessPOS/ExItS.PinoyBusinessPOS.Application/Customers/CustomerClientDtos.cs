namespace ExItS.PinoyBusinessPOS.Application.Customers;

public sealed record PosCustomerListItemDto(
    Guid CustomerId,
    Guid OrganizationId,
    string DisplayName,
    string? MobileNumber,
    string? Address,
    string? Notes,
    string Status,
    Guid? PlatformBusinessCustomerId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    string? LinkedPersonalPublicUserId = null,
    Guid? LinkedBuyerOrganizationId = null,
    string? LinkedBuyerPublicOrganizationId = null,
    string? PartyKind = null);

public sealed record PosCustomerDetailDto(
    Guid CustomerId,
    Guid OrganizationId,
    string DisplayName,
    string? MobileNumber,
    string? Address,
    string? Notes,
    string Status,
    Guid? PlatformBusinessCustomerId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    string? LinkedPersonalPublicUserId = null,
    Guid? LinkedBuyerOrganizationId = null,
    string? LinkedBuyerPublicOrganizationId = null,
    string? PartyKind = null);

public sealed record CreatePosCustomerRequest(
    string DisplayName,
    string? MobileNumber,
    string? Address,
    string? Notes,
    Guid? CustomerId = null,
    Guid? PlatformBusinessCustomerId = null,
    string? PartyKind = null,
    Guid? LinkedBuyerOrganizationId = null,
    string? LinkedBuyerPublicOrganizationId = null);

public sealed record UpdatePosCustomerRequest(
    string DisplayName,
    string? MobileNumber,
    string? Address,
    string? Notes,
    DateTimeOffset? ExpectedUpdatedAtUtc = null);

public sealed record PosCustomerPagedResult(
    List<PosCustomerListItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

/// <summary>
/// Narrow checkout selection row for CreateSale.
/// Kind=Customer: POS people. Kind=Business: Active B2B Organization counterparty (no POSCustomer).
/// </summary>
public sealed record CheckoutCustomerSearchItemDto(
    string Kind,
    string DisplayName,
    string Status,
    Guid? CustomerId = null,
    string? MobileNumber = null,
    Guid? ConnectionId = null,
    Guid? BuyerOrganizationId = null,
    string? BuyerPublicOrganizationId = null)
{
    public const string KindCustomer = "Customer";
    public const string KindBusiness = "Business";
}

public sealed record CheckoutCustomerSearchResult(
    List<CheckoutCustomerSearchItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record PosCustomerSyncPageResult(
    List<PosCustomerDetailDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    DateTimeOffset? NextCheckpointUtc);

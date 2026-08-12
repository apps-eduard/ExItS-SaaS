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
    DateTimeOffset UpdatedAtUtc);

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
    DateTimeOffset UpdatedAtUtc);

public sealed record CreatePosCustomerRequest(
    string DisplayName,
    string? MobileNumber,
    string? Address,
    string? Notes,
    Guid? CustomerId = null,
    Guid? PlatformBusinessCustomerId = null);

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

public sealed record PosCustomerSyncPageResult(
    List<PosCustomerDetailDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    DateTimeOffset? NextCheckpointUtc);

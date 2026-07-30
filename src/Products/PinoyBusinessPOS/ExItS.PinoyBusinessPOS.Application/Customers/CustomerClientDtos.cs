namespace ExItS.PinoyBusinessPOS.Application.Customers;

public sealed record PosCustomerListItemDto(
    Guid CustomerId,
    Guid OrganizationId,
    string DisplayName,
    string? MobileNumber,
    string? Address,
    string? Notes,
    string Status,
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
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record CreatePosCustomerRequest(
    string DisplayName,
    string? MobileNumber,
    string? Address,
    string? Notes);

public sealed record UpdatePosCustomerRequest(
    string DisplayName,
    string? MobileNumber,
    string? Address,
    string? Notes);

public sealed record PosCustomerPagedResult(
    List<PosCustomerListItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

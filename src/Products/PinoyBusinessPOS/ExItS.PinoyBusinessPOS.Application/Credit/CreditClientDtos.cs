namespace ExItS.PinoyBusinessPOS.Application.Credit;

public sealed record PosCreditEntryDto(
    Guid CreditEntryId,
    Guid OrganizationId,
    Guid CustomerId,
    decimal Amount,
    string Remarks,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ReversedAtUtc,
    string? ReversalReason);

public sealed record PosCustomerCreditSummaryDto(
    Guid CustomerId,
    Guid OrganizationId,
    decimal OutstandingAmount,
    int ActiveEntryCount,
    int TotalEntryCount);

public sealed record CreatePosCreditEntryRequest(decimal Amount, string Remarks);

public sealed record ReversePosCreditEntryRequest(string Reason);

public sealed record PosCreditEntryPagedResult(
    List<PosCreditEntryDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

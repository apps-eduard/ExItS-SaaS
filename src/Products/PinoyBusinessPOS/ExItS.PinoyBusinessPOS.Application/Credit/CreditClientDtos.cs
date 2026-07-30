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
    string? ReversalReason,
    DateOnly? CurrentDueDate);

public sealed record SetPosCreditDueDateRequest(DateOnly? DueDate, string Reason);

public sealed record ClearPosCreditDueDateRequest(string Reason);

public sealed record PosCreditDueDateChangeDto(
    Guid ChangeId,
    Guid OrganizationId,
    Guid CreditEntryId,
    Guid CustomerId,
    DateOnly? PreviousDueDate,
    DateOnly? NewDueDate,
    string Reason,
    Guid ChangedBy,
    DateTimeOffset ChangedAtUtc);

public sealed record PosCreditDueDateHistoryPagedResult(
    List<PosCreditDueDateChangeDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record PosCustomerOverdueSummaryDto(
    Guid CustomerId,
    Guid OrganizationId,
    decimal OutstandingAmount,
    decimal OverdueAmount,
    int OverdueCreditCount,
    DateOnly? EarliestOverdueDate,
    DateOnly? NextUpcomingDueDate,
    int CreditsWithoutDueDateCount,
    decimal ActiveCreditTotal,
    decimal ActiveRepaymentTotal,
    int ActiveCreditCount,
    int ActiveRepaymentCount,
    int TotalLedgerEntryCount);

public sealed record PosAgedCreditDto(
    Guid CreditEntryId,
    Guid OrganizationId,
    Guid CustomerId,
    decimal Amount,
    decimal RemainingUnpaidAmount,
    string Remarks,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateOnly? CurrentDueDate,
    string DueStatus,
    bool IsOverdue);

public sealed record PosAgedCreditPagedResult(
    List<PosAgedCreditDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record PosOverdueCustomerListItemDto(
    Guid CustomerId,
    Guid OrganizationId,
    string DisplayName,
    decimal OutstandingAmount,
    decimal OverdueAmount,
    int OverdueCreditCount,
    DateOnly? EarliestOverdueDate);

public sealed record PosOverdueCustomerPagedResult(
    List<PosOverdueCustomerListItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record PosCustomerCreditSummaryDto(
    Guid CustomerId,
    Guid OrganizationId,
    decimal OutstandingAmount,
    int ActiveEntryCount,
    int TotalEntryCount);

public sealed record CreatePosCreditEntryRequest(decimal Amount, string Remarks, Guid? CreditEntryId = null);

public sealed record ReversePosCreditEntryRequest(string Reason);

public sealed record PosCreditEntryPagedResult(
    List<PosCreditEntryDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record PosCreditSyncPageResult(
    List<PosCreditEntryDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    DateTimeOffset? NextCheckpointUtc);

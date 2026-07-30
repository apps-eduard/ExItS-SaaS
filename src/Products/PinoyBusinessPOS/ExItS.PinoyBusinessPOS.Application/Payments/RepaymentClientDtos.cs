namespace ExItS.PinoyBusinessPOS.Application.Payments;

public sealed record PosRepaymentDto(
    Guid RepaymentId,
    Guid OrganizationId,
    Guid CustomerId,
    decimal Amount,
    string? Remarks,
    string Status,
    DateTimeOffset RecordedAtUtc,
    Guid RecordedBy,
    DateTimeOffset? ReversedAtUtc,
    string? ReversalReason,
    Guid? ReversedBy);

public sealed record PosCustomerUtangSummaryDto(
    Guid CustomerId,
    Guid OrganizationId,
    decimal OutstandingAmount,
    decimal ActiveCreditTotal,
    decimal ActiveRepaymentTotal,
    int ActiveCreditCount,
    int ActiveRepaymentCount,
    int TotalLedgerEntryCount,
    decimal OverdueAmount = 0m,
    int OverdueCreditCount = 0,
    DateOnly? EarliestOverdueDate = null,
    DateOnly? NextUpcomingDueDate = null,
    int CreditsWithoutDueDateCount = 0);

public sealed record PosLedgerEntryDto(
    Guid EntryId,
    string EntryType,
    Guid OrganizationId,
    Guid CustomerId,
    decimal Amount,
    decimal SignedEffect,
    string? Remarks,
    string Status,
    DateTimeOffset RecordedAtUtc,
    Guid? RecordedBy,
    DateTimeOffset? ReversedAtUtc,
    string? ReversalReason,
    Guid? ReversedBy,
    decimal? RunningBalance);

public sealed record CreatePosRepaymentRequest(decimal Amount, string? Remarks, Guid? RepaymentId = null);

public sealed record ReversePosRepaymentRequest(string Reason);

public sealed record PosRepaymentPagedResult(
    List<PosRepaymentDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record PosRepaymentSyncPageResult(
    List<PosRepaymentDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    DateTimeOffset? NextCheckpointUtc);

public sealed record PosLedgerPagedResult(
    List<PosLedgerEntryDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

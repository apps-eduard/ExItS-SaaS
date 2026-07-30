namespace ExItS.PinoyBusinessPOS.Application.Statements;

/// <summary>Client JSON DTO for customer statement responses (camelCase API JSON).</summary>
public sealed record PosCustomerStatementLineDto(
    Guid EntryId,
    string EntryType,
    DateTimeOffset RecordedAtUtc,
    decimal Amount,
    decimal SignedEffect,
    string Status,
    string? Remarks,
    DateOnly? DueDate,
    string? DueStatus,
    bool IsOverdue,
    bool IsReversed,
    decimal RunningBalance);

public sealed record PosCustomerStatementDto(
    Guid OrganizationId,
    string? OrganizationDisplayName,
    Guid CustomerId,
    string CustomerDisplayName,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    decimal OpeningBalance,
    decimal ClosingBalance,
    decimal PeriodCreditTotal,
    decimal PeriodRepaymentTotal,
    decimal PeriodReversalCreditTotal,
    decimal PeriodReversalRepaymentTotal,
    decimal OutstandingBalance,
    decimal OverdueAmount,
    int OverdueCreditCount,
    DateTimeOffset GeneratedAtUtc,
    string CurrencyCode,
    string CultureName,
    IReadOnlyList<PosCustomerStatementLineDto> Lines);

/// <summary>Client JSON DTO for repayment receipt responses (camelCase API JSON).</summary>
public sealed record PosRepaymentReceiptDto(
    string ReceiptReference,
    Guid RepaymentId,
    Guid OrganizationId,
    string? OrganizationDisplayName,
    Guid CustomerId,
    string CustomerDisplayName,
    decimal Amount,
    string? Remarks,
    DateTimeOffset RecordedAtUtc,
    Guid? RecordedBy,
    string Status,
    bool IsReversed,
    DateTimeOffset? ReversedAtUtc,
    string? ReversalReason,
    Guid? ReversedBy,
    decimal? OutstandingAfterRepayment,
    DateTimeOffset GeneratedAtUtc,
    string CurrencyCode,
    string CultureName,
    string Disclaimer);

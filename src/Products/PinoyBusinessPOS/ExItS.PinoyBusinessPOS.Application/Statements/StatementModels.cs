using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Application.Statements;

public sealed record CustomerStatementLineDto(
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

public sealed record CustomerStatementDto(
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
    IReadOnlyList<CustomerStatementLineDto> Lines,
    decimal PeriodWriteOffTotal = 0m,
    decimal PeriodReversalWriteOffTotal = 0m);

public sealed record RepaymentReceiptDto(
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

public interface ICustomerStatementService
{
    Task<Common.ApplicationResult<CustomerStatementDto>> GenerateAsync(
        PosOrganizationId organizationId,
        Guid customerId,
        DateOnly periodStart,
        DateOnly periodEnd,
        string? organizationDisplayName,
        string currencyCode,
        string cultureName,
        CancellationToken cancellationToken = default);
}

public interface IRepaymentReceiptService
{
    Task<Common.ApplicationResult<RepaymentReceiptDto>> GetAsync(
        PosOrganizationId organizationId,
        Guid repaymentId,
        string? organizationDisplayName,
        string currencyCode,
        string cultureName,
        CancellationToken cancellationToken = default);
}

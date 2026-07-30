using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Domain.Credit;

/// <summary>
/// Organization-owned remarks-based credit entry. Append-only after create:
/// amount and remarks cannot be edited; corrections use explicit reversal with a reason.
/// Optional calendar due date is denormalized for reads; history is append-only.
/// When created via Product-Based Utang, <see cref="SourceSaleId"/> links to the originating sale.
/// </summary>
public sealed class CreditEntry
{
    public const int RemarksMaxLength = 512;
    public const int ReversalReasonMaxLength = 512;
    public const decimal MaxAmount = 999_999_999.99m;

    public CreditEntryId Id { get; }
    public PosOrganizationId OrganizationId { get; }
    public POSCustomerId CustomerId { get; }
    public decimal Amount { get; }
    public string Remarks { get; }
    public CreditEntryStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset? ReversedAtUtc { get; private set; }
    public string? ReversalReason { get; private set; }
    public DateOnly? CurrentDueDate { get; private set; }

    /// <summary>Originating Product-Based Utang sale when this credit was created at checkout; otherwise null.</summary>
    public SaleId? SourceSaleId { get; }

    private CreditEntry(
        CreditEntryId id,
        PosOrganizationId organizationId,
        POSCustomerId customerId,
        decimal amount,
        string remarks,
        CreditEntryStatus status,
        DateTimeOffset createdAtUtc,
        DateTimeOffset? reversedAtUtc,
        string? reversalReason,
        DateOnly? currentDueDate,
        SaleId? sourceSaleId)
    {
        Id = id;
        OrganizationId = organizationId;
        CustomerId = customerId;
        Amount = amount;
        Remarks = remarks;
        Status = status;
        CreatedAtUtc = createdAtUtc;
        ReversedAtUtc = reversedAtUtc;
        ReversalReason = reversalReason;
        CurrentDueDate = currentDueDate;
        SourceSaleId = sourceSaleId;
    }

    public static CreditEntry Create(
        PosOrganizationId organizationId,
        POSCustomerId customerId,
        decimal amount,
        string remarks,
        DateTimeOffset utcNow,
        CreditEntryId? id = null,
        SaleId? sourceSaleId = null)
    {
        EnsureUtc(utcNow);
        return new CreditEntry(
            id ?? CreditEntryId.New(),
            organizationId,
            customerId,
            NormalizeAmount(amount),
            NormalizeRemarks(remarks),
            CreditEntryStatus.Active,
            utcNow,
            null,
            null,
            null,
            sourceSaleId);
    }

    public static CreditEntry Rehydrate(
        CreditEntryId id,
        PosOrganizationId organizationId,
        POSCustomerId customerId,
        decimal amount,
        string remarks,
        CreditEntryStatus status,
        DateTimeOffset createdAtUtc,
        DateTimeOffset? reversedAtUtc,
        string? reversalReason,
        DateOnly? currentDueDate,
        SaleId? sourceSaleId = null) =>
        new(
            id,
            organizationId,
            customerId,
            amount,
            remarks,
            status,
            createdAtUtc,
            reversedAtUtc,
            reversalReason,
            currentDueDate,
            sourceSaleId);

    public void Reverse(string reason, DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        if (Status == CreditEntryStatus.Reversed)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCreditEntryStatusTransition,
                "Credit entry is already reversed.");
        }

        Status = CreditEntryStatus.Reversed;
        ReversedAtUtc = utcNow;
        ReversalReason = NormalizeReversalReason(reason);
    }

    /// <summary>
    /// Updates only the denormalized current due date. Financial fields are untouched.
    /// Reversed credits cannot receive due-date changes.
    /// </summary>
    public void ApplyCurrentDueDate(DateOnly? dueDate)
    {
        if (Status == CreditEntryStatus.Reversed)
        {
            throw new DomainException(
                DomainErrorCodes.CreditDueDateNotAllowedOnReversed,
                "Due dates cannot be set on a reversed credit entry.");
        }

        CurrentDueDate = dueDate;
    }

    public static decimal NormalizeAmount(decimal amount)
    {
        if (amount <= 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCreditAmount,
                "Credit amount must be a positive decimal.");
        }

        if (amount > MaxAmount)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCreditAmount,
                $"Credit amount must be at most {MaxAmount}.");
        }

        var rounded = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        if (rounded != amount)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCreditAmount,
                "Credit amount may have at most two decimal places.");
        }

        return rounded;
    }

    public static string NormalizeRemarks(string remarks)
    {
        if (string.IsNullOrWhiteSpace(remarks))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCreditRemarks,
                "Remarks are required for a credit entry.");
        }

        var trimmed = remarks.Trim();
        if (trimmed.Length > RemarksMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCreditRemarks,
                $"Remarks must be at most {RemarksMaxLength} characters.");
        }

        return trimmed;
    }

    public static string NormalizeReversalReason(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCreditReversalReason,
                "Reversal reason is required.");
        }

        var trimmed = reason.Trim();
        if (trimmed.Length > ReversalReasonMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCreditReversalReason,
                $"Reversal reason must be at most {ReversalReasonMaxLength} characters.");
        }

        return trimmed;
    }

    private static void EnsureUtc(DateTimeOffset utcNow)
    {
        if (utcNow.Offset != TimeSpan.Zero)
        {
            throw new DomainException(DomainErrorCodes.InvalidUtcTimestamp, "Timestamp must be UTC.");
        }
    }
}

using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Domain.Credit;

/// <summary>
/// Append-only audit of credit due-date set/change/clear. Does not alter credit amount,
/// remarks, recorded time, or ledger effect. Past due dates are allowed and may immediately
/// make unpaid active credits overdue under the FIFO aging read model.
/// </summary>
public sealed class CreditDueDateChange
{
    public const int ReasonMaxLength = 512;

    public CreditDueDateChangeId Id { get; }
    public PosOrganizationId OrganizationId { get; }
    public CreditEntryId CreditEntryId { get; }
    public POSCustomerId CustomerId { get; }
    public DateOnly? PreviousDueDate { get; }
    public DateOnly? NewDueDate { get; }
    public string Reason { get; }
    public Guid ChangedBy { get; }
    public DateTimeOffset ChangedAtUtc { get; }

    private CreditDueDateChange(
        CreditDueDateChangeId id,
        PosOrganizationId organizationId,
        CreditEntryId creditEntryId,
        POSCustomerId customerId,
        DateOnly? previousDueDate,
        DateOnly? newDueDate,
        string reason,
        Guid changedBy,
        DateTimeOffset changedAtUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        CreditEntryId = creditEntryId;
        CustomerId = customerId;
        PreviousDueDate = previousDueDate;
        NewDueDate = newDueDate;
        Reason = reason;
        ChangedBy = changedBy;
        ChangedAtUtc = changedAtUtc;
    }

    public static CreditDueDateChange Create(
        PosOrganizationId organizationId,
        CreditEntryId creditEntryId,
        POSCustomerId customerId,
        DateOnly? previousDueDate,
        DateOnly? newDueDate,
        string reason,
        Guid changedBy,
        DateTimeOffset utcNow,
        CreditDueDateChangeId? id = null)
    {
        if (utcNow.Offset != TimeSpan.Zero)
        {
            throw new DomainException(DomainErrorCodes.InvalidUtcTimestamp, "Timestamp must be UTC.");
        }

        if (changedBy == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCreditDueDateActor,
                "Actor id must be a non-empty GUID.");
        }

        if (previousDueDate == newDueDate)
        {
            throw new DomainException(
                DomainErrorCodes.CreditDueDateUnchanged,
                "New due date must differ from the previous due date.");
        }

        return new CreditDueDateChange(
            id ?? CreditDueDateChangeId.New(),
            organizationId,
            creditEntryId,
            customerId,
            previousDueDate,
            newDueDate,
            NormalizeReason(reason),
            changedBy,
            utcNow);
    }

    public static CreditDueDateChange Rehydrate(
        CreditDueDateChangeId id,
        PosOrganizationId organizationId,
        CreditEntryId creditEntryId,
        POSCustomerId customerId,
        DateOnly? previousDueDate,
        DateOnly? newDueDate,
        string reason,
        Guid changedBy,
        DateTimeOffset changedAtUtc) =>
        new(
            id,
            organizationId,
            creditEntryId,
            customerId,
            previousDueDate,
            newDueDate,
            reason,
            changedBy,
            changedAtUtc);

    public static string NormalizeReason(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCreditDueDateReason,
                "Due-date change reason is required.");
        }

        var trimmed = reason.Trim();
        if (trimmed.Length > ReasonMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCreditDueDateReason,
                $"Due-date change reason must be at most {ReasonMaxLength} characters.");
        }

        return trimmed;
    }
}

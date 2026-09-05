using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Domain.Expenses;

/// <summary>
/// Organization-owned store expense. Recorded once and immutable afterwards: the only permitted
/// transition is an explicit void with a reason and actor. Corrections are void + replacement.
///
/// <see cref="BranchId"/> null = organization-wide expense; non-null = branch-attributed expense
/// (opaque Platform OrganizationBranchId — not a POS FK).
///
/// Payment methods: Cash and ManualGCash only. Out of scope: suppliers, AP, wages, GL, tax/VAT,
/// OCR/attachments, split payments, gateways, and offline capture.
/// </summary>
public sealed class Expense
{
    public const int DescriptionMaxLength = 512;
    public const int PayeeMaxLength = 128;
    public const int VoidReasonMaxLength = 512;
    public const int GCashReferenceMaxLength = 64;

    public ExpenseId Id { get; }
    public PosOrganizationId OrganizationId { get; }
    /// <summary>Null = organization-wide; set = branch expense.</summary>
    public PosBranchId? BranchId { get; }
    public string ExpenseNumber { get; }
    public ExpenseCategoryId CategoryId { get; }
    public ExpenseStatus Status { get; private set; }
    public ExpensePaymentMethod PaymentMethod { get; }
    public decimal Amount { get; }
    public string Description { get; }
    public string? Payee { get; }
    public string? GCashReference { get; }
    public DateOnly ExpenseDate { get; }
    public DateTimeOffset RecordedAtUtc { get; }
    public Guid RecordedBy { get; }
    public DateTimeOffset? VoidedAtUtc { get; private set; }
    public Guid? VoidedBy { get; private set; }
    public string? VoidReason { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private Expense(
        ExpenseId id,
        PosOrganizationId organizationId,
        PosBranchId? branchId,
        string expenseNumber,
        ExpenseCategoryId categoryId,
        ExpenseStatus status,
        ExpensePaymentMethod paymentMethod,
        decimal amount,
        string description,
        string? payee,
        string? gcashReference,
        DateOnly expenseDate,
        DateTimeOffset recordedAtUtc,
        Guid recordedBy,
        DateTimeOffset? voidedAtUtc,
        Guid? voidedBy,
        string? voidReason,
        DateTimeOffset updatedAtUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        BranchId = branchId;
        ExpenseNumber = expenseNumber;
        CategoryId = categoryId;
        Status = status;
        PaymentMethod = paymentMethod;
        Amount = amount;
        Description = description;
        Payee = payee;
        GCashReference = gcashReference;
        ExpenseDate = expenseDate;
        RecordedAtUtc = recordedAtUtc;
        RecordedBy = recordedBy;
        VoidedAtUtc = voidedAtUtc;
        VoidedBy = voidedBy;
        VoidReason = voidReason;
        UpdatedAtUtc = updatedAtUtc;
    }

    /// <summary>
    /// Records a completed expense. The expense number is allocated server-side before this call;
    /// clients never supply one. Category activity is enforced by the application layer.
    /// </summary>
    public static Expense Record(
        PosOrganizationId organizationId,
        string expenseNumber,
        ExpenseCategoryId categoryId,
        ExpensePaymentMethod paymentMethod,
        decimal amount,
        string description,
        DateOnly expenseDate,
        Guid recordedBy,
        DateTimeOffset utcNow,
        string? payee = null,
        string? gcashReference = null,
        ExpenseId? id = null,
        PosBranchId? branchId = null)
    {
        ExpenseMoney.EnsureUtc(utcNow);
        ExpenseMoney.EnsureActor(recordedBy);

        var normalizedNumber = ExpenseNumbers.Normalize(expenseNumber);
        var normalizedAmount = ExpenseMoney.NormalizeAmount(amount);
        var normalizedDescription = NormalizeDescription(description);
        var normalizedPayee = NormalizePayee(payee);
        var reference = NormalizeGCashReference(paymentMethod, gcashReference);

        return new Expense(
            id ?? ExpenseId.New(),
            organizationId,
            branchId,
            normalizedNumber,
            categoryId,
            ExpenseStatus.Recorded,
            paymentMethod,
            normalizedAmount,
            normalizedDescription,
            normalizedPayee,
            reference,
            expenseDate,
            utcNow,
            recordedBy,
            null,
            null,
            null,
            utcNow);
    }

    public static Expense Rehydrate(
        ExpenseId id,
        PosOrganizationId organizationId,
        string expenseNumber,
        ExpenseCategoryId categoryId,
        ExpenseStatus status,
        ExpensePaymentMethod paymentMethod,
        decimal amount,
        string description,
        string? payee,
        string? gcashReference,
        DateOnly expenseDate,
        DateTimeOffset recordedAtUtc,
        Guid recordedBy,
        DateTimeOffset? voidedAtUtc,
        Guid? voidedBy,
        string? voidReason,
        DateTimeOffset updatedAtUtc,
        PosBranchId? branchId = null) =>
        new(
            id,
            organizationId,
            branchId,
            expenseNumber,
            categoryId,
            status,
            paymentMethod,
            amount,
            description,
            payee,
            gcashReference,
            expenseDate,
            recordedAtUtc,
            recordedBy,
            voidedAtUtc,
            voidedBy,
            voidReason,
            updatedAtUtc);

    /// <summary>
    /// Voids a recorded expense. Voiding is the only correction available. Only Recorded → Voided
    /// is permitted.
    /// </summary>
    public void Void(string reason, Guid voidedBy, DateTimeOffset utcNow)
    {
        ExpenseMoney.EnsureUtc(utcNow);
        ExpenseMoney.EnsureActor(voidedBy);

        if (Status == ExpenseStatus.Voided)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidExpenseStatusTransition,
                "Expense is already voided.");
        }

        var normalizedReason = NormalizeVoidReason(reason);

        Status = ExpenseStatus.Voided;
        VoidedAtUtc = utcNow;
        VoidedBy = voidedBy;
        VoidReason = normalizedReason;
        UpdatedAtUtc = utcNow;
    }

    public static string? NormalizeGCashReference(ExpensePaymentMethod paymentMethod, string? gcashReference)
    {
        if (string.IsNullOrWhiteSpace(gcashReference))
        {
            return null;
        }

        if (paymentMethod != ExpensePaymentMethod.ManualGCash)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidExpenseGCashReference,
                "A GCash reference can only be recorded on a manual GCash expense.");
        }

        var trimmed = gcashReference.Trim();
        if (trimmed.Length > GCashReferenceMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidExpenseGCashReference,
                $"GCash reference must be at most {GCashReferenceMaxLength} characters.");
        }

        return trimmed;
    }

    public static string NormalizeVoidReason(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidExpenseVoidReason,
                "A void reason is required.");
        }

        var trimmed = reason.Trim();
        if (trimmed.Length > VoidReasonMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidExpenseVoidReason,
                $"Void reason must be at most {VoidReasonMaxLength} characters.");
        }

        return trimmed;
    }

    public static string NormalizeDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidExpenseDescription,
                "Expense description is required.");
        }

        var trimmed = description.Trim();
        if (trimmed.Length > DescriptionMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidExpenseDescription,
                $"Expense description must be at most {DescriptionMaxLength} characters.");
        }

        return trimmed;
    }

    public static string? NormalizePayee(string? payee)
    {
        if (string.IsNullOrWhiteSpace(payee))
        {
            return null;
        }

        var trimmed = payee.Trim();
        if (trimmed.Length > PayeeMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidExpensePayee,
                $"Payee must be at most {PayeeMaxLength} characters.");
        }

        return trimmed;
    }
}

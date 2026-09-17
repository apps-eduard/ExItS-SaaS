using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Domain.Credit;

/// <summary>
/// Seller-owned B2B credit entry for an Organization buyer. Append-only after create:
/// amount and remarks cannot be edited; corrections use explicit reversal with a reason.
/// Optional calendar due date is denormalized for reads.
/// When created via Business Utang checkout, <see cref="SourceSaleId"/> links to the originating sale.
/// </summary>
public sealed class BusinessCreditEntry
{
    public const int RemarksMaxLength = CreditEntry.RemarksMaxLength;
    public const int ReversalReasonMaxLength = CreditEntry.ReversalReasonMaxLength;
    public const decimal MaxAmount = CreditEntry.MaxAmount;

    public BusinessCreditEntryId Id { get; }
    public PosOrganizationId SellerOrganizationId { get; }
    public PosOrganizationId BuyerOrganizationId { get; }
    /// <summary>Correlated connected-supplier relationship id (nullable; no Platform FK).</summary>
    public Guid? ConnectionId { get; }
    public decimal Amount => _amount;
    public string Remarks { get; }
    public CreditEntryStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset? ReversedAtUtc { get; private set; }
    public string? ReversalReason { get; private set; }
    public DateOnly? CurrentDueDate { get; private set; }

    /// <summary>Originating Business Utang sale when this credit was created at checkout; otherwise null.</summary>
    public SaleId? SourceSaleId { get; }

    private decimal _amount;

    private BusinessCreditEntry(
        BusinessCreditEntryId id,
        PosOrganizationId sellerOrganizationId,
        PosOrganizationId buyerOrganizationId,
        Guid? connectionId,
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
        SellerOrganizationId = sellerOrganizationId;
        BuyerOrganizationId = buyerOrganizationId;
        ConnectionId = connectionId;
        _amount = amount;
        Remarks = remarks;
        Status = status;
        CreatedAtUtc = createdAtUtc;
        ReversedAtUtc = reversedAtUtc;
        ReversalReason = reversalReason;
        CurrentDueDate = currentDueDate;
        SourceSaleId = sourceSaleId;
    }

    public static BusinessCreditEntry Create(
        PosOrganizationId sellerOrganizationId,
        PosOrganizationId buyerOrganizationId,
        decimal amount,
        string remarks,
        DateTimeOffset utcNow,
        Guid? connectionId = null,
        BusinessCreditEntryId? id = null,
        SaleId? sourceSaleId = null)
    {
        EnsureUtc(utcNow);
        return new BusinessCreditEntry(
            id ?? BusinessCreditEntryId.New(),
            sellerOrganizationId,
            buyerOrganizationId,
            NormalizeConnectionId(connectionId),
            CreditEntry.NormalizeAmount(amount),
            CreditEntry.NormalizeRemarks(remarks),
            CreditEntryStatus.Active,
            utcNow,
            null,
            null,
            null,
            sourceSaleId);
    }

    public static BusinessCreditEntry Rehydrate(
        BusinessCreditEntryId id,
        PosOrganizationId sellerOrganizationId,
        PosOrganizationId buyerOrganizationId,
        Guid? connectionId,
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
            sellerOrganizationId,
            buyerOrganizationId,
            connectionId,
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
                "Business credit entry is already reversed.");
        }

        Status = CreditEntryStatus.Reversed;
        ReversedAtUtc = utcNow;
        ReversalReason = CreditEntry.NormalizeReversalReason(reason);
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
                "Due dates cannot be set on a reversed business credit entry.");
        }

        CurrentDueDate = dueDate;
    }

    private static Guid? NormalizeConnectionId(Guid? connectionId) =>
        connectionId is Guid id && id != Guid.Empty ? id : null;

    private static void EnsureUtc(DateTimeOffset utcNow)
    {
        if (utcNow.Offset != TimeSpan.Zero)
        {
            throw new DomainException(DomainErrorCodes.InvalidUtcTimestamp, "Timestamp must be UTC.");
        }
    }
}

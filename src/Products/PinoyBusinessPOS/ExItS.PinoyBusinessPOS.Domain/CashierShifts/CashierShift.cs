using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Domain.CashierShifts;

/// <summary>
/// Organization-owned cashier shift. Opens directly in Open status with an opening cash float.
/// Close captures expected cash snapshot and variance; Cancel is allowed only before financial activity.
/// </summary>
public sealed class CashierShift
{
    public const int ClosingNotesMaxLength = 512;

    public CashierShiftId Id { get; }
    public PosOrganizationId OrganizationId { get; }
    public string ShiftNumber { get; }
    public Guid ActorId { get; }
    public CashierShiftStatus Status { get; private set; }
    public DateOnly BusinessDate { get; }
    public decimal OpeningCashAmount { get; private set; }
    public DateTimeOffset OpenedAtUtc { get; }
    public Guid OpenedBy { get; }
    public decimal? ClosingCashAmount { get; private set; }
    public decimal? ExpectedCashAmountSnapshot { get; private set; }
    public decimal? CashVarianceAmount { get; private set; }
    public string? ClosingNotes { get; private set; }
    public DateTimeOffset? ClosedAtUtc { get; private set; }
    public Guid? ClosedBy { get; private set; }
    public DateTimeOffset? CancelledAtUtc { get; private set; }
    public Guid? CancelledBy { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private CashierShift(
        CashierShiftId id,
        PosOrganizationId organizationId,
        string shiftNumber,
        Guid actorId,
        CashierShiftStatus status,
        DateOnly businessDate,
        decimal openingCashAmount,
        DateTimeOffset openedAtUtc,
        Guid openedBy,
        decimal? closingCashAmount,
        decimal? expectedCashAmountSnapshot,
        decimal? cashVarianceAmount,
        string? closingNotes,
        DateTimeOffset? closedAtUtc,
        Guid? closedBy,
        DateTimeOffset? cancelledAtUtc,
        Guid? cancelledBy,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        ShiftNumber = shiftNumber;
        ActorId = actorId;
        Status = status;
        BusinessDate = businessDate;
        OpeningCashAmount = openingCashAmount;
        OpenedAtUtc = openedAtUtc;
        OpenedBy = openedBy;
        ClosingCashAmount = closingCashAmount;
        ExpectedCashAmountSnapshot = expectedCashAmountSnapshot;
        CashVarianceAmount = cashVarianceAmount;
        ClosingNotes = closingNotes;
        ClosedAtUtc = closedAtUtc;
        ClosedBy = closedBy;
        CancelledAtUtc = cancelledAtUtc;
        CancelledBy = cancelledBy;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    /// <summary>Opens a shift directly in Open status with a non-negative opening float.</summary>
    public static CashierShift Open(
        PosOrganizationId organizationId,
        string shiftNumber,
        Guid actorId,
        decimal openingCashAmount,
        DateTimeOffset utcNow,
        DateOnly? businessDate = null,
        CashierShiftId? id = null,
        Guid? openedBy = null)
    {
        SaleMoney.EnsureUtc(utcNow);
        SaleMoney.EnsureActor(actorId);
        var opener = openedBy ?? actorId;
        SaleMoney.EnsureActor(opener);

        ValidateOpeningCash(openingCashAmount);

        return new CashierShift(
            id ?? CashierShiftId.New(),
            organizationId,
            CashierShiftNumbers.Normalize(shiftNumber),
            actorId,
            CashierShiftStatus.Open,
            businessDate ?? CashierShiftNumbers.BusinessDateOf(utcNow),
            SaleMoney.RoundMoney(openingCashAmount),
            utcNow,
            opener,
            closingCashAmount: null,
            expectedCashAmountSnapshot: null,
            cashVarianceAmount: null,
            closingNotes: null,
            closedAtUtc: null,
            closedBy: null,
            cancelledAtUtc: null,
            cancelledBy: null,
            utcNow,
            utcNow);
    }

    public static CashierShift Rehydrate(
        CashierShiftId id,
        PosOrganizationId organizationId,
        string shiftNumber,
        Guid actorId,
        CashierShiftStatus status,
        DateOnly businessDate,
        decimal openingCashAmount,
        DateTimeOffset openedAtUtc,
        Guid openedBy,
        decimal? closingCashAmount,
        decimal? expectedCashAmountSnapshot,
        decimal? cashVarianceAmount,
        string? closingNotes,
        DateTimeOffset? closedAtUtc,
        Guid? closedBy,
        DateTimeOffset? cancelledAtUtc,
        Guid? cancelledBy,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc) =>
        new(
            id,
            organizationId,
            shiftNumber,
            actorId,
            status,
            businessDate,
            openingCashAmount,
            openedAtUtc,
            openedBy,
            closingCashAmount,
            expectedCashAmountSnapshot,
            cashVarianceAmount,
            closingNotes,
            closedAtUtc,
            closedBy,
            cancelledAtUtc,
            cancelledBy,
            createdAtUtc,
            updatedAtUtc);

    public void Close(
        decimal closingCashAmount,
        decimal expectedCashAmount,
        Guid closedBy,
        DateTimeOffset utcNow,
        string? closingNotes = null)
    {
        SaleMoney.EnsureUtc(utcNow);
        SaleMoney.EnsureActor(closedBy);
        EnsureOpen();

        ValidateClosingCash(closingCashAmount);

        var roundedExpected = SaleMoney.RoundMoney(expectedCashAmount);
        var roundedClosing = SaleMoney.RoundMoney(closingCashAmount);
        var variance = SaleMoney.RoundMoney(roundedClosing - roundedExpected);

        Status = CashierShiftStatus.Closed;
        ClosingCashAmount = roundedClosing;
        ExpectedCashAmountSnapshot = roundedExpected;
        CashVarianceAmount = variance;
        ClosingNotes = NormalizeClosingNotes(closingNotes);
        ClosedAtUtc = utcNow;
        ClosedBy = closedBy;
        UpdatedAtUtc = utcNow;
    }

    public void Cancel(Guid cancelledBy, DateTimeOffset utcNow, bool hasLinkedSales, bool hasMovements)
    {
        SaleMoney.EnsureUtc(utcNow);
        SaleMoney.EnsureActor(cancelledBy);
        EnsureOpen();

        if (hasLinkedSales || hasMovements)
        {
            throw new DomainException(
                DomainErrorCodes.CashierShiftCancelBlockedByActivity,
                "An open shift with sales or cash movements cannot be cancelled.");
        }

        Status = CashierShiftStatus.Cancelled;
        CancelledAtUtc = utcNow;
        CancelledBy = cancelledBy;
        UpdatedAtUtc = utcNow;
    }

    public static void ValidateOpeningCash(decimal openingCashAmount)
    {
        if (openingCashAmount < 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCashierShiftOpeningCash,
                "Opening cash cannot be negative.");
        }

        if (!SaleMoney.HasAtMostDecimals(openingCashAmount, SaleMoney.MonetaryDecimals))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCashierShiftOpeningCash,
                "Opening cash must have at most 2 decimal places.");
        }
    }

    private static void ValidateClosingCash(decimal closingCashAmount)
    {
        if (closingCashAmount < 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCashierShiftClosingCash,
                "Closing cash cannot be negative.");
        }

        if (!SaleMoney.HasAtMostDecimals(closingCashAmount, SaleMoney.MonetaryDecimals))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCashierShiftClosingCash,
                "Closing cash must have at most 2 decimal places.");
        }
    }

    private static string? NormalizeClosingNotes(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
        {
            return null;
        }

        var trimmed = notes.Trim();
        if (trimmed.Length > ClosingNotesMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCashierShiftClosingNotes,
                $"Closing notes must be at most {ClosingNotesMaxLength} characters.");
        }

        return trimmed;
    }

    private void EnsureOpen()
    {
        if (Status != CashierShiftStatus.Open)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCashierShiftStatusTransition,
                "The operation requires an open shift.");
        }
    }
}

using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Registers;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Domain.CashierShifts;

/// <summary>
/// Organization-owned cashier shift. Opens directly in Open status with an opening cash float.
/// Close snapshots expected cash; counted cash and variance are optional unless the snapshotted
/// closing <see cref="CashCountMode"/> is Required. Cancel is allowed only before financial activity.
/// Every new shift links to exactly one Active Register (P10-WP07); legacy rows may have null RegisterId.
/// </summary>
public sealed class CashierShift
{
    public const int ClosingNotesMaxLength = 512;

    public CashierShiftId Id { get; }
    public PosOrganizationId OrganizationId { get; }
    public string ShiftNumber { get; }
    public Guid ActorId { get; }
    public RegisterId? RegisterId { get; }
    public CashierShiftStatus Status { get; private set; }
    public DateOnly BusinessDate { get; }
    public CashCountMode EffectiveOpeningCashCountMode { get; }
    public CashCountMode EffectiveClosingCashCountMode { get; }
    /// <summary>Mirrors <see cref="EffectiveClosingCashCountMode"/> for API / close back-compat.</summary>
    public CashCountMode EffectiveCashCountMode => EffectiveClosingCashCountMode;
    public bool OpeningCashCounted { get; private set; }
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
    public IReadOnlyList<CashCountDenominationLine> OpeningDenominationLines { get; private set; }
    public IReadOnlyList<CashCountDenominationLine> ClosingDenominationLines { get; private set; }

    private CashierShift(
        CashierShiftId id,
        PosOrganizationId organizationId,
        string shiftNumber,
        Guid actorId,
        RegisterId? registerId,
        CashierShiftStatus status,
        DateOnly businessDate,
        CashCountMode effectiveOpeningCashCountMode,
        CashCountMode effectiveClosingCashCountMode,
        bool openingCashCounted,
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
        DateTimeOffset updatedAtUtc,
        IReadOnlyList<CashCountDenominationLine>? openingDenominationLines = null,
        IReadOnlyList<CashCountDenominationLine>? closingDenominationLines = null)
    {
        Id = id;
        OrganizationId = organizationId;
        ShiftNumber = shiftNumber;
        ActorId = actorId;
        RegisterId = registerId;
        Status = status;
        BusinessDate = businessDate;
        EffectiveOpeningCashCountMode = effectiveOpeningCashCountMode;
        EffectiveClosingCashCountMode = effectiveClosingCashCountMode;
        OpeningCashCounted = openingCashCounted;
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
        OpeningDenominationLines = openingDenominationLines ?? Array.Empty<CashCountDenominationLine>();
        ClosingDenominationLines = closingDenominationLines ?? Array.Empty<CashCountDenominationLine>();
    }

    /// <summary>
    /// Opens a shift on an Active Register. Opening cash is required only when
    /// the opening mode is Required. Skipped counts persist as not-counted (float 0).
    /// Legacy <paramref name="cashCountMode"/> sets both opening and closing when the
    /// dedicated modes are omitted.
    /// </summary>
    public static CashierShift Open(
        PosOrganizationId organizationId,
        string shiftNumber,
        Guid actorId,
        RegisterId registerId,
        decimal? openingCashAmount,
        DateTimeOffset utcNow,
        DateOnly? businessDate = null,
        CashierShiftId? id = null,
        Guid? openedBy = null,
        CashCountMode? cashCountMode = null,
        CashCountMode? openingCashCountMode = null,
        CashCountMode? closingCashCountMode = null,
        IReadOnlyList<CashCountDenominationLine>? openingDenominationLines = null)
    {
        SaleMoney.EnsureUtc(utcNow);
        SaleMoney.EnsureActor(actorId);
        var opener = openedBy ?? actorId;
        SaleMoney.EnsureActor(opener);
        ArgumentNullException.ThrowIfNull(registerId);

        var (openingMode, closingMode) = ResolveEffectiveModes(
            cashCountMode,
            openingCashCountMode,
            closingCashCountMode);

        var counted = openingCashAmount is not null;
        if (CashCountModes.RequiresPhysicalCount(openingMode) && !counted)
        {
            throw new DomainException(
                DomainErrorCodes.CashierShiftOpeningCashCountRequired,
                "Opening cash count is required before this shift can become active.");
        }

        IReadOnlyList<CashCountDenominationLine> openingLines = Array.Empty<CashCountDenominationLine>();
        if (openingDenominationLines is { Count: > 0 })
        {
            if (!counted)
            {
                throw new DomainException(
                    DomainErrorCodes.CashCountDenominationTotalMismatch,
                    "A denomination breakdown requires an authoritative opening cash count.");
            }

            openingLines = CashCountDenominationBreakdown.EnsureMatchesSubmittedTotal(
                openingCashAmount!.Value,
                openingDenominationLines);
        }

        if (counted)
        {
            ValidateOpeningCash(openingCashAmount!.Value);
        }

        return new CashierShift(
            id ?? CashierShiftId.New(),
            organizationId,
            CashierShiftNumbers.Normalize(shiftNumber),
            actorId,
            registerId,
            CashierShiftStatus.Open,
            businessDate ?? CashierShiftNumbers.BusinessDateOf(utcNow),
            openingMode,
            closingMode,
            counted,
            counted ? SaleMoney.RoundMoney(openingCashAmount!.Value) : 0m,
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
            utcNow,
            openingLines,
            Array.Empty<CashCountDenominationLine>());
    }

    public static CashierShift Rehydrate(
        CashierShiftId id,
        PosOrganizationId organizationId,
        string shiftNumber,
        Guid actorId,
        RegisterId? registerId,
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
        DateTimeOffset updatedAtUtc,
        CashCountMode? effectiveCashCountMode = null,
        CashCountMode? effectiveOpeningCashCountMode = null,
        CashCountMode? effectiveClosingCashCountMode = null,
        bool openingCashCounted = true,
        IReadOnlyList<CashCountDenominationLine>? openingDenominationLines = null,
        IReadOnlyList<CashCountDenominationLine>? closingDenominationLines = null)
    {
        var (openingMode, closingMode) = ResolveEffectiveModes(
            effectiveCashCountMode,
            effectiveOpeningCashCountMode,
            effectiveClosingCashCountMode);
        return new(
            id,
            organizationId,
            shiftNumber,
            actorId,
            registerId,
            status,
            businessDate,
            openingMode,
            closingMode,
            openingCashCounted,
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
            updatedAtUtc,
            openingDenominationLines,
            closingDenominationLines);
    }

    public void Close(
        decimal? closingCashAmount,
        decimal expectedCashAmount,
        Guid closedBy,
        DateTimeOffset utcNow,
        string? closingNotes = null,
        IReadOnlyList<CashCountDenominationLine>? closingDenominationLines = null)
    {
        SaleMoney.EnsureUtc(utcNow);
        SaleMoney.EnsureActor(closedBy);
        EnsureOpen();

        if (CashCountModes.RequiresPhysicalCount(EffectiveClosingCashCountMode) && closingCashAmount is null)
        {
            throw new DomainException(
                DomainErrorCodes.CashierShiftClosingCashCountRequired,
                "Counted cash is required before this shift can close.");
        }

        var roundedExpected = SaleMoney.RoundMoney(expectedCashAmount);
        decimal? roundedClosing = null;
        decimal? variance = null;
        IReadOnlyList<CashCountDenominationLine> closingLines = Array.Empty<CashCountDenominationLine>();
        if (closingDenominationLines is { Count: > 0 })
        {
            if (closingCashAmount is null)
            {
                throw new DomainException(
                    DomainErrorCodes.CashCountDenominationTotalMismatch,
                    "A denomination breakdown requires an authoritative closing cash count.");
            }

            closingLines = CashCountDenominationBreakdown.EnsureMatchesSubmittedTotal(
                closingCashAmount.Value,
                closingDenominationLines);
        }

        if (closingCashAmount is not null)
        {
            ValidateClosingCash(closingCashAmount.Value);
            roundedClosing = SaleMoney.RoundMoney(closingCashAmount.Value);
            variance = SaleMoney.RoundMoney(roundedClosing.Value - roundedExpected);
        }

        Status = CashierShiftStatus.Closed;
        ClosingDenominationLines = closingLines;
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

    private static (CashCountMode Opening, CashCountMode Closing) ResolveEffectiveModes(
        CashCountMode? cashCountMode,
        CashCountMode? openingCashCountMode,
        CashCountMode? closingCashCountMode)
    {
        if (cashCountMode is not null && openingCashCountMode is null && closingCashCountMode is null)
        {
            return (cashCountMode.Value, cashCountMode.Value);
        }

        return (
            openingCashCountMode ?? cashCountMode ?? CashCountModes.Default,
            closingCashCountMode ?? cashCountMode ?? CashCountModes.Default);
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

using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Domain.CashierShifts;

/// <summary>Immutable cash drawer movement recorded on an Open shift. Not expense or accounting.</summary>
public sealed class CashierShiftMovement
{
    public const int ReasonMaxLength = 256;
    public const int ReferenceMaxLength = 128;

    public CashierShiftMovementId Id { get; }
    public CashierShiftId ShiftId { get; }
    public PosOrganizationId OrganizationId { get; }
    public CashierShiftMovementType MovementType { get; }
    public decimal Amount { get; }
    public string Reason { get; }
    public string? Reference { get; }
    public DateTimeOffset RecordedAtUtc { get; }
    public Guid RecordedBy { get; }

    private CashierShiftMovement(
        CashierShiftMovementId id,
        CashierShiftId shiftId,
        PosOrganizationId organizationId,
        CashierShiftMovementType movementType,
        decimal amount,
        string reason,
        string? reference,
        DateTimeOffset recordedAtUtc,
        Guid recordedBy)
    {
        Id = id;
        ShiftId = shiftId;
        OrganizationId = organizationId;
        MovementType = movementType;
        Amount = amount;
        Reason = reason;
        Reference = reference;
        RecordedAtUtc = recordedAtUtc;
        RecordedBy = recordedBy;
    }

    public static CashierShiftMovement Create(
        CashierShiftId shiftId,
        PosOrganizationId organizationId,
        CashierShiftMovementType movementType,
        decimal amount,
        string reason,
        Guid recordedBy,
        DateTimeOffset utcNow,
        string? reference = null,
        CashierShiftMovementId? id = null)
    {
        SaleMoney.EnsureUtc(utcNow);
        SaleMoney.EnsureActor(recordedBy);

        if (amount <= 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCashierShiftMovementAmount,
                "Movement amount must be greater than zero.");
        }

        if (!SaleMoney.HasAtMostDecimals(amount, SaleMoney.MonetaryDecimals))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCashierShiftMovementAmount,
                "Movement amount must have at most 2 decimal places.");
        }

        return new CashierShiftMovement(
            id ?? CashierShiftMovementId.New(),
            shiftId,
            organizationId,
            movementType,
            SaleMoney.RoundMoney(amount),
            NormalizeReason(reason),
            NormalizeReference(reference),
            utcNow,
            recordedBy);
    }

    public static CashierShiftMovement Rehydrate(
        CashierShiftMovementId id,
        CashierShiftId shiftId,
        PosOrganizationId organizationId,
        CashierShiftMovementType movementType,
        decimal amount,
        string reason,
        string? reference,
        DateTimeOffset recordedAtUtc,
        Guid recordedBy) =>
        new(id, shiftId, organizationId, movementType, amount, reason, reference, recordedAtUtc, recordedBy);

    public static string NormalizeReason(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCashierShiftMovementReason,
                "A movement reason is required.");
        }

        var trimmed = reason.Trim();
        if (trimmed.Length > ReasonMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCashierShiftMovementReason,
                $"Movement reason must be at most {ReasonMaxLength} characters.");
        }

        return trimmed;
    }

    private static string? NormalizeReference(string? reference)
    {
        if (string.IsNullOrWhiteSpace(reference))
        {
            return null;
        }

        var trimmed = reference.Trim();
        if (trimmed.Length > ReferenceMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCashierShiftMovementReference,
                $"Movement reference must be at most {ReferenceMaxLength} characters.");
        }

        return trimmed;
    }
}

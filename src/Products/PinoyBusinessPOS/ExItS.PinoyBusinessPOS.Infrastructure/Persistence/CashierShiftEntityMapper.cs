using ExItS.PinoyBusinessPOS.Domain.CashierShifts;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Registers;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.CashierShifts;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence;

internal static class CashierShiftEntityMapper
{
    public static CashierShift ToDomain(CashierShiftRecord record) =>
        CashierShift.Rehydrate(
            CashierShiftId.From(record.Id),
            PosOrganizationId.From(record.OrganizationId),
            record.ShiftNumber,
            record.ActorId,
            record.RegisterId is null ? null : RegisterId.From(record.RegisterId.Value),
            Enum.Parse<CashierShiftStatus>(record.Status, ignoreCase: true),
            record.BusinessDate,
            record.OpeningCashAmount,
            record.OpenedAtUtc,
            record.OpenedBy,
            record.ClosingCashAmount,
            record.ExpectedCashAmountSnapshot,
            record.CashVarianceAmount,
            record.ClosingNotes,
            record.ClosedAtUtc,
            record.ClosedBy,
            record.CancelledAtUtc,
            record.CancelledBy,
            record.CreatedAtUtc,
            record.UpdatedAtUtc);

    public static CashierShiftRecord ToRecord(CashierShift shift) =>
        new()
        {
            Id = shift.Id.Value,
            OrganizationId = shift.OrganizationId.Value,
            ShiftNumber = shift.ShiftNumber,
            ActorId = shift.ActorId,
            RegisterId = shift.RegisterId?.Value,
            Status = shift.Status.ToString(),
            BusinessDate = shift.BusinessDate,
            OpeningCashAmount = shift.OpeningCashAmount,
            OpenedAtUtc = shift.OpenedAtUtc,
            OpenedBy = shift.OpenedBy,
            ClosingCashAmount = shift.ClosingCashAmount,
            ExpectedCashAmountSnapshot = shift.ExpectedCashAmountSnapshot,
            CashVarianceAmount = shift.CashVarianceAmount,
            ClosingNotes = shift.ClosingNotes,
            ClosedAtUtc = shift.ClosedAtUtc,
            ClosedBy = shift.ClosedBy,
            CancelledAtUtc = shift.CancelledAtUtc,
            CancelledBy = shift.CancelledBy,
            CreatedAtUtc = shift.CreatedAtUtc,
            UpdatedAtUtc = shift.UpdatedAtUtc
        };

    public static void ApplyToRecord(CashierShift shift, CashierShiftRecord record)
    {
        record.Status = shift.Status.ToString();
        record.ClosingCashAmount = shift.ClosingCashAmount;
        record.ExpectedCashAmountSnapshot = shift.ExpectedCashAmountSnapshot;
        record.CashVarianceAmount = shift.CashVarianceAmount;
        record.ClosingNotes = shift.ClosingNotes;
        record.ClosedAtUtc = shift.ClosedAtUtc;
        record.ClosedBy = shift.ClosedBy;
        record.CancelledAtUtc = shift.CancelledAtUtc;
        record.CancelledBy = shift.CancelledBy;
        record.UpdatedAtUtc = shift.UpdatedAtUtc;
    }

    public static CashierShiftMovement ToDomain(CashierShiftMovementRecord record) =>
        CashierShiftMovement.Rehydrate(
            CashierShiftMovementId.From(record.Id),
            CashierShiftId.From(record.ShiftId),
            PosOrganizationId.From(record.OrganizationId),
            Enum.Parse<CashierShiftMovementType>(record.MovementType, ignoreCase: true),
            record.Amount,
            record.Reason,
            record.Reference,
            record.RecordedAtUtc,
            record.RecordedBy);

    public static CashierShiftMovementRecord ToRecord(CashierShiftMovement movement) =>
        new()
        {
            Id = movement.Id.Value,
            ShiftId = movement.ShiftId.Value,
            OrganizationId = movement.OrganizationId.Value,
            MovementType = movement.MovementType.ToString(),
            Amount = movement.Amount,
            Reason = movement.Reason,
            Reference = movement.Reference,
            RecordedAtUtc = movement.RecordedAtUtc,
            RecordedBy = movement.RecordedBy
        };
}

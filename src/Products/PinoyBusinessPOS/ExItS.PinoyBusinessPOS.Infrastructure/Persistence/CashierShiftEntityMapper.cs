using ExItS.PinoyBusinessPOS.Domain.CashierShifts;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Registers;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.CashierShifts;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence;

internal static class CashierShiftEntityMapper
{
    public static CashierShift ToDomain(
        CashierShiftRecord record,
        IReadOnlyList<CashierShiftCashCountLineRecord>? countLines = null)
    {
        var lines = countLines ?? Array.Empty<CashierShiftCashCountLineRecord>();
        return CashierShift.Rehydrate(
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
            record.UpdatedAtUtc,
            Enum.Parse<CashCountMode>(record.EffectiveCashCountMode, ignoreCase: true),
            record.OpeningCashCounted,
            lines.Where(l => l.CountKind == CashCountKinds.Opening)
                .OrderByDescending(l => l.DenominationValue)
                .Select(ToDomainLine)
                .ToList(),
            lines.Where(l => l.CountKind == CashCountKinds.Closing)
                .OrderByDescending(l => l.DenominationValue)
                .Select(ToDomainLine)
                .ToList());
    }

    public static IEnumerable<CashierShiftCashCountLineRecord> ToLineRecords(CashierShift shift)
    {
        foreach (var line in shift.OpeningDenominationLines)
        {
            yield return ToLineRecord(shift, CashCountKinds.Opening, line);
        }

        foreach (var line in shift.ClosingDenominationLines)
        {
            yield return ToLineRecord(shift, CashCountKinds.Closing, line);
        }
    }

    private static CashCountDenominationLine ToDomainLine(CashierShiftCashCountLineRecord record) =>
        new(record.DenominationValue, record.Quantity);

    private static CashierShiftCashCountLineRecord ToLineRecord(
        CashierShift shift,
        string countKind,
        CashCountDenominationLine line) =>
        new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = shift.OrganizationId.Value,
            ShiftId = shift.Id.Value,
            CountKind = countKind,
            DenominationValue = line.DenominationValue,
            Quantity = line.Quantity,
            LineTotal = line.LineTotal
        };

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
            EffectiveCashCountMode = shift.EffectiveCashCountMode.ToString(),
            OpeningCashCounted = shift.OpeningCashCounted,
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

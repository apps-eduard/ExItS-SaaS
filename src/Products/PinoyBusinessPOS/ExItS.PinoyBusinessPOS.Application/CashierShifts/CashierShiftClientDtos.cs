using ExItS.PinoyBusinessPOS.Application.OperationalSetup;

namespace ExItS.PinoyBusinessPOS.Application.CashierShifts;

public sealed record PosCashierShiftMovementDto(
    Guid MovementId,
    Guid ShiftId,
    Guid OrganizationId,
    string MovementType,
    decimal Amount,
    string Reason,
    string? Reference,
    DateTimeOffset RecordedAtUtc,
    Guid RecordedBy);

public sealed record PosCashierShiftDto(
    Guid ShiftId,
    Guid OrganizationId,
    string ShiftNumber,
    string Status,
    Guid ActorId,
    Guid? RegisterId,
    string? RegisterCode,
    string? RegisterName,
    DateOnly BusinessDate,
    decimal OpeningCashAmount,
    bool OpeningCashCounted,
    string EffectiveCashCountMode,
    DateTimeOffset OpenedAtUtc,
    Guid OpenedBy,
    decimal? ClosingCashAmount,
    decimal? ExpectedCashAmountSnapshot,
    decimal? CashVarianceAmount,
    string? ClosingCashCountState,
    string? ClosingNotes,
    DateTimeOffset? ClosedAtUtc,
    Guid? ClosedBy,
    DateTimeOffset? CancelledAtUtc,
    Guid? CancelledBy,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    IReadOnlyList<CashCountDenominationLineDto>? OpeningDenominationLines = null,
    IReadOnlyList<CashCountDenominationLineDto>? ClosingDenominationLines = null,
    string? EffectiveOpeningCashCountMode = null,
    string? EffectiveClosingCashCountMode = null);

public sealed record PosCashierShiftSummaryDto(
    Guid ShiftId,
    string ShiftNumber,
    string Status,
    decimal OpeningCashAmount,
    bool OpeningCashCounted,
    string EffectiveCashCountMode,
    decimal NetCashSales,
    decimal CashSalesTotal,
    decimal GCashSalesTotal,
    decimal UtangSalesTotal,
    decimal CashRefundsTotal,
    decimal TotalCashIn,
    decimal TotalCashOut,
    decimal ExpectedCashAmount,
    decimal? ClosingCashAmount,
    decimal? ExpectedCashAmountSnapshot,
    decimal? CashVarianceAmount,
    string? ClosingCashCountState,
    int CompletedCashCount,
    int VoidedCashCount,
    int CompletedGCashCount,
    int CompletedUtangCount,
    IReadOnlyList<PosCashierShiftMovementDto> Movements,
    IReadOnlyList<CashCountDenominationLineDto>? OpeningDenominationLines = null,
    IReadOnlyList<CashCountDenominationLineDto>? ClosingDenominationLines = null,
    string? EffectiveOpeningCashCountMode = null,
    string? EffectiveClosingCashCountMode = null);

public sealed record OpenCashierShiftRequest(
    Guid RegisterId,
    decimal? OpeningCashAmount = null,
    DateOnly? BusinessDate = null,
    IReadOnlyList<CashCountDenominationLineDto>? DenominationLines = null);

public sealed record CloseCashierShiftRequest(
    decimal? ClosingCashAmount = null,
    string? Notes = null,
    IReadOnlyList<CashCountDenominationLineDto>? DenominationLines = null);

public sealed record RecordCashierShiftMovementRequest(
    string MovementType,
    decimal Amount,
    string Reason,
    string? Reference = null,
    Guid? MovementId = null);

public sealed record PosCashierShiftPagedResult(
    List<PosCashierShiftDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

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
    DateOnly BusinessDate,
    decimal OpeningCashAmount,
    DateTimeOffset OpenedAtUtc,
    Guid OpenedBy,
    decimal? ClosingCashAmount,
    decimal? ExpectedCashAmountSnapshot,
    decimal? CashVarianceAmount,
    string? ClosingNotes,
    DateTimeOffset? ClosedAtUtc,
    Guid? ClosedBy,
    DateTimeOffset? CancelledAtUtc,
    Guid? CancelledBy,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record PosCashierShiftSummaryDto(
    Guid ShiftId,
    string ShiftNumber,
    string Status,
    decimal OpeningCashAmount,
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
    int CompletedCashCount,
    int VoidedCashCount,
    int CompletedGCashCount,
    int CompletedUtangCount,
    IReadOnlyList<PosCashierShiftMovementDto> Movements);

public sealed record OpenCashierShiftRequest(decimal OpeningCashAmount, DateOnly? BusinessDate = null);

public sealed record CloseCashierShiftRequest(decimal ClosingCashAmount, string? Notes = null);

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

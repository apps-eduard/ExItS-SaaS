using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.CashierShifts;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Offline;
using ExItS.PinoyBusinessPOS.Domain.CashierShifts;
using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Api.CashierShifts;

/// <summary>Organization-scoped cashier shift endpoints (P10-WP04). Online-only.</summary>
internal static class CashierShiftEndpoints
{
    public static IEndpointRouteBuilder MapCashierShiftEndpoints(this IEndpointRouteBuilder app)
    {
        MapCashierShifts(app.MapGroup("/api/v1/pos/cashier-shifts"));
        return app;
    }

    private static void MapCashierShifts(RouteGroupBuilder group)
    {
        group.MapGet("/", async (
            HttpRequest request,
            string? status,
            Guid? actorId,
            string? shiftNumber,
            string? fromBusinessDate,
            string? toBusinessDate,
            int? page,
            int? pageSize,
            CashierShiftQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewShifts, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!TryParseStatus(status, out var parsedStatus, out problem)
                || !TryParseDate(fromBusinessDate, "fromBusinessDate", out var parsedFrom, out problem)
                || !TryParseDate(toBusinessDate, "toBusinessDate", out var parsedTo, out problem))
            {
                return problem!;
            }

            var result = await queries
                .ListAsync(
                    organizationId,
                    new CashierShiftFilter(parsedStatus, actorId, shiftNumber, parsedFrom, parsedTo),
                    page,
                    pageSize,
                    ct)
                .ConfigureAwait(false);

            return Results.Ok(new PosCashierShiftPagedResult(
                result.Items.ToList(),
                result.TotalCount,
                result.Page,
                result.PageSize));
        });

        group.MapGet("/current", async (
            HttpRequest request,
            CashierShiftQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewShifts, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!PosOrganizationScope.TryGetActorId(request, out var actorId, out problem))
            {
                return problem!;
            }

            var shift = await queries.GetCurrentOpenForActorAsync(organizationId, actorId, ct).ConfigureAwait(false);
            return shift is null ? Results.NotFound() : Results.Ok(shift);
        });

        group.MapGet("/{shiftId:guid}", async (
            HttpRequest request,
            Guid shiftId,
            CashierShiftQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewShifts, out var organizationId, out var problem))
            {
                return problem!;
            }

            var shift = await queries.GetByIdAsync(organizationId, shiftId, ct).ConfigureAwait(false);
            return shift is null ? Results.NotFound() : Results.Ok(shift);
        });

        group.MapGet("/{shiftId:guid}/summary", async (
            HttpRequest request,
            Guid shiftId,
            CashierShiftQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewShifts, out var organizationId, out var problem))
            {
                return problem!;
            }

            var summary = await queries.GetSummaryAsync(organizationId, shiftId, ct).ConfigureAwait(false);
            return summary is null ? Results.NotFound() : Results.Ok(summary);
        });

        group.MapPost("/", async (
            HttpRequest request,
            OpenCashierShiftRequest body,
            OpenCashierShift useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManageShifts, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!PosOrganizationScope.TryGetActorId(request, out var actorId, out problem))
            {
                return problem!;
            }

            var result = await useCase
                .ExecuteAsync(organizationId, actorId, body.OpeningCashAmount, body.BusinessDate, ct)
                .ConfigureAwait(false);
            return PosApiResults.FromResult(
                result,
                shift =>
                {
                    var dto = CashierShiftQueryService.Map(shift);
                    return Results.Created($"/api/v1/pos/cashier-shifts/{dto.ShiftId:D}", dto);
                });
        });

        group.MapPost("/{shiftId:guid}/close", async (
            HttpRequest request,
            Guid shiftId,
            CloseCashierShiftRequest body,
            CloseCashierShift useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManageShifts, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!PosOrganizationScope.TryGetActorId(request, out var actorId, out problem))
            {
                return problem!;
            }

            var result = await useCase
                .ExecuteAsync(organizationId, shiftId, body.ClosingCashAmount, actorId, body.Notes, ct)
                .ConfigureAwait(false);
            return PosApiResults.FromResult(result, shift => Results.Ok(CashierShiftQueryService.Map(shift)));
        });

        group.MapPost("/{shiftId:guid}/cancel", async (
            HttpRequest request,
            Guid shiftId,
            CancelCashierShift useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManageShifts, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!PosOrganizationScope.TryGetActorId(request, out var actorId, out problem))
            {
                return problem!;
            }

            var result = await useCase.ExecuteAsync(organizationId, shiftId, actorId, ct).ConfigureAwait(false);
            return PosApiResults.FromResult(result, shift => Results.Ok(CashierShiftQueryService.Map(shift)));
        });

        group.MapPost("/{shiftId:guid}/movements", async (
            HttpRequest request,
            Guid shiftId,
            RecordCashierShiftMovementRequest body,
            RecordCashierShiftMovement useCase,
            IPosIdempotencyService idempotency,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManageShifts, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!PosOrganizationScope.TryGetActorId(request, out var actorId, out problem))
            {
                return problem!;
            }

            return await PosIdempotencyEndpointHelper.ExecuteMutationAsync(
                    request,
                    organizationId,
                    OfflineOperationTypes.CashierShiftMovement,
                    idempotency,
                    ct2 => useCase.ExecuteAsync(
                        organizationId,
                        shiftId,
                        body.MovementType,
                        body.Amount,
                        body.Reason,
                        actorId,
                        body.Reference,
                        body.MovementId,
                        ct2),
                    CashierShiftQueryService.MapMovement,
                    Results.Ok,
                    ct)
                .ConfigureAwait(false);
        });
    }

    private static bool TryAuthorize(
        HttpRequest request,
        IPosCommercialAccessAccessor access,
        UtangCapability capability,
        out Guid organizationId,
        out IResult? problem)
    {
        if (!PosOrganizationScope.TryGetOrganizationId(request, out organizationId, out problem))
        {
            return false;
        }

        return PosCommercialScope.TryAuthorize(access, capability, out problem);
    }

    private static bool TryParseStatus(string? status, out CashierShiftStatus? parsed, out IResult? problem)
    {
        parsed = null;
        problem = null;
        if (string.IsNullOrWhiteSpace(status))
        {
            return true;
        }

        if (Enum.TryParse<CashierShiftStatus>(status.Trim(), ignoreCase: true, out var value))
        {
            parsed = value;
            return true;
        }

        problem = PosApiResults.Problem(
            DomainErrorCodes.InvalidCashierShiftStatus,
            "Shift status must be Open, Closed, or Cancelled.",
            StatusCodes.Status400BadRequest);
        return false;
    }

    private static bool TryParseDate(string? value, string fieldName, out DateOnly? parsed, out IResult? problem)
    {
        parsed = null;
        problem = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (DateOnly.TryParse(value, out var date))
        {
            parsed = date;
            return true;
        }

        problem = PosApiResults.Problem(
            ApplicationErrorCodes.DomainViolation,
            $"{fieldName} must be a valid date (YYYY-MM-DD).",
            StatusCodes.Status400BadRequest);
        return false;
    }
}

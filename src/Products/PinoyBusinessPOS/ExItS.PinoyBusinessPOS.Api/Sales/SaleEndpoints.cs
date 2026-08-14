using System.Globalization;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Offline;
using ExItS.PinoyBusinessPOS.Application.Sales;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Api.Sales;

/// <summary>
/// Organization-scoped simple retail sales endpoints. Development-stage only: organization
/// scope comes from <c>X-Pos-Organization-Id</c>, the actor from <c>X-Dev-Platform-User-Id</c>, and
/// cross-organization access returns 404 (fail closed).
///
/// Totals for online carts are computed server-side from the live catalog. Offline cash sync may
/// supply immutable line snapshots (UnitPrice/UOM/SellingMode/LineTotal); the server validates
/// arithmetic consistency without replacing those snapshots from the live catalog. Product-Based
/// Utang checkout also creates a linked remarks credit. Tracked inventory is deducted atomically at
/// checkout and restored on void. No discount, tax, refund, split tender, or gateway surface exists here.
/// </summary>
internal static class SaleEndpoints
{
    public static IEndpointRouteBuilder MapSaleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/pos/sales");

        group.MapGet("/", async (
            HttpRequest request,
            string? status,
            string? paymentMethod,
            string? fromDate,
            string? toDate,
            string? saleNumber,
            int? page,
            int? pageSize,
            SaleQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewSales, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!TryParseStatus(status, out var parsedStatus, out problem)
                || !TryParsePaymentMethod(paymentMethod, out var parsedMethod, out problem)
                || !TryParseDate(fromDate, "fromDate", out var parsedFrom, out problem)
                || !TryParseDate(toDate, "toDate", out var parsedTo, out problem))
            {
                return problem!;
            }

            var filter = new SaleFilter(parsedStatus, parsedMethod, parsedFrom, parsedTo, saleNumber);
            var result = await queries.ListAsync(organizationId, filter, page, pageSize, ct).ConfigureAwait(false);
            return Results.Ok(result);
        });

        group.MapPost("/", async (
            HttpRequest request,
            CheckoutSaleRequest body,
            CheckoutSale useCase,
            IPosIdempotencyService idempotency,
            IPosCommercialAccessAccessor access,
            IPosDeviceTransactionAuthorizer deviceAuthorization,
            CancellationToken ct) =>
        {
            var isUtang = SalePaymentMethods.TryParse(body.PaymentMethod, out var method)
                && method == SalePaymentMethod.Utang;

            if (isUtang)
            {
                if (!TryAuthorize(request, access, UtangCapability.CreateSale, out var organizationId, out var problem)
                    || !PosCommercialScope.TryAuthorize(access, UtangCapability.CreateCredit, out problem))
                {
                    return problem!;
                }

                if (!PosOrganizationScope.TryGetActorId(request, out var actorId, out problem))
                {
                    return problem!;
                }

                var deviceDenied = await deviceAuthorization.EnsureAuthorizedAsync(request, organizationId, ct).ConfigureAwait(false);
                if (deviceDenied is not null) return deviceDenied;

                return await PosIdempotencyEndpointHelper.ExecuteMutationAsync(
                        request,
                        organizationId,
                        OfflineOperationTypes.SaleCheckout,
                        idempotency,
                        ct2 => useCase.ExecuteAsync(
                            organizationId,
                            body.Lines,
                            body.PaymentMethod,
                            actorId,
                            body.AmountTendered,
                            body.GCashReference,
                            body.SaleId,
                            body.CustomerId,
                            body.DueDate,
                            body.CreditEntryId,
                            body.ShiftId,
                            body.BuyerPartyKind,
                            body.BuyerDisplayNameSnapshot,
                            body.BuyerPersonalPublicUserId,
                            body.BuyerOrganizationId,
                            body.BuyerPublicOrganizationId,
                            ct2),
                        SaleQueryService.Map,
                        dto => Results.Created($"/api/v1/pos/sales/{dto.SaleId:D}", dto),
                        ct)
                    .ConfigureAwait(false);
            }

            if (!TryAuthorize(request, access, UtangCapability.CreateSale, out var cashOrgId, out var cashProblem))
            {
                return cashProblem!;
            }

            if (!PosOrganizationScope.TryGetActorId(request, out var cashActorId, out cashProblem))
            {
                return cashProblem!;
            }

            var cashDeviceDenied = await deviceAuthorization.EnsureAuthorizedAsync(request, cashOrgId, ct).ConfigureAwait(false);
            if (cashDeviceDenied is not null) return cashDeviceDenied;

            return await PosIdempotencyEndpointHelper.ExecuteMutationAsync(
                    request,
                    cashOrgId,
                    OfflineOperationTypes.SaleCheckout,
                    idempotency,
                    ct2 => useCase.ExecuteAsync(
                        cashOrgId,
                        body.Lines,
                        body.PaymentMethod,
                        cashActorId,
                        body.AmountTendered,
                        body.GCashReference,
                        body.SaleId,
                        body.CustomerId,
                        body.DueDate,
                        body.CreditEntryId,
                        body.ShiftId,
                        body.BuyerPartyKind,
                        body.BuyerDisplayNameSnapshot,
                        body.BuyerPersonalPublicUserId,
                        body.BuyerOrganizationId,
                        body.BuyerPublicOrganizationId,
                        ct2),
                    SaleQueryService.Map,
                    dto => Results.Created($"/api/v1/pos/sales/{dto.SaleId:D}", dto),
                    ct)
                .ConfigureAwait(false);
        });

        group.MapGet("/{saleId:guid}", async (
            HttpRequest request,
            Guid saleId,
            SaleQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewSales, out var organizationId, out var problem))
            {
                return problem!;
            }

            var sale = await queries.GetByIdAsync(organizationId, saleId, ct).ConfigureAwait(false);
            return sale is null
                ? PosApiResults.Problem(
                    ApplicationErrorCodes.SaleNotFound,
                    "Sale was not found.",
                    StatusCodes.Status404NotFound)
                : Results.Ok(sale);
        });

        group.MapPost("/{saleId:guid}/void", async (
            HttpRequest request,
            Guid saleId,
            VoidSaleRequest body,
            VoidSale useCase,
            SaleQueryService queries,
            IPosCommercialAccessAccessor access,
            IPosDeviceTransactionAuthorizer deviceAuthorization,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.VoidSale, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!PosOrganizationScope.TryGetActorId(request, out var actorId, out problem))
            {
                return problem!;
            }

            var deviceDenied = await deviceAuthorization.EnsureAuthorizedAsync(request, organizationId, ct).ConfigureAwait(false);
            if (deviceDenied is not null) return deviceDenied;

            // Peek payment method under VoidSale auth so Utang voids can require ReverseCredit too.
            var existing = await queries.GetByIdAsync(organizationId, saleId, ct).ConfigureAwait(false);
            if (existing is not null
                && string.Equals(existing.PaymentMethod, PosSaleOptions.UtangPaymentMethod, StringComparison.Ordinal)
                && !PosCommercialScope.TryAuthorize(access, UtangCapability.ReverseCredit, out problem))
            {
                return problem!;
            }

            var result = await useCase
                .ExecuteAsync(organizationId, saleId, body.Reason, actorId, ct)
                .ConfigureAwait(false);
            return PosApiResults.FromResult(result, s => Results.Ok(SaleQueryService.Map(s)));
        });

        return app;
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

    private static bool TryParseStatus(string? status, out SaleStatus? parsed, out IResult? problem)
    {
        parsed = null;
        problem = null;
        if (string.IsNullOrWhiteSpace(status))
        {
            return true;
        }

        if (!Enum.TryParse<SaleStatus>(status, ignoreCase: true, out var value))
        {
            problem = PosApiResults.Problem(
                DomainErrorCodes.InvalidSaleStatus,
                $"Unrecognized sale status '{status}'.",
                StatusCodes.Status400BadRequest);
            return false;
        }

        parsed = value;
        return true;
    }

    private static bool TryParsePaymentMethod(
        string? paymentMethod,
        out SalePaymentMethod? parsed,
        out IResult? problem)
    {
        parsed = null;
        problem = null;
        if (string.IsNullOrWhiteSpace(paymentMethod))
        {
            return true;
        }

        if (!SalePaymentMethods.TryParse(paymentMethod, out var value))
        {
            problem = PosApiResults.Problem(
                DomainErrorCodes.InvalidSalePaymentMethod,
                $"Unrecognized payment method '{paymentMethod}'.",
                StatusCodes.Status400BadRequest);
            return false;
        }

        parsed = value;
        return true;
    }

    private static bool TryParseDate(string? value, string name, out DateOnly? parsed, out IResult? problem)
    {
        parsed = null;
        problem = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (!DateOnly.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var date))
        {
            problem = PosApiResults.Problem(
                ApplicationErrorCodes.DomainViolation,
                $"Query parameter '{name}' must be an ISO date (yyyy-MM-dd).",
                StatusCodes.Status400BadRequest);
            return false;
        }

        parsed = date;
        return true;
    }
}

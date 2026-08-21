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
/// checkout and restored on void. No refund, split tender, or gateway surface exists here.
///
/// Manual commercial discounts require <c>ApplyCommercialDiscount</c> in addition to
/// <c>CreateSale</c>, and are rejected outright on the offline snapshot path. Sale price overrides
/// require <c>OverrideSalePrice</c> (and <c>OverrideSalePriceUnlimited</c> when the server-computed
/// deviation exceeds the manager ceiling). Promotions and statutory discounts remain separate.
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
                    || !PosCommercialScope.TryAuthorize(access, UtangCapability.CreateCredit, out problem)
                    || !TryAuthorizeCheckoutAdjustments(access, body, out var allowUnlimited, out problem))
                {
                    return problem!;
                }

                if (!PosOrganizationScope.TryGetActorId(request, out var actorId, out problem))
                {
                    return problem!;
                }

                var deviceDenied = await deviceAuthorization.EnsureAuthorizedAsync(request, organizationId, ct).ConfigureAwait(false);
                if (deviceDenied is not null) return deviceDenied;
                if (!TryResolveCheckoutBranch(request, out var branchId, out problem))
                {
                    return problem!;
                }

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
                            branchId,
                            body.Discounts,
                            body.PriceOverrides,
                            allowUnlimited,
                            ct2),
                        SaleQueryService.Map,
                        dto => Results.Created($"/api/v1/pos/sales/{dto.SaleId:D}", dto),
                        ct)
                    .ConfigureAwait(false);
            }

            if (!TryAuthorize(request, access, UtangCapability.CreateSale, out var cashOrgId, out var cashProblem)
                || !TryAuthorizeCheckoutAdjustments(access, body, out var cashAllowUnlimited, out cashProblem))
            {
                return cashProblem!;
            }

            if (!PosOrganizationScope.TryGetActorId(request, out var cashActorId, out cashProblem))
            {
                return cashProblem!;
            }

            var cashDeviceDenied = await deviceAuthorization.EnsureAuthorizedAsync(request, cashOrgId, ct).ConfigureAwait(false);
            if (cashDeviceDenied is not null) return cashDeviceDenied;
            if (!TryResolveCheckoutBranch(request, out var cashBranchId, out cashProblem))
            {
                return cashProblem!;
            }

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
                        cashBranchId,
                        body.Discounts,
                        body.PriceOverrides,
                        cashAllowUnlimited,
                        ct2),
                    SaleQueryService.Map,
                    dto => Results.Created($"/api/v1/pos/sales/{dto.SaleId:D}", dto),
                    ct)
                .ConfigureAwait(false);
        });

        // Preview only: prices the cart, applies overrides and discounts plus tax, persists nothing.
        // Checkout revalidates independently, so a quote never authorizes the amounts it returns.
        group.MapPost("/quote", async (
            HttpRequest request,
            CheckoutSaleRequest body,
            CheckoutSale useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.CreateSale, out var organizationId, out var problem)
                || !TryAuthorizeCheckoutAdjustments(access, body, out var allowUnlimited, out problem))
            {
                return problem!;
            }

            var result = await useCase
                .QuoteAsync(organizationId, body.Lines, body.Discounts, body.PriceOverrides, allowUnlimited, ct)
                .ConfigureAwait(false);
            return PosApiResults.FromResult(result, Results.Ok);
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

            PosOrganizationScope.TryGetOptionalBranchId(request, out var branchId);
            var result = await useCase
                .ExecuteAsync(organizationId, saleId, body.Reason, actorId, branchId, ct)
                .ConfigureAwait(false);
            return PosApiResults.FromResult(result, s => Results.Ok(SaleQueryService.Map(s)));
        });

        return app;
    }

    /// <summary>
    /// Gates commercial discounts and sale price overrides. Unlimited is probed (not required) so
    /// StoreManager can override within the server-enforced 100% ceiling; Owner/Admin with the
    /// unlimited feature pass <c>allowUnlimited = true</c>. Client-claimed percentages are ignored.
    /// </summary>
    private static bool TryAuthorizeCheckoutAdjustments(
        IPosCommercialAccessAccessor access,
        CheckoutSaleRequest body,
        out bool allowUnlimitedSalePriceOverride,
        out IResult? problem)
    {
        allowUnlimitedSalePriceOverride = false;
        problem = null;

        if (body.Discounts is { Count: > 0 }
            && !PosCommercialScope.TryAuthorize(access, UtangCapability.ApplyCommercialDiscount, out problem))
        {
            return false;
        }

        if (body.PriceOverrides is not { Count: > 0 })
        {
            return true;
        }

        if (!PosCommercialScope.TryAuthorize(access, UtangCapability.OverrideSalePrice, out problem))
        {
            return false;
        }

        // Soft probe: presence of unlimited capability widens the domain ceiling; absence does not
        // deny the request here — domain still rejects deviations above the manager limit.
        allowUnlimitedSalePriceOverride = PosCommercialScope.TryAuthorize(
            access,
            UtangCapability.OverrideSalePriceUnlimited,
            out _);
        return true;
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

    private static bool TryResolveCheckoutBranch(HttpRequest request, out Guid? branchId, out IResult? problem)
    {
        problem = null;
        if (!PosOrganizationScope.TryGetOptionalBranchId(request, out branchId))
        {
            problem = PosApiResults.Problem(
                DomainErrorCodes.InvalidBranchId,
                $"Header '{PosOrganizationHeaders.BranchHeaderName}' must be a non-empty GUID.",
                StatusCodes.Status400BadRequest);
            return false;
        }

        var environment = request.HttpContext.RequestServices.GetRequiredService<IHostEnvironment>();
        if (branchId is null && !environment.IsEnvironment("Testing"))
        {
            problem = PosApiResults.Problem(
                ApplicationErrorCodes.SaleBranchRequired,
                $"Header '{PosOrganizationHeaders.BranchHeaderName}' is required for checkout.",
                StatusCodes.Status400BadRequest);
            return false;
        }

        return true;
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

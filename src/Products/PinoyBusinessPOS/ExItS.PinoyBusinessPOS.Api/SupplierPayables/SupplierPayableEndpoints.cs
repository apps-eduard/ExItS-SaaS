using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Offline;
using ExItS.PinoyBusinessPOS.Application.SupplierPayables;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.SupplierPayables;

namespace ExItS.PinoyBusinessPOS.Api.SupplierPayables;

/// <summary>
/// Organization-scoped supplier payable endpoints (POS-SUPPLIER-PAYABLES-01 / ADR-023).
/// Separate from Customer Utang. Online-only — payments use server Idempotency-Key replay.
/// </summary>
internal static class SupplierPayableEndpoints
{
    public static IEndpointRouteBuilder MapSupplierPayableEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/pos/supplier-payables");

        group.MapGet("/", async (
            HttpRequest request,
            Guid? supplierId,
            string? status,
            int? page,
            int? pageSize,
            SupplierPayableQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewPurchasing, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!TryParseStatus(status, out _, out problem))
            {
                return problem!;
            }

            try
            {
                var result = await queries
                    .ListAsync(organizationId, supplierId, status, page, pageSize, ct)
                    .ConfigureAwait(false);
                return Results.Ok(result);
            }
            catch (DomainException ex)
            {
                return PosApiResults.Problem(ex.ErrorCode, ex.Message, StatusCodes.Status400BadRequest);
            }
        });

        group.MapGet("/{payableId:guid}", async (
            HttpRequest request,
            Guid payableId,
            SupplierPayableQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewPurchasing, out var organizationId, out var problem))
            {
                return problem!;
            }

            var dto = await queries.GetByIdAsync(organizationId, payableId, ct).ConfigureAwait(false);
            return dto is null
                ? PosApiResults.Problem(
                    ApplicationErrorCodes.SupplierPayableNotFound,
                    "Supplier payable was not found.",
                    StatusCodes.Status404NotFound)
                : Results.Ok(dto);
        });

        group.MapGet("/{payableId:guid}/payments", async (
            HttpRequest request,
            Guid payableId,
            SupplierPayableQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewPurchasing, out var organizationId, out var problem))
            {
                return problem!;
            }

            var payable = await queries.GetByIdAsync(organizationId, payableId, ct).ConfigureAwait(false);
            if (payable is null)
            {
                return PosApiResults.Problem(
                    ApplicationErrorCodes.SupplierPayableNotFound,
                    "Supplier payable was not found.",
                    StatusCodes.Status404NotFound);
            }

            var payments = await queries
                .ListPaymentsAsync(organizationId, payableId, ct)
                .ConfigureAwait(false);
            return Results.Ok(payments);
        });

        group.MapPost("/{payableId:guid}/payments", async (
            HttpRequest request,
            Guid payableId,
            RecordSupplierPayablePaymentRequest body,
            RecordSupplierPayablePayment useCase,
            IPosIdempotencyService idempotency,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManagePurchasing, out var organizationId, out var problem)
                || !PosOrganizationScope.TryGetActorId(request, out var actorId, out problem))
            {
                return problem!;
            }

            return await PosIdempotencyEndpointHelper
                .ExecuteMutationAsync(
                    request,
                    organizationId,
                    OfflineOperationTypes.SupplierPayablePayment,
                    idempotency,
                    ct2 => useCase.ExecuteAsync(organizationId, payableId, body, actorId, ct2),
                    dto => dto,
                    dto => Results.Created(
                        $"/api/v1/pos/supplier-payables/{payableId:D}/payments/{dto.PaymentId:D}",
                        dto),
                    ct)
                .ConfigureAwait(false);
        });

        app.MapGet("/api/v1/pos/suppliers/{supplierId:guid}/payable-summary", async (
            HttpRequest request,
            Guid supplierId,
            SupplierPayableQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewPurchasing, out var organizationId, out var problem))
            {
                return problem!;
            }

            var summary = await queries
                .GetSupplierSummaryAsync(organizationId, supplierId, ct)
                .ConfigureAwait(false);
            return Results.Ok(summary);
        });

        app.MapGet("/api/v1/pos/reports/supplier-payables", async (
            HttpRequest request,
            Guid? supplierId,
            string? status,
            bool? outstandingOnly,
            SupplierPayableQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewPurchasing, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!TryParseStatus(status, out _, out problem))
            {
                return problem!;
            }

            var report = await queries
                .ListForReportAsync(
                    organizationId,
                    supplierId,
                    status,
                    outstandingOnly ?? false,
                    ct)
                .ConfigureAwait(false);
            return Results.Ok(report);
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

    private static bool TryParseStatus(string? status, out SupplierPayableStatus? parsed, out IResult? problem)
    {
        parsed = null;
        problem = null;
        if (string.IsNullOrWhiteSpace(status))
        {
            return true;
        }

        if (!Enum.TryParse<SupplierPayableStatus>(status, ignoreCase: true, out var value))
        {
            problem = PosApiResults.Problem(
                DomainErrorCodes.InvalidSupplierPayableStatusTransition,
                $"Unrecognized payable status '{status}'.",
                StatusCodes.Status400BadRequest);
            return false;
        }

        parsed = value;
        return true;
    }
}

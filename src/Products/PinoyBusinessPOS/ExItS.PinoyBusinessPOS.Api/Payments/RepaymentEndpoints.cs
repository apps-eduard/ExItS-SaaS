using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Offline;
using ExItS.PinoyBusinessPOS.Application.Payments;
using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Api.Payments;

/// <summary>
/// Organization-scoped Utang repayment and ledger endpoints.
/// Outstanding = active credits − active repayments. Overpayment is rejected.
/// Inactive customers may repay existing debt. Not SaaS subscription payments.
/// </summary>
internal static class RepaymentEndpoints
{
    public static IEndpointRouteBuilder MapRepaymentEndpoints(this IEndpointRouteBuilder app)
    {
        var customerGroup = app.MapGroup("/api/v1/pos/customers/{customerId:guid}");

        customerGroup.MapGet("/utang-summary", async (
            HttpRequest request,
            Guid customerId,
            POSCustomerQueryService customers,
            IOutstandingBalanceService outstanding,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!PosOrganizationScope.TryGetOrganizationId(request, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!PosCommercialScope.TryAuthorize(access, UtangCapability.ViewCustomersAndHistory, out problem))
            {
                return problem!;
            }

            var customer = await customers.GetByIdAsync(organizationId, customerId, ct).ConfigureAwait(false);
            if (customer is null)
            {
                return PosApiResults.Problem(
                    ApplicationErrorCodes.CustomerNotFound,
                    "Customer was not found.",
                    StatusCodes.Status404NotFound);
            }

            var summary = await outstanding.GetSummaryAsync(organizationId, customerId, ct).ConfigureAwait(false);
            return Results.Ok(summary);
        });

        customerGroup.MapGet("/ledger", async (
            HttpRequest request,
            Guid customerId,
            int? page,
            int? pageSize,
            POSCustomerQueryService customers,
            UtangLedgerQueryService ledger,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!PosOrganizationScope.TryGetOrganizationId(request, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!PosCommercialScope.TryAuthorize(access, UtangCapability.ViewCustomersAndHistory, out problem))
            {
                return problem!;
            }

            var customer = await customers.GetByIdAsync(organizationId, customerId, ct).ConfigureAwait(false);
            if (customer is null)
            {
                return PosApiResults.Problem(
                    ApplicationErrorCodes.CustomerNotFound,
                    "Customer was not found.",
                    StatusCodes.Status404NotFound);
            }

            var result = await ledger.ListAsync(organizationId, customerId, page, pageSize, ct).ConfigureAwait(false);
            return Results.Ok(result);
        });

        customerGroup.MapGet("/repayments", async (
            HttpRequest request,
            Guid customerId,
            int? page,
            int? pageSize,
            POSCustomerQueryService customers,
            RepaymentQueryService repayments,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!PosOrganizationScope.TryGetOrganizationId(request, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!PosCommercialScope.TryAuthorize(access, UtangCapability.ViewCustomersAndHistory, out problem))
            {
                return problem!;
            }

            var customer = await customers.GetByIdAsync(organizationId, customerId, ct).ConfigureAwait(false);
            if (customer is null)
            {
                return PosApiResults.Problem(
                    ApplicationErrorCodes.CustomerNotFound,
                    "Customer was not found.",
                    StatusCodes.Status404NotFound);
            }

            var result = await repayments
                .ListByCustomerAsync(organizationId, customerId, page, pageSize, ct)
                .ConfigureAwait(false);
            return Results.Ok(result);
        });

        customerGroup.MapPost("/repayments", async (
            HttpRequest request,
            Guid customerId,
            CreateRepaymentRequest body,
            CreateRepayment useCase,
            IPosIdempotencyService idempotency,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!PosOrganizationScope.TryGetOrganizationId(request, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!PosCommercialScope.TryAuthorize(access, UtangCapability.RecordRepayment, out problem))
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
                    OfflineOperationTypes.RepaymentCreate,
                    idempotency,
                    ct2 => useCase.ExecuteAsync(
                        organizationId,
                        customerId,
                        body.Amount,
                        body.Remarks,
                        actorId,
                        body.RepaymentId,
                        ct2),
                    RepaymentQueryService.Map,
                    dto => Results.Created(
                        $"/api/v1/pos/repayments/{dto.RepaymentId:D}",
                        dto),
                    ct)
                .ConfigureAwait(false);
        });

        var repaymentGroup = app.MapGroup("/api/v1/pos/repayments");

        repaymentGroup.MapGet("/{repaymentId:guid}", async (
            HttpRequest request,
            Guid repaymentId,
            RepaymentQueryService repayments,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!PosOrganizationScope.TryGetOrganizationId(request, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!PosCommercialScope.TryAuthorize(access, UtangCapability.ViewCustomersAndHistory, out problem))
            {
                return problem!;
            }

            var repayment = await repayments.GetByIdAsync(organizationId, repaymentId, ct).ConfigureAwait(false);
            return repayment is null
                ? PosApiResults.Problem(
                    ApplicationErrorCodes.RepaymentNotFound,
                    "Repayment was not found.",
                    StatusCodes.Status404NotFound)
                : Results.Ok(repayment);
        });

        repaymentGroup.MapPost("/{repaymentId:guid}/reverse", async (
            HttpRequest request,
            Guid repaymentId,
            ReverseRepaymentRequest body,
            ReverseRepayment useCase,
            IPosIdempotencyService idempotency,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!PosOrganizationScope.TryGetOrganizationId(request, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!PosCommercialScope.TryAuthorize(access, UtangCapability.ReverseRepayment, out problem))
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
                    OfflineOperationTypes.RepaymentReverse,
                    idempotency,
                    ct2 => useCase.ExecuteAsync(organizationId, repaymentId, body.Reason, actorId, ct2),
                    RepaymentQueryService.Map,
                    Results.Ok,
                    ct)
                .ConfigureAwait(false);
        });

        return app;
    }
}

public sealed record CreateRepaymentRequest(decimal Amount, string? Remarks, Guid? RepaymentId = null);

public sealed record ReverseRepaymentRequest(string Reason);

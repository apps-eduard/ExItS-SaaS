using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Credit;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Offline;

namespace ExItS.PinoyBusinessPOS.Api.Credit;

/// <summary>
/// Organization-scoped remarks-based credit endpoints. Outstanding is derived from active
/// entries only. Append-only: no edit/delete; corrections use explicit reversal.
/// Not repayments, ledger journals, SaaS payments, sales, or inventory.
/// Development-stage org scope via <c>X-Pos-Organization-Id</c> (404 fail-closed across orgs).
/// </summary>
internal static class CreditEndpoints
{
    public static IEndpointRouteBuilder MapCreditEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/pos/customers/{customerId:guid}");

        group.MapGet("/credit-summary", async (
            HttpRequest request,
            Guid customerId,
            POSCustomerQueryService customers,
            CreditEntryQueryService credit,
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

            var summary = await credit.GetSummaryAsync(organizationId, customerId, ct).ConfigureAwait(false);
            return Results.Ok(summary);
        });

        group.MapGet("/credit-entries", async (
            HttpRequest request,
            Guid customerId,
            int? page,
            int? pageSize,
            POSCustomerQueryService customers,
            CreditEntryQueryService credit,
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

            var result = await credit
                .ListByCustomerAsync(organizationId, customerId, page, pageSize, ct)
                .ConfigureAwait(false);
            return Results.Ok(result);
        });

        group.MapPost("/credit-entries", async (
            HttpRequest request,
            Guid customerId,
            CreateCreditEntryRequest body,
            CreateCreditEntry useCase,
            IPosIdempotencyService idempotency,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!PosOrganizationScope.TryGetOrganizationId(request, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!PosCommercialScope.TryAuthorize(access, UtangCapability.CreateCredit, out problem))
            {
                return problem!;
            }

            return await PosIdempotencyEndpointHelper.ExecuteMutationAsync(
                    request,
                    organizationId,
                    OfflineOperationTypes.CreditCreate,
                    idempotency,
                    ct2 => useCase.ExecuteAsync(
                        organizationId,
                        customerId,
                        body.Amount,
                        body.Remarks,
                        body.CreditEntryId,
                        ct2),
                    CreditEntryQueryService.Map,
                    dto => Results.Created(
                        $"/api/v1/pos/customers/{customerId:D}/credit-entries/{dto.CreditEntryId:D}",
                        dto),
                    ct)
                .ConfigureAwait(false);
        });

        group.MapGet("/credit-entries/{entryId:guid}", async (
            HttpRequest request,
            Guid customerId,
            Guid entryId,
            CreditEntryQueryService credit,
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

            var entry = await credit.GetByIdAsync(organizationId, customerId, entryId, ct).ConfigureAwait(false);
            return entry is null
                ? PosApiResults.Problem(
                    ApplicationErrorCodes.CreditEntryNotFound,
                    "Credit entry was not found.",
                    StatusCodes.Status404NotFound)
                : Results.Ok(entry);
        });

        group.MapPost("/credit-entries/{entryId:guid}/reverse", async (
            HttpRequest request,
            Guid customerId,
            Guid entryId,
            ReverseCreditEntryRequest body,
            ReverseCreditEntry useCase,
            IPosIdempotencyService idempotency,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!PosOrganizationScope.TryGetOrganizationId(request, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!PosCommercialScope.TryAuthorize(access, UtangCapability.ReverseCredit, out problem))
            {
                return problem!;
            }

            return await PosIdempotencyEndpointHelper.ExecuteMutationAsync(
                    request,
                    organizationId,
                    OfflineOperationTypes.CreditReverse,
                    idempotency,
                    ct2 => useCase.ExecuteAsync(organizationId, customerId, entryId, body.Reason, ct2),
                    CreditEntryQueryService.Map,
                    Results.Ok,
                    ct)
                .ConfigureAwait(false);
        });

        return app;
    }
}

public sealed record CreateCreditEntryRequest(decimal Amount, string Remarks, Guid? CreditEntryId = null);

public sealed record ReverseCreditEntryRequest(string Reason);

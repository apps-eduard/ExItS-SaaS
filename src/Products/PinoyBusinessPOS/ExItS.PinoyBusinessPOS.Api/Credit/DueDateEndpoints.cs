using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Credit;
using ExItS.PinoyBusinessPOS.Application.Offline;

namespace ExItS.PinoyBusinessPOS.Api.Credit;

/// <summary>
/// Due-date mutations and overdue read models. Calendar dates only; history is append-only.
/// FIFO aging is derived (not persisted). Past due dates are allowed and overdue immediately when unpaid.
/// Development-stage org/actor via headers (404 fail-closed across orgs).
/// </summary>
internal static class DueDateEndpoints
{
    public static IEndpointRouteBuilder MapDueDateEndpoints(this IEndpointRouteBuilder app)
    {
        var creditGroup = app.MapGroup("/api/v1/pos/credit/{creditEntryId:guid}");

        creditGroup.MapPut("/due-date", async (
            HttpRequest request,
            Guid creditEntryId,
            SetCreditDueDateRequest body,
            SetCreditDueDate useCase,
            IPosIdempotencyService idempotency,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!PosOrganizationScope.TryGetOrganizationId(request, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!PosCommercialScope.TryAuthorize(access, UtangCapability.MutateDueDate, out problem))
            {
                return problem!;
            }

            if (!PosOrganizationScope.TryGetActorId(request, out var actorId, out var actorProblem))
            {
                return actorProblem!;
            }

            var operationType = body.DueDate is null
                ? OfflineOperationTypes.CreditDueDateClear
                : OfflineOperationTypes.CreditDueDateSet;

            return await PosIdempotencyEndpointHelper.ExecuteMutationAsync(
                    request,
                    organizationId,
                    operationType,
                    idempotency,
                    ct2 => useCase.ExecuteAsync(
                        organizationId,
                        creditEntryId,
                        body.DueDate,
                        body.Reason,
                        actorId,
                        body.ExpectedCurrentDueDate,
                        body.CheckExpectedDueDate,
                        ct2),
                    CreditEntryQueryService.Map,
                    Results.Ok,
                    ct)
                .ConfigureAwait(false);
        });

        creditGroup.MapDelete("/due-date", async (
            HttpRequest request,
            Guid creditEntryId,
            SetCreditDueDate useCase,
            IPosIdempotencyService idempotency,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!PosOrganizationScope.TryGetOrganizationId(request, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!PosCommercialScope.TryAuthorize(access, UtangCapability.MutateDueDate, out problem))
            {
                return problem!;
            }

            if (!PosOrganizationScope.TryGetActorId(request, out var actorId, out var actorProblem))
            {
                return actorProblem!;
            }

            var reason = request.Query["reason"].FirstOrDefault() ?? string.Empty;
            DateOnly? expectedCurrentDueDate = null;
            if (DateOnly.TryParse(request.Query["expectedCurrentDueDate"].FirstOrDefault(), out var parsedExpected))
            {
                expectedCurrentDueDate = parsedExpected;
            }

            var checkExpectedDueDate = string.Equals(
                request.Query["checkExpectedDueDate"].FirstOrDefault(),
                "true",
                StringComparison.OrdinalIgnoreCase);

            return await PosIdempotencyEndpointHelper.ExecuteMutationAsync(
                    request,
                    organizationId,
                    OfflineOperationTypes.CreditDueDateClear,
                    idempotency,
                    ct2 => useCase.ExecuteAsync(
                        organizationId,
                        creditEntryId,
                        null,
                        reason,
                        actorId,
                        expectedCurrentDueDate,
                        checkExpectedDueDate,
                        ct2),
                    CreditEntryQueryService.Map,
                    Results.Ok,
                    ct)
                .ConfigureAwait(false);
        });

        creditGroup.MapGet("/due-date-history", async (
            HttpRequest request,
            Guid creditEntryId,
            int? page,
            int? pageSize,
            CreditDueDateHistoryQuery query,
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

            var result = await query.ListAsync(organizationId, creditEntryId, page, pageSize, ct).ConfigureAwait(false);
            return result is null
                ? PosApiResults.Problem(
                    ApplicationErrorCodes.CreditEntryNotFound,
                    "Credit entry was not found.",
                    StatusCodes.Status404NotFound)
                : Results.Ok(result);
        });

        app.MapGet("/api/v1/pos/customers/{customerId:guid}/overdue-summary", async (
            HttpRequest request,
            Guid customerId,
            OverdueQueryService overdue,
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

            var summary = await overdue.GetCustomerSummaryAsync(organizationId, customerId, ct).ConfigureAwait(false);
            return summary is null
                ? PosApiResults.Problem(
                    ApplicationErrorCodes.CustomerNotFound,
                    "Customer was not found.",
                    StatusCodes.Status404NotFound)
                : Results.Ok(summary);
        });

        app.MapGet("/api/v1/pos/customers/{customerId:guid}/aged-credits", async (
            HttpRequest request,
            Guid customerId,
            string? filter,
            int? page,
            int? pageSize,
            OverdueQueryService overdue,
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

            var result = await overdue
                .ListCustomerCreditsAsync(organizationId, customerId, filter, page, pageSize, ct)
                .ConfigureAwait(false);
            return Results.Ok(result);
        });

        app.MapGet("/api/v1/pos/overdue/customers", async (
            HttpRequest request,
            int? page,
            int? pageSize,
            OverdueQueryService overdue,
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

            var result = await overdue
                .ListOrganizationOverdueCustomersAsync(organizationId, page, pageSize, ct)
                .ConfigureAwait(false);
            return Results.Ok(result);
        });

        app.MapGet("/api/v1/pos/overdue/credits", async (
            HttpRequest request,
            int? page,
            int? pageSize,
            OverdueQueryService overdue,
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

            var result = await overdue
                .ListOrganizationOverdueCreditsAsync(organizationId, page, pageSize, ct)
                .ConfigureAwait(false);
            return Results.Ok(result);
        });

        return app;
    }
}

public sealed record SetCreditDueDateRequest(
    DateOnly? DueDate,
    string Reason,
    DateOnly? ExpectedCurrentDueDate = null,
    bool CheckExpectedDueDate = false);

using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Credit;

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
            CancellationToken ct) =>
        {
            if (!PosOrganizationScope.TryGetOrganizationId(request, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!PosOrganizationScope.TryGetActorId(request, out var actorId, out var actorProblem))
            {
                return actorProblem!;
            }

            var result = await useCase
                .ExecuteAsync(organizationId, creditEntryId, body.DueDate, body.Reason, actorId, ct)
                .ConfigureAwait(false);

            return PosApiResults.FromResult(result, e => Results.Ok(CreditEntryQueryService.Map(e)));
        });

        creditGroup.MapDelete("/due-date", async (
            HttpRequest request,
            Guid creditEntryId,
            string? reason,
            SetCreditDueDate useCase,
            CancellationToken ct) =>
        {
            if (!PosOrganizationScope.TryGetOrganizationId(request, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!PosOrganizationScope.TryGetActorId(request, out var actorId, out var actorProblem))
            {
                return actorProblem!;
            }

            var result = await useCase
                .ExecuteAsync(organizationId, creditEntryId, null, reason ?? string.Empty, actorId, ct)
                .ConfigureAwait(false);

            return PosApiResults.FromResult(result, e => Results.Ok(CreditEntryQueryService.Map(e)));
        });

        creditGroup.MapGet("/due-date-history", async (
            HttpRequest request,
            Guid creditEntryId,
            int? page,
            int? pageSize,
            CreditDueDateHistoryQuery query,
            CancellationToken ct) =>
        {
            if (!PosOrganizationScope.TryGetOrganizationId(request, out var organizationId, out var problem))
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
            CancellationToken ct) =>
        {
            if (!PosOrganizationScope.TryGetOrganizationId(request, out var organizationId, out var problem))
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
            CancellationToken ct) =>
        {
            if (!PosOrganizationScope.TryGetOrganizationId(request, out var organizationId, out var problem))
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
            CancellationToken ct) =>
        {
            if (!PosOrganizationScope.TryGetOrganizationId(request, out var organizationId, out var problem))
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
            CancellationToken ct) =>
        {
            if (!PosOrganizationScope.TryGetOrganizationId(request, out var organizationId, out var problem))
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

public sealed record SetCreditDueDateRequest(DateOnly? DueDate, string Reason);

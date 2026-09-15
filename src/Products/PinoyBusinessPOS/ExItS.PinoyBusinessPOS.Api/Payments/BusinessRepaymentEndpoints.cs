using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Application.Payments;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Api.Payments;

internal static class BusinessRepaymentEndpoints
{
    public static IEndpointRouteBuilder MapBusinessRepaymentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/pos/connected-suppliers/business-customers/{connectionId:guid}");

        group.MapGet("/utang-summary", async (
            HttpRequest request,
            Guid connectionId,
            IConnectedSupplierRelationshipRepository relationships,
            BusinessOutstandingBalanceService outstanding,
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

            var relationship = await relationships
                .GetAsync(ConnectedSupplierRelationshipId.From(connectionId), ct)
                .ConfigureAwait(false);
            var seller = PosOrganizationId.From(organizationId);
            if (relationship is null || relationship.SupplierOrganizationId != seller)
            {
                return PosApiResults.Problem(
                    ConnectedSupplierErrorCodes.NotFound,
                    "Business customer relationship was not found.",
                    StatusCodes.Status404NotFound);
            }

            var summary = await outstanding
                .GetSummaryAsync(
                    relationship.SupplierOrganizationId.Value,
                    relationship.BuyerOrganizationId.Value,
                    connectionId,
                    ct)
                .ConfigureAwait(false);
            return Results.Ok(summary);
        });

        group.MapGet("/repayments", async (
            HttpRequest request,
            Guid connectionId,
            int? page,
            int? pageSize,
            BusinessRepaymentQueryService repayments,
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

            var result = await repayments
                .ListByConnectionAsync(organizationId, connectionId, page, pageSize, ct)
                .ConfigureAwait(false);
            return Results.Ok(result);
        });

        group.MapPost("/repayments", async (
            HttpRequest request,
            Guid connectionId,
            CreateRepaymentRequest body,
            CreateBusinessRepayment useCase,
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

            var result = await useCase
                .ExecuteAsync(
                    organizationId,
                    connectionId,
                    new CreateUtangRepaymentCommand(
                        body.Amount,
                        body.Remarks,
                        body.PaymentMethod,
                        body.CheckNumber,
                        body.BankName,
                        body.CheckDate,
                        body.AccountName,
                        body.Reference,
                        body.RepaymentId),
                    actorId,
                    ct)
                .ConfigureAwait(false);

            return PosApiResults.FromResult(
                result,
                r => Results.Created(
                    $"/api/v1/pos/connected-suppliers/business-repayments/{r.Id.Value:D}",
                    BusinessRepaymentMapper.Map(r)));
        });

        var repaymentGroup = app.MapGroup("/api/v1/pos/connected-suppliers/business-repayments");

        repaymentGroup.MapPost("/{repaymentId:guid}/clear-check", async (
            HttpRequest request,
            Guid repaymentId,
            ClearBusinessCheckRepayment useCase,
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

            var result = await useCase.ExecuteAsync(organizationId, repaymentId, actorId, ct).ConfigureAwait(false);
            return PosApiResults.FromResult(result, r => Results.Ok(BusinessRepaymentMapper.Map(r)));
        });

        repaymentGroup.MapPost("/{repaymentId:guid}/bounce-check", async (
            HttpRequest request,
            Guid repaymentId,
            CheckDispositionRequest body,
            BounceBusinessCheckRepayment useCase,
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

            var result = await useCase
                .ExecuteAsync(organizationId, repaymentId, actorId, body.Reason, ct)
                .ConfigureAwait(false);
            return PosApiResults.FromResult(result, r => Results.Ok(BusinessRepaymentMapper.Map(r)));
        });

        repaymentGroup.MapPost("/{repaymentId:guid}/cancel-check", async (
            HttpRequest request,
            Guid repaymentId,
            CheckDispositionRequest body,
            CancelBusinessCheckRepayment useCase,
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

            var result = await useCase
                .ExecuteAsync(organizationId, repaymentId, actorId, body.Reason, ct)
                .ConfigureAwait(false);
            return PosApiResults.FromResult(result, r => Results.Ok(BusinessRepaymentMapper.Map(r)));
        });

        return app;
    }
}

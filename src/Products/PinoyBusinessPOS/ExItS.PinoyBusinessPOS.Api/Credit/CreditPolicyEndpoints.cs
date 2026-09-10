using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Credit;
using ExItS.PinoyBusinessPOS.Application.Offline;

namespace ExItS.PinoyBusinessPOS.Api.Credit;

/// <summary>
/// Customer credit policy configure / approve / disable / history endpoints.
/// Outstanding remains ledger-derived; policy rows authorize NEW Utang only.
/// </summary>
internal static class CreditPolicyEndpoints
{
    public static IEndpointRouteBuilder MapCustomerCreditPolicyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/pos/customers/{customerId:guid}/credit-policy");

        group.MapGet("/", async (
            HttpRequest request,
            Guid customerId,
            GetCustomerCreditPolicy useCase,
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

            var result = await useCase
                .ExecuteAsync(organizationId, customerId, ct)
                .ConfigureAwait(false);
            return PosApiResults.FromResult(result, Results.Ok);
        });

        group.MapPut("/", async (
            HttpRequest request,
            Guid customerId,
            UpsertCustomerCreditPolicyRequest body,
            UpsertCustomerCreditPolicy useCase,
            IPosIdempotencyService idempotency,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!PosOrganizationScope.TryGetOrganizationId(request, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!PosCommercialScope.TryAuthorize(access, UtangCapability.ManageCustomerCreditPolicy, out problem))
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
                    OfflineOperationTypes.CustomerCreditPolicyUpsert,
                    idempotency,
                    ct2 => useCase.ExecuteAsync(
                        organizationId,
                        customerId,
                        body.CreditLimit,
                        body.DefaultTermDays,
                        actorId,
                        body.Reason,
                        body.ExpectedUpdatedAtUtc,
                        ct2),
                    dto => dto,
                    Results.Ok,
                    ct)
                .ConfigureAwait(false);
        });

        group.MapPost("/approve", async (
            HttpRequest request,
            Guid customerId,
            ApproveCustomerCreditPolicyRequest body,
            ApproveCustomerCreditPolicy useCase,
            IPosIdempotencyService idempotency,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!PosOrganizationScope.TryGetOrganizationId(request, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!PosCommercialScope.TryAuthorize(access, UtangCapability.ApproveCustomerCreditPolicy, out problem))
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
                    OfflineOperationTypes.CustomerCreditPolicyApprove,
                    idempotency,
                    ct2 => useCase.ExecuteAsync(
                        organizationId,
                        customerId,
                        actorId,
                        body.Reason,
                        body.ExpectedUpdatedAtUtc,
                        ct2),
                    dto => dto,
                    Results.Ok,
                    ct)
                .ConfigureAwait(false);
        });

        group.MapPost("/disable", async (
            HttpRequest request,
            Guid customerId,
            DisableCustomerCreditPolicyRequest body,
            DisableCustomerCreditPolicy useCase,
            IPosIdempotencyService idempotency,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!PosOrganizationScope.TryGetOrganizationId(request, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!PosCommercialScope.TryAuthorize(access, UtangCapability.ManageCustomerCreditPolicy, out problem))
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
                    OfflineOperationTypes.CustomerCreditPolicyDisable,
                    idempotency,
                    ct2 => useCase.ExecuteAsync(
                        organizationId,
                        customerId,
                        actorId,
                        body.Reason,
                        body.ExpectedUpdatedAtUtc,
                        ct2),
                    dto => dto,
                    Results.Ok,
                    ct)
                .ConfigureAwait(false);
        });

        group.MapGet("/history", async (
            HttpRequest request,
            Guid customerId,
            int? page,
            int? pageSize,
            ListCustomerCreditPolicyHistory useCase,
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

            var result = await useCase
                .ExecuteAsync(organizationId, customerId, page, pageSize, ct)
                .ConfigureAwait(false);
            return PosApiResults.FromResult(result, Results.Ok);
        });

        return app;
    }
}

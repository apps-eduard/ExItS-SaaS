using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Credit;
using ExItS.PinoyBusinessPOS.Application.Offline;

namespace ExItS.PinoyBusinessPOS.Api.Credit;

/// <summary>
/// B2B business-customer credit policy endpoints under connected-suppliers.
/// Does not create POSCustomer stubs. Outstanding is always 0 until a B2B ledger exists.
/// </summary>
internal static class BusinessCustomerCreditPolicyEndpoints
{
    public static IEndpointRouteBuilder MapBusinessCustomerCreditPolicyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(
            "/api/v1/pos/connected-suppliers/business-customers/{connectionId:guid}/credit-policy");

        // Register each verb once. Mapping both "" and "/" collapses to the same template and
        // throws AmbiguousMatchException (HTTP 500) on every GET/PUT to this leaf.
        group.MapGet("/", GetAsync);
        group.MapPut("/", UpsertAsync);

        group.MapPost("/approve", async (
            HttpRequest request,
            Guid connectionId,
            ApproveBusinessCustomerCreditPolicyRequest body,
            ApproveBusinessCustomerCreditPolicy useCase,
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
                    OfflineOperationTypes.BusinessCustomerCreditPolicyApprove,
                    idempotency,
                    ct2 => useCase.ExecuteAsync(
                        organizationId,
                        connectionId,
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
            Guid connectionId,
            DisableBusinessCustomerCreditPolicyRequest body,
            DisableBusinessCustomerCreditPolicy useCase,
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
                    OfflineOperationTypes.BusinessCustomerCreditPolicyDisable,
                    idempotency,
                    ct2 => useCase.ExecuteAsync(
                        organizationId,
                        connectionId,
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
            Guid connectionId,
            int? page,
            int? pageSize,
            ListBusinessCustomerCreditPolicyHistory useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!PosOrganizationScope.TryGetOrganizationId(request, out var organizationId, out var problem))
            {
                return problem!;
            }

            // Align with GetBusinessCustomer (ViewSuppliers).
            if (!PosCommercialScope.TryAuthorize(access, UtangCapability.ViewSuppliers, out problem))
            {
                return problem!;
            }

            var result = await useCase
                .ExecuteAsync(organizationId, connectionId, page, pageSize, ct)
                .ConfigureAwait(false);
            return PosApiResults.FromResult(result, Results.Ok);
        });

        return app;
    }

    private static async Task<IResult> GetAsync(
        HttpRequest request,
        Guid connectionId,
        GetBusinessCustomerCreditPolicy useCase,
        IPosCommercialAccessAccessor access,
        CancellationToken ct)
    {
        if (!PosOrganizationScope.TryGetOrganizationId(request, out var organizationId, out var problem))
        {
            return problem!;
        }

        // Align with GetBusinessCustomer (ViewSuppliers).
        if (!PosCommercialScope.TryAuthorize(access, UtangCapability.ViewSuppliers, out problem))
        {
            return problem!;
        }

        var result = await useCase
            .ExecuteAsync(organizationId, connectionId, ct)
            .ConfigureAwait(false);
        return PosApiResults.FromResult(result, Results.Ok);
    }

    private static async Task<IResult> UpsertAsync(
        HttpRequest request,
        Guid connectionId,
        UpsertBusinessCustomerCreditPolicyRequest body,
        UpsertBusinessCustomerCreditPolicy useCase,
        IPosIdempotencyService idempotency,
        IPosCommercialAccessAccessor access,
        CancellationToken ct)
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
                OfflineOperationTypes.BusinessCustomerCreditPolicyUpsert,
                idempotency,
                ct2 => useCase.ExecuteAsync(
                    organizationId,
                    connectionId,
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
    }
}

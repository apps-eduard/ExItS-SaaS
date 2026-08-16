using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Application.Offline;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;

namespace ExItS.PinoyBusinessPOS.Api.CustomerOrdering;

internal static class CustomerOrderEndpoints
{
    public static IEndpointRouteBuilder MapCustomerOrderEndpoints(this IEndpointRouteBuilder app)
    {
        MapSellerOrders(app.MapGroup("/api/v1/pos/organizations/{organizationId:guid}/customer-orders"));
        MapCustomerFacing(app.MapGroup("/api/v1/pos/customer-orders"));
        return app;
    }

    private static void MapSellerOrders(RouteGroupBuilder group)
    {
        group.MapGet("/", async (
            HttpRequest request,
            Guid organizationId,
            string? status,
            string? fulfillmentType,
            Guid? branchId,
            string? orderNumber,
            int? page,
            int? pageSize,
            CustomerOrderQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorizeSeller(request, access, organizationId, UtangCapability.ViewCustomerOrders, out var problem))
            {
                return problem!;
            }

            if (!TryParseStatus(status, out var parsedStatus, out problem)
                || !TryParseFulfillmentType(fulfillmentType, out var parsedType, out problem))
            {
                return problem!;
            }

            var filter = new CustomerOrderFilter(parsedStatus, parsedType, branchId, orderNumber);
            var result = await queries.ListAsync(organizationId, filter, page, pageSize, ct).ConfigureAwait(false);
            return Results.Ok(result);
        });

        group.MapPost("/", async (
            HttpRequest request,
            Guid organizationId,
            PlaceCustomerOrderRequest body,
            PlaceCustomerOrder useCase,
            IPosIdempotencyService idempotency,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorizeSeller(request, access, organizationId, UtangCapability.PlaceCustomerOrders, out var problem)
                || !PosOrganizationScope.TryGetActorId(request, out var actorId, out problem))
            {
                return problem!;
            }

            return await PosIdempotencyEndpointHelper.ExecuteMutationAsync(
                    request,
                    organizationId,
                    OfflineOperationTypes.CustomerOrderPlace,
                    idempotency,
                    ct2 => useCase.ExecuteAsync(organizationId, body, actorId, ct2),
                    dto => dto,
                    dto => Results.Created(
                        $"/api/v1/pos/organizations/{organizationId:D}/customer-orders/{dto.OrderId:D}",
                        dto),
                    ct)
                .ConfigureAwait(false);
        });

        group.MapPost("/quote-delivery", async (
            HttpRequest request,
            Guid organizationId,
            QuoteCustomerOrderDeliveryRequest body,
            QuoteCustomerOrderDelivery useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorizeSellerAny(
                    request,
                    access,
                    organizationId,
                    [UtangCapability.PlaceCustomerOrders, UtangCapability.ViewCustomerOrders],
                    out var problem))
            {
                return problem!;
            }

            return PosApiResults.FromResult(
                await useCase.ExecuteAsync(organizationId, body, ct).ConfigureAwait(false),
                Results.Ok);
        });

        group.MapGet("/{orderId:guid}", async (
            HttpRequest request,
            Guid organizationId,
            Guid orderId,
            CustomerOrderQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorizeSeller(request, access, organizationId, UtangCapability.ViewCustomerOrders, out var problem))
            {
                return problem!;
            }

            var order = await queries.GetByIdAsync(organizationId, orderId, ct).ConfigureAwait(false);
            return order is null
                ? PosApiResults.Problem(
                    ApplicationErrorCodes.CustomerOrderNotFound,
                    "Customer order was not found.",
                    StatusCodes.Status404NotFound)
                : Results.Ok(order);
        });

        group.MapPost("/{orderId:guid}/accept", async (
            HttpRequest request,
            Guid organizationId,
            Guid orderId,
            AcceptCustomerOrder useCase,
            IPosIdempotencyService idempotency,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorizeSeller(request, access, organizationId, UtangCapability.ManageCustomerOrders, out var problem)
                || !PosOrganizationScope.TryGetActorId(request, out var actorId, out problem))
            {
                return problem!;
            }

            return await PosIdempotencyEndpointHelper.ExecuteMutationAsync(
                    request,
                    organizationId,
                    OfflineOperationTypes.CustomerOrderAccept,
                    idempotency,
                    ct2 => useCase.ExecuteAsync(organizationId, orderId, actorId, ct2),
                    dto => dto,
                    Results.Ok,
                    ct)
                .ConfigureAwait(false);
        });

        group.MapPost("/{orderId:guid}/reject", async (
            HttpRequest request,
            Guid organizationId,
            Guid orderId,
            RejectCustomerOrderRequest body,
            RejectCustomerOrder useCase,
            IPosIdempotencyService idempotency,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorizeSeller(request, access, organizationId, UtangCapability.ManageCustomerOrders, out var problem)
                || !PosOrganizationScope.TryGetActorId(request, out var actorId, out problem))
            {
                return problem!;
            }

            return await PosIdempotencyEndpointHelper.ExecuteMutationAsync(
                    request,
                    organizationId,
                    OfflineOperationTypes.CustomerOrderReject,
                    idempotency,
                    ct2 => useCase.ExecuteAsync(organizationId, orderId, body, actorId, ct2),
                    dto => dto,
                    Results.Ok,
                    ct)
                .ConfigureAwait(false);
        });

        group.MapPost("/{orderId:guid}/cancel", async (
            HttpRequest request,
            Guid organizationId,
            Guid orderId,
            CancelCustomerOrder useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorizeSeller(request, access, organizationId, UtangCapability.ManageCustomerOrders, out var problem)
                || !PosOrganizationScope.TryGetActorId(request, out var actorId, out problem))
            {
                return problem!;
            }

            return PosApiResults.FromResult(
                await useCase.ExecuteAsync(organizationId, orderId, actorId, ct).ConfigureAwait(false),
                Results.Ok);
        });

        MapFulfillment(group, "start-preparing", (u, org, id, ct) => u.StartPreparingAsync(org, id, ct));
        MapFulfillment(group, "mark-ready", (u, org, id, ct) => u.MarkReadyAsync(org, id, ct));
        MapFulfillment(group, "mark-out-for-delivery", (u, org, id, ct) => u.MarkOutForDeliveryAsync(org, id, ct));
        MapFulfillment(group, "mark-delivered", (u, org, id, ct) => u.MarkDeliveredAsync(org, id, ct));
        MapFulfillment(group, "mark-collected", (u, org, id, ct) => u.MarkCollectedAsync(org, id, ct));

        group.MapPost("/{orderId:guid}/complete", async (
            HttpRequest request,
            Guid organizationId,
            Guid orderId,
            CompleteCustomerOrder useCase,
            IPosIdempotencyService idempotency,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorizeSeller(request, access, organizationId, UtangCapability.ManageCustomerOrders, out var problem)
                || !PosOrganizationScope.TryGetActorId(request, out var actorId, out problem))
            {
                return problem!;
            }

            return await PosIdempotencyEndpointHelper.ExecuteMutationAsync(
                    request,
                    organizationId,
                    OfflineOperationTypes.CustomerOrderComplete,
                    idempotency,
                    ct2 => useCase.ExecuteAsync(organizationId, orderId, actorId, ct2),
                    dto => dto,
                    Results.Ok,
                    ct)
                .ConfigureAwait(false);
        });
    }

    private static void MapFulfillment(
        RouteGroupBuilder group,
        string action,
        Func<AdvanceCustomerOrderFulfillment, Guid, Guid, CancellationToken, Task<ApplicationResult<CustomerOrderDto>>> execute)
    {
        group.MapPost($"/{{orderId:guid}}/{action}", async (
            HttpRequest request,
            Guid organizationId,
            Guid orderId,
            AdvanceCustomerOrderFulfillment useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorizeSeller(request, access, organizationId, UtangCapability.ManageCustomerOrders, out var problem))
            {
                return problem!;
            }

            return PosApiResults.FromResult(
                await execute(useCase, organizationId, orderId, ct).ConfigureAwait(false),
                Results.Ok);
        });
    }

    private static void MapCustomerFacing(RouteGroupBuilder group)
    {
        group.MapGet("/mine", async (
            HttpRequest request,
            string? partyType,
            Guid? buyerOrganizationId,
            int? page,
            int? pageSize,
            CustomerOrderQueryService queries,
            CancellationToken ct) =>
        {
            if (!PosOrganizationScope.TryGetActorId(request, out var actorId, out var problem))
            {
                return problem!;
            }

            var parsedParty = CustomerPartyType.Personal;
            if (!string.IsNullOrWhiteSpace(partyType)
                && !Enum.TryParse(partyType, true, out parsedParty))
            {
                return PosApiResults.Problem(
                    DomainErrorCodes.InvalidCustomerOrderParty,
                    "Customer party type is invalid.",
                    StatusCodes.Status400BadRequest);
            }

            var result = await queries
                .ListMineAsync(
                    parsedParty,
                    parsedParty == CustomerPartyType.Personal ? actorId : null,
                    parsedParty == CustomerPartyType.Organization ? buyerOrganizationId : null,
                    page,
                    pageSize,
                    ct)
                .ConfigureAwait(false);
            return Results.Ok(result);
        });

        group.MapGet("/mine/{orderId:guid}", async (
            HttpRequest request,
            Guid orderId,
            string? partyType,
            Guid? buyerOrganizationId,
            CustomerOrderQueryService queries,
            CancellationToken ct) =>
        {
            if (!PosOrganizationScope.TryGetActorId(request, out var actorId, out var problem))
            {
                return problem!;
            }

            var parsedParty = CustomerPartyType.Personal;
            if (!string.IsNullOrWhiteSpace(partyType)
                && !Enum.TryParse(partyType, true, out parsedParty))
            {
                return PosApiResults.Problem(
                    DomainErrorCodes.InvalidCustomerOrderParty,
                    "Customer party type is invalid.",
                    StatusCodes.Status400BadRequest);
            }

            var order = await queries
                .GetMineByIdAsync(
                    orderId,
                    parsedParty,
                    parsedParty == CustomerPartyType.Personal ? actorId : null,
                    parsedParty == CustomerPartyType.Organization ? buyerOrganizationId : null,
                    ct)
                .ConfigureAwait(false);
            return order is null ? Results.NotFound() : Results.Ok(order);
        });

        group.MapPost("/organizations/{sellerOrganizationId:guid}", async (
            HttpRequest request,
            Guid sellerOrganizationId,
            PlaceCustomerOrderRequest body,
            PlaceCustomerOrder useCase,
            IPosIdempotencyService idempotency,
            CancellationToken ct) =>
        {
            if (!PosOrganizationScope.TryGetActorId(request, out var actorId, out var problem))
            {
                return problem!;
            }

            if (!PartyMatchesCaller(body, actorId, out problem))
            {
                return problem!;
            }

            return await PosIdempotencyEndpointHelper.ExecuteMutationAsync(
                    request,
                    sellerOrganizationId,
                    OfflineOperationTypes.CustomerOrderPlace,
                    idempotency,
                    ct2 => useCase.ExecuteAsync(sellerOrganizationId, body, actorId, ct2),
                    dto => dto,
                    dto => Results.Created(
                        $"/api/v1/pos/organizations/{sellerOrganizationId:D}/customer-orders/{dto.OrderId:D}",
                        dto),
                    ct)
                .ConfigureAwait(false);
        });

        group.MapPost("/organizations/{sellerOrganizationId:guid}/quote-delivery", async (
            Guid sellerOrganizationId,
            QuoteCustomerOrderDeliveryRequest body,
            QuoteCustomerOrderDelivery useCase,
            CancellationToken ct) =>
            PosApiResults.FromResult(
                await useCase.ExecuteAsync(sellerOrganizationId, body, ct).ConfigureAwait(false),
                Results.Ok));

        group.MapPost("/organizations/{sellerOrganizationId:guid}/{orderId:guid}/cancel", async (
            HttpRequest request,
            Guid sellerOrganizationId,
            Guid orderId,
            CancelCustomerOrder useCase,
            CancellationToken ct) =>
        {
            if (!PosOrganizationScope.TryGetActorId(request, out var actorId, out var problem))
            {
                return problem!;
            }

            return PosApiResults.FromResult(
                await useCase.ExecuteAsync(sellerOrganizationId, orderId, actorId, ct).ConfigureAwait(false),
                Results.Ok);
        });
    }

    private static bool PartyMatchesCaller(PlaceCustomerOrderRequest body, Guid actorId, out IResult? problem)
    {
        problem = null;
        if (!Enum.TryParse<CustomerPartyType>(body.CustomerPartyType, true, out var partyType))
        {
            problem = PosApiResults.Problem(
                DomainErrorCodes.InvalidCustomerOrderParty,
                "Customer party type is invalid.",
                StatusCodes.Status400BadRequest);
            return false;
        }

        if (partyType == CustomerPartyType.Personal
            && body.CustomerPlatformUserId is Guid userId
            && userId == actorId)
        {
            return true;
        }

        // Organization buyer placement: actor must be a member of the buyer org (validated later by Platform).
        // V1: require buyer organization id present; full membership check is residual for storefront WP.
        if (partyType == CustomerPartyType.Organization
            && body.CustomerBuyerOrganizationId is Guid buyerOrg
            && buyerOrg != Guid.Empty)
        {
            return true;
        }

        problem = PosApiResults.Problem(
            ApplicationErrorCodes.CustomerOrderPartyMismatch,
            "Customer party must match the authenticated caller.",
            StatusCodes.Status403Forbidden);
        return false;
    }

    private static bool TryAuthorizeSeller(
        HttpRequest request,
        IPosCommercialAccessAccessor access,
        Guid pathOrganizationId,
        UtangCapability capability,
        out IResult? problem) =>
        TryAuthorizeSellerAny(request, access, pathOrganizationId, [capability], out problem);

    private static bool TryAuthorizeSellerAny(
        HttpRequest request,
        IPosCommercialAccessAccessor access,
        Guid pathOrganizationId,
        IReadOnlyList<UtangCapability> capabilities,
        out IResult? problem)
    {
        if (!PosOrganizationScope.TryGetOrganizationId(request, out var headerOrgId, out problem))
        {
            return false;
        }

        if (headerOrgId != pathOrganizationId)
        {
            problem = PosApiResults.Problem(
                ApplicationErrorCodes.OrganizationRequired,
                "Organization scope header must match the seller organization path.",
                StatusCodes.Status400BadRequest);
            return false;
        }

        IResult? lastProblem = null;
        foreach (var capability in capabilities)
        {
            if (PosCommercialScope.TryAuthorize(access, capability, out var authProblem))
            {
                problem = null;
                return true;
            }

            lastProblem = authProblem;
        }

        problem = lastProblem;
        return false;
    }

    private static bool TryParseStatus(string? status, out CustomerOrderStatus? parsed, out IResult? problem)
    {
        parsed = null;
        problem = null;
        if (string.IsNullOrWhiteSpace(status))
        {
            return true;
        }

        if (!Enum.TryParse<CustomerOrderStatus>(status, true, out var value))
        {
            problem = PosApiResults.Problem(
                DomainErrorCodes.InvalidCustomerOrderStatusTransition,
                $"Unrecognized customer order status '{status}'.",
                StatusCodes.Status400BadRequest);
            return false;
        }

        parsed = value;
        return true;
    }

    private static bool TryParseFulfillmentType(
        string? fulfillmentType,
        out CustomerOrderFulfillmentType? parsed,
        out IResult? problem)
    {
        parsed = null;
        problem = null;
        if (string.IsNullOrWhiteSpace(fulfillmentType))
        {
            return true;
        }

        if (!Enum.TryParse<CustomerOrderFulfillmentType>(fulfillmentType, true, out var value))
        {
            problem = PosApiResults.Problem(
                DomainErrorCodes.InvalidCustomerOrderFulfillmentType,
                $"Unrecognized fulfillment type '{fulfillmentType}'.",
                StatusCodes.Status400BadRequest);
            return false;
        }

        parsed = value;
        return true;
    }
}

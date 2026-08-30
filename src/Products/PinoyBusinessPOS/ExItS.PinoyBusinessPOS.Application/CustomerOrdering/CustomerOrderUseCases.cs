using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Application.CustomerOrdering;

public sealed class CustomerOrderQueryService
{
    private readonly ICustomerOrderRepository _orders;

    public CustomerOrderQueryService(ICustomerOrderRepository orders) => _orders = orders;

    public async Task<CustomerOrderDto?> GetByIdAsync(
        Guid sellerOrganizationId,
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await _orders
            .GetByIdAsync(
                PosOrganizationId.From(sellerOrganizationId),
                CustomerOrderId.From(orderId),
                cancellationToken)
            .ConfigureAwait(false);
        return order is null ? null : CustomerOrderMaps.Map(order);
    }

    public async Task<CustomerOrderPagedResult> ListAsync(
        Guid sellerOrganizationId,
        CustomerOrderFilter filter,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = PosPagination.Normalize(page, pageSize);
        var (items, total) = await _orders
            .ListAsync(PosOrganizationId.From(sellerOrganizationId), filter, skip, take, cancellationToken)
            .ConfigureAwait(false);
        return new CustomerOrderPagedResult(
            items.Select(CustomerOrderMaps.MapListItem).ToList(),
            total,
            page ?? 1,
            take);
    }

    public async Task<CustomerOrderPagedResult> ListMineAsync(
        CustomerPartyType partyType,
        Guid? platformUserId,
        Guid? buyerOrganizationId,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = PosPagination.Normalize(page, pageSize);
        var (items, total) = await _orders
            .ListForCustomerPartyAsync(partyType, platformUserId, buyerOrganizationId, skip, take, cancellationToken)
            .ConfigureAwait(false);
        return new CustomerOrderPagedResult(
            items.Select(CustomerOrderMaps.MapListItem).ToList(),
            total,
            page ?? 1,
            take);
    }

    public async Task<CustomerOrderDto?> GetMineByIdAsync(
        Guid orderId,
        CustomerPartyType partyType,
        Guid? platformUserId,
        Guid? buyerOrganizationId,
        CancellationToken cancellationToken = default)
    {
        var order = await _orders
            .GetForCustomerPartyAsync(
                CustomerOrderId.From(orderId),
                partyType,
                platformUserId,
                buyerOrganizationId,
                cancellationToken)
            .ConfigureAwait(false);
        return order is null ? null : CustomerOrderMaps.Map(order);
    }
}

public sealed class PlaceCustomerOrder
{
    private readonly ICustomerOrderRepository _orders;
    private readonly ICatalogProductRepository _products;
    private readonly ICustomerOrderBranchDirectory _branches;
    private readonly ICustomerOrderStockService _stock;
    private readonly IOrganizationBusinessNotificationPublisher _notifications;
    private readonly ISellerCustomerOrderingCapability _sellerCapability;
    private readonly ILinkedCustomerPlatformAuthorization? _linkedCustomerAuth;
    private readonly IPOSCustomerRepository? _customers;
    private readonly IClock _clock;

    public PlaceCustomerOrder(
        ICustomerOrderRepository orders,
        ICatalogProductRepository products,
        ICustomerOrderBranchDirectory branches,
        ICustomerOrderStockService stock,
        IClock clock,
        ISellerCustomerOrderingCapability? sellerCapability = null,
        IOrganizationBusinessNotificationPublisher? notifications = null,
        ILinkedCustomerPlatformAuthorization? linkedCustomerAuth = null,
        IPOSCustomerRepository? customers = null)
    {
        _orders = orders;
        _products = products;
        _branches = branches;
        _stock = stock;
        _clock = clock;
        _sellerCapability = sellerCapability ?? new AllowAllSellerCustomerOrderingCapability();
        _notifications = notifications ?? new NoOpOrganizationBusinessNotificationPublisher();
        _linkedCustomerAuth = linkedCustomerAuth;
        _customers = customers;
    }

    public async Task<ApplicationResult<CustomerOrderDto>> ExecuteAsync(
        Guid sellerOrganizationId,
        PlaceCustomerOrderRequest request,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var orgId = PosOrganizationId.From(sellerOrganizationId);
            if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
            {
                var existing = await _orders
                    .FindByIdempotencyKeyAsync(orgId, request.IdempotencyKey, cancellationToken)
                    .ConfigureAwait(false);
                if (existing is not null)
                {
                    return ApplicationResult<CustomerOrderDto>.Success(CustomerOrderMaps.Map(existing));
                }
            }

            var sellerCapability = await _sellerCapability
                .ResolveAsync(sellerOrganizationId, cancellationToken)
                .ConfigureAwait(false);
            if (!sellerCapability.CanCustomerOrder)
            {
                return ApplicationResult<CustomerOrderDto>.Failure(
                    ApplicationErrorCodes.CustomerOrderOrderingUnavailable,
                    "This merchant is not accepting customer orders.");
            }

            if (!Enum.TryParse<CustomerOrderFulfillmentType>(request.FulfillmentType, true, out var fulfillmentType))
            {
                return ApplicationResult<CustomerOrderDto>.Failure(
                    DomainErrorCodes.InvalidCustomerOrderFulfillmentType,
                    "Fulfillment type is invalid.");
            }

            if (fulfillmentType == CustomerOrderFulfillmentType.Delivery
                && !sellerCapability.CanCustomerDelivery)
            {
                return ApplicationResult<CustomerOrderDto>.Failure(
                    ApplicationErrorCodes.CustomerOrderOrderingUnavailable,
                    "This merchant is not accepting delivery orders.");
            }

            if (!Enum.TryParse<CustomerPartyType>(request.CustomerPartyType, true, out var partyType))
            {
                return ApplicationResult<CustomerOrderDto>.Failure(
                    DomainErrorCodes.InvalidCustomerOrderParty,
                    "Customer party type is invalid.");
            }

            var party = partyType == CustomerPartyType.Personal
                ? CustomerOrderParty.Personal(
                    request.CustomerPlatformUserId ?? Guid.Empty,
                    request.CustomerDisplayName)
                : CustomerOrderParty.Organization(
                    request.CustomerBuyerOrganizationId ?? Guid.Empty,
                    request.CustomerBuyerPublicOrganizationId ?? string.Empty,
                    request.CustomerDisplayName);

            Guid? platformBusinessCustomerId = null;
            if (partyType == CustomerPartyType.Personal)
            {
                var linked = await ValidatePersonalLinkedCustomerAsync(
                        orgId,
                        sellerOrganizationId,
                        request,
                        cancellationToken)
                    .ConfigureAwait(false);
                if (!linked.IsSuccess)
                {
                    return ApplicationResult<CustomerOrderDto>.Failure(linked.ErrorCode!, linked.ErrorMessage!);
                }

                platformBusinessCustomerId = linked.Value;
            }

            var branch = await _branches
                .GetBranchAsync(sellerOrganizationId, request.FulfillmentBranchId, cancellationToken)
                .ConfigureAwait(false);
            if (branch is null)
            {
                return ApplicationResult<CustomerOrderDto>.Failure(
                    ApplicationErrorCodes.CustomerOrderBranchNotFound,
                    "Fulfillment branch was not found for this organization.");
            }

            if (fulfillmentType == CustomerOrderFulfillmentType.Pickup && !branch.PickupOperational)
            {
                return ApplicationResult<CustomerOrderDto>.Failure(
                    ApplicationErrorCodes.CustomerOrderBranchCapability,
                    branch.OnlineOrdersPaused
                        ? "This store is temporarily not accepting online orders."
                        : branch.CustomerOrderingEnabled
                            ? "Pickup is not available at this time."
                            : "Pickup is not enabled for this branch.");
            }

            if (fulfillmentType == CustomerOrderFulfillmentType.Delivery && !branch.DeliveryOperational)
            {
                return ApplicationResult<CustomerOrderDto>.Failure(
                    ApplicationErrorCodes.CustomerOrderBranchCapability,
                    branch.OnlineOrdersPaused
                        ? "This store is temporarily not accepting online orders."
                        : branch.DeliveryEnabled
                            ? "Delivery is not available at this time."
                            : "Delivery is not enabled for this branch.");
            }

            if (!branch.CustomerOrderingOperational)
            {
                return ApplicationResult<CustomerOrderDto>.Failure(
                    ApplicationErrorCodes.CustomerOrderOrderingUnavailable,
                    branch.OnlineOrdersPaused
                        ? "This store is temporarily not accepting online orders."
                        : "This store is not accepting online orders right now.");
            }

            if (request.Lines is null || request.Lines.Count == 0)
            {
                return ApplicationResult<CustomerOrderDto>.Failure(
                    DomainErrorCodes.CustomerOrderRequiresAtLeastOneLine,
                    "A customer order must contain at least one line.");
            }

            var productIds = request.Lines.Select(l => CatalogProductId.From(l.ProductId)).Distinct().ToList();
            var products = await _products.ListByIdsAsync(orgId, productIds, cancellationToken).ConfigureAwait(false);
            var byId = products.ToDictionary(p => p.Id.Value);

            var drafts = new List<CustomerOrderLineDraft>(request.Lines.Count);
            foreach (var line in request.Lines)
            {
                if (!byId.TryGetValue(line.ProductId, out var product))
                {
                    return ApplicationResult<CustomerOrderDto>.Failure(
                        ApplicationErrorCodes.SaleProductNotFound,
                        "One or more products were not found in this organization.");
                }

                if (product.Status != CatalogProductStatus.Active || !product.CanBeSold || product.SellingPrice <= 0m)
                {
                    return ApplicationResult<CustomerOrderDto>.Failure(
                        ApplicationErrorCodes.SaleProductNotActive,
                        "One or more products are not available for sale.");
                }

                // Server-side catalog price — never trust client unit price.
                drafts.Add(new CustomerOrderLineDraft(
                    product.Id,
                    product.Name,
                    product.Sku,
                    product.UnitOfMeasure,
                    line.Quantity,
                    product.SellingPrice,
                    line.Discount));
            }

            var availability = await _stock
                .EnsureAvailableAsync(orgId, drafts, request.FulfillmentBranchId, cancellationToken)
                .ConfigureAwait(false);
            if (!availability.IsSuccess)
            {
                return ApplicationResult<CustomerOrderDto>.Failure(availability);
            }

            CustomerOrderDeliverySnapshot? delivery = null;
            if (fulfillmentType == CustomerOrderFulfillmentType.Delivery)
            {
                if (request.Delivery is null)
                {
                    return ApplicationResult<CustomerOrderDto>.Failure(
                        DomainErrorCodes.InvalidCustomerOrderDelivery,
                        "Delivery details are required for delivery orders.");
                }

                var merchandiseSubtotal = SaleMoney.RoundMoney(
                    drafts.Sum(d => SaleMoney.RoundMoney(d.UnitPrice * d.Quantity) - d.Discount));
                var quote = await BuildDeliverySnapshotAsync(
                        sellerOrganizationId,
                        branch,
                        request.Delivery,
                        merchandiseSubtotal,
                        cancellationToken)
                    .ConfigureAwait(false);
                if (!quote.IsSuccess)
                {
                    return ApplicationResult<CustomerOrderDto>.Failure(quote.ErrorCode!, quote.ErrorMessage!);
                }

                delivery = quote.Value;
            }

            var now = _clock.UtcNow;
            var orderId = request.ClientOrderId is Guid id && id != Guid.Empty
                ? CustomerOrderId.From(id)
                : null;

            var created = await _orders
                .PlaceAsync(
                    orgId,
                    number => CustomerOrder.CreateSubmitted(
                        orgId,
                        number,
                        party,
                        fulfillmentType,
                        branch.BranchId,
                        branch.Name,
                        drafts,
                        actorId,
                        now,
                        delivery,
                        request.IdempotencyKey,
                        orderId,
                        CustomerOrderPaymentMethods.Parse(request.PaymentMethod),
                        platformBusinessCustomerId),
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            await _notifications
                .PublishAsync(
                    sellerOrganizationId,
                    sellerOrganizationId,
                    CustomerOrderNotificationTypes.Submitted,
                    created.Id.Value.ToString("D"),
                    "New customer order",
                    $"{created.OrderNumber} · {created.CustomerParty.DisplayNameSnapshot} · {created.Total:0.00}",
                    cancellationToken)
                .ConfigureAwait(false);

            if (party.PartyType == CustomerPartyType.Organization
                && party.BuyerOrganizationId is Guid buyerOrg
                && buyerOrg != Guid.Empty
                && buyerOrg != sellerOrganizationId)
            {
                await _notifications
                    .PublishAsync(
                        sellerOrganizationId,
                        buyerOrg,
                        CustomerOrderNotificationTypes.Submitted,
                        created.Id.Value.ToString("D"),
                        "Order placed",
                        $"{created.OrderNumber} was submitted.",
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            return ApplicationResult<CustomerOrderDto>.Success(CustomerOrderMaps.Map(created));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<CustomerOrderDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<CustomerOrderDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }

    private async Task<ApplicationResult<CustomerOrderDeliverySnapshot>> BuildDeliverySnapshotAsync(
        Guid sellerOrganizationId,
        CustomerOrderBranchSnapshot branch,
        PlaceCustomerOrderDeliveryRequest delivery,
        decimal merchandiseSubtotal,
        CancellationToken cancellationToken)
    {
        if (branch.Latitude is null || branch.Longitude is null)
        {
            return ApplicationResult<CustomerOrderDeliverySnapshot>.Failure(
                ApplicationErrorCodes.CustomerOrderDeliveryUnavailable,
                "Branch coordinates are required for delivery fee calculation.");
        }

        if (branch.DeliveryPolicy is null)
        {
            return ApplicationResult<CustomerOrderDeliverySnapshot>.Failure(
                ApplicationErrorCodes.CustomerOrderDeliveryUnavailable,
                "Branch delivery policy is not configured.");
        }

        var areaResolution = QuoteCustomerOrderDelivery.ResolveDeliveryServiceArea(
            branch,
            delivery.DeliveryServiceAreaId);
        if (!areaResolution.IsSuccess || areaResolution.Value is null)
        {
            return ApplicationResult<CustomerOrderDeliverySnapshot>.Failure(
                areaResolution.ErrorCode ?? ApplicationErrorCodes.CustomerOrderDeliveryServiceAreaInvalid,
                areaResolution.ErrorMessage ?? "The selected delivery service area is not available for this branch.");
        }

        var distanceKm = StraightLineDeliveryDistance.CalculateKm(
            branch.Latitude.Value,
            branch.Longitude.Value,
            delivery.DestinationLatitude,
            delivery.DestinationLongitude);

        CustomerOrderDeliveryFeeCalculator.Quote local;
        try
        {
            local = CustomerOrderDeliveryFeeCalculator.Calculate(
                branch.DeliveryPolicy,
                merchandiseSubtotal,
                distanceKm);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<CustomerOrderDeliverySnapshot>.Failure(ex.ErrorCode, ex.Message);
        }

        var policy = branch.DeliveryPolicy;
        var citySnapshot = areaResolution.Value.CityMunicipalityName;
        var snapshot = CustomerOrderDeliverySnapshot.Create(
            delivery.RecipientName,
            delivery.RecipientPhone,
            delivery.AddressLine1,
            delivery.AddressLine2,
            citySnapshot,
            delivery.DeliveryNotes,
            delivery.DestinationLatitude,
            delivery.DestinationLongitude,
            branch.Latitude.Value,
            branch.Longitude.Value,
            local.DistanceKm,
            policy.MinimumOrderAmount,
            policy.BaseDeliveryFee,
            policy.IncludedDistanceKm,
            policy.AdditionalFeePerKm,
            policy.MaximumDeliveryDistanceKm,
            policy.FreeDeliveryThreshold,
            local.DistanceCharge,
            local.DeliveryFee,
            local.FreeDeliveryApplied);
        return ApplicationResult<CustomerOrderDeliverySnapshot>.Success(snapshot);
    }

    private async Task<ApplicationResult<Guid>> ValidatePersonalLinkedCustomerAsync(
        PosOrganizationId orgId,
        Guid sellerOrganizationId,
        PlaceCustomerOrderRequest request,
        CancellationToken cancellationToken)
    {
        if (request.PlatformBusinessCustomerId is not Guid platformBusinessCustomerId
            || platformBusinessCustomerId == Guid.Empty)
        {
            return ApplicationResult<Guid>.Failure(
                ApplicationErrorCodes.CustomerOrderLinkedCustomerRequired,
                "A linked business customer is required for personal orders.");
        }

        if (request.CustomerPlatformUserId is not Guid personalUserId || personalUserId == Guid.Empty)
        {
            return ApplicationResult<Guid>.Failure(
                ApplicationErrorCodes.CustomerOrderPartyMismatch,
                "Customer party must match the authenticated caller.");
        }

        if (_linkedCustomerAuth is not null)
        {
            var platform = await _linkedCustomerAuth
                .VerifyAsync(sellerOrganizationId, platformBusinessCustomerId, cancellationToken)
                .ConfigureAwait(false);
            if (platform.Outcome != LinkedCustomerPlatformAuthorizationOutcome.Authorized
                || platform.Proof is null
                || platform.Proof.PersonalUserId != personalUserId
                || platform.Proof.OrganizationId != sellerOrganizationId)
            {
                return ApplicationResult<Guid>.Failure(
                    ApplicationErrorCodes.LinkedCustomerNotFound,
                    "Linked customer was not found.");
            }
        }

        if (_customers is not null)
        {
            var posCustomer = await _customers
                .FindByPlatformBusinessCustomerIdAsync(orgId, platformBusinessCustomerId, cancellationToken)
                .ConfigureAwait(false);
            if (posCustomer is null)
            {
                return ApplicationResult<Guid>.Failure(
                    ApplicationErrorCodes.LinkedCustomerNotFound,
                    "Linked customer was not found.");
            }
        }

        return ApplicationResult<Guid>.Success(platformBusinessCustomerId);
    }
}

public sealed class QuoteCustomerOrderDelivery
{
    private readonly ICustomerOrderBranchDirectory _branches;
    private readonly ISellerCustomerOrderingCapability _sellerCapability;

    public QuoteCustomerOrderDelivery(
        ICustomerOrderBranchDirectory branches,
        ISellerCustomerOrderingCapability? sellerCapability = null)
    {
        _branches = branches;
        _sellerCapability = sellerCapability ?? new AllowAllSellerCustomerOrderingCapability();
    }

    public async Task<ApplicationResult<QuoteCustomerOrderDeliveryDto>> ExecuteAsync(
        Guid sellerOrganizationId,
        QuoteCustomerOrderDeliveryRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sellerCapability = await _sellerCapability
                .ResolveAsync(sellerOrganizationId, cancellationToken)
                .ConfigureAwait(false);
            if (!sellerCapability.CanCustomerOrder)
            {
                return ApplicationResult<QuoteCustomerOrderDeliveryDto>.Failure(
                    ApplicationErrorCodes.CustomerOrderOrderingUnavailable,
                    "This merchant is not accepting customer orders.");
            }

            if (!sellerCapability.CanCustomerDelivery)
            {
                return ApplicationResult<QuoteCustomerOrderDeliveryDto>.Failure(
                    ApplicationErrorCodes.CustomerOrderOrderingUnavailable,
                    "This merchant is not accepting delivery orders.");
            }

            var branch = await _branches
                .GetBranchAsync(sellerOrganizationId, request.FulfillmentBranchId, cancellationToken)
                .ConfigureAwait(false);
            if (branch is null)
            {
                return ApplicationResult<QuoteCustomerOrderDeliveryDto>.Failure(
                    ApplicationErrorCodes.CustomerOrderBranchNotFound,
                    "Fulfillment branch was not found for this organization.");
            }

            if (!branch.CustomerOrderingOperational)
            {
                return ApplicationResult<QuoteCustomerOrderDeliveryDto>.Success(
                    new QuoteCustomerOrderDeliveryDto(
                        Available: false,
                        UnavailableReason: branch.OnlineOrdersPaused
                            ? "This store is temporarily not accepting online orders."
                            : "This store is not accepting online orders right now.",
                        DistanceKm: 0m,
                        ExtraDistanceKm: 0m,
                        DistanceCharge: 0m,
                        DeliveryFee: 0m,
                        FreeDeliveryApplied: false,
                        MinimumOrderAmount: branch.DeliveryPolicy?.MinimumOrderAmount ?? 0m,
                        MaximumDeliveryDistanceKm: branch.DeliveryPolicy?.MaximumDeliveryDistanceKm ?? 0m));
            }

            if (!branch.DeliveryOperational)
            {
                return ApplicationResult<QuoteCustomerOrderDeliveryDto>.Success(
                    new QuoteCustomerOrderDeliveryDto(
                        Available: false,
                        UnavailableReason: branch.DeliveryEnabled
                            ? "Delivery is not available at this time."
                            : "Delivery is not enabled for this branch.",
                        DistanceKm: 0m,
                        ExtraDistanceKm: 0m,
                        DistanceCharge: 0m,
                        DeliveryFee: 0m,
                        FreeDeliveryApplied: false,
                        MinimumOrderAmount: branch.DeliveryPolicy?.MinimumOrderAmount ?? 0m,
                        MaximumDeliveryDistanceKm: branch.DeliveryPolicy?.MaximumDeliveryDistanceKm ?? 0m));
            }

            if (!branch.DeliveryEnabled || branch.DeliveryPolicy is null
                || branch.Latitude is null || branch.Longitude is null)
            {
                return ApplicationResult<QuoteCustomerOrderDeliveryDto>.Success(
                    new QuoteCustomerOrderDeliveryDto(
                        Available: false,
                        UnavailableReason: "Delivery is not available for this branch.",
                        DistanceKm: 0m,
                        ExtraDistanceKm: 0m,
                        DistanceCharge: 0m,
                        DeliveryFee: 0m,
                        FreeDeliveryApplied: false,
                        MinimumOrderAmount: branch.DeliveryPolicy?.MinimumOrderAmount ?? 0m,
                        MaximumDeliveryDistanceKm: branch.DeliveryPolicy?.MaximumDeliveryDistanceKm ?? 0m));
            }

            var areaResolution = ResolveDeliveryServiceArea(branch, request.DeliveryServiceAreaId);
            if (!areaResolution.IsSuccess)
            {
                return ApplicationResult<QuoteCustomerOrderDeliveryDto>.Success(
                    new QuoteCustomerOrderDeliveryDto(
                        Available: false,
                        UnavailableReason: areaResolution.ErrorMessage,
                        DistanceKm: 0m,
                        ExtraDistanceKm: 0m,
                        DistanceCharge: 0m,
                        DeliveryFee: 0m,
                        FreeDeliveryApplied: false,
                        MinimumOrderAmount: branch.DeliveryPolicy.MinimumOrderAmount,
                        MaximumDeliveryDistanceKm: branch.DeliveryPolicy.MaximumDeliveryDistanceKm));
            }

            var distanceKm = StraightLineDeliveryDistance.CalculateKm(
                branch.Latitude.Value,
                branch.Longitude.Value,
                request.DestinationLatitude,
                request.DestinationLongitude);

            var local = CustomerOrderDeliveryFeeCalculator.Calculate(
                branch.DeliveryPolicy,
                request.MerchandiseSubtotal,
                distanceKm);
            return ApplicationResult<QuoteCustomerOrderDeliveryDto>.Success(
                new QuoteCustomerOrderDeliveryDto(
                    Available: true,
                    UnavailableReason: null,
                    local.DistanceKm,
                    local.ExtraDistanceKm,
                    local.DistanceCharge,
                    local.DeliveryFee,
                    local.FreeDeliveryApplied,
                    branch.DeliveryPolicy.MinimumOrderAmount,
                    branch.DeliveryPolicy.MaximumDeliveryDistanceKm));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<QuoteCustomerOrderDeliveryDto>.Success(
                new QuoteCustomerOrderDeliveryDto(
                    Available: false,
                    UnavailableReason: ex.Message,
                    DistanceKm: 0m,
                    ExtraDistanceKm: 0m,
                    DistanceCharge: 0m,
                    DeliveryFee: 0m,
                    FreeDeliveryApplied: false,
                    MinimumOrderAmount: 0m,
                    MaximumDeliveryDistanceKm: 0m));
        }
    }

    internal static ApplicationResult<CustomerOrderDeliveryServiceAreaSnapshot> ResolveDeliveryServiceArea(
        CustomerOrderBranchSnapshot branch,
        Guid? deliveryServiceAreaId)
    {
        var areas = branch.DeliveryServiceAreas ?? [];
        if (deliveryServiceAreaId is null || deliveryServiceAreaId == Guid.Empty)
        {
            return ApplicationResult<CustomerOrderDeliveryServiceAreaSnapshot>.Failure(
                ApplicationErrorCodes.CustomerOrderDeliveryServiceAreaInvalid,
                "A configured delivery service area is required.");
        }

        var match = areas.FirstOrDefault(a => a.Id == deliveryServiceAreaId.Value);
        if (match is null)
        {
            return ApplicationResult<CustomerOrderDeliveryServiceAreaSnapshot>.Failure(
                ApplicationErrorCodes.CustomerOrderDeliveryServiceAreaInvalid,
                "The selected delivery service area is not available for this branch.");
        }

        return ApplicationResult<CustomerOrderDeliveryServiceAreaSnapshot>.Success(match);
    }
}

public sealed class AcceptCustomerOrder
{
    private readonly ICustomerOrderRepository _orders;
    private readonly ICustomerOrderStockService _stock;
    private readonly IOrganizationBusinessNotificationPublisher _notifications;
    private readonly IPersonalBusinessNotificationPublisher _personalNotifications;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public AcceptCustomerOrder(
        ICustomerOrderRepository orders,
        ICustomerOrderStockService stock,
        IPosUnitOfWork unitOfWork,
        IClock clock,
        IOrganizationBusinessNotificationPublisher? notifications = null,
        IPersonalBusinessNotificationPublisher? personalNotifications = null)
    {
        _orders = orders;
        _stock = stock;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _notifications = notifications ?? new NoOpOrganizationBusinessNotificationPublisher();
        _personalNotifications = personalNotifications ?? new NoOpPersonalBusinessNotificationPublisher();
    }

    public async Task<ApplicationResult<CustomerOrderDto>> ExecuteAsync(
        Guid sellerOrganizationId,
        Guid orderId,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            CustomerOrder? accepted = null;
            var result = await _unitOfWork
                .ExecuteInSerializableTransactionAsync(async ct =>
                {
                    var orgId = PosOrganizationId.From(sellerOrganizationId);
                    var order = await _orders
                        .GetByIdAsync(orgId, CustomerOrderId.From(orderId), ct)
                        .ConfigureAwait(false);
                    if (order is null)
                    {
                        return ApplicationResult<CustomerOrderDto>.Failure(
                            ApplicationErrorCodes.CustomerOrderNotFound,
                            "Customer order was not found.");
                    }

                    var now = _clock.UtcNow;
                    order.Accept(actorId, now);
                    await _stock.ReserveForAcceptAsync(order, actorId, now, ct).ConfigureAwait(false);
                    await _orders.UpdateAsync(order, ct).ConfigureAwait(false);
                    accepted = order;
                    return ApplicationResult<CustomerOrderDto>.Success(CustomerOrderMaps.Map(order));
                }, cancellationToken)
                .ConfigureAwait(false);

            if (result.IsSuccess && accepted is not null)
            {
                await NotifyCustomerAsync(accepted, CustomerOrderNotificationTypes.Accepted, "Order accepted", cancellationToken)
                    .ConfigureAwait(false);
            }

            return result;
        }
        catch (DomainException ex)
        {
            return ApplicationResult<CustomerOrderDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<CustomerOrderDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }

    private Task NotifyCustomerAsync(
        CustomerOrder order,
        string relatedType,
        string title,
        CancellationToken cancellationToken) =>
        CustomerOrderLifecycleNotifier.NotifyCustomerAsync(
            _notifications,
            _personalNotifications,
            order,
            relatedType,
            title,
            cancellationToken);
}

public sealed class RejectCustomerOrder
{
    private readonly ICustomerOrderRepository _orders;
    private readonly ICustomerOrderStockService _stock;
    private readonly IOrganizationBusinessNotificationPublisher _notifications;
    private readonly IPersonalBusinessNotificationPublisher _personalNotifications;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public RejectCustomerOrder(
        ICustomerOrderRepository orders,
        ICustomerOrderStockService stock,
        IPosUnitOfWork unitOfWork,
        IClock clock,
        IOrganizationBusinessNotificationPublisher? notifications = null,
        IPersonalBusinessNotificationPublisher? personalNotifications = null)
    {
        _orders = orders;
        _stock = stock;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _notifications = notifications ?? new NoOpOrganizationBusinessNotificationPublisher();
        _personalNotifications = personalNotifications ?? new NoOpPersonalBusinessNotificationPublisher();
    }

    public async Task<ApplicationResult<CustomerOrderDto>> ExecuteAsync(
        Guid sellerOrganizationId,
        Guid orderId,
        RejectCustomerOrderRequest request,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Enum.TryParse<CustomerOrderRejectReason>(request.Reason, true, out var reason))
            {
                return ApplicationResult<CustomerOrderDto>.Failure(
                    DomainErrorCodes.InvalidCustomerOrderRejectReason,
                    "Reject reason is invalid.");
            }

            CustomerOrder? rejected = null;
            var result = await _unitOfWork
                .ExecuteInSerializableTransactionAsync(async ct =>
                {
                    var orgId = PosOrganizationId.From(sellerOrganizationId);
                    var order = await _orders
                        .GetByIdAsync(orgId, CustomerOrderId.From(orderId), ct)
                        .ConfigureAwait(false);
                    if (order is null)
                    {
                        return ApplicationResult<CustomerOrderDto>.Failure(
                            ApplicationErrorCodes.CustomerOrderNotFound,
                            "Customer order was not found.");
                    }

                    var now = _clock.UtcNow;
                    order.Reject(reason, request.Notes, actorId, now);
                    await _stock.ReleaseIfReservedAsync(order, now, ct).ConfigureAwait(false);
                    await _orders.UpdateAsync(order, ct).ConfigureAwait(false);
                    rejected = order;
                    return ApplicationResult<CustomerOrderDto>.Success(CustomerOrderMaps.Map(order));
                }, cancellationToken)
                .ConfigureAwait(false);

            if (result.IsSuccess && rejected is not null)
            {
                await CustomerOrderLifecycleNotifier
                    .NotifyCustomerAsync(
                        _notifications,
                        _personalNotifications,
                        rejected,
                        CustomerOrderNotificationTypes.Rejected,
                        "Order rejected",
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            return result;
        }
        catch (DomainException ex)
        {
            return ApplicationResult<CustomerOrderDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<CustomerOrderDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class CancelCustomerOrder
{
    private readonly ICustomerOrderRepository _orders;
    private readonly ICustomerOrderStockService _stock;
    private readonly IOrganizationBusinessNotificationPublisher _notifications;
    private readonly IPersonalBusinessNotificationPublisher _personalNotifications;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CancelCustomerOrder(
        ICustomerOrderRepository orders,
        ICustomerOrderStockService stock,
        IPosUnitOfWork unitOfWork,
        IClock clock,
        IOrganizationBusinessNotificationPublisher? notifications = null,
        IPersonalBusinessNotificationPublisher? personalNotifications = null)
    {
        _orders = orders;
        _stock = stock;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _notifications = notifications ?? new NoOpOrganizationBusinessNotificationPublisher();
        _personalNotifications = personalNotifications ?? new NoOpPersonalBusinessNotificationPublisher();
    }

    public async Task<ApplicationResult<CustomerOrderDto>> ExecuteAsync(
        Guid sellerOrganizationId,
        Guid orderId,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            CustomerOrder? cancelled = null;
            var result = await _unitOfWork
                .ExecuteInSerializableTransactionAsync(async ct =>
                {
                    var orgId = PosOrganizationId.From(sellerOrganizationId);
                    var order = await _orders
                        .GetByIdAsync(orgId, CustomerOrderId.From(orderId), ct)
                        .ConfigureAwait(false);
                    if (order is null)
                    {
                        return ApplicationResult<CustomerOrderDto>.Failure(
                            ApplicationErrorCodes.CustomerOrderNotFound,
                            "Customer order was not found.");
                    }

                    var now = _clock.UtcNow;
                    order.Cancel(actorId, now);
                    await _stock.ReleaseIfReservedAsync(order, now, ct).ConfigureAwait(false);
                    await _orders.UpdateAsync(order, ct).ConfigureAwait(false);
                    cancelled = order;
                    return ApplicationResult<CustomerOrderDto>.Success(CustomerOrderMaps.Map(order));
                }, cancellationToken)
                .ConfigureAwait(false);

            if (result.IsSuccess && cancelled is not null)
            {
                await _notifications
                    .PublishAsync(
                        sellerOrganizationId,
                        sellerOrganizationId,
                        CustomerOrderNotificationTypes.Cancelled,
                        cancelled.Id.Value.ToString("D"),
                        "Order cancelled",
                        $"{cancelled.OrderNumber} was cancelled.",
                        cancellationToken)
                    .ConfigureAwait(false);
                await CustomerOrderLifecycleNotifier
                    .NotifyCustomerAsync(
                        _notifications,
                        _personalNotifications,
                        cancelled,
                        CustomerOrderNotificationTypes.Cancelled,
                        "Order cancelled",
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            return result;
        }
        catch (DomainException ex)
        {
            return ApplicationResult<CustomerOrderDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<CustomerOrderDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class AdvanceCustomerOrderFulfillment
{
    private readonly ICustomerOrderRepository _orders;
    private readonly IOrganizationBusinessNotificationPublisher _notifications;
    private readonly IPersonalBusinessNotificationPublisher _personalNotifications;
    private readonly IClock _clock;

    public AdvanceCustomerOrderFulfillment(
        ICustomerOrderRepository orders,
        IClock clock,
        IOrganizationBusinessNotificationPublisher? notifications = null,
        IPersonalBusinessNotificationPublisher? personalNotifications = null)
    {
        _orders = orders;
        _clock = clock;
        _notifications = notifications ?? new NoOpOrganizationBusinessNotificationPublisher();
        _personalNotifications = personalNotifications ?? new NoOpPersonalBusinessNotificationPublisher();
    }

    public Task<ApplicationResult<CustomerOrderDto>> StartPreparingAsync(
        Guid sellerOrganizationId,
        Guid orderId,
        Guid actorId,
        CancellationToken cancellationToken = default) =>
        MutateAsync(
            sellerOrganizationId,
            orderId,
            actorId,
            o => o.StartPreparing(_clock.UtcNow, actorId),
            null,
            cancellationToken);

    public Task<ApplicationResult<CustomerOrderDto>> MarkReadyAsync(
        Guid sellerOrganizationId,
        Guid orderId,
        Guid actorId,
        CancellationToken cancellationToken = default) =>
        MutateAsync(
            sellerOrganizationId,
            orderId,
            actorId,
            o => o.MarkReady(_clock.UtcNow, actorId),
            CustomerOrderNotificationTypes.Ready,
            cancellationToken);

    public Task<ApplicationResult<CustomerOrderDto>> MarkOutForDeliveryAsync(
        Guid sellerOrganizationId,
        Guid orderId,
        Guid actorId,
        CancellationToken cancellationToken = default) =>
        MutateAsync(
            sellerOrganizationId,
            orderId,
            actorId,
            o => o.MarkOutForDelivery(_clock.UtcNow, actorId),
            CustomerOrderNotificationTypes.OutForDelivery,
            cancellationToken);

    public Task<ApplicationResult<CustomerOrderDto>> MarkDeliveredAsync(
        Guid sellerOrganizationId,
        Guid orderId,
        Guid actorId,
        CancellationToken cancellationToken = default) =>
        MutateAsync(
            sellerOrganizationId,
            orderId,
            actorId,
            o => o.MarkDelivered(_clock.UtcNow, actorId),
            CustomerOrderNotificationTypes.Delivered,
            cancellationToken);

    public Task<ApplicationResult<CustomerOrderDto>> MarkCollectedAsync(
        Guid sellerOrganizationId,
        Guid orderId,
        Guid actorId,
        CancellationToken cancellationToken = default) =>
        MutateAsync(
            sellerOrganizationId,
            orderId,
            actorId,
            o => o.MarkCollected(_clock.UtcNow, actorId),
            CustomerOrderNotificationTypes.Collected,
            cancellationToken);

    private async Task<ApplicationResult<CustomerOrderDto>> MutateAsync(
        Guid sellerOrganizationId,
        Guid orderId,
        Guid actorId,
        Action<CustomerOrder> mutate,
        string? notifyType,
        CancellationToken cancellationToken)
    {
        if (actorId == Guid.Empty)
        {
            return ApplicationResult<CustomerOrderDto>.Failure(
                ApplicationErrorCodes.ActorRequired,
                "An actor identifier is required to advance customer order fulfillment.");
        }

        try
        {
            var orgId = PosOrganizationId.From(sellerOrganizationId);
            var order = await _orders
                .GetByIdAsync(orgId, CustomerOrderId.From(orderId), cancellationToken)
                .ConfigureAwait(false);
            if (order is null)
            {
                return ApplicationResult<CustomerOrderDto>.Failure(
                    ApplicationErrorCodes.CustomerOrderNotFound,
                    "Customer order was not found.");
            }

            mutate(order);
            await _orders.UpdateAsync(order, cancellationToken).ConfigureAwait(false);
            if (notifyType is not null)
            {
                await CustomerOrderLifecycleNotifier
                    .NotifyCustomerAsync(
                        _notifications,
                        _personalNotifications,
                        order,
                        notifyType,
                        "Order update",
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            return ApplicationResult<CustomerOrderDto>.Success(CustomerOrderMaps.Map(order));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<CustomerOrderDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class CompleteCustomerOrder
{
    private readonly ICustomerOrderRepository _orders;
    private readonly ICatalogProductRepository _products;
    private readonly ICustomerOrderStockService _stock;
    private readonly ICustomerOrderUtangLedgerService _utangLedger;
    private readonly IOrganizationBusinessNotificationPublisher _notifications;
    private readonly IPersonalBusinessNotificationPublisher _personalNotifications;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CompleteCustomerOrder(
        ICustomerOrderRepository orders,
        ICatalogProductRepository products,
        ICustomerOrderStockService stock,
        ICustomerOrderUtangLedgerService utangLedger,
        IPosUnitOfWork unitOfWork,
        IClock clock,
        IOrganizationBusinessNotificationPublisher? notifications = null,
        IPersonalBusinessNotificationPublisher? personalNotifications = null)
    {
        _orders = orders;
        _products = products;
        _stock = stock;
        _utangLedger = utangLedger;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _notifications = notifications ?? new NoOpOrganizationBusinessNotificationPublisher();
        _personalNotifications = personalNotifications ?? new NoOpPersonalBusinessNotificationPublisher();
    }

    public async Task<ApplicationResult<CustomerOrderDto>> ExecuteAsync(
        Guid sellerOrganizationId,
        Guid orderId,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            CustomerOrder? completed = null;
            var result = await _unitOfWork
                .ExecuteInSerializableTransactionAsync(async ct =>
                {
                    var orgId = PosOrganizationId.From(sellerOrganizationId);
                    var order = await _orders
                        .GetByIdAsync(orgId, CustomerOrderId.From(orderId), ct)
                        .ConfigureAwait(false);
                    if (order is null)
                    {
                        return ApplicationResult<CustomerOrderDto>.Failure(
                            ApplicationErrorCodes.CustomerOrderNotFound,
                            "Customer order was not found.");
                    }

                    var now = _clock.UtcNow;
                    order.Complete(actorId, now);

                    var productIds = order.Lines.Select(l => l.ProductId).Distinct().ToList();
                    var products = await _products.ListByIdsAsync(orgId, productIds, ct).ConfigureAwait(false);
                    var byId = products.ToDictionary(p => p.Id.Value);
                    await _stock.ConsumeOnCompleteAsync(order, byId, actorId, now, ct).ConfigureAwait(false);
                    await _utangLedger
                        .PostOnCompleteIfNeededAsync(order, actorId, now, ct)
                        .ConfigureAwait(false);
                    await _orders.UpdateAsync(order, ct).ConfigureAwait(false);
                    completed = order;
                    return ApplicationResult<CustomerOrderDto>.Success(CustomerOrderMaps.Map(order));
                }, cancellationToken)
                .ConfigureAwait(false);

            if (result.IsSuccess && completed is not null)
            {
                await CustomerOrderLifecycleNotifier
                    .NotifyCustomerAsync(
                        _notifications,
                        _personalNotifications,
                        completed,
                        CustomerOrderNotificationTypes.Completed,
                        "Order completed",
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            return result;
        }
        catch (DomainException ex)
        {
            return ApplicationResult<CustomerOrderDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<CustomerOrderDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

internal static class CustomerOrderLifecycleNotifier
{
    public static async Task NotifyCustomerAsync(
        IOrganizationBusinessNotificationPublisher orgNotifications,
        IPersonalBusinessNotificationPublisher personalNotifications,
        CustomerOrder order,
        string relatedType,
        string title,
        CancellationToken cancellationToken)
    {
        if (order.CustomerParty.PartyType == CustomerPartyType.Organization
            && order.CustomerParty.BuyerOrganizationId is Guid buyerOrg
            && buyerOrg != Guid.Empty)
        {
            await orgNotifications
                .PublishAsync(
                    order.SellerOrganizationId.Value,
                    buyerOrg,
                    relatedType,
                    order.Id.Value.ToString("D"),
                    title,
                    $"{order.OrderNumber} · {order.Status}",
                    cancellationToken)
                .ConfigureAwait(false);
            return;
        }

        if (order.CustomerParty.PartyType == CustomerPartyType.Personal
            && order.CustomerParty.PlatformUserId is Guid personalUserId
            && personalUserId != Guid.Empty)
        {
            await personalNotifications
                .PublishAsync(
                    order.SellerOrganizationId.Value,
                    personalUserId,
                    relatedType,
                    order.Id.Value.ToString("D"),
                    title,
                    $"{order.OrderNumber} · {order.Status}",
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }
}

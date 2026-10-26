using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Realtime;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Application.CustomerOrdering;

public sealed class AdjustCustomerOrderLines
{
    private readonly ICustomerOrderRepository _orders;
    private readonly ICustomerOrderStockService _stock;
    private readonly IOrganizationBusinessNotificationPublisher _notifications;
    private readonly IPersonalBusinessNotificationPublisher _personalNotifications;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IPosRealtimeChangePublisher _realtime;

    public AdjustCustomerOrderLines(
        ICustomerOrderRepository orders,
        ICustomerOrderStockService stock,
        IPosUnitOfWork unitOfWork,
        IClock clock,
        IOrganizationBusinessNotificationPublisher? notifications = null,
        IPersonalBusinessNotificationPublisher? personalNotifications = null,
        IPosRealtimeChangePublisher? realtime = null)
    {
        _orders = orders;
        _stock = stock;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _notifications = notifications ?? new NoOpOrganizationBusinessNotificationPublisher();
        _personalNotifications = personalNotifications ?? new NoOpPersonalBusinessNotificationPublisher();
        _realtime = realtime ?? NullPosRealtimeChangePublisher.Instance;
    }

    public async Task<ApplicationResult<CustomerOrderDto>> ExecuteAsync(
        Guid sellerOrganizationId,
        Guid orderId,
        AdjustCustomerOrderLinesRequest request,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            CustomerOrder? updated = null;
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

                    if (request.Lines is null || request.Lines.Count == 0)
                    {
                        return ApplicationResult<CustomerOrderDto>.Failure(
                            DomainErrorCodes.InvalidCustomerOrderLineAdjustment,
                            "Provide at least one line adjustment.");
                    }

                    var adjustments = new List<CustomerOrderLineAdjustment>();
                    foreach (var line in request.Lines)
                    {
                        if (!TryParseAdjustmentReason(line.Reason, out var reason))
                        {
                            return ApplicationResult<CustomerOrderDto>.Failure(
                                DomainErrorCodes.InvalidCustomerOrderLineAdjustment,
                                "Adjustment reason is invalid.");
                        }

                        adjustments.Add(new CustomerOrderLineAdjustment(
                            line.LineId,
                            line.NewQuantity,
                            reason,
                            line.Note));
                    }

                    var now = _clock.UtcNow;
                    var releases = order.AdjustLines(adjustments, actorId, now);
                    await _stock
                        .ReleaseReservationDeltasAsync(order, releases, now, ct)
                        .ConfigureAwait(false);

                    if (order.Status == CustomerOrderStatus.Cancelled
                        && order.StockReservationState == CustomerOrderStockReservationState.Reserved)
                    {
                        order.MarkStockReleased(now);
                    }

                    await _orders.UpdateAsync(order, ct).ConfigureAwait(false);
                    updated = order;
                    return ApplicationResult<CustomerOrderDto>.Success(CustomerOrderMaps.Map(order));
                }, cancellationToken)
                .ConfigureAwait(false);

            if (result.IsSuccess && updated is not null)
            {
                var eventType = updated.Status == CustomerOrderStatus.Cancelled
                    ? PosRealtimeEventTypes.CustomerOrderCancelled
                    : PosRealtimeEventTypes.CustomerOrderLinesAdjusted;
                await PosRealtimeCustomerOrderPublish
                    .PublishAsync(_realtime, updated, eventType, cancellationToken)
                    .ConfigureAwait(false);
                await CustomerOrderLifecycleNotifier
                    .NotifyCustomerAsync(
                        _notifications,
                        _personalNotifications,
                        updated,
                        updated.Status == CustomerOrderStatus.Cancelled
                            ? CustomerOrderNotificationTypes.Cancelled
                            : CustomerOrderNotificationTypes.LinesAdjusted,
                        updated.Status == CustomerOrderStatus.Cancelled
                            ? "Your order was cancelled"
                            : "Your order was updated",
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

    internal static bool TryParseAdjustmentReason(
        string? raw,
        out CustomerOrderLineAdjustmentReason reason)
    {
        reason = default;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        // Reject pure numeric payloads so undefined integers cannot sneak past TryParse.
        var trimmed = raw.Trim();
        if (!Enum.TryParse(trimmed, ignoreCase: true, out reason))
        {
            return false;
        }

        return Enum.IsDefined(reason);
    }
}

public sealed class CancelCustomerOrderBySeller
{
    private readonly ICustomerOrderRepository _orders;
    private readonly ICustomerOrderStockService _stock;
    private readonly IOrganizationBusinessNotificationPublisher _notifications;
    private readonly IPersonalBusinessNotificationPublisher _personalNotifications;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IPosRealtimeChangePublisher _realtime;

    public CancelCustomerOrderBySeller(
        ICustomerOrderRepository orders,
        ICustomerOrderStockService stock,
        IPosUnitOfWork unitOfWork,
        IClock clock,
        IOrganizationBusinessNotificationPublisher? notifications = null,
        IPersonalBusinessNotificationPublisher? personalNotifications = null,
        IPosRealtimeChangePublisher? realtime = null)
    {
        _orders = orders;
        _stock = stock;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _notifications = notifications ?? new NoOpOrganizationBusinessNotificationPublisher();
        _personalNotifications = personalNotifications ?? new NoOpPersonalBusinessNotificationPublisher();
        _realtime = realtime ?? NullPosRealtimeChangePublisher.Instance;
    }

    public async Task<ApplicationResult<CustomerOrderDto>> ExecuteAsync(
        Guid sellerOrganizationId,
        Guid orderId,
        CancelCustomerOrderBySellerRequest request,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            CustomerOrder? updated = null;
            var result = await _unitOfWork
                .ExecuteInSerializableTransactionAsync(async ct =>
                {
                    var order = await _orders
                        .GetByIdAsync(
                            PosOrganizationId.From(sellerOrganizationId),
                            CustomerOrderId.From(orderId),
                            ct)
                        .ConfigureAwait(false);
                    if (order is null)
                    {
                        return ApplicationResult<CustomerOrderDto>.Failure(
                            ApplicationErrorCodes.CustomerOrderNotFound,
                            "Customer order was not found.");
                    }

                    if (!Enum.TryParse<CustomerOrderCancelReason>(request.Reason, true, out var reason))
                    {
                        return ApplicationResult<CustomerOrderDto>.Failure(
                            DomainErrorCodes.InvalidCustomerOrderCancelReason,
                            "Cancellation reason is invalid.");
                    }

                    var now = _clock.UtcNow;
                    order.CancelBySeller(reason, request.Notes, actorId, now);
                    await _stock.ReleaseIfReservedAsync(order, now, ct).ConfigureAwait(false);
                    await _orders.UpdateAsync(order, ct).ConfigureAwait(false);
                    updated = order;
                    return ApplicationResult<CustomerOrderDto>.Success(CustomerOrderMaps.Map(order));
                }, cancellationToken)
                .ConfigureAwait(false);

            if (result.IsSuccess && updated is not null)
            {
                await PosRealtimeCustomerOrderPublish
                    .PublishAsync(
                        _realtime,
                        updated,
                        PosRealtimeEventTypes.CustomerOrderCancelled,
                        cancellationToken)
                    .ConfigureAwait(false);
                await CustomerOrderLifecycleNotifier
                    .NotifyCustomerAsync(
                        _notifications,
                        _personalNotifications,
                        updated,
                        CustomerOrderNotificationTypes.Cancelled,
                        "Your order was cancelled",
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

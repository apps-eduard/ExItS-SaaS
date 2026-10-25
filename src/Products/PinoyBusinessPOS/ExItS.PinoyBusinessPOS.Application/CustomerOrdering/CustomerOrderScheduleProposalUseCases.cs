using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Realtime;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Application.CustomerOrdering;

public sealed class ProposeCustomerOrderSchedule
{
    private readonly ICustomerOrderRepository _orders;
    private readonly ICustomerOrderBranchDirectory _branches;
    private readonly IOrganizationBusinessNotificationPublisher _notifications;
    private readonly IPersonalBusinessNotificationPublisher _personalNotifications;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IPosRealtimeChangePublisher _realtime;

    public ProposeCustomerOrderSchedule(
        ICustomerOrderRepository orders,
        ICustomerOrderBranchDirectory branches,
        IPosUnitOfWork unitOfWork,
        IClock clock,
        IOrganizationBusinessNotificationPublisher? notifications = null,
        IPersonalBusinessNotificationPublisher? personalNotifications = null,
        IPosRealtimeChangePublisher? realtime = null)
    {
        _orders = orders;
        _branches = branches;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _notifications = notifications ?? new NoOpOrganizationBusinessNotificationPublisher();
        _personalNotifications = personalNotifications ?? new NoOpPersonalBusinessNotificationPublisher();
        _realtime = realtime ?? NullPosRealtimeChangePublisher.Instance;
    }

    public async Task<ApplicationResult<CustomerOrderDto>> ExecuteAsync(
        Guid sellerOrganizationId,
        Guid orderId,
        ProposeCustomerOrderScheduleRequest request,
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

                    var branch = await _branches
                        .GetBranchAsync(sellerOrganizationId, order.FulfillmentBranchId, ct)
                        .ConfigureAwait(false);
                    if (branch is null)
                    {
                        return ApplicationResult<CustomerOrderDto>.Failure(
                            ApplicationErrorCodes.CustomerOrderBranchNotFound,
                            "Fulfillment branch was not found for this organization.");
                    }

                    if (branch.OnlineOrdersPaused)
                    {
                        return ApplicationResult<CustomerOrderDto>.Failure(
                            ApplicationErrorCodes.CustomerOrderBranchCapability,
                            "This store is temporarily not accepting online orders.");
                    }

                    var scheduleTimeZoneId = CustomerOrderScheduleProposalHelpers.FirstNonBlank(
                        branch.TimeZoneId,
                        order.EffectiveFulfillmentTimeZoneId,
                        order.PendingScheduleProposal?.TimeZoneId);
                    var now = _clock.UtcNow;
                    var resolved = RequestedFulfillmentSchedule.Resolve(
                        order.FulfillmentType,
                        request.Date,
                        request.Time,
                        scheduleTimeZoneId,
                        now,
                        branch.OperatingHours,
                        branch.MinPreparationLeadMinutes,
                        branch.MaxAdvanceScheduleDays);
                    if (!resolved.IsSuccess)
                    {
                        return ApplicationResult<CustomerOrderDto>.Failure(
                            resolved.ErrorCode!,
                            resolved.ErrorMessage!);
                    }

                    var (local, zoneId, atUtc) = resolved.Value;
                    if (local is null || zoneId is null || atUtc is null)
                    {
                        return ApplicationResult<CustomerOrderDto>.Failure(
                            DomainErrorCodes.InvalidCustomerOrderPickupRequest,
                            "Choose a date and time for the proposed schedule.");
                    }

                    var proposalId = request.ClientProposalId is Guid clientId && clientId != Guid.Empty
                        ? clientId
                        : Guid.NewGuid();

                    order.ProposeSchedule(
                        proposalId,
                        local,
                        zoneId,
                        atUtc.Value,
                        request.Note,
                        actorId,
                        now);
                    await _orders.UpdateAsync(order, ct).ConfigureAwait(false);
                    updated = order;
                    return ApplicationResult<CustomerOrderDto>.Success(CustomerOrderMaps.Map(order));
                }, cancellationToken)
                .ConfigureAwait(false);

            if (result.IsSuccess && updated is not null)
            {
                await CustomerOrderLifecycleNotifier
                    .NotifyCustomerAsync(
                        _notifications,
                        _personalNotifications,
                        updated,
                        CustomerOrderNotificationTypes.ScheduleProposed,
                        "Seller proposed a new schedule",
                        cancellationToken)
                    .ConfigureAwait(false);
                await PosRealtimeCustomerOrderPublish
                    .PublishAsync(
                        _realtime,
                        updated,
                        PosRealtimeEventTypes.CustomerOrderScheduleProposed,
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

public sealed class AcceptCustomerOrderScheduleProposal
{
    private readonly ICustomerOrderRepository _orders;
    private readonly ICustomerOrderBranchDirectory _branches;
    private readonly IOrganizationBusinessNotificationPublisher _notifications;
    private readonly IPersonalBusinessNotificationPublisher _personalNotifications;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IPosRealtimeChangePublisher _realtime;

    public AcceptCustomerOrderScheduleProposal(
        ICustomerOrderRepository orders,
        ICustomerOrderBranchDirectory branches,
        IPosUnitOfWork unitOfWork,
        IClock clock,
        IOrganizationBusinessNotificationPublisher? notifications = null,
        IPersonalBusinessNotificationPublisher? personalNotifications = null,
        IPosRealtimeChangePublisher? realtime = null)
    {
        _orders = orders;
        _branches = branches;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _notifications = notifications ?? new NoOpOrganizationBusinessNotificationPublisher();
        _personalNotifications = personalNotifications ?? new NoOpPersonalBusinessNotificationPublisher();
        _realtime = realtime ?? NullPosRealtimeChangePublisher.Instance;
    }

    public async Task<ApplicationResult<CustomerOrderDto>> ExecuteAsync(
        Guid sellerOrganizationId,
        Guid orderId,
        RespondCustomerOrderScheduleProposalRequest request,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            CustomerOrder? updated = null;
            var result = await _unitOfWork
                .ExecuteInSerializableTransactionAsync(async ct =>
                {
                    var order = await LoadBuyerOrderAsync(sellerOrganizationId, orderId, actorId, ct)
                        .ConfigureAwait(false);
                    if (order is null)
                    {
                        return ApplicationResult<CustomerOrderDto>.Failure(
                            ApplicationErrorCodes.CustomerOrderNotFound,
                            "Customer order was not found.");
                    }

                    if (order.PendingScheduleProposal is null
                        || order.PendingScheduleProposal.ProposalId != request.ProposalId)
                    {
                        return ApplicationResult<CustomerOrderDto>.Failure(
                            DomainErrorCodes.InvalidCustomerOrderStatusTransition,
                            "This schedule proposal is no longer current.");
                    }

                    var branch = await _branches
                        .GetBranchAsync(sellerOrganizationId, order.FulfillmentBranchId, ct)
                        .ConfigureAwait(false);
                    if (branch is null)
                    {
                        return ApplicationResult<CustomerOrderDto>.Failure(
                            ApplicationErrorCodes.CustomerOrderBranchNotFound,
                            "Fulfillment branch was not found for this organization.");
                    }

                    var local = order.PendingScheduleProposal.Local;
                    var space = local.IndexOf(' ');
                    if (space <= 0 || space >= local.Length - 1)
                    {
                        return ApplicationResult<CustomerOrderDto>.Failure(
                            DomainErrorCodes.InvalidCustomerOrderPickupRequest,
                            "The proposed schedule is not valid for this store.");
                    }

                    var scheduleTimeZoneId = CustomerOrderScheduleProposalHelpers.FirstNonBlank(
                        branch.TimeZoneId,
                        order.PendingScheduleProposal.TimeZoneId,
                        order.EffectiveFulfillmentTimeZoneId);
                    var now = _clock.UtcNow;
                    var revalidated = RequestedFulfillmentSchedule.Resolve(
                        order.FulfillmentType,
                        local[..space],
                        local[(space + 1)..],
                        scheduleTimeZoneId,
                        now,
                        branch.OperatingHours,
                        branch.MinPreparationLeadMinutes,
                        branch.MaxAdvanceScheduleDays);
                    if (!revalidated.IsSuccess)
                    {
                        return ApplicationResult<CustomerOrderDto>.Failure(
                            revalidated.ErrorCode!,
                            revalidated.ErrorMessage!);
                    }

                    order.AcceptScheduleProposal(request.ProposalId, actorId, now);
                    await _orders.UpdateAsync(order, ct).ConfigureAwait(false);
                    updated = order;
                    return ApplicationResult<CustomerOrderDto>.Success(CustomerOrderMaps.Map(order));
                }, cancellationToken)
                .ConfigureAwait(false);

            if (result.IsSuccess && updated is not null)
            {
                await NotifySellerAsync(
                        updated,
                        CustomerOrderNotificationTypes.ScheduleAccepted,
                        "Customer accepted the proposed schedule",
                        cancellationToken)
                    .ConfigureAwait(false);
                await PosRealtimeCustomerOrderPublish
                    .PublishAsync(
                        _realtime,
                        updated,
                        PosRealtimeEventTypes.CustomerOrderScheduleAccepted,
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

    private async Task<CustomerOrder?> LoadBuyerOrderAsync(
        Guid sellerOrganizationId,
        Guid orderId,
        Guid actorId,
        CancellationToken cancellationToken)
    {
        var orgId = PosOrganizationId.From(sellerOrganizationId);
        var order = await _orders
            .GetByIdAsync(orgId, CustomerOrderId.From(orderId), cancellationToken)
            .ConfigureAwait(false);
        if (order is null)
        {
            return null;
        }

        if (order.CustomerParty.PartyType != CustomerPartyType.Personal
            || order.CustomerParty.PlatformUserId != actorId)
        {
            return null;
        }

        return order;
    }

    private Task NotifySellerAsync(
        CustomerOrder order,
        string relatedType,
        string title,
        CancellationToken cancellationToken)
    {
        var sellerOrg = order.SellerOrganizationId.Value;
        return _notifications.PublishAsync(
            sellerOrg,
            sellerOrg,
            relatedType,
            order.Id.Value.ToString("D"),
            title,
            $"{order.OrderNumber} · {order.Status}",
            cancellationToken);
    }
}

public sealed class DeclineCustomerOrderScheduleProposal
{
    private readonly ICustomerOrderRepository _orders;
    private readonly IOrganizationBusinessNotificationPublisher _notifications;
    private readonly IPersonalBusinessNotificationPublisher _personalNotifications;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IPosRealtimeChangePublisher _realtime;

    public DeclineCustomerOrderScheduleProposal(
        ICustomerOrderRepository orders,
        IPosUnitOfWork unitOfWork,
        IClock clock,
        IOrganizationBusinessNotificationPublisher? notifications = null,
        IPersonalBusinessNotificationPublisher? personalNotifications = null,
        IPosRealtimeChangePublisher? realtime = null)
    {
        _orders = orders;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _notifications = notifications ?? new NoOpOrganizationBusinessNotificationPublisher();
        _personalNotifications = personalNotifications ?? new NoOpPersonalBusinessNotificationPublisher();
        _realtime = realtime ?? NullPosRealtimeChangePublisher.Instance;
    }

    public async Task<ApplicationResult<CustomerOrderDto>> ExecuteAsync(
        Guid sellerOrganizationId,
        Guid orderId,
        RespondCustomerOrderScheduleProposalRequest request,
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
                    if (order is null
                        || order.CustomerParty.PartyType != CustomerPartyType.Personal
                        || order.CustomerParty.PlatformUserId != actorId)
                    {
                        return ApplicationResult<CustomerOrderDto>.Failure(
                            ApplicationErrorCodes.CustomerOrderNotFound,
                            "Customer order was not found.");
                    }

                    var now = _clock.UtcNow;
                    order.DeclineScheduleProposal(request.ProposalId, actorId, now);
                    await _orders.UpdateAsync(order, ct).ConfigureAwait(false);
                    updated = order;
                    return ApplicationResult<CustomerOrderDto>.Success(CustomerOrderMaps.Map(order));
                }, cancellationToken)
                .ConfigureAwait(false);

            if (result.IsSuccess && updated is not null)
            {
                var sellerOrg = updated.SellerOrganizationId.Value;
                await _notifications
                    .PublishAsync(
                        sellerOrg,
                        sellerOrg,
                        CustomerOrderNotificationTypes.ScheduleDeclined,
                        updated.Id.Value.ToString("D"),
                        "Customer declined the proposed schedule",
                        $"{updated.OrderNumber} · {updated.Status}",
                        cancellationToken)
                    .ConfigureAwait(false);
                await PosRealtimeCustomerOrderPublish
                    .PublishAsync(
                        _realtime,
                        updated,
                        PosRealtimeEventTypes.CustomerOrderScheduleDeclined,
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

internal static class CustomerOrderScheduleProposalHelpers
{
    internal static string? FirstNonBlank(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }
}

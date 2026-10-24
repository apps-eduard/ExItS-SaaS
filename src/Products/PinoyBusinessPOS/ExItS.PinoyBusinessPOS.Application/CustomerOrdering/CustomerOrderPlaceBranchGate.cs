using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;

namespace ExItS.PinoyBusinessPOS.Application.CustomerOrdering;

/// <summary>
/// Branch capability and open/closed scheduling rules for placing a customer order.
/// </summary>
internal static class CustomerOrderPlaceBranchGate
{
    internal static ApplicationResult Validate(
        CustomerOrderBranchSnapshot branch,
        CustomerOrderFulfillmentType fulfillmentType,
        bool canCustomerDelivery,
        bool hasRequestedSchedule)
    {
        if (branch.OnlineOrdersPaused)
        {
            return ApplicationResult.Failure(
                ApplicationErrorCodes.CustomerOrderBranchCapability,
                "This store is temporarily not accepting online orders.");
        }

        if (!branch.CustomerOrderingEnabled)
        {
            return ApplicationResult.Failure(
                ApplicationErrorCodes.CustomerOrderOrderingUnavailable,
                "This store is not accepting online orders right now.");
        }

        if (fulfillmentType == CustomerOrderFulfillmentType.Delivery && !canCustomerDelivery)
        {
            return ApplicationResult.Failure(
                ApplicationErrorCodes.CustomerOrderOrderingUnavailable,
                "This merchant is not accepting delivery orders.");
        }

        if (fulfillmentType == CustomerOrderFulfillmentType.Pickup)
        {
            if (!IsPickupCapable(branch))
            {
                return ApplicationResult.Failure(
                    ApplicationErrorCodes.CustomerOrderBranchCapability,
                    branch.PickupEnabled
                        ? "Pickup is not available at this time."
                        : "Pickup is not enabled for this branch.");
            }
        }
        else if (!IsDeliveryCapable(branch))
        {
            return ApplicationResult.Failure(
                ApplicationErrorCodes.CustomerOrderBranchCapability,
                branch.DeliveryEnabled
                    ? "Delivery is not available at this time."
                    : "Delivery is not enabled for this branch.");
        }

        if (IsStoreClosed(branch))
        {
            if (!branch.AcceptScheduledOrdersWhileClosed)
            {
                return ApplicationResult.Failure(
                    ApplicationErrorCodes.CustomerOrderOrderingUnavailable,
                    "This store is not accepting online orders right now.");
            }

            if (!CanScheduleWhileClosed(branch, fulfillmentType, canCustomerDelivery))
            {
                return ApplicationResult.Failure(
                    ApplicationErrorCodes.CustomerOrderOrderingUnavailable,
                    "Scheduled orders are not available for this fulfillment method while the store is closed.");
            }

            if (!hasRequestedSchedule)
            {
                return ApplicationResult.Failure(
                    ApplicationErrorCodes.CustomerOrderOrderingUnavailable,
                    fulfillmentType == CustomerOrderFulfillmentType.Delivery
                        ? "This store is closed. Choose a delivery date and time to place your order."
                        : "This store is closed. Choose a pickup date and time to place your order.");
            }
        }

        return ApplicationResult.Success();
    }

    /// <summary>
    /// Closed by scheduled business hours (not paused / not ordering-disabled).
    /// When ordering is enabled and not paused, a non-operational branch is treated as closed-by-hours
    /// (<see cref="CustomerOrderBranchSnapshot.CustomerOrderingOperational"/> requires open-now).
    /// </summary>
    internal static bool IsStoreClosed(CustomerOrderBranchSnapshot branch) =>
        branch.CustomerOrderingEnabled
        && !branch.OnlineOrdersPaused
        && !branch.CustomerOrderingOperational;

    internal static bool CanScheduleWhileClosed(
        CustomerOrderBranchSnapshot branch,
        CustomerOrderFulfillmentType fulfillmentType,
        bool canCustomerDelivery)
    {
        if (!branch.CustomerOrderingEnabled
            || branch.OnlineOrdersPaused
            || !branch.AcceptScheduledOrdersWhileClosed)
        {
            return false;
        }

        return fulfillmentType == CustomerOrderFulfillmentType.Pickup
            ? IsPickupCapable(branch)
            : canCustomerDelivery && IsDeliveryCapable(branch);
    }

    internal static bool IsPickupCapable(CustomerOrderBranchSnapshot branch) =>
        branch.PickupEnabled && (!MethodReadyConfigured(branch) || branch.PickupReady);

    internal static bool IsDeliveryCapable(CustomerOrderBranchSnapshot branch) =>
        branch.DeliveryEnabled && (!MethodReadyConfigured(branch) || branch.DeliveryReady);

    private static bool MethodReadyConfigured(CustomerOrderBranchSnapshot branch) =>
        branch.PickupReady || branch.DeliveryReady;

    internal static bool HasRequestedSchedule(
        CustomerOrderFulfillmentType fulfillmentType,
        PlaceCustomerOrderRequest request)
    {
        if (fulfillmentType == CustomerOrderFulfillmentType.Pickup)
        {
            return !string.IsNullOrWhiteSpace(request.RequestedPickupDate)
                || !string.IsNullOrWhiteSpace(request.RequestedPickupTime);
        }

        return !string.IsNullOrWhiteSpace(request.RequestedDeliveryDate)
            || !string.IsNullOrWhiteSpace(request.RequestedDeliveryTime);
    }
}

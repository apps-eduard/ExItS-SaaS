using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;

public sealed partial class CustomerOrder
{
    private readonly List<CustomerOrderActivityEvent> _activityEvents = [];

    /// <summary>Merchandise subtotal at submit (before any seller line adjustments).</summary>
    public decimal OriginalMerchandiseSubtotal { get; private set; }

    /// <summary>Order total at submit (before any seller line adjustments).</summary>
    public decimal OriginalTotal { get; private set; }

    public CustomerOrderCancelReason? CancelReason { get; private set; }
    public string? CancelNotes { get; private set; }

    public IReadOnlyList<CustomerOrderActivityEvent> ActivityEvents => _activityEvents;

    /// <summary>
    /// Seller reduces and/or cancels lines under the disclosed partial-fulfillment policy.
    /// Does not require buyer approval. Allowed through Ready / ReadyForPickup (pickup-time
    /// customer-driven line changes). Paid stays Paid; overpayment is a refund/credit obligation.
    /// Returns reservation release deltas (productId → quantity to release) for the application layer.
    /// </summary>
    public IReadOnlyList<(Catalog.CatalogProductId ProductId, decimal ReleaseQuantity)> AdjustLines(
        IReadOnlyList<CustomerOrderLineAdjustment> adjustments,
        Guid actorId,
        DateTimeOffset utcNow)
    {
        SaleMoney.EnsureUtc(utcNow);
        EnsureActor(actorId);
        ArgumentNullException.ThrowIfNull(adjustments);

        if (adjustments.Count == 0)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderLineAdjustment,
                "Provide at least one line adjustment.");
        }

        EnsureCanAdjustLines();

        var previousTotal = Total;
        var wasPaid = PaymentStatus == CustomerOrderPaymentStatus.Paid;
        var releases = new List<(Catalog.CatalogProductId ProductId, decimal ReleaseQuantity)>();
        var touched = false;

        foreach (var adjustment in adjustments)
        {
            var line = _lines.FirstOrDefault(l => l.Id.Value == adjustment.LineId);
            if (line is null)
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidCustomerOrderLineAdjustment,
                    "One or more adjusted lines were not found on this order.");
            }

            var previousQty = line.Quantity;
            if (adjustment.NewQuantity == previousQty)
            {
                continue;
            }

            line.ApplySellerQuantityAdjustment(adjustment.NewQuantity, adjustment.Reason, adjustment.Note);
            touched = true;

            var released = previousQty - line.Quantity;
            if (released > 0m
                && StockReservationState == CustomerOrderStockReservationState.Reserved)
            {
                releases.Add((line.ProductId, released));
            }

            var action = line.IsCancelled
                ? CustomerOrderActivityEvent.Actions.ItemCancelled
                : CustomerOrderActivityEvent.Actions.QuantityReduced;
            var summary = line.IsCancelled
                ? $"{line.NameSnapshot} cancelled (was {FormatQty(previousQty, line)})."
                : $"{line.NameSnapshot} reduced from {FormatQty(previousQty, line)} to {FormatQty(line.Quantity, line)}.";

            _activityEvents.Add(new CustomerOrderActivityEvent(
                Guid.NewGuid(),
                utcNow,
                actorId,
                action,
                line.Id.Value,
                line.NameSnapshot,
                previousQty,
                line.Quantity,
                adjustment.Reason.ToString(),
                line.AdjustmentNote,
                previousTotal,
                null,
                summary));
        }

        if (!touched)
        {
            return [];
        }

        RecalculateTotalsFromActiveLines();

        _activityEvents.Add(new CustomerOrderActivityEvent(
            Guid.NewGuid(),
            utcNow,
            actorId,
            CustomerOrderActivityEvent.Actions.OrderTotalAdjusted,
            previousTotal: previousTotal,
            newTotal: Total,
            summary: $"Order total adjusted from {previousTotal:0.00} to {Total:0.00}."));

        // Paid stays Paid. Overpayment (AmountReceived − Total) is the explicit refund/credit obligation.
        if (wasPaid && AmountReceived is decimal received && received > Total)
        {
            var overpayment = SaleMoney.RoundMoney(received - Total);
            _activityEvents.Add(new CustomerOrderActivityEvent(
                Guid.NewGuid(),
                utcNow,
                actorId,
                CustomerOrderActivityEvent.Actions.OrderTotalAdjusted,
                previousTotal: previousTotal,
                newTotal: Total,
                summary: $"Paid order adjusted; refund/credit obligation {overpayment:0.00} (payment status remains Paid)."));
        }

        if (_lines.All(l => l.IsCancelled))
        {
            CancelInternal(
                CustomerOrderCancelReason.AllItemsCancelled,
                "All items were cancelled by the seller.",
                actorId,
                utcNow);
        }

        UpdatedAtUtc = utcNow;
        return releases;
    }

    /// <summary>Seller cancels an eligible active order (includes customer refusal at pickup/delivery).</summary>
    public void CancelBySeller(
        CustomerOrderCancelReason reason,
        string? notes,
        Guid actorId,
        DateTimeOffset utcNow)
    {
        SaleMoney.EnsureUtc(utcNow);
        EnsureActor(actorId);

        if (Status == CustomerOrderStatus.Cancelled)
        {
            return;
        }

        if (Status is CustomerOrderStatus.Completed or CustomerOrderStatus.Rejected)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderStatusTransition,
                "Completed or rejected orders cannot be cancelled.");
        }

        if (FulfillmentStatus is CustomerOrderFulfillmentStatus.Collected
            or CustomerOrderFulfillmentStatus.Delivered)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderStatusTransition,
                "Collected or delivered orders cannot be cancelled through ordinary actions.");
        }

        if (!Enum.IsDefined(reason))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderCancelReason,
                "Cancellation reason is invalid.");
        }

        if ((reason is CustomerOrderCancelReason.CustomerRefusedPickup
                or CustomerOrderCancelReason.CustomerRefusedDelivery
                or CustomerOrderCancelReason.Other)
            && string.IsNullOrWhiteSpace(notes))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderCancelReason,
                "Enter a seller note for this cancellation.");
        }

        EnsureSellerCancelAllowed(reason);
        CancelInternal(reason, notes, actorId, utcNow);
        var action = reason is CustomerOrderCancelReason.CustomerRefusedPickup
                or CustomerOrderCancelReason.CustomerRefusedDelivery
            ? CustomerOrderActivityEvent.Actions.CustomerRefused
            : CustomerOrderActivityEvent.Actions.OrderCancelled;
        _activityEvents.Add(new CustomerOrderActivityEvent(
            Guid.NewGuid(),
            utcNow,
            actorId,
            action,
            reasonCode: reason.ToString(),
            note: CancelNotes,
            summary: $"Order cancelled ({reason})."));
    }

    /// <summary>Buyer cancellation with lifecycle cutoff (Submitted always; Accepted only while Pending).</summary>
    public void CancelByBuyer(Guid actorId, DateTimeOffset utcNow)
    {
        SaleMoney.EnsureUtc(utcNow);
        EnsureActor(actorId);

        if (Status == CustomerOrderStatus.Cancelled)
        {
            return;
        }

        if (Status == CustomerOrderStatus.Submitted)
        {
            CancelInternal(CustomerOrderCancelReason.CustomerRequest, null, actorId, utcNow);
            _activityEvents.Add(new CustomerOrderActivityEvent(
                Guid.NewGuid(),
                utcNow,
                actorId,
                CustomerOrderActivityEvent.Actions.OrderCancelled,
                reasonCode: CustomerOrderCancelReason.CustomerRequest.ToString(),
                summary: "Customer cancelled the order."));
            return;
        }

        if (Status == CustomerOrderStatus.Accepted
            && FulfillmentStatus == CustomerOrderFulfillmentStatus.Pending)
        {
            CancelInternal(CustomerOrderCancelReason.CustomerRequest, null, actorId, utcNow);
            _activityEvents.Add(new CustomerOrderActivityEvent(
                Guid.NewGuid(),
                utcNow,
                actorId,
                CustomerOrderActivityEvent.Actions.OrderCancelled,
                reasonCode: CustomerOrderCancelReason.CustomerRequest.ToString(),
                summary: "Customer cancelled before preparation."));
            return;
        }

        throw new DomainException(
            DomainErrorCodes.InvalidCustomerOrderStatusTransition,
            Status == CustomerOrderStatus.Accepted && FulfillmentStatus == CustomerOrderFulfillmentStatus.Preparing
                ? "This order is already being prepared and cannot be cancelled directly."
                : "This order can no longer be cancelled directly.");
    }

    private void CancelInternal(
        CustomerOrderCancelReason reason,
        string? notes,
        Guid actorId,
        DateTimeOffset utcNow)
    {
        Status = CustomerOrderStatus.Cancelled;
        CancelReason = reason;
        CancelNotes = NormalizeRejectNotes(notes);
        CancelledAtUtc = utcNow;
        CancelledBy = actorId;
        PendingScheduleProposal = null;
        UpdatedAtUtc = utcNow;
    }

    private void EnsureCanAdjustLines()
    {
        if (Status is CustomerOrderStatus.Completed
            or CustomerOrderStatus.Rejected
            or CustomerOrderStatus.Cancelled
            or CustomerOrderStatus.Draft)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderStatusTransition,
                "This order can no longer be adjusted.");
        }

        // Pickup Ready stays adjustable (customer may refuse or drop lines at handoff).
        // Close after the order leaves the counter / leaves for delivery.
        if (FulfillmentStatus is CustomerOrderFulfillmentStatus.OutForDelivery
            or CustomerOrderFulfillmentStatus.Collected
            or CustomerOrderFulfillmentStatus.Delivered)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderStatusTransition,
                "Lines cannot be adjusted after the order is out for delivery, delivered, or collected.");
        }
    }

    private void EnsureSellerCancelAllowed(CustomerOrderCancelReason reason)
    {
        if (FulfillmentStatus == CustomerOrderFulfillmentStatus.ReadyForPickup)
        {
            if (reason is not (CustomerOrderCancelReason.CustomerRefusedPickup
                or CustomerOrderCancelReason.Other))
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidCustomerOrderCancelReason,
                    "Ready-for-pickup cancellations require Customer Refused Pickup (or Other with a seller note).");
            }

            return;
        }

        if (FulfillmentStatus == CustomerOrderFulfillmentStatus.Ready)
        {
            if (reason is not (CustomerOrderCancelReason.CustomerRefusedDelivery
                or CustomerOrderCancelReason.SellerUnableToFulfill
                or CustomerOrderCancelReason.Other))
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidCustomerOrderCancelReason,
                    "Ready delivery cancellations require an authorized delivery exception reason.");
            }

            return;
        }

        if (FulfillmentStatus == CustomerOrderFulfillmentStatus.OutForDelivery)
        {
            if (reason is not (CustomerOrderCancelReason.CustomerRefusedDelivery
                or CustomerOrderCancelReason.SellerUnableToFulfill
                or CustomerOrderCancelReason.Other))
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidCustomerOrderCancelReason,
                    "Out-for-delivery cancellations require an authorized delivery exception reason.");
            }
        }
    }

    private void RecalculateTotalsFromActiveLines()
    {
        var merchandise = SaleMoney.RoundMoney(_lines.Where(l => !l.IsCancelled).Sum(l => l.LineTotal));
        // Never increase delivery fee; keep quoted fee while any line remains active.
        var deliveryFee = _lines.Any(l => !l.IsCancelled) ? DeliveryFee : 0m;
        var total = SaleMoney.RoundMoney(merchandise + deliveryFee);
        if (total > MaxTotal)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderTotal,
                "The customer order total is too large.");
        }

        MerchandiseSubtotal = merchandise;
        DeliveryFee = deliveryFee;
        Total = total;
    }

    private static string FormatQty(decimal quantity, CustomerOrderLine line) =>
        $"{quantity:0.###} {line.UnitSnapshot}";

    internal void SeedOriginalTotalsIfUnset()
    {
        if (OriginalMerchandiseSubtotal == 0m && OriginalTotal == 0m && MerchandiseSubtotal > 0m)
        {
            OriginalMerchandiseSubtotal = MerchandiseSubtotal;
            OriginalTotal = Total;
        }
    }

    internal void ReplaceActivityEvents(IEnumerable<CustomerOrderActivityEvent> events)
    {
        _activityEvents.Clear();
        _activityEvents.AddRange(events);
    }

    internal void SetOriginalTotals(decimal originalMerchandiseSubtotal, decimal originalTotal)
    {
        OriginalMerchandiseSubtotal = originalMerchandiseSubtotal;
        OriginalTotal = originalTotal;
    }
}

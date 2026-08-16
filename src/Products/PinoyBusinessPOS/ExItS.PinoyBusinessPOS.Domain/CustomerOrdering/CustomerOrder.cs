using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;

/// <summary>
/// Seller-organization-owned customer order for pickup or local delivery.
/// Distinct from purchasing <c>PurchaseOrder</c> / connected-supplier flows.
/// </summary>
public sealed class CustomerOrder
{
    public const int MaxLineCount = 200;
    public const int BranchNameSnapshotMaxLength = 128;
    public const int RejectNotesMaxLength = 512;
    public const int IdempotencyKeyMaxLength = 128;
    public const decimal MaxTotal = 999_999_999.99m;

    private readonly List<CustomerOrderLine> _lines;

    public CustomerOrderId Id { get; }
    public PosOrganizationId SellerOrganizationId { get; }
    public string OrderNumber { get; }
    public CustomerOrderStatus Status { get; private set; }
    public CustomerOrderFulfillmentStatus FulfillmentStatus { get; private set; }
    public CustomerOrderPaymentStatus PaymentStatus { get; private set; }
    public CustomerOrderFulfillmentType FulfillmentType { get; }
    public Guid FulfillmentBranchId { get; }
    public string BranchNameSnapshot { get; }
    public CustomerOrderParty CustomerParty { get; }
    public IReadOnlyList<CustomerOrderLine> Lines => _lines;
    public decimal MerchandiseSubtotal { get; }
    public decimal DeliveryFee { get; }
    public decimal Total { get; }
    public CustomerOrderDeliverySnapshot? DeliverySnapshot { get; }
    public CustomerOrderStockReservationState StockReservationState { get; private set; }
    public CustomerOrderRejectReason? RejectReason { get; private set; }
    public string? RejectNotes { get; private set; }
    public string? IdempotencyKey { get; }

    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset? SubmittedAtUtc { get; private set; }
    public Guid? SubmittedBy { get; private set; }
    public DateTimeOffset? AcceptedAtUtc { get; private set; }
    public Guid? AcceptedBy { get; private set; }
    public DateTimeOffset? RejectedAtUtc { get; private set; }
    public Guid? RejectedBy { get; private set; }
    public DateTimeOffset? CancelledAtUtc { get; private set; }
    public Guid? CancelledBy { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public Guid? CompletedBy { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private CustomerOrder(
        CustomerOrderId id,
        PosOrganizationId sellerOrganizationId,
        string orderNumber,
        CustomerOrderStatus status,
        CustomerOrderFulfillmentStatus fulfillmentStatus,
        CustomerOrderPaymentStatus paymentStatus,
        CustomerOrderFulfillmentType fulfillmentType,
        Guid fulfillmentBranchId,
        string branchNameSnapshot,
        CustomerOrderParty customerParty,
        List<CustomerOrderLine> lines,
        decimal merchandiseSubtotal,
        decimal deliveryFee,
        decimal total,
        CustomerOrderDeliverySnapshot? deliverySnapshot,
        CustomerOrderStockReservationState stockReservationState,
        CustomerOrderRejectReason? rejectReason,
        string? rejectNotes,
        string? idempotencyKey,
        DateTimeOffset createdAtUtc,
        DateTimeOffset? submittedAtUtc,
        Guid? submittedBy,
        DateTimeOffset? acceptedAtUtc,
        Guid? acceptedBy,
        DateTimeOffset? rejectedAtUtc,
        Guid? rejectedBy,
        DateTimeOffset? cancelledAtUtc,
        Guid? cancelledBy,
        DateTimeOffset? completedAtUtc,
        Guid? completedBy,
        DateTimeOffset updatedAtUtc)
    {
        Id = id;
        SellerOrganizationId = sellerOrganizationId;
        OrderNumber = orderNumber;
        Status = status;
        FulfillmentStatus = fulfillmentStatus;
        PaymentStatus = paymentStatus;
        FulfillmentType = fulfillmentType;
        FulfillmentBranchId = fulfillmentBranchId;
        BranchNameSnapshot = branchNameSnapshot;
        CustomerParty = customerParty;
        _lines = lines;
        MerchandiseSubtotal = merchandiseSubtotal;
        DeliveryFee = deliveryFee;
        Total = total;
        DeliverySnapshot = deliverySnapshot;
        StockReservationState = stockReservationState;
        RejectReason = rejectReason;
        RejectNotes = rejectNotes;
        IdempotencyKey = idempotencyKey;
        CreatedAtUtc = createdAtUtc;
        SubmittedAtUtc = submittedAtUtc;
        SubmittedBy = submittedBy;
        AcceptedAtUtc = acceptedAtUtc;
        AcceptedBy = acceptedBy;
        RejectedAtUtc = rejectedAtUtc;
        RejectedBy = rejectedBy;
        CancelledAtUtc = cancelledAtUtc;
        CancelledBy = cancelledBy;
        CompletedAtUtc = completedAtUtc;
        CompletedBy = completedBy;
        UpdatedAtUtc = updatedAtUtc;
    }

    /// <summary>
    /// V1 factory: creates the order already in <see cref="CustomerOrderStatus.Submitted"/>.
    /// </summary>
    public static CustomerOrder CreateSubmitted(
        PosOrganizationId sellerOrganizationId,
        string orderNumber,
        CustomerOrderParty customerParty,
        CustomerOrderFulfillmentType fulfillmentType,
        Guid fulfillmentBranchId,
        string branchNameSnapshot,
        IReadOnlyList<CustomerOrderLineDraft> lines,
        Guid submittedBy,
        DateTimeOffset utcNow,
        CustomerOrderDeliverySnapshot? deliverySnapshot = null,
        string? idempotencyKey = null,
        CustomerOrderId? id = null)
    {
        SaleMoney.EnsureUtc(utcNow);
        EnsureActor(submittedBy);
        ArgumentNullException.ThrowIfNull(customerParty);
        customerParty.EnsureConsistent();

        if (fulfillmentBranchId == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderBranch,
                "Fulfillment branch id cannot be empty.");
        }

        if (lines is null || lines.Count == 0)
        {
            throw new DomainException(
                DomainErrorCodes.CustomerOrderRequiresAtLeastOneLine,
                "A customer order must contain at least one line.");
        }

        if (lines.Count > MaxLineCount)
        {
            throw new DomainException(
                DomainErrorCodes.CustomerOrderRequiresAtLeastOneLine,
                $"A customer order may contain at most {MaxLineCount} lines.");
        }

        ValidateFulfillment(fulfillmentType, deliverySnapshot);

        var orderId = id ?? CustomerOrderId.New();
        var orderLines = new List<CustomerOrderLine>(lines.Count);
        for (var i = 0; i < lines.Count; i++)
        {
            orderLines.Add(CustomerOrderLine.Create(orderId, i + 1, lines[i]));
        }

        var merchandiseSubtotal = SaleMoney.RoundMoney(orderLines.Sum(l => l.LineTotal));
        var deliveryFee = fulfillmentType == CustomerOrderFulfillmentType.Delivery
            ? deliverySnapshot!.FinalDeliveryFee
            : 0m;
        var total = SaleMoney.RoundMoney(merchandiseSubtotal + deliveryFee);

        if (merchandiseSubtotal < 0m || deliveryFee < 0m || total < 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderTotal,
                "Order money amounts cannot be negative.");
        }

        if (total > MaxTotal)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderTotal,
                "The customer order total is too large.");
        }

        if (SaleMoney.RoundMoney(merchandiseSubtotal + deliveryFee) != total)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderTotal,
                "Merchandise subtotal plus delivery fee must equal total.");
        }

        if (fulfillmentType == CustomerOrderFulfillmentType.Delivery
            && deliverySnapshot!.FinalDeliveryFee != deliveryFee)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderDeliveryFee,
                "Delivery fee must match the delivery snapshot final fee.");
        }

        return new CustomerOrder(
            orderId,
            sellerOrganizationId,
            CustomerOrderNumbers.Normalize(orderNumber),
            CustomerOrderStatus.Submitted,
            CustomerOrderFulfillmentStatus.Pending,
            CustomerOrderPaymentStatus.Unpaid,
            fulfillmentType,
            fulfillmentBranchId,
            NormalizeBranchName(branchNameSnapshot),
            customerParty,
            orderLines,
            merchandiseSubtotal,
            deliveryFee,
            total,
            deliverySnapshot,
            CustomerOrderStockReservationState.None,
            rejectReason: null,
            rejectNotes: null,
            NormalizeIdempotencyKey(idempotencyKey),
            utcNow,
            utcNow,
            submittedBy,
            acceptedAtUtc: null,
            acceptedBy: null,
            rejectedAtUtc: null,
            rejectedBy: null,
            cancelledAtUtc: null,
            cancelledBy: null,
            completedAtUtc: null,
            completedBy: null,
            utcNow);
    }

    public static CustomerOrder Rehydrate(
        CustomerOrderId id,
        PosOrganizationId sellerOrganizationId,
        string orderNumber,
        CustomerOrderStatus status,
        CustomerOrderFulfillmentStatus fulfillmentStatus,
        CustomerOrderPaymentStatus paymentStatus,
        CustomerOrderFulfillmentType fulfillmentType,
        Guid fulfillmentBranchId,
        string branchNameSnapshot,
        CustomerOrderParty customerParty,
        IEnumerable<CustomerOrderLine> lines,
        decimal merchandiseSubtotal,
        decimal deliveryFee,
        decimal total,
        CustomerOrderDeliverySnapshot? deliverySnapshot,
        CustomerOrderStockReservationState stockReservationState,
        CustomerOrderRejectReason? rejectReason,
        string? rejectNotes,
        string? idempotencyKey,
        DateTimeOffset createdAtUtc,
        DateTimeOffset? submittedAtUtc,
        Guid? submittedBy,
        DateTimeOffset? acceptedAtUtc,
        Guid? acceptedBy,
        DateTimeOffset? rejectedAtUtc,
        Guid? rejectedBy,
        DateTimeOffset? cancelledAtUtc,
        Guid? cancelledBy,
        DateTimeOffset? completedAtUtc,
        Guid? completedBy,
        DateTimeOffset updatedAtUtc) =>
        new(
            id,
            sellerOrganizationId,
            orderNumber,
            status,
            fulfillmentStatus,
            paymentStatus,
            fulfillmentType,
            fulfillmentBranchId,
            branchNameSnapshot,
            customerParty,
            lines.OrderBy(l => l.LineNumber).ToList(),
            merchandiseSubtotal,
            deliveryFee,
            total,
            deliverySnapshot,
            stockReservationState,
            rejectReason,
            rejectNotes,
            idempotencyKey,
            createdAtUtc,
            submittedAtUtc,
            submittedBy,
            acceptedAtUtc,
            acceptedBy,
            rejectedAtUtc,
            rejectedBy,
            cancelledAtUtc,
            cancelledBy,
            completedAtUtc,
            completedBy,
            updatedAtUtc);

    public void Accept(Guid actorId, DateTimeOffset utcNow)
    {
        SaleMoney.EnsureUtc(utcNow);
        EnsureActor(actorId);

        if (Status == CustomerOrderStatus.Accepted
            && FulfillmentStatus == CustomerOrderFulfillmentStatus.Preparing)
        {
            return;
        }

        EnsureStatus(CustomerOrderStatus.Submitted, "Only submitted orders can be accepted.");

        Status = CustomerOrderStatus.Accepted;
        FulfillmentStatus = CustomerOrderFulfillmentStatus.Preparing;
        AcceptedAtUtc = utcNow;
        AcceptedBy = actorId;
        UpdatedAtUtc = utcNow;
    }

    public void Reject(
        CustomerOrderRejectReason reason,
        string? notes,
        Guid actorId,
        DateTimeOffset utcNow)
    {
        SaleMoney.EnsureUtc(utcNow);
        EnsureActor(actorId);

        if (Status == CustomerOrderStatus.Rejected)
        {
            return;
        }

        EnsureStatus(CustomerOrderStatus.Submitted, "Only submitted orders can be rejected.");

        if (!Enum.IsDefined(reason))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderRejectReason,
                "Reject reason is invalid.");
        }

        Status = CustomerOrderStatus.Rejected;
        RejectReason = reason;
        RejectNotes = NormalizeRejectNotes(notes);
        RejectedAtUtc = utcNow;
        RejectedBy = actorId;
        UpdatedAtUtc = utcNow;
    }

    public void Cancel(Guid actorId, DateTimeOffset utcNow)
    {
        SaleMoney.EnsureUtc(utcNow);
        EnsureActor(actorId);

        if (Status == CustomerOrderStatus.Cancelled)
        {
            return;
        }

        if (Status is not (CustomerOrderStatus.Draft or CustomerOrderStatus.Submitted))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderStatusTransition,
                "Only draft or submitted orders can be cancelled by the customer.");
        }

        Status = CustomerOrderStatus.Cancelled;
        CancelledAtUtc = utcNow;
        CancelledBy = actorId;
        UpdatedAtUtc = utcNow;
    }

    public void StartPreparing(DateTimeOffset utcNow)
    {
        SaleMoney.EnsureUtc(utcNow);

        if (FulfillmentStatus == CustomerOrderFulfillmentStatus.Preparing)
        {
            return;
        }

        EnsureStatus(CustomerOrderStatus.Accepted, "Only accepted orders can start preparing.");
        throw new DomainException(
            DomainErrorCodes.InvalidCustomerOrderFulfillmentTransition,
            "Accepted orders should already be preparing; unexpected fulfillment status.");
    }

    public void MarkReady(DateTimeOffset utcNow)
    {
        SaleMoney.EnsureUtc(utcNow);

        var target = FulfillmentType == CustomerOrderFulfillmentType.Delivery
            ? CustomerOrderFulfillmentStatus.Ready
            : CustomerOrderFulfillmentStatus.ReadyForPickup;

        if (FulfillmentStatus == target)
        {
            return;
        }

        EnsureStatus(CustomerOrderStatus.Accepted, "Only accepted orders can be marked ready.");
        EnsureFulfillment(CustomerOrderFulfillmentStatus.Preparing, "Only preparing orders can be marked ready.");

        FulfillmentStatus = target;
        UpdatedAtUtc = utcNow;
    }

    public void MarkOutForDelivery(DateTimeOffset utcNow)
    {
        SaleMoney.EnsureUtc(utcNow);

        if (FulfillmentType != CustomerOrderFulfillmentType.Delivery)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderFulfillmentTransition,
                "Out-for-delivery applies only to delivery orders.");
        }

        if (FulfillmentStatus == CustomerOrderFulfillmentStatus.OutForDelivery)
        {
            return;
        }

        EnsureStatus(CustomerOrderStatus.Accepted, "Only accepted orders can go out for delivery.");
        EnsureFulfillment(CustomerOrderFulfillmentStatus.Ready, "Only ready delivery orders can go out for delivery.");

        FulfillmentStatus = CustomerOrderFulfillmentStatus.OutForDelivery;
        UpdatedAtUtc = utcNow;
    }

    public void MarkDelivered(DateTimeOffset utcNow)
    {
        SaleMoney.EnsureUtc(utcNow);

        if (FulfillmentType != CustomerOrderFulfillmentType.Delivery)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderFulfillmentTransition,
                "Delivered applies only to delivery orders.");
        }

        if (FulfillmentStatus == CustomerOrderFulfillmentStatus.Delivered)
        {
            return;
        }

        EnsureStatus(CustomerOrderStatus.Accepted, "Only accepted orders can be marked delivered.");
        EnsureFulfillment(
            CustomerOrderFulfillmentStatus.OutForDelivery,
            "Only out-for-delivery orders can be marked delivered.");

        FulfillmentStatus = CustomerOrderFulfillmentStatus.Delivered;
        UpdatedAtUtc = utcNow;
    }

    public void MarkCollected(DateTimeOffset utcNow)
    {
        SaleMoney.EnsureUtc(utcNow);

        if (FulfillmentType != CustomerOrderFulfillmentType.Pickup)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderFulfillmentTransition,
                "Collected applies only to pickup orders.");
        }

        if (FulfillmentStatus == CustomerOrderFulfillmentStatus.Collected)
        {
            return;
        }

        EnsureStatus(CustomerOrderStatus.Accepted, "Only accepted orders can be marked collected.");
        EnsureFulfillment(
            CustomerOrderFulfillmentStatus.ReadyForPickup,
            "Only ready-for-pickup orders can be marked collected.");

        FulfillmentStatus = CustomerOrderFulfillmentStatus.Collected;
        UpdatedAtUtc = utcNow;
    }

    public void Complete(Guid actorId, DateTimeOffset utcNow)
    {
        SaleMoney.EnsureUtc(utcNow);
        EnsureActor(actorId);

        if (Status == CustomerOrderStatus.Completed)
        {
            return;
        }

        EnsureStatus(CustomerOrderStatus.Accepted, "Only accepted orders can be completed.");

        if (FulfillmentStatus is not (CustomerOrderFulfillmentStatus.Delivered or CustomerOrderFulfillmentStatus.Collected))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderFulfillmentTransition,
                "Only delivered or collected orders can be completed.");
        }

        Status = CustomerOrderStatus.Completed;
        CompletedAtUtc = utcNow;
        CompletedBy = actorId;
        UpdatedAtUtc = utcNow;
    }

    public void MarkStockReserved(DateTimeOffset utcNow)
    {
        SaleMoney.EnsureUtc(utcNow);

        if (StockReservationState == CustomerOrderStockReservationState.Reserved)
        {
            return;
        }

        if (StockReservationState != CustomerOrderStockReservationState.None)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderStockReservation,
                "Stock can only be reserved from the None state.");
        }

        if (Status is not (CustomerOrderStatus.Accepted or CustomerOrderStatus.Submitted))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderStockReservation,
                "Stock reservation is not allowed in the current order status.");
        }

        StockReservationState = CustomerOrderStockReservationState.Reserved;
        UpdatedAtUtc = utcNow;
    }

    public void MarkStockReleased(DateTimeOffset utcNow)
    {
        SaleMoney.EnsureUtc(utcNow);

        if (StockReservationState == CustomerOrderStockReservationState.Released)
        {
            return;
        }

        if (StockReservationState != CustomerOrderStockReservationState.Reserved)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderStockReservation,
                "Stock can only be released from the Reserved state.");
        }

        StockReservationState = CustomerOrderStockReservationState.Released;
        UpdatedAtUtc = utcNow;
    }

    public void MarkStockConsumed(DateTimeOffset utcNow)
    {
        SaleMoney.EnsureUtc(utcNow);

        if (StockReservationState == CustomerOrderStockReservationState.Consumed)
        {
            return;
        }

        if (StockReservationState != CustomerOrderStockReservationState.Reserved)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderStockReservation,
                "Stock can only be consumed from the Reserved state.");
        }

        StockReservationState = CustomerOrderStockReservationState.Consumed;
        UpdatedAtUtc = utcNow;
    }

    private static void ValidateFulfillment(
        CustomerOrderFulfillmentType fulfillmentType,
        CustomerOrderDeliverySnapshot? deliverySnapshot)
    {
        switch (fulfillmentType)
        {
            case CustomerOrderFulfillmentType.Pickup:
                if (deliverySnapshot is not null)
                {
                    throw new DomainException(
                        DomainErrorCodes.InvalidCustomerOrderDelivery,
                        "Pickup orders must not carry a delivery snapshot.");
                }

                break;

            case CustomerOrderFulfillmentType.Delivery:
                if (deliverySnapshot is null)
                {
                    throw new DomainException(
                        DomainErrorCodes.InvalidCustomerOrderDelivery,
                        "Delivery orders require a delivery snapshot.");
                }

                break;

            default:
                throw new DomainException(
                    DomainErrorCodes.InvalidCustomerOrderFulfillmentType,
                    "Fulfillment type is invalid.");
        }
    }

    private void EnsureStatus(CustomerOrderStatus required, string message)
    {
        if (Status != required)
        {
            throw new DomainException(DomainErrorCodes.InvalidCustomerOrderStatusTransition, message);
        }
    }

    private void EnsureFulfillment(CustomerOrderFulfillmentStatus required, string message)
    {
        if (FulfillmentStatus != required)
        {
            throw new DomainException(DomainErrorCodes.InvalidCustomerOrderFulfillmentTransition, message);
        }
    }

    private static void EnsureActor(Guid actorId)
    {
        if (actorId == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderActor,
                "A non-empty actor identifier is required.");
        }
    }

    private static string NormalizeBranchName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderBranch,
                "Branch name snapshot is required.");
        }

        var trimmed = value.Trim();
        if (trimmed.Length > BranchNameSnapshotMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderBranch,
                $"Branch name snapshot must be at most {BranchNameSnapshotMaxLength} characters.");
        }

        return trimmed;
    }

    private static string? NormalizeRejectNotes(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
        {
            return null;
        }

        var trimmed = notes.Trim();
        if (trimmed.Length > RejectNotesMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderRejectNotes,
                $"Reject notes must be at most {RejectNotesMaxLength} characters.");
        }

        return trimmed;
    }

    private static string? NormalizeIdempotencyKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        var trimmed = key.Trim();
        if (trimmed.Length > IdempotencyKeyMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderIdempotencyKey,
                $"Idempotency key must be at most {IdempotencyKeyMaxLength} characters.");
        }

        return trimmed;
    }
}

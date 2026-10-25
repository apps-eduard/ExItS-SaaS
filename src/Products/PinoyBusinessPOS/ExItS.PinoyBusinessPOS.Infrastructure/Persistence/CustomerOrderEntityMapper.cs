using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.CustomerOrdering;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence;

internal static class CustomerOrderEntityMapper
{
    public static CustomerOrder ToDomain(CustomerOrderRecord record, IEnumerable<CustomerOrderLineRecord> lineRecords)
    {
        var orderId = CustomerOrderId.From(record.Id);
        var sellerOrgId = PosOrganizationId.From(record.SellerOrganizationId);

        var lines = lineRecords
            .OrderBy(l => l.LineNumber)
            .Select(l => CustomerOrderLine.Rehydrate(
                CustomerOrderLineId.From(l.Id),
                orderId,
                CatalogProductId.From(l.ProductId),
                l.LineNumber,
                l.NameSnapshot,
                l.SkuSnapshot,
                UnitOfMeasures.Parse(l.UnitSnapshot),
                l.Quantity,
                l.UnitPrice,
                l.Discount,
                l.LineTotal,
                l.OriginalQuantity > 0m ? l.OriginalQuantity : l.Quantity,
                l.OriginalDiscount > 0m || l.Discount == 0m ? l.OriginalDiscount : l.Discount,
                string.IsNullOrWhiteSpace(l.AdjustmentReason)
                    ? null
                    : Enum.Parse<CustomerOrderLineAdjustmentReason>(l.AdjustmentReason, ignoreCase: true),
                l.AdjustmentNote))
            .ToList();

        // Incomplete Delivery rows must not take down list/detail queries (NRE → HTTP 500).
        // Prefer returning the order without a snapshot over failing the whole "My orders" page.
        CustomerOrderDeliverySnapshot? delivery = TryMapDeliverySnapshot(record);

        return CustomerOrder.Rehydrate(
            orderId,
            sellerOrgId,
            record.OrderNumber,
            Enum.Parse<CustomerOrderStatus>(record.Status, ignoreCase: true),
            Enum.Parse<CustomerOrderFulfillmentStatus>(record.FulfillmentStatus, ignoreCase: true),
            Enum.Parse<CustomerOrderPaymentStatus>(record.PaymentStatus, ignoreCase: true),
            string.IsNullOrWhiteSpace(record.PaymentMethod)
                ? CustomerOrderPaymentMethod.Cash
                : CustomerOrderPaymentMethods.Parse(record.PaymentMethod),
            Enum.Parse<CustomerOrderFulfillmentType>(record.FulfillmentType, ignoreCase: true),
            record.FulfillmentBranchId,
            record.BranchNameSnapshot,
            CustomerOrderParty.Rehydrate(
                Enum.Parse<CustomerPartyType>(record.CustomerPartyType, ignoreCase: true),
                record.CustomerDisplayNameSnapshot,
                record.CustomerPlatformUserId,
            record.CustomerBuyerOrganizationId,
            record.CustomerBuyerPublicOrganizationId),
            lines,
            record.MerchandiseSubtotal,
            record.DeliveryFee,
            record.Total,
            delivery,
            Enum.Parse<CustomerOrderStockReservationState>(record.StockReservationState, ignoreCase: true),
            string.IsNullOrWhiteSpace(record.RejectReason)
                ? null
                : Enum.Parse<CustomerOrderRejectReason>(record.RejectReason, ignoreCase: true),
            record.RejectNotes,
            record.IdempotencyKey,
            record.CreatedAtUtc,
            record.SubmittedAtUtc,
            record.SubmittedBy,
            record.AcceptedAtUtc,
            record.AcceptedBy,
            record.RejectedAtUtc,
            record.RejectedBy,
            record.CancelledAtUtc,
            record.CancelledBy,
            record.CompletedAtUtc,
            record.CompletedBy,
            record.ReadyAtUtc,
            record.ReadyBy,
            record.OutForDeliveryAtUtc,
            record.OutForDeliveryBy,
            record.DeliveredAtUtc,
            record.DeliveredBy,
            record.CollectedAtUtc,
            record.CollectedBy,
            record.UpdatedAtUtc,
            record.PlatformBusinessCustomerId,
            record.PaymentReference,
            record.RequestedPickupLocal,
            record.RequestedPickupTimeZoneId,
            record.RequestedPickupAtUtc,
            record.RequestedDeliveryLocal,
            record.RequestedDeliveryTimeZoneId,
            record.RequestedDeliveryAtUtc,
            record.PaymentConfirmedAtUtc,
            record.PaymentConfirmedBy,
            record.AmountReceived,
            record.ConfirmedFulfillmentLocal,
            record.ConfirmedFulfillmentTimeZoneId,
            record.ConfirmedFulfillmentAtUtc,
            MapPendingProposal(record),
            record.OriginalMerchandiseSubtotal > 0m ? record.OriginalMerchandiseSubtotal : record.MerchandiseSubtotal,
            record.OriginalTotal > 0m ? record.OriginalTotal : record.Total,
            string.IsNullOrWhiteSpace(record.CancelReason)
                ? null
                : Enum.Parse<CustomerOrderCancelReason>(record.CancelReason, ignoreCase: true),
            record.CancelNotes,
            CustomerOrderActivityJson.Deserialize(record.ActivityEventsJson),
            record.PreferredFulfillmentDate,
            string.IsNullOrWhiteSpace(record.PreferredFulfillmentPeriod)
                ? null
                : Enum.Parse<CustomerOrderPreferredFulfillmentPeriod>(record.PreferredFulfillmentPeriod, ignoreCase: true),
            record.PreferredFulfillmentTimeZoneId,
            record.PreferredFulfillmentWindowStartUtc,
            record.PreferredFulfillmentWindowEndUtc);
    }

    private static CustomerOrderScheduleProposal? MapPendingProposal(CustomerOrderRecord record)
    {
        if (record.PendingScheduleProposalId is not Guid proposalId
            || string.IsNullOrWhiteSpace(record.PendingScheduleProposalLocal)
            || string.IsNullOrWhiteSpace(record.PendingScheduleProposalTimeZoneId)
            || record.PendingScheduleProposalAtUtc is null
            || record.PendingScheduleProposalBy is null
            || record.PendingScheduleProposalProposedAtUtc is null)
        {
            return null;
        }

        return new CustomerOrderScheduleProposal(
            proposalId,
            record.PendingScheduleProposalLocal,
            record.PendingScheduleProposalTimeZoneId,
            record.PendingScheduleProposalAtUtc.Value,
            record.PendingScheduleProposalNote,
            record.PendingScheduleProposalBy.Value,
            record.PendingScheduleProposalProposedAtUtc.Value);
    }

    public static CustomerOrderRecord ToRecord(CustomerOrder order)
    {
        var record = new CustomerOrderRecord
        {
            Id = order.Id.Value,
            SellerOrganizationId = order.SellerOrganizationId.Value,
            OrderNumber = order.OrderNumber,
            Status = order.Status.ToString(),
            FulfillmentStatus = order.FulfillmentStatus.ToString(),
            PaymentStatus = order.PaymentStatus.ToString(),
            PaymentMethod = CustomerOrderPaymentMethods.ToCode(order.PaymentMethod),
            PaymentReference = order.PaymentReference,
            AmountReceived = order.AmountReceived,
            PaymentConfirmedAtUtc = order.PaymentConfirmedAtUtc,
            PaymentConfirmedBy = order.PaymentConfirmedBy,
            RequestedPickupLocal = order.RequestedPickupLocal,
            RequestedPickupTimeZoneId = order.RequestedPickupTimeZoneId,
            RequestedPickupAtUtc = order.RequestedPickupAtUtc,
            RequestedDeliveryLocal = order.RequestedDeliveryLocal,
            RequestedDeliveryTimeZoneId = order.RequestedDeliveryTimeZoneId,
            RequestedDeliveryAtUtc = order.RequestedDeliveryAtUtc,
            PreferredFulfillmentDate = order.PreferredFulfillmentDate,
            PreferredFulfillmentPeriod = order.PreferredFulfillmentPeriod?.ToString(),
            PreferredFulfillmentTimeZoneId = order.PreferredFulfillmentTimeZoneId,
            PreferredFulfillmentWindowStartUtc = order.PreferredFulfillmentWindowStartUtc,
            PreferredFulfillmentWindowEndUtc = order.PreferredFulfillmentWindowEndUtc,
            ConfirmedFulfillmentLocal = order.ConfirmedFulfillmentLocal,
            ConfirmedFulfillmentTimeZoneId = order.ConfirmedFulfillmentTimeZoneId,
            ConfirmedFulfillmentAtUtc = order.ConfirmedFulfillmentAtUtc,
            PendingScheduleProposalId = order.PendingScheduleProposal?.ProposalId,
            PendingScheduleProposalLocal = order.PendingScheduleProposal?.Local,
            PendingScheduleProposalTimeZoneId = order.PendingScheduleProposal?.TimeZoneId,
            PendingScheduleProposalAtUtc = order.PendingScheduleProposal?.AtUtc,
            PendingScheduleProposalNote = order.PendingScheduleProposal?.Note,
            PendingScheduleProposalBy = order.PendingScheduleProposal?.ProposedBy,
            PendingScheduleProposalProposedAtUtc = order.PendingScheduleProposal?.ProposedAtUtc,
            FulfillmentType = order.FulfillmentType.ToString(),
            FulfillmentBranchId = order.FulfillmentBranchId,
            BranchNameSnapshot = order.BranchNameSnapshot,
            CustomerPartyType = order.CustomerParty.PartyType.ToString(),
            CustomerDisplayNameSnapshot = order.CustomerParty.DisplayNameSnapshot,
            CustomerPlatformUserId = order.CustomerParty.PlatformUserId,
            PlatformBusinessCustomerId = order.PlatformBusinessCustomerId,
            CustomerBuyerOrganizationId = order.CustomerParty.BuyerOrganizationId,
            CustomerBuyerPublicOrganizationId = order.CustomerParty.BuyerPublicOrganizationId,
            MerchandiseSubtotal = order.MerchandiseSubtotal,
            DeliveryFee = order.DeliveryFee,
            Total = order.Total,
            OriginalMerchandiseSubtotal = order.OriginalMerchandiseSubtotal,
            OriginalTotal = order.OriginalTotal,
            CancelReason = order.CancelReason?.ToString(),
            CancelNotes = order.CancelNotes,
            ActivityEventsJson = CustomerOrderActivityJson.Serialize(order.ActivityEvents),
            StockReservationState = order.StockReservationState.ToString(),
            RejectReason = order.RejectReason?.ToString(),
            RejectNotes = order.RejectNotes,
            IdempotencyKey = order.IdempotencyKey,
            CreatedAtUtc = order.CreatedAtUtc,
            SubmittedAtUtc = order.SubmittedAtUtc,
            SubmittedBy = order.SubmittedBy,
            AcceptedAtUtc = order.AcceptedAtUtc,
            AcceptedBy = order.AcceptedBy,
            RejectedAtUtc = order.RejectedAtUtc,
            RejectedBy = order.RejectedBy,
            CancelledAtUtc = order.CancelledAtUtc,
            CancelledBy = order.CancelledBy,
            CompletedAtUtc = order.CompletedAtUtc,
            CompletedBy = order.CompletedBy,
            ReadyAtUtc = order.ReadyAtUtc,
            ReadyBy = order.ReadyBy,
            OutForDeliveryAtUtc = order.OutForDeliveryAtUtc,
            OutForDeliveryBy = order.OutForDeliveryBy,
            DeliveredAtUtc = order.DeliveredAtUtc,
            DeliveredBy = order.DeliveredBy,
            CollectedAtUtc = order.CollectedAtUtc,
            CollectedBy = order.CollectedBy,
            UpdatedAtUtc = order.UpdatedAtUtc
        };

        ApplyDelivery(order.DeliverySnapshot, record);
        return record;
    }

    public static CustomerOrderLineRecord ToRecord(CustomerOrderLine line, Guid sellerOrganizationId) =>
        new()
        {
            Id = line.Id.Value,
            OrderId = line.OrderId.Value,
            SellerOrganizationId = sellerOrganizationId,
            ProductId = line.ProductId.Value,
            LineNumber = line.LineNumber,
            NameSnapshot = line.NameSnapshot,
            SkuSnapshot = line.SkuSnapshot,
            UnitSnapshot = UnitOfMeasures.ToCode(line.UnitSnapshot),
            Quantity = line.Quantity,
            OriginalQuantity = line.OriginalQuantity,
            OriginalDiscount = line.OriginalDiscount,
            UnitPrice = line.UnitPrice,
            Discount = line.Discount,
            LineTotal = line.LineTotal,
            AdjustmentReason = line.AdjustmentReason?.ToString(),
            AdjustmentNote = line.AdjustmentNote
        };

    public static void ApplyLineToRecord(CustomerOrderLine line, CustomerOrderLineRecord record)
    {
        record.Quantity = line.Quantity;
        record.OriginalQuantity = line.OriginalQuantity;
        record.OriginalDiscount = line.OriginalDiscount;
        record.Discount = line.Discount;
        record.LineTotal = line.LineTotal;
        record.AdjustmentReason = line.AdjustmentReason?.ToString();
        record.AdjustmentNote = line.AdjustmentNote;
    }

    public static void ApplyToRecord(CustomerOrder order, CustomerOrderRecord record)
    {
        record.Status = order.Status.ToString();
        record.FulfillmentStatus = order.FulfillmentStatus.ToString();
        record.PaymentStatus = order.PaymentStatus.ToString();
        record.PaymentReference = order.PaymentReference;
        record.AmountReceived = order.AmountReceived;
        record.PaymentConfirmedAtUtc = order.PaymentConfirmedAtUtc;
        record.PaymentConfirmedBy = order.PaymentConfirmedBy;
        record.MerchandiseSubtotal = order.MerchandiseSubtotal;
        record.DeliveryFee = order.DeliveryFee;
        record.Total = order.Total;
        record.OriginalMerchandiseSubtotal = order.OriginalMerchandiseSubtotal;
        record.OriginalTotal = order.OriginalTotal;
        record.CancelReason = order.CancelReason?.ToString();
        record.CancelNotes = order.CancelNotes;
        record.ActivityEventsJson = CustomerOrderActivityJson.Serialize(order.ActivityEvents);
        record.StockReservationState = order.StockReservationState.ToString();
        record.RejectReason = order.RejectReason?.ToString();
        record.RejectNotes = order.RejectNotes;
        record.PendingScheduleProposalId = order.PendingScheduleProposal?.ProposalId;
        record.PendingScheduleProposalLocal = order.PendingScheduleProposal?.Local;
        record.PendingScheduleProposalTimeZoneId = order.PendingScheduleProposal?.TimeZoneId;
        record.PendingScheduleProposalAtUtc = order.PendingScheduleProposal?.AtUtc;
        record.PendingScheduleProposalNote = order.PendingScheduleProposal?.Note;
        record.PendingScheduleProposalBy = order.PendingScheduleProposal?.ProposedBy;
        record.PendingScheduleProposalProposedAtUtc = order.PendingScheduleProposal?.ProposedAtUtc;
        record.ConfirmedFulfillmentLocal = order.ConfirmedFulfillmentLocal;
        record.ConfirmedFulfillmentTimeZoneId = order.ConfirmedFulfillmentTimeZoneId;
        record.ConfirmedFulfillmentAtUtc = order.ConfirmedFulfillmentAtUtc;
        record.AcceptedAtUtc = order.AcceptedAtUtc;
        record.AcceptedBy = order.AcceptedBy;
        record.RejectedAtUtc = order.RejectedAtUtc;
        record.RejectedBy = order.RejectedBy;
        record.CancelledAtUtc = order.CancelledAtUtc;
        record.CancelledBy = order.CancelledBy;
        record.CompletedAtUtc = order.CompletedAtUtc;
        record.CompletedBy = order.CompletedBy;
        record.ReadyAtUtc = order.ReadyAtUtc;
        record.ReadyBy = order.ReadyBy;
        record.OutForDeliveryAtUtc = order.OutForDeliveryAtUtc;
        record.OutForDeliveryBy = order.OutForDeliveryBy;
        record.DeliveredAtUtc = order.DeliveredAtUtc;
        record.DeliveredBy = order.DeliveredBy;
        record.CollectedAtUtc = order.CollectedAtUtc;
        record.CollectedBy = order.CollectedBy;
        record.UpdatedAtUtc = order.UpdatedAtUtc;
    }

    private static void ApplyDelivery(CustomerOrderDeliverySnapshot? snapshot, CustomerOrderRecord record)
    {
        if (snapshot is null)
        {
            return;
        }

        record.DeliveryRecipientName = snapshot.RecipientName;
        record.DeliveryRecipientPhone = snapshot.RecipientPhone;
        record.DeliveryAddressLine1 = snapshot.AddressLine1;
        record.DeliveryAddressLine2 = snapshot.AddressLine2;
        record.DeliveryCity = snapshot.City;
        record.DeliveryNotes = snapshot.DeliveryNotes;
        record.DeliveryDestinationLatitude = snapshot.DestinationLatitude;
        record.DeliveryDestinationLongitude = snapshot.DestinationLongitude;
        record.DeliveryBranchLatitudeSnapshot = snapshot.BranchLatitudeSnapshot;
        record.DeliveryBranchLongitudeSnapshot = snapshot.BranchLongitudeSnapshot;
        record.DeliveryDistanceKm = snapshot.DistanceKm;
        record.DeliveryMinimumOrderAmountSnapshot = snapshot.MinimumOrderAmountSnapshot;
        record.DeliveryBaseFeeSnapshot = snapshot.BaseDeliveryFeeSnapshot;
        record.DeliveryIncludedDistanceKmSnapshot = snapshot.IncludedDistanceKmSnapshot;
        record.DeliveryAdditionalFeePerKmSnapshot = snapshot.AdditionalFeePerKmSnapshot;
        record.DeliveryMaximumDistanceKmSnapshot = snapshot.MaximumDeliveryDistanceKmSnapshot;
        record.DeliveryFreeThresholdSnapshot = snapshot.FreeDeliveryThresholdSnapshot;
        record.DeliveryDistanceCharge = snapshot.DistanceCharge;
        record.DeliveryFinalFee = snapshot.FinalDeliveryFee;
        record.DeliveryFreeApplied = snapshot.FreeDeliveryApplied;
        record.DeliveryDistanceExceptionApplied = snapshot.DistanceExceptionApplied;
    }

    internal static CustomerOrderDeliverySnapshot? TryMapDeliverySnapshot(CustomerOrderRecord record)
    {
        if (!string.Equals(record.FulfillmentType, nameof(CustomerOrderFulfillmentType.Delivery), StringComparison.Ordinal))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(record.DeliveryRecipientName)
            || string.IsNullOrWhiteSpace(record.DeliveryAddressLine1)
            || record.DeliveryDestinationLatitude is null
            || record.DeliveryDestinationLongitude is null
            || record.DeliveryBranchLatitudeSnapshot is null
            || record.DeliveryBranchLongitudeSnapshot is null
            || record.DeliveryDistanceKm is null
            || record.DeliveryMinimumOrderAmountSnapshot is null
            || record.DeliveryBaseFeeSnapshot is null
            || record.DeliveryIncludedDistanceKmSnapshot is null
            || record.DeliveryAdditionalFeePerKmSnapshot is null
            || record.DeliveryMaximumDistanceKmSnapshot is null
            || record.DeliveryDistanceCharge is null
            || record.DeliveryFinalFee is null
            || record.DeliveryFreeApplied is null)
        {
            return null;
        }

        return CustomerOrderDeliverySnapshot.Rehydrate(
            record.DeliveryRecipientName,
            record.DeliveryRecipientPhone,
            record.DeliveryAddressLine1,
            record.DeliveryAddressLine2,
            record.DeliveryCity,
            record.DeliveryNotes,
            record.DeliveryDestinationLatitude.Value,
            record.DeliveryDestinationLongitude.Value,
            record.DeliveryBranchLatitudeSnapshot.Value,
            record.DeliveryBranchLongitudeSnapshot.Value,
            record.DeliveryDistanceKm.Value,
            record.DeliveryMinimumOrderAmountSnapshot.Value,
            record.DeliveryBaseFeeSnapshot.Value,
            record.DeliveryIncludedDistanceKmSnapshot.Value,
            record.DeliveryAdditionalFeePerKmSnapshot.Value,
            record.DeliveryMaximumDistanceKmSnapshot.Value,
            record.DeliveryFreeThresholdSnapshot,
            record.DeliveryDistanceCharge.Value,
            record.DeliveryFinalFee.Value,
            record.DeliveryFreeApplied.Value,
            record.DeliveryDistanceExceptionApplied ?? false);
    }
}

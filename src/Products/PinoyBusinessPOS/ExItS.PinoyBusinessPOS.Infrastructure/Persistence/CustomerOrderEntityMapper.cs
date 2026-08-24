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
                l.LineTotal))
            .ToList();

        CustomerOrderDeliverySnapshot? delivery = null;
        if (string.Equals(record.FulfillmentType, nameof(CustomerOrderFulfillmentType.Delivery), StringComparison.Ordinal))
        {
            delivery = CustomerOrderDeliverySnapshot.Rehydrate(
                record.DeliveryRecipientName!,
                record.DeliveryRecipientPhone,
                record.DeliveryAddressLine1!,
                record.DeliveryAddressLine2,
                record.DeliveryCity,
                record.DeliveryNotes,
                record.DeliveryDestinationLatitude!.Value,
                record.DeliveryDestinationLongitude!.Value,
                record.DeliveryBranchLatitudeSnapshot!.Value,
                record.DeliveryBranchLongitudeSnapshot!.Value,
                record.DeliveryDistanceKm!.Value,
                record.DeliveryMinimumOrderAmountSnapshot!.Value,
                record.DeliveryBaseFeeSnapshot!.Value,
                record.DeliveryIncludedDistanceKmSnapshot!.Value,
                record.DeliveryAdditionalFeePerKmSnapshot!.Value,
                record.DeliveryMaximumDistanceKmSnapshot!.Value,
                record.DeliveryFreeThresholdSnapshot,
                record.DeliveryDistanceCharge!.Value,
                record.DeliveryFinalFee!.Value,
                record.DeliveryFreeApplied!.Value);
        }

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
            record.PlatformBusinessCustomerId);
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
            UnitPrice = line.UnitPrice,
            Discount = line.Discount,
            LineTotal = line.LineTotal
        };

    public static void ApplyToRecord(CustomerOrder order, CustomerOrderRecord record)
    {
        record.Status = order.Status.ToString();
        record.FulfillmentStatus = order.FulfillmentStatus.ToString();
        record.PaymentStatus = order.PaymentStatus.ToString();
        record.StockReservationState = order.StockReservationState.ToString();
        record.RejectReason = order.RejectReason?.ToString();
        record.RejectNotes = order.RejectNotes;
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
    }
}

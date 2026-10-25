import type { QueryClient } from "@tanstack/react-query";
import { PosRealtimeEventTypes, type PosRealtimeEventEnvelope } from "@/realtime/pos-realtime-event-types";

const CUSTOMER_ORDER_EVENTS = new Set<string>([
  PosRealtimeEventTypes.CustomerOrderSubmitted,
  PosRealtimeEventTypes.CustomerOrderAccepted,
  PosRealtimeEventTypes.CustomerOrderRejected,
  PosRealtimeEventTypes.CustomerOrderCancelled,
  PosRealtimeEventTypes.CustomerOrderPreparing,
  PosRealtimeEventTypes.CustomerOrderReadyForPickup,
  PosRealtimeEventTypes.CustomerOrderOutForDelivery,
  PosRealtimeEventTypes.CustomerOrderDelivered,
  PosRealtimeEventTypes.CustomerOrderCollected,
  PosRealtimeEventTypes.CustomerOrderCompleted,
  PosRealtimeEventTypes.CustomerOrderScheduleProposed,
  PosRealtimeEventTypes.CustomerOrderScheduleAccepted,
  PosRealtimeEventTypes.CustomerOrderScheduleDeclined,
  PosRealtimeEventTypes.CustomerOrderLinesAdjusted,
  PosRealtimeEventTypes.PaymentStatusChanged,
]);

export function applyPosRealtimeEventToQueries(
  queryClient: QueryClient,
  envelope: PosRealtimeEventEnvelope,
): void {
  const sellerOrgId = envelope.data?.sellerOrganizationId
    ?? envelope.data?.organizationId
    ?? envelope.scopeId
    ?? null;

  if (
    envelope.eventType === PosRealtimeEventTypes.ConnectedCommerceSettingsChanged
    || envelope.eventType === PosRealtimeEventTypes.PaymentMethodSettingsChanged
  ) {
    if (sellerOrgId) {
      void queryClient.invalidateQueries({
        predicate: (q) =>
          Array.isArray(q.queryKey)
          && q.queryKey.includes(sellerOrgId)
          && (q.queryKey.includes("connected-commerce")
            || q.queryKey.includes("payment-methods")
            || q.queryKey.includes("fulfillment")),
      });
    }
    return;
  }

  if (
    envelope.eventType === PosRealtimeEventTypes.CatalogPricingChanged
    || envelope.eventType === PosRealtimeEventTypes.InventoryStockChanged
  ) {
    void queryClient.invalidateQueries({
      predicate: (q) =>
        Array.isArray(q.queryKey)
        && (q.queryKey[0] === "catalog"
          || q.queryKey[0] === "inventory"
          || q.queryKey[0] === "inventory-transfers"
          || q.queryKey[0] === "inventory-transfer"
          || q.queryKey[0] === "stock-requests"
          || q.queryKey[0] === "stock-request"
          || q.queryKey[0] === "stock-request-activity"
          || q.queryKey[0] === "storefront"
          || q.queryKey[0] === "todays-prices"),
    });
    return;
  }

  if (!CUSTOMER_ORDER_EVENTS.has(envelope.eventType)) {
    return;
  }

  const orderId = envelope.resourceId;

  void queryClient.invalidateQueries({ queryKey: ["personal", "my-orders"] });
  if (orderId) {
    void queryClient.invalidateQueries({ queryKey: ["personal", "my-order", orderId] });
  }

  if (sellerOrgId) {
    void queryClient.invalidateQueries({ queryKey: ["seller-orders", sellerOrgId] });
    if (orderId) {
      void queryClient.invalidateQueries({ queryKey: ["seller-order", sellerOrgId, orderId] });
    }
  } else {
    void queryClient.invalidateQueries({ queryKey: ["seller-orders"] });
    void queryClient.invalidateQueries({ queryKey: ["seller-order"] });
  }
}

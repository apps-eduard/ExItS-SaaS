import type { CustomerOrderDto } from "@/api/pos/pos-customer-orders-client";

export type SellerOrderAction =
  | "Accept"
  | "Reject"
  | "StartPreparing"
  | "MarkReady"
  | "OutForDelivery"
  | "MarkDelivered"
  | "MarkCollected"
  | "Complete";

function is(value: string, expected: string): boolean {
  return value.localeCompare(expected, undefined, { sensitivity: "accent" }) === 0;
}

/** Allowed seller transitions mirroring MAUI SellerOrderDetail — server remains authoritative. */
export function availableSellerActions(order: CustomerOrderDto): SellerOrderAction[] {
  if (is(order.status, "Submitted")) {
    return ["Accept", "Reject"];
  }

  if (!is(order.status, "Accepted")) {
    return [];
  }

  const actions: SellerOrderAction[] = [];

  if (is(order.fulfillmentStatus, "Pending")) {
    actions.push("StartPreparing");
  }
  if (is(order.fulfillmentStatus, "Preparing")) {
    actions.push("MarkReady");
  }
  if (is(order.fulfillmentType, "Delivery") && is(order.fulfillmentStatus, "Ready")) {
    actions.push("OutForDelivery");
  }
  if (is(order.fulfillmentType, "Delivery") && is(order.fulfillmentStatus, "OutForDelivery")) {
    actions.push("MarkDelivered");
  }
  if (is(order.fulfillmentType, "Pickup") && is(order.fulfillmentStatus, "ReadyForPickup")) {
    actions.push("MarkCollected");
  }
  if (is(order.fulfillmentStatus, "Delivered") || is(order.fulfillmentStatus, "Collected")) {
    actions.push("Complete");
  }

  return actions;
}

export type SellerOrderFilter = "New" | "Preparing" | "Ready" | "Issues" | "All";

export function sellerFilterApiStatus(filter: SellerOrderFilter): string | undefined {
  switch (filter) {
    case "New":
      return "Submitted";
    case "Preparing":
    case "Ready":
      return "Accepted";
    default:
      return undefined;
  }
}

export function filterSellerOrdersClientSide<
  T extends { status: string; fulfillmentStatus: string },
>(items: T[], filter: SellerOrderFilter): T[] {
  if (filter === "All") {
    return items;
  }
  if (filter === "New") {
    return items.filter((o) => is(o.status, "Submitted"));
  }
  if (filter === "Preparing") {
    return items.filter(
      (o) =>
        is(o.status, "Accepted") &&
        (is(o.fulfillmentStatus, "Pending") || is(o.fulfillmentStatus, "Preparing")),
    );
  }
  if (filter === "Ready") {
    return items.filter(
      (o) =>
        is(o.fulfillmentStatus, "Ready") ||
        is(o.fulfillmentStatus, "ReadyForPickup") ||
        is(o.fulfillmentStatus, "OutForDelivery"),
    );
  }
  if (filter === "Issues") {
    return items.filter((o) => is(o.status, "Rejected") || is(o.status, "Cancelled"));
  }
  return items;
}

export function displayOrderStatusKey(order: {
  status: string;
  fulfillmentStatus: string;
  fulfillmentType: string;
}): string {
  if (is(order.status, "Submitted")) return "orders.statusNew";
  if (is(order.status, "Rejected")) return "orders.statusRejected";
  if (is(order.status, "Cancelled")) return "orders.statusCancelled";
  if (is(order.status, "Completed")) return "orders.statusCompleted";
  if (is(order.fulfillmentStatus, "Pending")) return "orders.statusAccepted";
  if (is(order.fulfillmentStatus, "Preparing")) return "orders.statusPreparing";
  if (is(order.fulfillmentStatus, "Ready") || is(order.fulfillmentStatus, "ReadyForPickup")) {
    return is(order.fulfillmentType, "Pickup") ? "orders.statusReadyPickup" : "orders.statusReady";
  }
  if (is(order.fulfillmentStatus, "OutForDelivery")) return "orders.statusOutForDelivery";
  if (is(order.fulfillmentStatus, "Delivered")) return "orders.statusDelivered";
  if (is(order.fulfillmentStatus, "Collected")) return "orders.statusCollected";
  return "orders.statusAccepted";
}

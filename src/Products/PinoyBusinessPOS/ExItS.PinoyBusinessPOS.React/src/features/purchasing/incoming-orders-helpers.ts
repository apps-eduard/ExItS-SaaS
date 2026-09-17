import type { ConnectedPurchaseOrder } from "@/api/pos/pos-connected-suppliers-client";

/** UI filter keys (domain status in parentheses). */
export type IncomingOrdersUiFilter =
  | "all"
  | "pending"
  | "accepted"
  | "preparing"
  | "completed"
  | "declined";

/** Canonical supplier completion: buyer outstanding is zero. */
export function isIncomingOrderCompleted(
  order: Pick<ConnectedPurchaseOrder, "displayStatus" | "buyerReceivingStatus" | "status">,
): boolean {
  const key = (order.displayStatus || order.buyerReceivingStatus || "").trim();
  return (
    key === "Completed" ||
    key === "CompletedRemainingCancelled" ||
    key === "ReceivedByBuyer" ||
    key === "ReceivedWithIssues" ||
    key === "Received"
  );
}

export function isIncomingOrderPartiallyReceived(
  order: Pick<ConnectedPurchaseOrder, "displayStatus" | "buyerReceivingStatus">,
): boolean {
  const key = (order.displayStatus || order.buyerReceivingStatus || "").trim();
  return key === "PartiallyReceived";
}

export function isIncomingOrderAwaitingBuyerReceipt(
  order: Pick<ConnectedPurchaseOrder, "displayStatus" | "buyerReceivingStatus" | "status">,
): boolean {
  if (isIncomingOrderCompleted(order) || isIncomingOrderPartiallyReceived(order)) {
    return false;
  }
  const key = (order.displayStatus || order.buyerReceivingStatus || "").trim();
  return (
    key === "AwaitingBuyerReceipt" ||
    key === "Shipped" ||
    key === "Ready" ||
    order.status === "Fulfilled"
  );
}

export function uiFilterToApiStatus(filter: IncomingOrdersUiFilter): string | undefined {
  switch (filter) {
    case "pending":
      return "New";
    case "accepted":
      return "Accepted";
    case "preparing":
      return "Preparing";
    case "completed":
      // Completion is buyer-outstanding-driven; fetch Fulfilled+Accepted and filter client-side.
      return undefined;
    case "declined":
      return "Declined";
    case "all":
    default:
      return undefined;
  }
}

/** Counts for status tabs — excludes `all` (no badge on All). */
export type IncomingOrdersStatusCounts = Record<
  Exclude<IncomingOrdersUiFilter, "all">,
  number
>;

export function countIncomingOrdersByUiFilter(
  orders: ReadonlyArray<
    Pick<ConnectedPurchaseOrder, "status" | "displayStatus" | "buyerReceivingStatus">
  >,
): IncomingOrdersStatusCounts {
  const counts: IncomingOrdersStatusCounts = {
    pending: 0,
    accepted: 0,
    preparing: 0,
    completed: 0,
    declined: 0,
  };
  for (const order of orders) {
    if (isIncomingOrderCompleted(order)) {
      counts.completed += 1;
      continue;
    }
    switch (order.status) {
      case "New":
        counts.pending += 1;
        break;
      case "Accepted":
        counts.accepted += 1;
        break;
      case "Preparing":
        counts.preparing += 1;
        break;
      case "Fulfilled":
        // Awaiting buyer receipt — not completed.
        break;
      case "Declined":
        counts.declined += 1;
        break;
      default:
        break;
    }
  }
  return counts;
}

export function filterIncomingOrdersByUiStatus(
  orders: ReadonlyArray<ConnectedPurchaseOrder>,
  filter: IncomingOrdersUiFilter,
): ConnectedPurchaseOrder[] {
  if (filter === "all") {
    return [...orders];
  }
  if (filter === "completed") {
    return orders.filter((order) => isIncomingOrderCompleted(order));
  }
  const apiStatus = uiFilterToApiStatus(filter);
  if (!apiStatus) {
    return [...orders];
  }
  return orders.filter(
    (order) => order.status === apiStatus && !isIncomingOrderCompleted(order),
  );
}

export function incomingOrderStatusTone(
  status: string,
  displayStatus?: string,
): "success" | "warning" | "info" | "danger" {
  const key = (displayStatus || status || "").trim();
  if (
    key === "Completed" ||
    key === "CompletedRemainingCancelled" ||
    key === "ReceivedByBuyer" ||
    key === "Received" ||
    key === "ReceivedWithIssues"
  ) {
    return "success";
  }
  if (key === "PartiallyReceived") {
    return "warning";
  }
  switch (status) {
    case "New":
      return "warning";
    case "Accepted":
    case "Preparing":
      return "info";
    case "Fulfilled":
      return "info";
    case "Declined":
      return "danger";
    case "Withdrawn":
    case "ChangesProposed":
      return "info";
    default:
      return "info";
  }
}

export function countIncomingLines(order: Pick<ConnectedPurchaseOrder, "lines">): number {
  return order.lines.length;
}

export function countIncomingUnits(order: Pick<ConnectedPurchaseOrder, "lines">): number {
  return order.lines.reduce((sum, line) => sum + line.qty, 0);
}

export function filterIncomingOrdersBySearch(
  orders: ReadonlyArray<ConnectedPurchaseOrder>,
  search: string,
): ConnectedPurchaseOrder[] {
  const q = search.trim().toLowerCase();
  if (!q) {
    return [...orders];
  }
  return orders.filter((order) => {
    const tokens = [
      order.buyerPoNumber ?? "",
      order.buyerDisplayName ?? "",
      order.supplierBranchName ?? "",
      order.status,
      order.displayStatus,
    ];
    return tokens.some((token) => token.toLowerCase().includes(q));
  });
}

export function formatIncomingLineMath(qty: number, unitPrice: number, lineTotal: number): string {
  const q = Number.isInteger(qty) ? String(qty) : qty.toFixed(2);
  return `${q} × ${formatCompactPeso(unitPrice)} = ${formatCompactPeso(lineTotal)}`;
}

function formatCompactPeso(amount: number): string {
  return `₱${amount.toLocaleString("en-PH", {
    minimumFractionDigits: amount % 1 === 0 ? 0 : 2,
    maximumFractionDigits: 2,
  })}`;
}

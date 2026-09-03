import type { ConnectedPurchaseOrder } from "@/api/pos/pos-connected-suppliers-client";

/** UI filter keys (domain status in parentheses). */
export type IncomingOrdersUiFilter =
  | "all"
  | "pending"
  | "accepted"
  | "preparing"
  | "completed"
  | "declined";

export function uiFilterToApiStatus(filter: IncomingOrdersUiFilter): string | undefined {
  switch (filter) {
    case "pending":
      return "New";
    case "accepted":
      return "Accepted";
    case "preparing":
      return "Preparing";
    case "completed":
      return "Fulfilled";
    case "declined":
      return "Declined";
    case "all":
    default:
      return undefined;
  }
}

export function incomingOrderStatusTone(
  status: string,
): "success" | "warning" | "info" | "danger" {
  switch (status) {
    case "New":
      return "warning";
    case "Accepted":
    case "Preparing":
      return "info";
    case "Fulfilled":
      return "success";
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

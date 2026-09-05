/** Pure helpers for warehouse dashboard presentation. */

export type MovementTypeTotal = {
  movementType: string;
  quantityTotal: number;
  count: number;
};

export type TransferListItem = {
  transferId: string;
  transferNumber?: string | null;
  sourceBranchId: string;
  sourceBranchName?: string | null;
  destinationBranchId: string;
  destinationBranchName?: string | null;
  status: string;
  totalSentQty: number;
};

export type StockRequestListItem = {
  stockRequestId: string;
  requestNumber?: string | null;
  status: string;
  destinationLocationName?: string | null;
  lineCount: number;
};

export function classifyTransferBuckets(items: ReadonlyArray<TransferListItem>, direction: "incoming" | "outgoing") {
  const awaitingDispatch = direction === "outgoing" ? items.filter((i) => i.status === "Draft").length : 0;
  const inTransit = items.filter((i) => i.status === "InTransit").length;
  const incomingToReceive =
    direction === "incoming"
      ? items.filter((i) => i.status === "InTransit" || i.status === "PartiallyReceived").length
      : 0;
  const partiallyReceived = items.filter((i) => i.status === "PartiallyReceived").length;
  const received = items.filter((i) => i.status === "Received").length;
  return { awaitingDispatch, inTransit, incomingToReceive, partiallyReceived, received };
}

export function topDestinationsFromOutgoing(
  items: ReadonlyArray<TransferListItem>,
  limit = 5,
): Array<{ destinationBranchId: string; name: string; units: number }> {
  const map = new Map<string, { name: string; units: number }>();
  for (const item of items) {
    if (item.status === "Cancelled") continue;
    const key = item.destinationBranchId;
    const prev = map.get(key) ?? {
      name: item.destinationBranchName?.trim() || key.slice(0, 8),
      units: 0,
    };
    prev.units += item.totalSentQty;
    map.set(key, prev);
  }
  return [...map.entries()]
    .map(([destinationBranchId, v]) => ({ destinationBranchId, name: v.name, units: v.units }))
    .sort((a, b) => b.units - a.units)
    .slice(0, limit);
}

export function summarizeMovementTypes(byType: ReadonlyArray<MovementTypeTotal>) {
  const pick = (...keys: string[]) =>
    byType
      .filter((row) => keys.some((k) => k.toLowerCase() === row.movementType.toLowerCase()))
      .reduce((sum, row) => sum + row.quantityTotal, 0);

  return {
    receivedFromSuppliers: pick("PurchaseReceive", "GoodsReceipt", "DirectPurchaseReceive"),
    transferIn: pick("TransferIn"),
    transferOut: pick("TransferOut"),
    adjustments: pick("AdjustmentIncrease", "AdjustmentDecrease", "Adjustment"),
    wasteLoss: pick("Waste", "Loss", "WasteLoss", "ExpiredWaste"),
    production: pick("ProductionOutput", "ProductionConsume", "Production"),
  };
}

export function topMovedProductsFromRows(
  rows: ReadonlyArray<Record<string, unknown>>,
  direction: "outbound" | "inbound",
  limit = 5,
): Array<{ productId: string; productName: string; quantity: number }> {
  const map = new Map<string, { productName: string; quantity: number }>();
  for (const row of rows) {
    const productId = String(row.productId ?? row.ProductId ?? "");
    const productName = String(row.productName ?? row.ProductName ?? "").trim();
    const qty = Number(row.quantityEffect ?? row.QuantityEffect ?? 0);
    if (!productId || !Number.isFinite(qty) || qty === 0) continue;
    if (direction === "outbound" && qty >= 0) continue;
    if (direction === "inbound" && qty <= 0) continue;
    const prev = map.get(productId) ?? {
      productName: productName || productId.slice(0, 8),
      quantity: 0,
    };
    if (productName) prev.productName = productName;
    prev.quantity += Math.abs(qty);
    map.set(productId, prev);
  }
  return [...map.entries()]
    .map(([productId, v]) => ({
      productId,
      productName: v.productName,
      quantity: v.quantity,
    }))
    .sort((a, b) => b.quantity - a.quantity)
    .slice(0, limit);
}

export function stockRequestStatusCounts(items: ReadonlyArray<StockRequestListItem>) {
  const pending = items.filter((i) => i.status === "Pending").length;
  const inProgress = items.filter((i) => i.status === "InProgress").length;
  const partiallyFulfilled = items.filter((i) => i.status === "PartiallyFulfilled").length;
  const fulfilled = items.filter((i) => i.status === "Fulfilled").length;
  return { pending, inProgress, partiallyFulfilled, fulfilled };
}

export function buildWarehouseAttentionItems(input: {
  lowStock: number;
  expiry: number;
  awaitingDispatch: number;
  incomingToReceive: number;
  receivablePos: number;
  pendingStockRequests: number;
}): Array<{ key: string; count: number; href: string; labelKey: string }> {
  const items: Array<{ key: string; count: number; href: string; labelKey: string }> = [];
  if (input.lowStock > 0) {
    items.push({
      key: "lowStock",
      count: input.lowStock,
      href: "/inventory?lowStock=1",
      labelKey: "warehouseDashboard.attention.lowStock",
    });
  }
  if (input.expiry > 0) {
    items.push({
      key: "expiry",
      count: input.expiry,
      href: "/inventory/expiration",
      labelKey: "warehouseDashboard.attention.expiry",
    });
  }
  if (input.awaitingDispatch > 0) {
    items.push({
      key: "dispatch",
      count: input.awaitingDispatch,
      href: "/inventory/transfers?direction=outgoing",
      labelKey: "warehouseDashboard.attention.dispatch",
    });
  }
  if (input.incomingToReceive > 0) {
    items.push({
      key: "receiveTransfer",
      count: input.incomingToReceive,
      href: "/inventory/transfers?direction=incoming",
      labelKey: "warehouseDashboard.attention.receiveTransfer",
    });
  }
  if (input.receivablePos > 0) {
    items.push({
      key: "receivePo",
      count: input.receivablePos,
      href: "/purchasing/receive-stock",
      labelKey: "warehouseDashboard.attention.receivePo",
    });
  }
  if (input.pendingStockRequests > 0) {
    items.push({
      key: "stockRequests",
      count: input.pendingStockRequests,
      href: "/inventory/stock-requests",
      labelKey: "warehouseDashboard.attention.stockRequests",
    });
  }
  return items;
}

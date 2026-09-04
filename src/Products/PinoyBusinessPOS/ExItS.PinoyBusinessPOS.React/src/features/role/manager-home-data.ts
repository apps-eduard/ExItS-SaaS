/**
 * Pure helpers for Manager Operations Home attention + snapshot composition.
 * Metrics come from server-authoritative APIs — never invent counts.
 */

export type ManagerAttentionKind =
  | "lowStock"
  | "expiry"
  | "orders"
  | "purchasing"
  | "transfers"
  | "utang"
  | "shift";

export type ManagerAttentionItem = {
  kind: ManagerAttentionKind;
  count: number;
  /** Optional money amount for utang-style items. */
  amount?: number;
  href: string;
  testId: string;
};

export type ManagerAttentionInputs = {
  lowStockProductCount?: number | null;
  expiredLotCount?: number | null;
  nearExpiryLotCount?: number | null;
  submittedOrderCount?: number | null;
  receivablePoCount?: number | null;
  pendingIncomingTransferCount?: number | null;
  overdueUtangAmount?: number | null;
  /** When true and no open shift, surface open-shift action (retail only). */
  shiftNeedsOpen?: boolean;
};

export function buildManagerAttentionItems(
  inputs: ManagerAttentionInputs,
  options?: { includeOrders?: boolean; includeShift?: boolean },
): ManagerAttentionItem[] {
  const includeOrders = options?.includeOrders !== false;
  const includeShift = options?.includeShift === true;
  const items: ManagerAttentionItem[] = [];

  const lowStock = Math.max(0, inputs.lowStockProductCount ?? 0);
  if (lowStock > 0) {
    items.push({
      kind: "lowStock",
      count: lowStock,
      href: "/inventory",
      testId: "manager-attention-low-stock",
    });
  }

  const expired = Math.max(0, inputs.expiredLotCount ?? 0);
  const near = Math.max(0, inputs.nearExpiryLotCount ?? 0);
  const expiryTotal = expired + near;
  if (expiryTotal > 0) {
    items.push({
      kind: "expiry",
      count: expiryTotal,
      href: "/inventory/expiration",
      testId: "manager-attention-expiry",
    });
  }

  if (includeOrders) {
    const orders = Math.max(0, inputs.submittedOrderCount ?? 0);
    if (orders > 0) {
      items.push({
        kind: "orders",
        count: orders,
        href: "/orders",
        testId: "manager-attention-orders",
      });
    }
  }

  const receivable = Math.max(0, inputs.receivablePoCount ?? 0);
  if (receivable > 0) {
    items.push({
      kind: "purchasing",
      count: receivable,
      href: "/purchasing/receipts",
      testId: "manager-attention-purchasing",
    });
  }

  const transfers = Math.max(0, inputs.pendingIncomingTransferCount ?? 0);
  if (transfers > 0) {
    items.push({
      kind: "transfers",
      count: transfers,
      href: "/inventory/transfers",
      testId: "manager-attention-transfers",
    });
  }

  const overdue = inputs.overdueUtangAmount ?? 0;
  if (overdue > 0) {
    items.push({
      kind: "utang",
      count: 1,
      amount: overdue,
      href: "/customers",
      testId: "manager-attention-utang",
    });
  }

  if (includeShift && inputs.shiftNeedsOpen) {
    items.push({
      kind: "shift",
      count: 1,
      href: "/shifts/open",
      testId: "manager-attention-shift",
    });
  }

  return items;
}

export type ManagerSnapshotModule = {
  key: "inventory" | "orders" | "purchasing" | "utang" | "transfers";
  href: string;
  testId: string;
  summaryKind: "inventory" | "orders" | "purchasing" | "utang" | "transfers";
  lowStock?: number;
  expiry?: number;
  orderCount?: number;
  receivableCount?: number;
  transferCount?: number;
  overdueAmount?: number;
  outstandingAmount?: number;
};

export function buildRetailSnapshotModules(input: {
  canInventory: boolean;
  canOrders: boolean;
  canPurchasing: boolean;
  canCustomers: boolean;
  lowStock: number;
  expiry: number;
  orderCount: number;
  receivableCount: number;
  overdueAmount: number;
  outstandingAmount: number;
}): ManagerSnapshotModule[] {
  const modules: ManagerSnapshotModule[] = [];

  if (input.canInventory && (input.lowStock > 0 || input.expiry > 0)) {
    modules.push({
      key: "inventory",
      href: "/inventory",
      testId: "manager-snapshot-inventory",
      summaryKind: "inventory",
      lowStock: input.lowStock,
      expiry: input.expiry,
    });
  } else if (input.canInventory) {
    modules.push({
      key: "inventory",
      href: "/inventory",
      testId: "manager-snapshot-inventory",
      summaryKind: "inventory",
      lowStock: 0,
      expiry: 0,
    });
  }

  if (input.canOrders) {
    modules.push({
      key: "orders",
      href: "/orders",
      testId: "manager-snapshot-orders",
      summaryKind: "orders",
      orderCount: input.orderCount,
    });
  }

  if (input.canPurchasing) {
    modules.push({
      key: "purchasing",
      href: "/purchasing",
      testId: "manager-snapshot-purchasing",
      summaryKind: "purchasing",
      receivableCount: input.receivableCount,
    });
  }

  if (input.canCustomers && (input.overdueAmount > 0 || input.outstandingAmount > 0)) {
    modules.push({
      key: "utang",
      href: "/customers",
      testId: "manager-snapshot-utang",
      summaryKind: "utang",
      overdueAmount: input.overdueAmount,
      outstandingAmount: input.outstandingAmount,
    });
  }

  return modules.slice(0, 4);
}

export function buildWarehouseSnapshotModules(input: {
  canInventory: boolean;
  canPurchasing: boolean;
  lowStock: number;
  expiry: number;
  receivableCount: number;
  transferCount: number;
}): ManagerSnapshotModule[] {
  const modules: ManagerSnapshotModule[] = [];

  if (input.canInventory) {
    modules.push({
      key: "inventory",
      href: "/inventory",
      testId: "manager-snapshot-inventory",
      summaryKind: "inventory",
      lowStock: input.lowStock,
      expiry: input.expiry,
    });
    modules.push({
      key: "transfers",
      href: "/inventory/transfers",
      testId: "manager-snapshot-transfers",
      summaryKind: "transfers",
      transferCount: input.transferCount,
    });
  }

  if (input.canPurchasing) {
    modules.push({
      key: "purchasing",
      href: "/purchasing",
      testId: "manager-snapshot-purchasing",
      summaryKind: "purchasing",
      receivableCount: input.receivableCount,
    });
  }

  return modules.slice(0, 4);
}

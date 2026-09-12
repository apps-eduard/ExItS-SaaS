import {
  isPurchaseOrderReceivable,
  type PosPurchaseOrderDto,
} from "@/api/pos/pos-purchase-orders-client";

/**
 * Purchasing hub / sidenav activity counts — shared query keys.
 * Hub tabs and sidenav badge MUST reuse these keys (no duplicate network when both mount).
 */
export function purchasingHubPurchaseOrdersQueryKey(
  organizationId: string | undefined,
  branchId: string | undefined,
) {
  return ["purchasing-hub", "purchase-orders", organizationId, branchId] as const;
}

export function purchasingHubIncomingPendingQueryKey(
  organizationId: string | undefined,
  branchId: string | undefined,
) {
  return ["purchasing-hub", "incoming-pending", organizationId, branchId] as const;
}

export function purchasingHubDirectPurchasesQueryKey(
  organizationId: string | undefined,
  branchId: string | undefined,
) {
  return ["purchasing-hub", "direct-purchases", organizationId, branchId] as const;
}

export function purchasingHubSuppliersQueryKey(
  organizationId: string | undefined,
  branchId: string | undefined,
) {
  return ["purchasing-hub", "suppliers", organizationId, branchId] as const;
}

/** Prefix for mutation invalidation — refreshes hub tabs + sidenav together. */
export const PURCHASING_HUB_QUERY_PREFIX = ["purchasing-hub"] as const;

export type PurchasingActivityBuckets = {
  /** Pending buyer POs awaiting accept (Incoming orders · New). */
  incomingPendingCount: number;
  /**
   * Receivable outbound POs (Ready to receive).
   * Subset of Purchase orders — must NOT be added to ordersTotal.
   */
  receivableCount: number;
};

/**
 * Canonical sidenav / module activity count for Purchasing.
 *
 * INCLUDED (non-overlapping workflow activity):
 * - incomingPendingCount — buyer→seller incoming awaiting action
 * - receivableCount — ordered/partial POs ready to receive
 *
 * EXCLUDED (avoid double-count / master-data noise):
 * - Purchase orders totalCount — includes receivable subset + drafts/history
 * - Direct purchases totalCount — history catalog, not “needs attention”
 * - Suppliers totalCount — master data, not Purchasing workflow activity
 */
export function derivePurchasingNavigationCount(buckets: PurchasingActivityBuckets): number {
  const incoming = Math.max(0, buckets.incomingPendingCount);
  const receivable = Math.max(0, buckets.receivableCount);
  return incoming + receivable;
}

export function countReceivablePurchaseOrders(
  items: ReadonlyArray<PosPurchaseOrderDto> | undefined | null,
): number {
  return (items ?? []).filter((po) => isPurchaseOrderReceivable(po)).length;
}

/** CountBadge display — avoid widening the sidenav. */
export function formatPurchasingNavBadgeCount(count: number): string {
  if (count > 99) return "99+";
  return String(count);
}

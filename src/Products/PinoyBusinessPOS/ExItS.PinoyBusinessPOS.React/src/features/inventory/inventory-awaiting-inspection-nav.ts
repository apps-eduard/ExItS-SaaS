/** Shared React Query key for awaiting-inspection queue + sidenav badge. */
export function inventoryAwaitingInspectionQueryKey(
  organizationId: string | undefined,
  branchId: string | undefined,
) {
  return ["inventory-transfers", "awaiting-inspection", organizationId, branchId] as const;
}

/** Prefix for mutation invalidation — refreshes queue page + sidenav together. */
export const INVENTORY_AWAITING_INSPECTION_QUERY_PREFIX = [
  "inventory-transfers",
  "awaiting-inspection",
] as const;

/** CountBadge display — avoid widening the sidenav. */
export function formatInspectionNavBadgeCount(count: number): string {
  if (count > 99) return "99+";
  return String(count);
}

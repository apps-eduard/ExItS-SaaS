/** Pure helpers for stock request UX. */

import type { StatusChipTone } from "@/components/exits/StatusChip";

export type StockRequestStatusCode =
  | "Pending"
  | "Approved"
  | "Preparing"
  | "InProgress"
  | "InTransit"
  | "Fulfilled"
  | "PartiallyFulfilled"
  | "Rejected"
  | "Cancelled"
  | string;

export type RetailStockRequestTab =
  | "submitted"
  | "inProgress"
  | "inTransit"
  | "completed"
  | "all";

export type WarehouseStockRequestTab =
  | "incoming"
  | "preparing"
  | "dispatched"
  | "history"
  | "all";

export type StockRequestTab = RetailStockRequestTab | WarehouseStockRequestTab;

const COMPLETED_STATUSES = new Set([
  "Fulfilled",
  "PartiallyFulfilled",
  "Rejected",
  "Cancelled",
]);

const IN_PROGRESS_STATUSES = new Set(["Approved", "Preparing", "InProgress"]);

export function pickPreferredSourceId(
  routes: ReadonlyArray<{ sourceLocationId: string; isPreferred: boolean; isActive: boolean }>,
): string | null {
  const active = routes.filter((r) => r.isActive);
  if (active.length === 0) return null;
  return active.find((r) => r.isPreferred)?.sourceLocationId ?? active[0]?.sourceLocationId ?? null;
}

export function remainingRequestQty(requested: number, fulfilled: number, inProgress: number): number {
  return Math.max(0, requested - fulfilled - inProgress);
}

export function hasConfiguredInternalSource(
  routes: ReadonlyArray<{ isActive: boolean }>,
): boolean {
  return routes.some((r) => r.isActive);
}

export function normalizeStockRequestStatus(status: string): string {
  if (status === "InProgress") return "Preparing";
  return status;
}

export function retailTabStatuses(tab: RetailStockRequestTab): ReadonlySet<string> | null {
  switch (tab) {
    case "submitted":
      return new Set(["Pending"]);
    case "inProgress":
      return IN_PROGRESS_STATUSES;
    case "inTransit":
      return new Set(["InTransit"]);
    case "completed":
      return COMPLETED_STATUSES;
    case "all":
      return null;
  }
}

export function warehouseTabStatuses(tab: WarehouseStockRequestTab): ReadonlySet<string> | null {
  switch (tab) {
    case "incoming":
      return new Set(["Pending"]);
    case "preparing":
      return IN_PROGRESS_STATUSES;
    case "dispatched":
      return new Set(["InTransit"]);
    case "history":
      return COMPLETED_STATUSES;
    case "all":
      return null;
  }
}

export function stockRequestMatchesTab(
  status: string,
  tab: StockRequestTab,
  mode: "retail" | "warehouse",
): boolean {
  const statuses =
    mode === "warehouse"
      ? warehouseTabStatuses(tab as WarehouseStockRequestTab)
      : retailTabStatuses(tab as RetailStockRequestTab);
  if (statuses == null) return true;
  return statuses.has(status) || (status === "InProgress" && statuses.has("Preparing"));
}

export function filterStockRequestsByTab<T extends { status: string }>(
  items: ReadonlyArray<T>,
  tab: StockRequestTab,
  mode: "retail" | "warehouse",
): T[] {
  return items.filter((item) => stockRequestMatchesTab(item.status, tab, mode));
}

export function stockRequestStatusLabelKey(status: string): string {
  const normalized = normalizeStockRequestStatus(status);
  switch (normalized) {
    case "Pending":
      return "stockRequest.status.pending";
    case "Approved":
      return "stockRequest.status.approved";
    case "Preparing":
      return "stockRequest.status.preparing";
    case "InTransit":
      return "stockRequest.status.inTransit";
    case "Fulfilled":
      return "stockRequest.status.fulfilled";
    case "PartiallyFulfilled":
      return "stockRequest.status.partiallyFulfilled";
    case "Rejected":
      return "stockRequest.status.rejected";
    case "Cancelled":
      return "stockRequest.status.cancelled";
    default:
      return "stockRequest.status.pending";
  }
}

export function stockRequestStatusTone(status: string): StatusChipTone {
  switch (normalizeStockRequestStatus(status)) {
    case "Pending":
      return "warning";
    case "Approved":
    case "Preparing":
      return "info";
    case "InTransit":
      return "info";
    case "Fulfilled":
      return "success";
    case "PartiallyFulfilled":
      return "warning";
    case "Rejected":
    case "Cancelled":
      return "danger";
    default:
      return "neutral";
  }
}

export function canCancelStockRequestAsDestination(status: string): boolean {
  return status === "Pending" || status === "Approved" || status === "Preparing" || status === "InProgress";
}

export function isStockRequestOpenForSourceActions(status: string): boolean {
  return (
    status === "Pending" ||
    status === "Approved" ||
    status === "Preparing" ||
    status === "InProgress"
  );
}

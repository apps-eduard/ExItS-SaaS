import type { CatalogProductReadinessItem } from "@/api/pos/pos-connected-suppliers-client";
import {
  mapBackendStatusToUserState,
  type UserCatalogState,
} from "@/features/suppliers/connected-catalog-readiness";

/** Items that can be multi-selected for bulk connect (not Attention conflicts). */
export function isBulkConnectSelectable(item: CatalogProductReadinessItem): boolean {
  const state = mapBackendStatusToUserState(item.status);
  return state === "newProduct" || state === "checkMatch";
}

export function canBulkAddAsNew(item: CatalogProductReadinessItem): boolean {
  const state = mapBackendStatusToUserState(item.status);
  return state === "newProduct" || state === "checkMatch";
}

export function canBulkConfirmMatch(item: CatalogProductReadinessItem): boolean {
  return (
    mapBackendStatusToUserState(item.status) === "checkMatch"
    && Boolean(item.candidateBuyerProductId)
  );
}

export function partitionBulkConnectSelection(
  items: ReadonlyArray<CatalogProductReadinessItem>,
  selectedIds: ReadonlySet<string>,
): {
  addAsNew: CatalogProductReadinessItem[];
  confirmMatch: CatalogProductReadinessItem[];
} {
  const selected = items.filter((item) => selectedIds.has(item.exposureId));
  return {
    addAsNew: selected.filter(canBulkAddAsNew),
    confirmMatch: selected.filter(canBulkConfirmMatch),
  };
}

export function catalogItemState(item: CatalogProductReadinessItem): UserCatalogState {
  return mapBackendStatusToUserState(item.status);
}

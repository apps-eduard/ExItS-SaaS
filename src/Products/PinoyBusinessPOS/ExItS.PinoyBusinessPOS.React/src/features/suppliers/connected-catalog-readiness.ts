import type { CatalogProductReadinessItem, CatalogReadinessResult } from "@/api/pos/pos-connected-suppliers-client";

/** Backend readiness statuses. */
export type BackendReadinessStatus = "Ready" | "New" | "Review" | "Conflict" | "AlreadyLinked";

/** User-facing Shared Catalog states. */
export type UserCatalogState = "linked" | "newProduct" | "checkMatch" | "attention" | "unclassified";

export type CatalogReadinessFilter = "all" | "newProduct" | "checkMatch" | "attention" | "linked";

export function mapBackendStatusToUserState(
  status: string | null | undefined,
): UserCatalogState {
  if (status == null || status === "") {
    return "unclassified";
  }
  switch (status) {
    case "Ready":
    case "AlreadyLinked":
      return "linked";
    case "New":
      return "newProduct";
    case "Review":
      return "checkMatch";
    case "Conflict":
      return "attention";
    default:
      return "unclassified";
  }
}

export function userStateMatchesFilter(
  state: UserCatalogState,
  filter: CatalogReadinessFilter,
): boolean {
  if (filter === "all") {
    return state !== "unclassified";
  }
  return state === filter;
}

export function countByUserState(result: CatalogReadinessResult | null | undefined): {
  all: number;
  newProduct: number;
  checkMatch: number;
  attention: number;
  linked: number;
} {
  if (!result) {
    return { all: 0, newProduct: 0, checkMatch: 0, attention: 0, linked: 0 };
  }
  const linked = result.ready;
  const newProduct = result.new;
  const checkMatch = result.review;
  const attention = result.conflict;
  return {
    all: linked + newProduct + checkMatch + attention,
    newProduct,
    checkMatch,
    attention,
    linked,
  };
}

/** Resolve card state from readiness row; never invent New when readiness is absent. */
export function resolveCardState(
  readiness: CatalogProductReadinessItem | undefined,
  readinessLoaded: boolean,
): UserCatalogState {
  if (!readinessLoaded || !readiness) {
    return "unclassified";
  }
  return mapBackendStatusToUserState(readiness.status);
}

export function filterReadinessItems(
  items: CatalogProductReadinessItem[],
  filter: CatalogReadinessFilter,
  search: string,
): CatalogProductReadinessItem[] {
  const q = search.trim().toLowerCase();
  return items.filter((item) => {
    const state = mapBackendStatusToUserState(item.status);
    if (!userStateMatchesFilter(state, filter)) {
      return false;
    }
    if (!q) {
      return true;
    }
    const haystack = [item.supplierName, item.supplierSku ?? "", item.supplierBarcode ?? ""]
      .join(" ")
      .toLowerCase();
    return haystack.includes(q);
  });
}

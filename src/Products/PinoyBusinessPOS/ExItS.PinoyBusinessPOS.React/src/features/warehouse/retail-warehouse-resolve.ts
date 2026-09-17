import { isWarehouseBranch } from "@/features/branches/branch-type";
import { normalizeBranchStatusFilter } from "@/features/branches/branch-code";

export type RetailWarehouseOrgBranch = {
  branchId: string;
  name: string;
  branchType?: string | null;
  isActive?: boolean;
  status?: string | null;
};

export type RetailWarehouseSupplyRoute = {
  sourceLocationId: string;
  destinationLocationId?: string;
  isActive: boolean;
  isPreferred: boolean;
};

export type RetailWarehouseResolveState =
  | { kind: "no-warehouse" }
  | { kind: "no-assignment" }
  | {
      kind: "ready";
      supplyWarehouseId: string;
      supplyWarehouseName: string;
      isPreferred: boolean;
    };

function isActiveBranch(branch: RetailWarehouseOrgBranch): boolean {
  if (typeof branch.isActive === "boolean") {
    return branch.isActive;
  }
  return normalizeBranchStatusFilter(branch.status ?? "Active") === "Active";
}

/**
 * Resolve retail→warehouse supply readiness for the current retail branch.
 * A: no active Warehouse branchType in org
 * B: warehouses exist but no active warehouse-only supply route for this retail branch
 * C: preferred or first active warehouse route
 */
export function resolveRetailWarehouseSupply(
  orgBranches: ReadonlyArray<RetailWarehouseOrgBranch>,
  supplyRoutes: ReadonlyArray<RetailWarehouseSupplyRoute>,
  retailBranchId: string,
): RetailWarehouseResolveState {
  const activeWarehouses = orgBranches.filter(
    (b) => isActiveBranch(b) && isWarehouseBranch(b.branchType),
  );
  if (activeWarehouses.length === 0) {
    return { kind: "no-warehouse" };
  }

  const warehouseIds = new Set(activeWarehouses.map((b) => b.branchId));
  const nameById = new Map(activeWarehouses.map((b) => [b.branchId, b.name] as const));

  const activeWarehouseRoutes = supplyRoutes.filter((route) => {
    if (!route.isActive) return false;
    if (!warehouseIds.has(route.sourceLocationId)) return false;
    if (route.destinationLocationId && route.destinationLocationId !== retailBranchId) {
      return false;
    }
    return true;
  });

  if (activeWarehouseRoutes.length === 0) {
    return { kind: "no-assignment" };
  }

  const preferred = activeWarehouseRoutes.find((r) => r.isPreferred) ?? activeWarehouseRoutes[0]!;
  return {
    kind: "ready",
    supplyWarehouseId: preferred.sourceLocationId,
    supplyWarehouseName: nameById.get(preferred.sourceLocationId) ?? preferred.sourceLocationId,
    isPreferred: preferred.isPreferred,
  };
}

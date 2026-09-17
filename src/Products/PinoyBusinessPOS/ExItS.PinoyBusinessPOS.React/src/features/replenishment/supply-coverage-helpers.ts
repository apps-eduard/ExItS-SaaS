import { isWarehouseBranch } from "@/features/branches/branch-type";
import { normalizeBranchStatusFilter } from "@/features/branches/branch-code";

export type CoverageLocation = {
  id: string;
  name: string;
  code?: string | null;
  branchType?: string | null;
  status: string;
  areaId?: string | null;
  areaName?: string | null;
};

export type CoverageRoute = {
  sourceLocationId: string;
  destinationLocationId: string;
  isActive: boolean;
  isPreferred: boolean;
};

export type AreaTriState = "unchecked" | "partial" | "checked";

export function activeLocations(locations: ReadonlyArray<CoverageLocation>): CoverageLocation[] {
  return locations.filter((l) => normalizeBranchStatusFilter(l.status) === "Active");
}

export function warehouseSources(locations: ReadonlyArray<CoverageLocation>): CoverageLocation[] {
  return activeLocations(locations).filter((l) => isWarehouseBranch(l.branchType));
}

export function retailDestinations(locations: ReadonlyArray<CoverageLocation>): CoverageLocation[] {
  return activeLocations(locations).filter((l) => !isWarehouseBranch(l.branchType));
}

export function otherWarehouses(
  locations: ReadonlyArray<CoverageLocation>,
  currentWarehouseId: string,
): CoverageLocation[] {
  return warehouseSources(locations).filter((l) => l.id !== currentWarehouseId);
}

export function activeRoutesFromSource(
  routes: ReadonlyArray<CoverageRoute>,
  sourceId: string,
): CoverageRoute[] {
  return routes.filter((r) => r.isActive && r.sourceLocationId === sourceId);
}

export function connectedDestinationIds(
  routes: ReadonlyArray<CoverageRoute>,
  sourceId: string,
): Set<string> {
  return new Set(activeRoutesFromSource(routes, sourceId).map((r) => r.destinationLocationId));
}

export function preferredSourceByDestination(
  routes: ReadonlyArray<CoverageRoute>,
): Map<string, string> {
  const map = new Map<string, string>();
  for (const route of routes) {
    if (route.isActive && route.isPreferred) {
      map.set(route.destinationLocationId, route.sourceLocationId);
    }
  }
  return map;
}

export function areaRetailMembers(
  locations: ReadonlyArray<CoverageLocation>,
  areaId: string | null,
): CoverageLocation[] {
  const retail = retailDestinations(locations);
  if (areaId === null) {
    return retail.filter((l) => !l.areaId);
  }
  return retail.filter((l) => l.areaId === areaId);
}

export function areaTriState(
  memberIds: ReadonlyArray<string>,
  selected: ReadonlySet<string>,
): AreaTriState {
  if (memberIds.length === 0) return "unchecked";
  const selectedCount = memberIds.filter((id) => selected.has(id)).length;
  if (selectedCount === 0) return "unchecked";
  if (selectedCount === memberIds.length) return "checked";
  return "partial";
}

export function toggleAreaSelection(
  memberIds: ReadonlyArray<string>,
  selected: ReadonlySet<string>,
): Set<string> {
  const next = new Set(selected);
  const state = areaTriState(memberIds, selected);
  if (state === "checked") {
    for (const id of memberIds) next.delete(id);
  } else {
    for (const id of memberIds) next.add(id);
  }
  return next;
}

export function filterLocationsBySearch(
  locations: ReadonlyArray<CoverageLocation>,
  search: string,
): CoverageLocation[] {
  const q = search.trim().toLowerCase();
  if (!q) return [...locations];
  return locations.filter(
    (l) =>
      l.name.toLowerCase().includes(q) ||
      (l.code ?? "").toLowerCase().includes(q),
  );
}

export function warehouseCoverageSummary(
  locations: ReadonlyArray<CoverageLocation>,
  routes: ReadonlyArray<CoverageRoute>,
  warehouseId: string,
): {
  retailCount: number;
  warehouseCount: number;
  fullAreaNames: string[];
  partialAreas: Array<{ name: string; selected: number; total: number }>;
} {
  const connected = connectedDestinationIds(routes, warehouseId);
  const retail = retailDestinations(locations).filter((l) => connected.has(l.id));
  const warehouses = otherWarehouses(locations, warehouseId).filter((l) => connected.has(l.id));

  const areaNames = new Map<string, string>();
  for (const loc of retailDestinations(locations)) {
    if (loc.areaId && loc.areaName) areaNames.set(loc.areaId, loc.areaName);
  }

  const fullAreaNames: string[] = [];
  const partialAreas: Array<{ name: string; selected: number; total: number }> = [];
  for (const [areaId, name] of areaNames) {
    const members = areaRetailMembers(locations, areaId);
    const selected = members.filter((m) => connected.has(m.id)).length;
    if (selected === 0) continue;
    if (selected === members.length) fullAreaNames.push(name);
    else partialAreas.push({ name, selected, total: members.length });
  }

  return {
    retailCount: retail.length,
    warehouseCount: warehouses.length,
    fullAreaNames,
    partialAreas,
  };
}

/** Keep warehouse-only active routes for stock request source pickers. */
export function warehouseOnlyActiveRoutes<T extends CoverageRoute>(
  routes: ReadonlyArray<T>,
  locationById: ReadonlyMap<string, CoverageLocation>,
): T[] {
  return routes.filter((r) => {
    if (!r.isActive) return false;
    const source = locationById.get(r.sourceLocationId);
    return source ? isWarehouseBranch(source.branchType) : false;
  });
}

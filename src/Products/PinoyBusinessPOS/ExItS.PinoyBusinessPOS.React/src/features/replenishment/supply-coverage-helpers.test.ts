import { describe, expect, it } from "vitest";
import {
  areaTriState,
  connectedDestinationIds,
  filterLocationsBySearch,
  otherWarehouses,
  preferredSourceByDestination,
  retailDestinations,
  toggleAreaSelection,
  warehouseCoverageSummary,
  warehouseOnlyActiveRoutes,
  warehouseSources,
  type CoverageLocation,
  type CoverageRoute,
} from "@/features/replenishment/supply-coverage-helpers";

const locations: CoverageLocation[] = [
  { id: "w1", name: "Panay Warehouse", code: "PW", branchType: "Warehouse", status: "Active", areaId: null },
  { id: "w2", name: "Central Warehouse", code: "CW", branchType: "Warehouse", status: "Active", areaId: null },
  {
    id: "b1",
    name: "Main Branch",
    code: "MB",
    branchType: "Retail",
    status: "Active",
    areaId: "a1",
    areaName: "Pasi Norte",
  },
  {
    id: "b2",
    name: "Pac Passi",
    code: "PP",
    branchType: "Retail",
    status: "Active",
    areaId: "a1",
    areaName: "Pasi Norte",
  },
  {
    id: "b3",
    name: "Airport Branch",
    code: "AB",
    branchType: "Retail",
    status: "Active",
    areaId: null,
  },
  { id: "b4", name: "Inactive Retail", branchType: "Retail", status: "Inactive", areaId: "a1", areaName: "Pasi Norte" },
];

const routes: CoverageRoute[] = [
  { sourceLocationId: "w1", destinationLocationId: "b1", isActive: true, isPreferred: true },
  { sourceLocationId: "w1", destinationLocationId: "b2", isActive: true, isPreferred: false },
  { sourceLocationId: "w2", destinationLocationId: "b1", isActive: true, isPreferred: false },
  { sourceLocationId: "b1", destinationLocationId: "b2", isActive: true, isPreferred: false },
];

describe("supply-coverage-helpers", () => {
  it("lists only active warehouses as supply sources", () => {
    expect(warehouseSources(locations).map((l) => l.id)).toEqual(["w1", "w2"]);
  });

  it("lists active retail destinations including unassigned", () => {
    expect(retailDestinations(locations).map((l) => l.id)).toEqual(["b1", "b2", "b3"]);
  });

  it("supports area tri-state and toggle without wiping unrelated selections", () => {
    const members = ["b1", "b2"];
    let selected = new Set(["b1", "b3"]);
    expect(areaTriState(members, selected)).toBe("partial");
    selected = toggleAreaSelection(members, selected);
    expect(selected.has("b1")).toBe(true);
    expect(selected.has("b2")).toBe(true);
    expect(selected.has("b3")).toBe(true);
    expect(areaTriState(members, selected)).toBe("checked");
    selected = toggleAreaSelection(members, selected);
    expect(selected.has("b1")).toBe(false);
    expect(selected.has("b2")).toBe(false);
    expect(selected.has("b3")).toBe(true);
  });

  it("allows the same retail branch under multiple warehouses", () => {
    expect(connectedDestinationIds(routes, "w1").has("b1")).toBe(true);
    expect(connectedDestinationIds(routes, "w2").has("b1")).toBe(true);
  });

  it("preserves preferred source map per destination", () => {
    expect(preferredSourceByDestination(routes).get("b1")).toBe("w1");
  });

  it("summarizes coverage without treating areas as owned", () => {
    const summary = warehouseCoverageSummary(locations, routes, "w1");
    expect(summary.retailCount).toBe(2);
    expect(summary.warehouseCount).toBe(0);
    expect(summary.fullAreaNames).toContain("Pasi Norte");
    expect(summary.partialAreas).toEqual([]);
  });

  it("marks area partial when only some members are connected", () => {
    const partialRoutes: CoverageRoute[] = [
      { sourceLocationId: "w1", destinationLocationId: "b1", isActive: true, isPreferred: true },
    ];
    const summary = warehouseCoverageSummary(locations, partialRoutes, "w1");
    expect(summary.partialAreas[0]?.name).toBe("Pasi Norte");
    expect(summary.partialAreas[0]?.selected).toBe(1);
    expect(summary.partialAreas[0]?.total).toBe(2);
  });

  it("filters warehouse-only active routes for stock request", () => {
    const byId = new Map(locations.map((l) => [l.id, l]));
    const filtered = warehouseOnlyActiveRoutes(routes, byId);
    expect(filtered.every((r) => r.sourceLocationId === "w1" || r.sourceLocationId === "w2")).toBe(true);
    expect(filtered.some((r) => r.sourceLocationId === "b1")).toBe(false);
  });

  it("keeps hidden selections when searching", () => {
    const selected = new Set(["b1", "b2"]);
    const visible = filterLocationsBySearch(retailDestinations(locations), "Airport");
    expect(visible.map((l) => l.id)).toEqual(["b3"]);
    expect(selected.has("b1")).toBe(true);
    expect(selected.has("b2")).toBe(true);
  });

  it("excludes current warehouse from other warehouses section", () => {
    expect(otherWarehouses(locations, "w1").map((l) => l.id)).toEqual(["w2"]);
  });
});

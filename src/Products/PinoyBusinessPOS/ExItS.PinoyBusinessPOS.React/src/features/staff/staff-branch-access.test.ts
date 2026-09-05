import { describe, expect, it } from "vitest";
import type { PlatformBranch } from "@/api/platform/platform-auth-client";
import {
  branchIdsEqual,
  countActiveLocationsInArea,
  formatStaffBranchAccessSummary,
  isImplicitAllBranchesMembershipRole,
  modeToScope,
  resolvePrimaryOrOnlyBranch,
  scopeToMode,
  shouldOfferAreaScope,
} from "@/features/staff/staff-branch-access";

const main: PlatformBranch = {
  id: "main",
  organizationId: "org",
  code: "MAIN",
  name: "Main Branch",
  isPrimary: true,
  status: "Active",
  branchType: "Retail",
  areaId: "south",
};

const north: PlatformBranch = {
  id: "north",
  organizationId: "org",
  code: "NORTH",
  name: "North",
  isPrimary: false,
  status: "Active",
  branchType: "Retail",
  areaId: "north",
};

const south: PlatformBranch = {
  id: "south",
  organizationId: "org",
  code: "SOUTH",
  name: "South",
  isPrimary: false,
  status: "Active",
  branchType: "Retail",
  areaId: "south",
};

const warehouse: PlatformBranch = {
  id: "wh",
  organizationId: "org",
  code: "WH1",
  name: "Iloilo Warehouse",
  isPrimary: false,
  status: "Active",
  branchType: "Warehouse",
  areaId: "south",
};

const formatCount = (count: number) => (count === 1 ? "1 location" : `${count} locations`);
const formatArea = (name: string) => `${name} Area`;

describe("staff-branch-access helpers", () => {
  it("detects owner/admin implicit all-branch access", () => {
    expect(isImplicitAllBranchesMembershipRole("OrganizationOwner")).toBe(true);
    expect(isImplicitAllBranchesMembershipRole("OrganizationAdministrator")).toBe(true);
    expect(isImplicitAllBranchesMembershipRole("OrganizationMember")).toBe(false);
  });

  it("prefers the primary branch for single-branch default", () => {
    expect(resolvePrimaryOrOnlyBranch([north, main])?.id).toBe("main");
    expect(resolvePrimaryOrOnlyBranch([north])?.id).toBe("north");
  });

  it("compares branch id sets regardless of order", () => {
    expect(branchIdsEqual(["a", "b"], ["b", "a"])).toBe(true);
    expect(branchIdsEqual(["a"], ["a", "b"])).toBe(false);
  });

  it("maps persisted scope without inferring from branch equality", () => {
    expect(scopeToMode("AllActive")).toBe("all");
    expect(scopeToMode("Explicit")).toBe("specific");
    expect(scopeToMode("Areas")).toBe("areas");
    expect(modeToScope("all")).toBe("AllActive");
    expect(modeToScope("specific")).toBe("Explicit");
    expect(modeToScope("areas")).toBe("Areas");
  });

  // AREA01-18: a single-branch shop with no areas keeps the simple two-way choice.
  it("keeps the single-branch experience free of area setup", () => {
    expect(shouldOfferAreaScope({ activeBranchCount: 1, activeAreaCount: 0 })).toBe(false);
    expect(shouldOfferAreaScope({ activeBranchCount: 1, activeAreaCount: 3 })).toBe(false);
    expect(shouldOfferAreaScope({ activeBranchCount: 4, activeAreaCount: 0 })).toBe(false);
    expect(shouldOfferAreaScope({ activeBranchCount: 4, activeAreaCount: 2 })).toBe(true);
  });

  it("derives retail and warehouse counts for an area from location data", () => {
    expect(countActiveLocationsInArea("south", [main, north, south, warehouse])).toEqual({
      total: 3,
      retail: 2,
      warehouse: 1,
    });
    expect(countActiveLocationsInArea("north", [main, north, south, warehouse])).toEqual({
      total: 1,
      retail: 1,
      warehouse: 0,
    });
  });

  it("summarises area scope by area name and location count", () => {
    expect(
      formatStaffBranchAccessSummary({
        membershipRole: "OrganizationMember",
        scope: "Areas",
        activeBranches: [main, north, south, warehouse],
        assignedIds: ["main", "south", "wh"],
        allActiveLabel: "All active locations",
        automaticAllLabel: "All active locations",
        unknownLabel: "Not assigned",
        areaNames: ["South"],
        areasLabel: "Areas",
        formatLocationCount: formatCount,
        formatSingleAreaName: formatArea,
      }),
    ).toBe("South Area · 3 locations");

    expect(
      formatStaffBranchAccessSummary({
        membershipRole: "OrganizationMember",
        scope: "Areas",
        activeBranches: [main, north, south, warehouse],
        assignedIds: ["main", "north", "south", "wh"],
        allActiveLabel: "All active locations",
        automaticAllLabel: "All active locations",
        unknownLabel: "Not assigned",
        areaNames: ["North", "South"],
        areasLabel: "Areas",
        formatLocationCount: formatCount,
        formatSingleAreaName: formatArea,
      }),
    ).toBe("North + South · 4 locations");
  });

  it("formats manage-staff branch summaries from scope", () => {
    expect(
      formatStaffBranchAccessSummary({
        membershipRole: "OrganizationOwner",
        scope: null,
        activeBranches: [main, north],
        assignedIds: [],
        allActiveLabel: "All active locations",
        automaticAllLabel: "All active locations",
        unknownLabel: "Not assigned",
        formatLocationCount: formatCount,
      }),
    ).toBe("All active locations");

    expect(
      formatStaffBranchAccessSummary({
        membershipRole: "OrganizationMember",
        scope: "AllActive",
        activeBranches: [main, north],
        assignedIds: [],
        allActiveLabel: "All active locations",
        automaticAllLabel: "All active locations",
        unknownLabel: "Not assigned",
        formatLocationCount: formatCount,
      }),
    ).toBe("All active locations");

    expect(
      formatStaffBranchAccessSummary({
        membershipRole: "OrganizationMember",
        scope: "Explicit",
        activeBranches: [main],
        assignedIds: ["main"],
        allActiveLabel: "All active locations",
        automaticAllLabel: "All active locations",
        unknownLabel: "Not assigned",
        formatLocationCount: formatCount,
      }),
    ).toBe("Main Branch · 1 location");

    expect(
      formatStaffBranchAccessSummary({
        membershipRole: "OrganizationMember",
        scope: "Explicit",
        activeBranches: [main, north, south],
        assignedIds: ["main", "north", "south"],
        allActiveLabel: "All active locations",
        automaticAllLabel: "All active locations",
        unknownLabel: "Not assigned",
        formatLocationCount: formatCount,
      }),
    ).toBe("Main Branch + 2 · 3 locations");

    expect(
      formatStaffBranchAccessSummary({
        membershipRole: "OrganizationMember",
        scope: "Explicit",
        activeBranches: [main, north],
        assignedIds: ["north"],
        allActiveLabel: "All active locations",
        automaticAllLabel: "All active locations",
        unknownLabel: "Not assigned",
        formatLocationCount: formatCount,
      }),
    ).toBe("North · 1 location");
  });
});

import { describe, expect, it } from "vitest";
import type { PlatformBranch } from "@/api/platform/platform-auth-client";
import {
  branchIdsEqual,
  formatStaffBranchAccessSummary,
  isImplicitAllBranchesMembershipRole,
  modeToScope,
  resolvePrimaryOrOnlyBranch,
  scopeToMode,
} from "@/features/staff/staff-branch-access";

const main: PlatformBranch = {
  id: "main",
  organizationId: "org",
  code: "MAIN",
  name: "Main Branch",
  isPrimary: true,
  status: "Active",
};

const north: PlatformBranch = {
  id: "north",
  organizationId: "org",
  code: "NORTH",
  name: "North",
  isPrimary: false,
  status: "Active",
};

const south: PlatformBranch = {
  id: "south",
  organizationId: "org",
  code: "SOUTH",
  name: "South",
  isPrimary: false,
  status: "Active",
};

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
    expect(modeToScope("all")).toBe("AllActive");
    expect(modeToScope("specific")).toBe("Explicit");
  });

  it("formats manage-staff branch summaries from scope", () => {
    expect(
      formatStaffBranchAccessSummary({
        membershipRole: "OrganizationOwner",
        scope: null,
        activeBranches: [main, north],
        assignedIds: [],
        allActiveLabel: "All branches",
        automaticAllLabel: "All branches",
        unknownLabel: "Not assigned",
      }),
    ).toBe("All branches");

    expect(
      formatStaffBranchAccessSummary({
        membershipRole: "OrganizationMember",
        scope: "AllActive",
        activeBranches: [main, north],
        assignedIds: [],
        allActiveLabel: "All branches",
        automaticAllLabel: "All branches",
        unknownLabel: "Not assigned",
      }),
    ).toBe("All branches");

    expect(
      formatStaffBranchAccessSummary({
        membershipRole: "OrganizationMember",
        scope: "Explicit",
        activeBranches: [main],
        assignedIds: ["main"],
        allActiveLabel: "All branches",
        automaticAllLabel: "All branches",
        unknownLabel: "Not assigned",
      }),
    ).toBe("Main Branch");

    expect(
      formatStaffBranchAccessSummary({
        membershipRole: "OrganizationMember",
        scope: "Explicit",
        activeBranches: [main, north, south],
        assignedIds: ["main", "north", "south"],
        allActiveLabel: "All branches",
        automaticAllLabel: "All branches",
        unknownLabel: "Not assigned",
      }),
    ).toBe("Main Branch + 2");

    expect(
      formatStaffBranchAccessSummary({
        membershipRole: "OrganizationMember",
        scope: "Explicit",
        activeBranches: [main, north],
        assignedIds: ["north"],
        allActiveLabel: "All branches",
        automaticAllLabel: "All branches",
        unknownLabel: "Not assigned",
      }),
    ).toBe("North");
  });
});

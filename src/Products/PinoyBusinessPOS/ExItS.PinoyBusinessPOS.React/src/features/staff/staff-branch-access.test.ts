import { describe, expect, it } from "vitest";
import type { PlatformBranch } from "@/api/platform/platform-auth-client";
import {
  branchIdsEqual,
  formatStaffBranchAccessSummary,
  inferBranchScopeMode,
  isImplicitAllBranchesMembershipRole,
  resolvePrimaryOrOnlyBranch,
} from "@/features/staff/staff-branch-access";

const main: PlatformBranch = {
  id: "main",
  organizationId: "org",
  code: "MAIN",
  name: "Main Store",
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

  it("infers all vs specific from current assignments", () => {
    expect(inferBranchScopeMode(["main", "north"], ["north", "main"])).toBe("all");
    expect(inferBranchScopeMode(["main", "north"], ["main"])).toBe("specific");
  });

  it("formats manage-staff branch summaries", () => {
    expect(
      formatStaffBranchAccessSummary({
        membershipRole: "OrganizationOwner",
        activeBranches: [main, north],
        assignedIds: [],
        allActiveLabel: "All active branches",
        automaticAllLabel: "All active branches",
        unknownLabel: "Not assigned",
      }),
    ).toBe("All active branches");

    expect(
      formatStaffBranchAccessSummary({
        membershipRole: "OrganizationMember",
        activeBranches: [main],
        assignedIds: ["main"],
        allActiveLabel: "All active branches",
        automaticAllLabel: "All active branches",
        unknownLabel: "Not assigned",
      }),
    ).toBe("Main Store");

    expect(
      formatStaffBranchAccessSummary({
        membershipRole: "OrganizationMember",
        activeBranches: [main, north],
        assignedIds: ["main", "north"],
        allActiveLabel: "All active branches",
        automaticAllLabel: "All active branches",
        unknownLabel: "Not assigned",
      }),
    ).toBe("All active branches");

    expect(
      formatStaffBranchAccessSummary({
        membershipRole: "OrganizationMember",
        activeBranches: [main, north],
        assignedIds: ["north"],
        allActiveLabel: "All active branches",
        automaticAllLabel: "All active branches",
        unknownLabel: "Not assigned",
      }),
    ).toBe("North");
  });
});

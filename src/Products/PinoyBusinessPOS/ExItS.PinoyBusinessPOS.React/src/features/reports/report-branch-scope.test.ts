import { describe, expect, it } from "vitest";
import {
  canSelectAllBranches,
  reportScopeModeForClassic,
  reportScopeModeForOperational,
  resolveReportBranchIdQuery,
} from "@/features/reports/report-branch-scope";

describe("report-branch-scope", () => {
  it("defaults current branch query for branch-capable reports", () => {
    expect(
      resolveReportBranchIdQuery("branch", { mode: "current" }, "branch-a"),
    ).toBe("branch-a");
    expect(resolveReportBranchIdQuery("branch", { mode: "all" }, "branch-a")).toBeUndefined();
    expect(
      resolveReportBranchIdQuery("branch", { mode: "branch", branchId: "branch-b" }, "branch-a"),
    ).toBe("branch-b");
  });

  it("never sends branchId for organization-only reports", () => {
    expect(
      resolveReportBranchIdQuery("organization_only", { mode: "current" }, "branch-a"),
    ).toBeUndefined();
    expect(
      resolveReportBranchIdQuery("organization_only", { mode: "all" }, "branch-a"),
    ).toBeUndefined();
  });

  it("classifies stock-count and inventory-status as organization-only", () => {
    expect(reportScopeModeForOperational("stock-count-variance")).toBe("organization_only");
    expect(reportScopeModeForOperational("inventory-status")).toBe("organization_only");
    expect(reportScopeModeForOperational("sales-summary")).toBe("branch");
    expect(reportScopeModeForClassic("sales")).toBe("branch");
    expect(reportScopeModeForClassic("expenses")).toBe("organization_only");
  });

  it("allows all-branches for owner/manager/reporting", () => {
    expect(canSelectAllBranches({ isOwner: true })).toBe(true);
    expect(canSelectAllBranches({ isManager: true })).toBe(true);
    expect(canSelectAllBranches({ isReportingUser: true })).toBe(true);
    expect(canSelectAllBranches({})).toBe(false);
  });
});

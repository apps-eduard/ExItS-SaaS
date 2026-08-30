import { describe, expect, it } from "vitest";
import { en } from "@/i18n/locales/en";
import {
  resolveDashboardBranchDisplayName,
  resolveDashboardBranchScopeLabel,
  resolveDashboardOrganizationScopeLabel,
} from "@/features/reports/dashboard-scope";

const t = (key: keyof typeof en) => en[key];

describe("dashboard-scope", () => {
  it("labels organization scope explicitly", () => {
    expect(resolveDashboardOrganizationScopeLabel(t)).toBe("Organization-wide");
  });

  it("labels current branch with name when available", () => {
    expect(
      resolveDashboardBranchScopeLabel(t, { mode: "current" }, "Main Branch"),
    ).toBe("Branch: Main Branch");
  });

  it("labels all branches selection", () => {
    expect(resolveDashboardBranchScopeLabel(t, { mode: "all" }, null)).toBe("All branches");
  });

  it("labels explicit branch selection by id when name missing", () => {
    expect(
      resolveDashboardBranchScopeLabel(
        t,
        { mode: "branch", branchId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb" },
        "Second Branch",
      ),
    ).toBe("Branch: Second Branch");
  });

  it("falls back to generic branch label without display name", () => {
    expect(resolveDashboardBranchScopeLabel(t, { mode: "current" }, null)).toBe("Branch");
  });

  it("resolves branch display name from branch list", () => {
    const branches = [
      { id: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", name: "Main Branch" },
      { id: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb", name: "Second Branch" },
    ];
    expect(
      resolveDashboardBranchDisplayName(
        { mode: "branch", branchId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb" },
        "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
        "Main Branch",
        branches,
      ),
    ).toBe("Second Branch");
  });
});

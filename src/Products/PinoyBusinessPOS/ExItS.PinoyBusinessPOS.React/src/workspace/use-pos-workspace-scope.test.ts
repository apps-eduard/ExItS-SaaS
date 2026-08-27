import { describe, expect, it } from "vitest";
import { posWorkspaceScopeFromBound } from "@/workspace/use-pos-workspace-scope";
import type { BoundWorkspace } from "@/workspace/types";

describe("posWorkspaceScopeFromBound", () => {
  it("returns org scope without branch for Manage Business bind", () => {
    const bound: BoundWorkspace = {
      organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
      organizationDisplayName: "Mica Org",
      branchId: null,
      branchName: null,
      experience: "manage_business",
    };
    expect(posWorkspaceScopeFromBound(bound)).toEqual({
      organizationId: bound.organizationId,
      branchId: null,
    });
  });

  it("includes branch when bound", () => {
    const bound: BoundWorkspace = {
      organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
      organizationDisplayName: "Mica Org",
      branchId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
      branchName: "Main",
      experience: "operations",
    };
    expect(posWorkspaceScopeFromBound(bound)?.branchId).toBe(bound.branchId);
  });
});

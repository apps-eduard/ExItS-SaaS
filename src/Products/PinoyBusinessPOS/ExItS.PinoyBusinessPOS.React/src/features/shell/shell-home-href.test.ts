import { describe, expect, it } from "vitest";
import { shellHomeHref, PERSONAL_HOME_HREF } from "@/features/shell/shell-home-href";
import type { BoundWorkspace } from "@/workspace/types";

function bound(
  partial: Partial<BoundWorkspace> & Pick<BoundWorkspace, "experience">,
): BoundWorkspace {
  return {
    organizationId: "org-1",
    organizationDisplayName: "Demo Org",
    branchId: "branch-1",
    branchName: "Main",
    branchType: "Retail",
    ...partial,
  };
}

describe("shellHomeHref", () => {
  it("returns Personal home when unbound", () => {
    expect(shellHomeHref(null)).toBe(PERSONAL_HOME_HREF);
    expect(shellHomeHref(undefined)).toBe("/personal");
  });

  it("returns warehouse home for warehouse branches", () => {
    expect(
      shellHomeHref(bound({ experience: "operations", branchType: "Warehouse" })),
    ).toBe("/warehouse");
  });

  it("returns working-experience home for retail org", () => {
    expect(shellHomeHref(bound({ experience: "manage_business" }))).toBe("/org");
    expect(shellHomeHref(bound({ experience: "operations" }))).toBe("/role/manager");
    expect(shellHomeHref(bound({ experience: "start_selling" }))).toBe("/sell");
  });
});

import { describe, expect, it } from "vitest";
import { NAV_GROUPS, isNavBranch, navLeaves, resolveSectionId } from "./nav";

describe("playground nav", () => {
  it("exposes Buttons children and aliases legacy #buttons", () => {
    const components = NAV_GROUPS.find((group) => group.label === "Components");
    const buttons = components?.items.find((entry) => isNavBranch(entry) && entry.label === "Buttons");
    expect(buttons && isNavBranch(buttons)).toBe(true);
    if (!buttons || !isNavBranch(buttons)) return;

    expect(buttons.children.map((child) => child.id)).toEqual(["button", "button-group", "button-dropdown"]);
    expect(resolveSectionId("buttons")).toBe("button");
    expect(resolveSectionId("button-group")).toBe("button-group");
    expect(navLeaves().some((leaf) => leaf.id === "button")).toBe(true);
    expect(navLeaves().some((leaf) => leaf.id === "buttons" as never)).toBe(false);
  });
});

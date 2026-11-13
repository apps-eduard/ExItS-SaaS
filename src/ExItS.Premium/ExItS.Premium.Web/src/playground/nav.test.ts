import { describe, expect, it } from "vitest";
import { NAV_GROUPS, branchContains, isNavBranch, navLeaves, resolveSectionId } from "./nav";

describe("playground nav", () => {
  it("exposes Buttons children and aliases legacy #buttons", () => {
    const components = NAV_GROUPS.find((group) => group.label === "Components");
    const buttons = components?.items.find((entry) => isNavBranch(entry) && entry.label === "Buttons");
    expect(buttons && isNavBranch(buttons)).toBe(true);
    if (!buttons || !isNavBranch(buttons)) return;

    expect(buttons.children.map((child) => child.id)).toEqual(["button", "button-dropdown", "button-group"]);
    expect(buttons.children.map((child) => child.status)).toEqual(["ready", "planned", "next"]);
    expect(branchContains(buttons, "button-dropdown")).toBe(true);
    expect(branchContains(buttons, "inputs")).toBe(false);
    expect(resolveSectionId("buttons")).toBe("button");
    expect(resolveSectionId("button-group")).toBe("button-group");
    expect(navLeaves().some((leaf) => leaf.id === "button")).toBe(true);
    expect(navLeaves().some((leaf) => leaf.id === "buttons" as never)).toBe(false);
  });

  it("nests Input and Search under Inputs", () => {
    const components = NAV_GROUPS.find((group) => group.label === "Components");
    const inputs = components?.items.find((entry) => isNavBranch(entry) && entry.label === "Inputs");
    expect(inputs && isNavBranch(inputs)).toBe(true);
    if (!inputs || !isNavBranch(inputs)) return;

    expect(inputs.children.map((child) => child.id)).toEqual(["inputs", "search"]);
    expect(inputs.children.map((child) => child.label)).toEqual(["Input", "Search"]);
    expect(inputs.children.map((child) => child.status)).toEqual(["ready", "ready"]);
    expect(branchContains(inputs, "search")).toBe(true);
    expect(navLeaves().some((leaf) => leaf.id === "search")).toBe(true);
  });

  it("sorts Components entries A–Z by label", () => {
    const components = NAV_GROUPS.find((group) => group.label === "Components");
    expect(components).toBeTruthy();
    const labels = components!.items.map((entry) => entry.label);
    expect(labels).toEqual([...labels].sort((a, b) => a.localeCompare(b)));
  });
});

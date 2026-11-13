import { describe, expect, it } from "vitest";
import { playgroundBreadcrumbTrail } from "./breadcrumb-trail";
import { resolveSectionId } from "./nav";

describe("playgroundBreadcrumbTrail", () => {
  it("maps Buttons children to Design System › Buttons › Current", () => {
    expect(playgroundBreadcrumbTrail("button").map((item) => item.label)).toEqual([
      "Design System",
      "Buttons",
      "Button",
    ]);
    expect(playgroundBreadcrumbTrail("button-group").map((item) => item.label)).toEqual([
      "Design System",
      "Buttons",
      "Button group",
    ]);
    expect(playgroundBreadcrumbTrail("button-dropdown").map((item) => item.label)).toEqual([
      "Design System",
      "Buttons",
      "Button dropdown",
    ]);
  });

  it("keeps prior crumbs clickable and the current page inert", () => {
    const trail = playgroundBreadcrumbTrail("button");
    expect(trail[0]?.href).toBe("#overview");
    expect(trail[1]?.href).toBe("#button");
    expect(trail[2]?.href).toBeUndefined();
  });

  it("maps foundation leaves through the Foundations group", () => {
    expect(playgroundBreadcrumbTrail("colors").map((item) => item.label)).toEqual([
      "Design System",
      "Foundations",
      "Colors",
    ]);
  });

  it("treats overview as the Design System root", () => {
    const trail = playgroundBreadcrumbTrail("overview");
    expect(trail).toHaveLength(1);
    expect(trail[0]?.label).toBe("Design System");
    expect(trail[0]?.href).toBeUndefined();
  });

  it("preserves legacy hash aliases for navigation targets", () => {
    expect(resolveSectionId("buttons")).toBe("button");
    expect(playgroundBreadcrumbTrail(resolveSectionId("buttons")!).map((item) => item.id)).toEqual([
      "design-system",
      "branch-Buttons",
      "button",
    ]);
  });
});

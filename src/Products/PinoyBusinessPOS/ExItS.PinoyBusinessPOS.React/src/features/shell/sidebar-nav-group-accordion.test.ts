import { describe, expect, it } from "vitest";
import {
  areAllGroupsExpanded,
  findActiveGroupId,
  resolveGroupExpandedMap,
} from "@/features/shell/sidebar-nav-group-accordion";

describe("sidebar-nav-group-accordion helpers", () => {
  it("defaults missing groups to expanded", () => {
    expect(resolveGroupExpandedMap({ stock: false }, ["stock", "daily"])).toEqual({
      stock: false,
      daily: true,
    });
  });

  it("finds the group that owns the active item", () => {
    const groups = [
      { id: "daily", items: [{ id: "home" }] },
      { id: "stock", items: [{ id: "inventory" }, { id: "purchasing" }] },
    ];
    expect(findActiveGroupId(groups, "purchasing")).toBe("stock");
    expect(findActiveGroupId(groups, "missing")).toBeNull();
  });

  it("detects all-expanded vs partial", () => {
    expect(areAllGroupsExpanded({ a: true, b: true }, ["a", "b"])).toBe(true);
    expect(areAllGroupsExpanded({ a: true, b: false }, ["a", "b"])).toBe(false);
  });
});

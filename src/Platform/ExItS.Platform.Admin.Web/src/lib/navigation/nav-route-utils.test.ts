import { describe, expect, it } from "vitest";
import { collectOpenStateForPath, itemIsActive, pathMatches } from "@/lib/navigation/nav-route-utils";
import type { ResolvedNavigationSection } from "@/lib/navigation/navigation-types";

const sections: ResolvedNavigationSection[] = [
  {
    id: "billing",
    labelKey: "nav.group.billing",
    icon: "receipt",
    items: [
      {
        id: "payments",
        labelKey: "nav.payments",
        href: "/admin/payments",
        icon: "credit-card",
        presentation: "link",
      },
    ],
  },
];

describe("nav-route-utils", () => {
  it("matches paths with optional search", () => {
    expect(pathMatches("/admin/payments", "/admin/payments", "")).toBe(true);
    expect(pathMatches("/admin/payments", "/admin", "")).toBe(false);
  });

  it("collects section and group ancestors for the active route", () => {
    const { sectionIds, groupIds } = collectOpenStateForPath(sections, "/admin/payments", "");
    expect(sectionIds).toEqual(["billing"]);
    expect(groupIds).toEqual([]);
    expect(itemIsActive(sections[0]!.items[0]!, "/admin/payments", "")).toBe(true);
  });
});

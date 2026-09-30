import { describe, expect, it } from "vitest";
import {
  flattenPersonalNavItems,
  buildPersonalSidebarGroups,
  matchPersonalNavItem,
} from "@/features/personal/personal-nav-config";

describe("personal-nav-config", () => {
  it("matches home exactly without catching nested personal routes", () => {
    const items = flattenPersonalNavItems(buildPersonalSidebarGroups());
    expect(matchPersonalNavItem("/personal", items)).toBe("home");
    expect(matchPersonalNavItem("/personal/utang/lent", items)).toBe("utang");
    expect(matchPersonalNavItem("/personal/people", items)).toBe("people");
    expect(matchPersonalNavItem("/personal/linked-merchants", items)).toBe("stores");
  });
});

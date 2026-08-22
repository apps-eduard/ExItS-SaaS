import { describe, expect, it } from "vitest";
import { pageBackNav } from "@/navigation/page-back-nav";

describe("pageBackNav", () => {
  it("uses explicit parent routes instead of history back", () => {
    expect(pageBackNav.managerHome.to).toBe("/role/manager");
    expect(pageBackNav.shifts.to).toBe("/shifts");
    expect(pageBackNav.customers.to).toBe("/customers");
    expect(pageBackNav.registers.to).toBe("/registers");
    expect(Object.values(pageBackNav).every((entry) => entry.to.startsWith("/"))).toBe(true);
  });
});

import { describe, expect, it } from "vitest";
import { pageBackNav, personalPageBackNav } from "@/navigation/page-back-nav";

describe("pageBackNav", () => {
  it("uses explicit parent routes instead of history back", () => {
    expect(pageBackNav.managerHome.to).toBe("/role/manager");
    expect(pageBackNav.shifts.to).toBe("/shifts");
    expect(pageBackNav.customers.to).toBe("/customers");
    expect(pageBackNav.registers.to).toBe("/registers");
    expect(Object.values(pageBackNav).every((entry) => entry.to.startsWith("/"))).toBe(true);
  });
});

describe("personalPageBackNav", () => {
  it("uses explicit personal parent routes instead of history back", () => {
    expect(personalPageBackNav.home.to).toBe("/personal");
    expect(personalPageBackNav.more.to).toBe("/personal/more");
    expect(personalPageBackNav.utang.to).toBe("/personal/utang");
    expect(personalPageBackNav.todo.to).toBe("/personal/todo");
    expect(personalPageBackNav.orders.to).toBe("/personal/orders");
    expect(personalPageBackNav.merchants.to).toBe("/personal/linked-merchants");
    expect(personalPageBackNav.explore.to).toBe("/personal/explore-pos");
    expect(
      Object.values(personalPageBackNav).every((entry) => entry.to.startsWith("/personal")),
    ).toBe(true);
  });
});

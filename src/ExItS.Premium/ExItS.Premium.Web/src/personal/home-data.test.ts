import { describe, expect, it, vi } from "vitest";
import { formatAmount, loadPersonalHome, moneyTotals } from "./home-data";

describe("personal home data", () => {
  it("keeps each currency separate", () => {
    expect(moneyTotals([
      { currencyCode: "PHP", currentBalance: 10 },
      { currencyCode: "USD", currentBalance: 2 },
      { currencyCode: "PHP", currentBalance: 5 },
    ])).toEqual([
      { currency: "PHP", amount: 15 },
      { currency: "USD", amount: 2 },
    ]);
    expect(formatAmount(15, "PHP")).toMatch(/15/);
  });

  it("loads empty records without inventing balances", async () => {
    vi.stubGlobal("fetch", vi.fn(async (input: RequestInfo | URL) => {
      const url = String(input);
      if (url.includes("linked-merchants")) return new Response(JSON.stringify({ items: [], totalCount: 0 }), { status: 200 });
      if (url.includes("dashboard")) return new Response(JSON.stringify({ pendingConfirmationCount: 0, utangAvailable: true }), { status: 200 });
      return new Response(JSON.stringify([]), { status: 200 });
    }));

    const home = await loadPersonalHome();
    expect(home.lent).toEqual([]);
    expect(home.borrowed).toEqual([]);
    expect(home.stores).toEqual([]);
    expect(home.tasks).toEqual([]);
    expect(home.activity).toEqual([]);
    vi.unstubAllGlobals();
  });

  it("uses returned balances and leaves a failed section unavailable", async () => {
    vi.stubGlobal("fetch", vi.fn(async (input: RequestInfo | URL) => {
      const url = String(input);
      if (url.includes("/lent")) {
        return new Response(JSON.stringify([{
          id: "rel-1",
          currencyCode: "PHP",
          currentBalance: 40,
          dueDateUtc: "2026-12-01T00:00:00Z",
          status: "Active",
          updatedAtUtc: "2026-11-01T00:00:00Z",
        }]), { status: 200 });
      }
      if (url.includes("/todos")) return new Response(JSON.stringify({ detail: "Personal tasks are unavailable." }), { status: 503 });
      if (url.includes("linked-merchants")) return new Response(JSON.stringify({ items: [] }), { status: 200 });
      if (url.includes("dashboard")) return new Response(JSON.stringify({ pendingConfirmationCount: 1 }), { status: 200 });
      return new Response(JSON.stringify([]), { status: 200 });
    }));

    const home = await loadPersonalHome(new Date("2026-11-16T00:00:00Z"));
    expect(home.lent).toEqual([{ currency: "PHP", amount: 40 }]);
    expect(home.upcoming[0]?.label).toMatch(/Money lent/);
    expect(home.attention).toContain("1 payment confirmation waiting.");
    expect(home.unavailable.tasks).toMatch(/try again/i);
    expect(home.unavailable.tasks).not.toMatch(/\/api\//);
    expect(home.tasks).toEqual([]);
    vi.unstubAllGlobals();
  });
});

import { describe, expect, it, vi } from "vitest";
import { canChangeLedger, formatAmount, loadPersonalHome, moneyTotals, ownershipLabels } from "./home-data";

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

  it("keeps my records and shared-with-me ledgers apart", async () => {
    const me = "11111111-1111-1111-1111-111111111111";
    const other = "22222222-2222-2222-2222-222222222222";
    const row = (
      id: string,
      balance: number,
      currency: string,
      extra: Record<string, unknown>,
    ) => ({
      id,
      currencyCode: currency,
      currentBalance: balance,
      dueDateUtc: null,
      status: "Active",
      updatedAtUtc: "2026-11-01T00:00:00Z",
      ...extra,
    });

    vi.stubGlobal("fetch", vi.fn(async (input: RequestInfo | URL) => {
      const url = String(input);
      if (url.includes("/lent")) {
        return new Response(JSON.stringify([
          row("private-lent", 10, "PHP", { perspective: "Lent", isLedgerOwner: true, isSharedLedger: false, isPrivate: true, ledgerOwnerUserIdentityId: me }),
          row("shared-lent-mine", 15, "PHP", { perspective: "Lent", isLedgerOwner: true, isSharedLedger: true, isPrivate: false, ledgerOwnerUserIdentityId: me }),
          row("shared-lent-theirs", 7, "USD", { perspective: "Lent", isLedgerOwner: false, isSharedLedger: true, isPrivate: false, ledgerOwnerUserIdentityId: other }),
          row("closed-lent", 99, "PHP", { perspective: "Lent", isLedgerOwner: true, isPrivate: true, status: "Closed" }),
          row("private-lent", 10, "PHP", { perspective: "Lent", isLedgerOwner: true, isPrivate: true, ledgerOwnerUserIdentityId: me }),
        ]), { status: 200 });
      }
      if (url.includes("/borrowed")) {
        return new Response(JSON.stringify([
          row("private-owe", 4, "PHP", { perspective: "Borrowed", isLedgerOwner: true, isSharedLedger: false, isPrivate: true, ledgerOwnerUserIdentityId: me }),
          row("shared-owe-mine", 6, "EUR", { perspective: "Borrowed", isLedgerOwner: true, isSharedLedger: true, isPrivate: false, ledgerOwnerUserIdentityId: me }),
          row("shared-owe-theirs", 3, "PHP", { perspective: "Borrowed", isLedgerOwner: false, isSharedLedger: true, isPrivate: false, ledgerOwnerUserIdentityId: other }),
          row("archived-owe", 50, "PHP", { perspective: "Borrowed", isLedgerOwner: true, isPrivate: true, status: "Archived" }),
          row("private-lent", 10, "PHP", { perspective: "Lent", isLedgerOwner: true, isPrivate: true, ledgerOwnerUserIdentityId: me }),
        ]), { status: 200 });
      }
      if (url.includes("dashboard")) return new Response(JSON.stringify({ pendingConfirmationCount: 0, utangAvailable: true }), { status: 200 });
      if (url.includes("linked-merchants")) return new Response(JSON.stringify({ items: [] }), { status: 200 });
      return new Response(JSON.stringify([]), { status: 200 });
    }));

    const home = await loadPersonalHome();
    expect(home.lent).toEqual([{ currency: "PHP", amount: 25 }]);
    expect(home.borrowed).toEqual([
      { currency: "PHP", amount: 4 },
      { currency: "EUR", amount: 6 },
    ]);
    expect(home.sharedLent).toEqual([{ currency: "USD", amount: 7 }]);
    expect(home.sharedBorrowed).toEqual([{ currency: "PHP", amount: 3 }]);
    expect(JSON.stringify(home)).not.toContain("99");
    expect(JSON.stringify(home)).not.toContain("50");
    expect(ownershipLabels({ isLedgerOwner: true, isSharedLedger: false, isPrivate: true })).toEqual(["private"]);
    expect(ownershipLabels({ isLedgerOwner: true, isSharedLedger: true, isPrivate: false })).toEqual(["sharedByMe"]);
    expect(ownershipLabels({ isLedgerOwner: false, isSharedLedger: true, isPrivate: false })).toEqual(["sharedWithMe", "readOnly"]);
    expect(canChangeLedger({ isLedgerOwner: true })).toBe(true);
    expect(canChangeLedger({ isLedgerOwner: false })).toBe(false);
    vi.unstubAllGlobals();
  });
});

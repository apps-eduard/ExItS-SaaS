import "fake-indexeddb/auto";
import { describe, expect, it } from "vitest";
import { openOfflineDatabase, organizationScopeKey } from "@/offline/db";
import {
  getCachedPriceAuthority,
  isPriceAuthorityUsable,
  loadUsablePriceAuthorities,
  priceAuthorityLeaseKey,
  pruneExpiredPriceAuthorities,
  putPriceAuthorities,
} from "@/offline/price-authority-cache";
import { mockPriceAuthority } from "@/test/mock-price-authority";

const productId = "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee";
const packUnitId = "ffffffff-ffff-4fff-8fff-ffffffffffff";

async function openDb(userId: string) {
  return openOfflineDatabase(
    "Organization",
    organizationScopeKey({
      userId,
      organizationId: "11111111-1111-4111-8111-111111111111",
      branchId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
      installationDeviceId: "22222222-2222-4222-8222-222222222222",
    }),
  );
}

function expiredAt(offsetHours: number) {
  const issued = new Date(Date.now() - offsetHours * 60 * 60 * 1000);
  return {
    issuedAtUtc: issued.toISOString(),
    expiresAtUtc: new Date(issued.getTime() + 8 * 60 * 60 * 1000).toISOString(),
  };
}

describe("offline price lease cache", () => {
  it("stores one lease per sellable line shape", async () => {
    const db = await openDb("user-shapes");
    const base = mockPriceAuthority({ productId, unitPrice: 10 });
    const pack = mockPriceAuthority({ productId, sellingUnitId: packUnitId, unitPrice: 55 });

    await putPriceAuthorities(db, [base, pack]);

    expect((await getCachedPriceAuthority(db, productId, null))?.unitPrice).toBe(10);
    expect((await getCachedPriceAuthority(db, productId, packUnitId))?.unitPrice).toBe(55);
    expect(await getCachedPriceAuthority(db, productId, "no-such-unit")).toBeNull();
    db.close();
  });

  it("replaces a lease when the server issues a newer one for the same shape", async () => {
    const db = await openDb("user-replace");
    await putPriceAuthorities(db, [mockPriceAuthority({ productId, unitPrice: 10 })]);
    await putPriceAuthorities(db, [mockPriceAuthority({ productId, unitPrice: 12 })]);

    const lookup = await loadUsablePriceAuthorities(db);
    expect(lookup.size).toBe(1);
    expect(lookup.get(priceAuthorityLeaseKey(productId, null))?.unitPrice).toBe(12);
    db.close();
  });

  it("treats a closed window as no lease at all", () => {
    expect(isPriceAuthorityUsable(mockPriceAuthority({ productId }))).toBe(true);
    expect(isPriceAuthorityUsable(mockPriceAuthority({ productId, ...expiredAt(30) }))).toBe(false);
    // A window that never opened cannot be trusted either.
    expect(
      isPriceAuthorityUsable({
        issuedAtUtc: new Date().toISOString(),
        expiresAtUtc: new Date(Date.now() - 1000).toISOString(),
      }),
    ).toBe(false);
    expect(isPriceAuthorityUsable({ issuedAtUtc: "not-a-date", expiresAtUtc: "also-not" })).toBe(
      false,
    );
  });

  it("omits and then drops leases whose window has closed", async () => {
    const db = await openDb("user-expiry");
    const live = mockPriceAuthority({ productId, unitPrice: 10 });
    const stale = mockPriceAuthority({
      productId: "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeee1",
      ...expiredAt(30),
    });
    await putPriceAuthorities(db, [live, stale]);

    const usable = await loadUsablePriceAuthorities(db);
    expect([...usable.keys()]).toEqual([priceAuthorityLeaseKey(productId, null)]);

    expect(await pruneExpiredPriceAuthorities(db)).toBe(1);
    expect(await db.count("priceAuthorities")).toBe(1);
    db.close();
  });
});

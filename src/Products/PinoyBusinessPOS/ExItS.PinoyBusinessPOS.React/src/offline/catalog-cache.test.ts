import "fake-indexeddb/auto";
import { describe, expect, it } from "vitest";
import type { PosCatalogProductDto, PosProductCategoryDto } from "@/api/pos/pos-catalog-types";
import {
  listCachedCatalogCategories,
  listCachedCatalogProducts,
  replaceCatalogCache,
} from "@/offline/catalog-cache";
import { openOfflineDatabase, organizationScopeKey } from "@/offline/db";
import {
  isSellReadinessSnapshotUsable,
  loadSellReadinessSnapshot,
  saveSellReadinessSnapshot,
} from "@/offline/sell-readiness-snapshot";

const organizationId = "11111111-1111-4111-8111-111111111111";

function product(productId: string, name: string): PosCatalogProductDto {
  return {
    productId,
    organizationId,
    name,
    unitOfMeasure: "pc",
    sellingMode: "PerItem",
    sellingPrice: 25,
    status: "Active",
    createdAtUtc: "2026-08-21T00:00:00Z",
    updatedAtUtc: "2026-08-21T00:00:00Z",
  };
}

function category(categoryId: string, name: string): PosProductCategoryDto {
  return {
    categoryId,
    organizationId,
    name,
    status: "Active",
    createdAtUtc: "2026-08-21T00:00:00Z",
    updatedAtUtc: "2026-08-21T00:00:00Z",
  };
}

function openDb(userId: string) {
  return openOfflineDatabase(
    "Organization",
    organizationScopeKey({
      userId,
      organizationId,
      branchId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
      installationDeviceId: "22222222-2222-4222-8222-222222222222",
    }),
  );
}

describe("RMAP-21D Sell catalog cache", () => {
  it("fails closed to an empty catalog before any write-through", async () => {
    const db = await openDb("catalog-empty");
    expect(await listCachedCatalogProducts(db)).toEqual([]);
    expect(await listCachedCatalogCategories(db)).toEqual([]);
    db.close();
  });

  it("replaces the cache so a removed product does not linger offline", async () => {
    const db = await openDb("catalog-replace");
    await replaceCatalogCache(
      db,
      [product("p-1", "Coke"), product("p-2", "Bigas")],
      [category("c-1", "Drinks")],
    );
    expect((await listCachedCatalogProducts(db)).map((item) => item.name).sort()).toEqual([
      "Bigas",
      "Coke",
    ]);

    await replaceCatalogCache(db, [product("p-2", "Bigas")], []);
    expect((await listCachedCatalogProducts(db)).map((item) => item.productId)).toEqual(["p-2"]);
    expect(await listCachedCatalogCategories(db)).toEqual([]);
    db.close();
  });
});

describe("RMAP-21D Sell readiness snapshot", () => {
  it("round-trips the last-good readiness", async () => {
    const db = await openDb("readiness-roundtrip");
    expect(await loadSellReadinessSnapshot(db)).toBeNull();

    await saveSellReadinessSnapshot(db, {
      deviceReady: true,
      moneyPostReady: true,
      shiftId: "cccccccc-cccc-4ccc-8ccc-cccccccccccc",
      openShiftNumber: "S-1001",
    });

    const loaded = await loadSellReadinessSnapshot(db);
    expect(loaded?.shiftId).toBe("cccccccc-cccc-4ccc-8ccc-cccccccccccc");
    expect(loaded?.openShiftNumber).toBe("S-1001");
    expect(isSellReadinessSnapshotUsable(loaded)).toBe(true);
    db.close();
  });

  it("treats a stale or incomplete snapshot as unusable", () => {
    expect(isSellReadinessSnapshotUsable(null)).toBe(false);
    expect(
      isSellReadinessSnapshotUsable({
        deviceReady: true,
        moneyPostReady: true,
        shiftId: null,
        openShiftNumber: null,
        capturedAt: new Date().toISOString(),
      }),
    ).toBe(false);
    expect(
      isSellReadinessSnapshotUsable({
        deviceReady: true,
        moneyPostReady: true,
        shiftId: "cccccccc-cccc-4ccc-8ccc-cccccccccccc",
        openShiftNumber: "S-1001",
        capturedAt: new Date(Date.now() - 3 * 24 * 60 * 60 * 1000).toISOString(),
      }),
    ).toBe(false);
  });
});

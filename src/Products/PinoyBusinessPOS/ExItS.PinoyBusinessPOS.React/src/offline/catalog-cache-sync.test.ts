import "fake-indexeddb/auto";
import { describe, expect, it, vi, beforeEach } from "vitest";
import type { PosCatalogProductDto, PosProductCategoryDto } from "@/api/pos/pos-catalog-types";
import * as catalogCache from "@/offline/catalog-cache";
import {
  catalogCacheFingerprint,
  getCatalogCacheSyncStats,
  resetCatalogCacheSyncStateForTests,
  syncCatalogCacheIfNeeded,
} from "@/offline/catalog-cache-sync";
import { openOfflineDatabase, organizationScopeKey } from "@/offline/db";

const organizationId = "11111111-1111-4111-8111-111111111111";
const branchId = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";

function product(productId: string): PosCatalogProductDto {
  return {
    productId,
    organizationId,
    name: productId,
    unitOfMeasure: "pc",
    sellingMode: "PerItem",
    sellingPrice: 25,
    status: "Active",
    createdAtUtc: "2026-08-21T00:00:00Z",
    updatedAtUtc: "2026-08-21T00:00:00Z",
  };
}

function category(categoryId: string): PosProductCategoryDto {
  return {
    categoryId,
    organizationId,
    name: categoryId,
    status: "Active",
    createdAtUtc: "2026-08-21T00:00:00Z",
    updatedAtUtc: "2026-08-21T00:00:00Z",
  };
}

function openDb(suffix: string) {
  return openOfflineDatabase(
    "Organization",
    organizationScopeKey({
      userId: `user-${suffix}`,
      organizationId,
      branchId,
      installationDeviceId: "22222222-2222-4222-8222-222222222222",
    }),
  );
}

describe("catalog cache sync deduplication", () => {
  beforeEach(() => {
    resetCatalogCacheSyncStateForTests();
    vi.restoreAllMocks();
  });

  it("writes once for identical browse snapshots across repeated calls", async () => {
    const replaceSpy = vi.spyOn(catalogCache, "replaceCatalogCache").mockResolvedValue();
    const db = await openDb("dedupe-once");
    const scope = { organizationId, branchId };
    const products = [product("p-1"), product("p-2")];
    const categories = [category("c-1")];

    for (let index = 0; index < 20; index += 1) {
      await syncCatalogCacheIfNeeded(db, scope, products, categories);
    }

    expect(replaceSpy).toHaveBeenCalledTimes(1);
    expect(getCatalogCacheSyncStats(db).writeCount).toBe(1);
    db.close();
  });

  it("coalesces concurrent writes for the same fingerprint", async () => {
    const replaceSpy = vi.spyOn(catalogCache, "replaceCatalogCache").mockImplementation(
      () => new Promise((resolve) => window.setTimeout(resolve, 20)),
    );
    const db = await openDb("dedupe-concurrent");
    const scope = { organizationId, branchId };
    const products = [product("p-1")];
    const categories = [category("c-1")];

    await Promise.all([
      syncCatalogCacheIfNeeded(db, scope, products, categories),
      syncCatalogCacheIfNeeded(db, scope, products, categories),
      syncCatalogCacheIfNeeded(db, scope, products, categories),
    ]);

    expect(replaceSpy).toHaveBeenCalledTimes(1);
    db.close();
  });

  it("writes again when the catalog snapshot changes", async () => {
    vi.spyOn(catalogCache, "replaceCatalogCache").mockResolvedValue();
    const db = await openDb("dedupe-version");
    const scope = { organizationId, branchId };
    const firstProducts = [product("p-1")];
    const nextProducts = [{ ...product("p-1"), updatedAtUtc: "2026-08-22T00:00:00Z" }];
    const categories = [category("c-1")];

    await syncCatalogCacheIfNeeded(db, scope, firstProducts, categories);
    await syncCatalogCacheIfNeeded(db, scope, nextProducts, categories);

    expect(getCatalogCacheSyncStats(db).writeCount).toBe(2);
    expect(catalogCacheFingerprint(scope, firstProducts, categories)).not.toBe(
      catalogCacheFingerprint(scope, nextProducts, categories),
    );
    db.close();
  });
});

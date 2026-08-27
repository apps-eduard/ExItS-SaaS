import "fake-indexeddb/auto";
import { useEffect } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, waitFor } from "@testing-library/react";
import type { PosCatalogProductDto, PosProductCategoryDto } from "@/api/pos/pos-catalog-types";
import * as catalogCache from "@/offline/catalog-cache";
import {
  getCatalogCacheSyncStats,
  resetCatalogCacheSyncStateForTests,
  syncCatalogCacheIfNeeded,
} from "@/offline/catalog-cache-sync";
import { openOfflineDatabase, organizationScopeKey } from "@/offline/db";

const organizationId = "11111111-1111-1111-1111-111111111111";
const branchId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";

const categories: PosProductCategoryDto[] = [
  {
    categoryId: "dddddddd-dddd-dddd-dddd-dddddddddddd",
    organizationId,
    name: "Drinks",
    status: "Active",
    createdAtUtc: "2026-01-01T00:00:00Z",
    updatedAtUtc: "2026-01-01T00:00:00Z",
  },
];

const products: PosCatalogProductDto[] = [
  {
    productId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
    organizationId,
    name: "Coke 330ml",
    unitOfMeasure: "Bottle",
    sellingMode: "PerItem",
    sellingPrice: 25,
    status: "Active",
    createdAtUtc: "2026-01-01T00:00:00Z",
    updatedAtUtc: "2026-01-01T00:00:00Z",
  },
];

const scope = { organizationId, branchId };

/** Mirrors SellFloorPage write-through effect dependencies without mounting the full sell floor. */
function SellCatalogCacheSyncProbe(props: {
  db: Awaited<ReturnType<typeof openOfflineDatabase>>;
  online: boolean;
}) {
  useEffect(() => {
    if (!props.online) {
      return;
    }
    void syncCatalogCacheIfNeeded(props.db, scope, products, categories).catch(() => {
      // Same fail-open behavior as SellFloorPage.
    });
  }, [props.db, props.online]);

  return null;
}

describe("SellFloorPage remount sync", () => {
  beforeEach(() => {
    resetCatalogCacheSyncStateForTests();
    vi.restoreAllMocks();
    vi.spyOn(catalogCache, "replaceCatalogCache").mockResolvedValue();
  });

  it("does not accumulate IndexedDB catalog rewrites across repeated Sell remounts", async () => {
    const db = await openOfflineDatabase(
      "Organization",
      organizationScopeKey({
        userId: "user-remount",
        organizationId,
        branchId,
        installationDeviceId: "aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee",
      }),
    );

    for (let cycle = 0; cycle < 20; cycle += 1) {
      const { unmount } = render(<SellCatalogCacheSyncProbe db={db} online />);
      await waitFor(() => {
        expect(getCatalogCacheSyncStats(db).writeCount).toBeGreaterThanOrEqual(1);
      });
      unmount();
    }

    expect(catalogCache.replaceCatalogCache).toHaveBeenCalledTimes(1);
    expect(getCatalogCacheSyncStats(db).writeCount).toBe(1);
    db.close();
  });
});

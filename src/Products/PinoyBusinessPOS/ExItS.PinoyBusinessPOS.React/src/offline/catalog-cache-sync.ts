import type { PosCatalogProductDto, PosProductCategoryDto } from "@/api/pos/pos-catalog-types";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import { replaceCatalogCache } from "@/offline/catalog-cache";
import type { OfflineDb } from "@/offline/db";

export type CatalogCacheScope = Pick<PosWorkspaceScope, "organizationId" | "branchId">;

/** Stable fingerprint for a full-catalog browse snapshot (org + branch + row versions). */
export function catalogCacheFingerprint(
  scope: CatalogCacheScope,
  products: ReadonlyArray<PosCatalogProductDto>,
  categories: ReadonlyArray<PosProductCategoryDto>,
): string {
  const productSig = products
    .map((product) => `${product.productId}:${product.updatedAtUtc}`)
    .sort()
    .join("|");
  const categorySig = categories
    .map((category) => `${category.categoryId}:${category.updatedAtUtc}`)
    .sort()
    .join("|");
  return `${scope.organizationId}::${scope.branchId}::${productSig}::${categorySig}`;
}

type SyncState = {
  lastSyncedFingerprint: string | null;
  inFlight: Promise<void> | null;
  inFlightFingerprint: string | null;
  writeCount: number;
};

const syncByDb = new Map<string, SyncState>();

function dbKey(db: OfflineDb): string {
  return db.name;
}

function stateFor(db: OfflineDb): SyncState {
  const key = dbKey(db);
  let state = syncByDb.get(key);
  if (!state) {
    state = {
      lastSyncedFingerprint: null,
      inFlight: null,
      inFlightFingerprint: null,
      writeCount: 0,
    };
    syncByDb.set(key, state);
  }
  return state;
}

export function resetCatalogCacheSyncStateForTests(): void {
  syncByDb.clear();
}

export function getCatalogCacheSyncStats(db: OfflineDb): {
  writeCount: number;
  lastSyncedFingerprint: string | null;
  inFlight: boolean;
} {
  const state = stateFor(db);
  return {
    writeCount: state.writeCount,
    lastSyncedFingerprint: state.lastSyncedFingerprint,
    inFlight: state.inFlight !== null,
  };
}

/**
 * Write-through offline catalog once per genuinely fresh browse snapshot.
 * Remounting Sell with the same React Query payload must not rewrite IndexedDB again.
 */
export async function syncCatalogCacheIfNeeded(
  db: OfflineDb,
  scope: CatalogCacheScope,
  products: ReadonlyArray<PosCatalogProductDto>,
  categories: ReadonlyArray<PosProductCategoryDto>,
): Promise<void> {
  const fingerprint = catalogCacheFingerprint(scope, products, categories);
  const state = stateFor(db);

  if (fingerprint === state.lastSyncedFingerprint) {
    return;
  }

  if (state.inFlight && state.inFlightFingerprint === fingerprint) {
    await state.inFlight;
    return;
  }

  if (state.inFlight) {
    await state.inFlight.catch(() => {
      // Prior write failure must not block a newer snapshot.
    });
    if (fingerprint === state.lastSyncedFingerprint) {
      return;
    }
  }

  state.inFlightFingerprint = fingerprint;
  state.inFlight = (async () => {
    try {
      await replaceCatalogCache(db, products, categories);
      state.lastSyncedFingerprint = fingerprint;
      state.writeCount += 1;
    } finally {
      state.inFlight = null;
      state.inFlightFingerprint = null;
    }
  })();

  await state.inFlight;
}

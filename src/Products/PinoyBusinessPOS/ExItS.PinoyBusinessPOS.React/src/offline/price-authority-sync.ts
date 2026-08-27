import type { PosCatalogProductDto } from "@/api/pos/pos-catalog-types";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import { refreshPriceAuthoritiesForProducts } from "@/offline/price-authority-refresh";
import type { OfflineDb } from "@/offline/db";

export type PriceAuthorityScope = Pick<PosWorkspaceScope, "organizationId" | "branchId">;

/** Same catalog snapshot fingerprint used for lease refresh deduplication. */
export function priceAuthorityRefreshFingerprint(
  scope: PriceAuthorityScope,
  products: ReadonlyArray<PosCatalogProductDto>,
): string {
  const productSig = products
    .map((product) => `${product.productId}:${product.updatedAtUtc}`)
    .sort()
    .join("|");
  return `${scope.organizationId}::${scope.branchId}::${productSig}`;
}

type RefreshState = {
  lastRefreshedFingerprint: string | null;
  inFlight: Promise<number> | null;
  inFlightFingerprint: string | null;
  refreshCount: number;
};

const refreshByDb = new Map<string, RefreshState>();

function dbKey(db: OfflineDb): string {
  return db.name;
}

function stateFor(db: OfflineDb): RefreshState {
  const key = dbKey(db);
  let state = refreshByDb.get(key);
  if (!state) {
    state = {
      lastRefreshedFingerprint: null,
      inFlight: null,
      inFlightFingerprint: null,
      refreshCount: 0,
    };
    refreshByDb.set(key, state);
  }
  return state;
}

export function resetPriceAuthoritySyncStateForTests(): void {
  refreshByDb.clear();
}

export function getPriceAuthoritySyncStats(db: OfflineDb): {
  refreshCount: number;
  lastRefreshedFingerprint: string | null;
  inFlight: boolean;
} {
  const state = stateFor(db);
  return {
    refreshCount: state.refreshCount,
    lastRefreshedFingerprint: state.lastRefreshedFingerprint,
    inFlight: state.inFlight !== null,
  };
}

/**
 * Issue offline price leases once per catalog browse snapshot. Aborts when the Sell route unmounts,
 * but identical remounts must not pile up duplicate lease requests.
 */
export async function refreshPriceAuthoritiesIfNeeded(
  db: OfflineDb,
  workspace: PriceAuthorityScope,
  products: ReadonlyArray<PosCatalogProductDto>,
  signal?: AbortSignal,
): Promise<number> {
  if (signal?.aborted) {
    return 0;
  }

  const fingerprint = priceAuthorityRefreshFingerprint(workspace, products);
  const state = stateFor(db);

  if (fingerprint === state.lastRefreshedFingerprint) {
    return 0;
  }

  if (state.inFlight && state.inFlightFingerprint === fingerprint) {
    return state.inFlight;
  }

  if (state.inFlight) {
    await state.inFlight.catch(() => {});
    if (signal?.aborted || fingerprint === state.lastRefreshedFingerprint) {
      return 0;
    }
  }

  state.inFlightFingerprint = fingerprint;
  state.inFlight = (async () => {
    try {
      const count = await refreshPriceAuthoritiesForProducts(db, workspace, products, signal);
      if (!signal?.aborted) {
        state.lastRefreshedFingerprint = fingerprint;
        state.refreshCount += 1;
      }
      return count;
    } finally {
      state.inFlight = null;
      state.inFlightFingerprint = null;
    }
  })();

  return state.inFlight;
}

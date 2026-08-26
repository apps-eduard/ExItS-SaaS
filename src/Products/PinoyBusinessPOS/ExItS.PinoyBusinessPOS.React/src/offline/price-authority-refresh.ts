import type { PosCatalogProductDto } from "@/api/pos/pos-catalog-types";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import {
  issueOfflinePriceAuthorities,
  MAX_OFFLINE_PRICE_AUTHORITIES_PER_REQUEST,
  type OfflinePriceAuthorityRequestItem,
} from "@/api/pos/pos-offline-price-authority-client";
import { activeSellUnits } from "@/cart/sell-cart-helpers";
import type { OfflineDb } from "@/offline/db";
import { pruneExpiredPriceAuthorities, putPriceAuthorities } from "@/offline/price-authority-cache";

/**
 * Sell-floor lease refresh (RMAP-21 Review Repair 01).
 *
 * Runs beside the catalog write-through: a product the cashier can see on a warm sell floor is a
 * product they may need to sell after the network drops, so the price is leased at the same moment
 * it is cached. Every sellable shape gets its own lease, because a pack and a single piece are two
 * different prices and the server signs each one separately.
 */
export function priceAuthorityRequestsFor(
  products: ReadonlyArray<PosCatalogProductDto>,
): OfflinePriceAuthorityRequestItem[] {
  const items: OfflinePriceAuthorityRequestItem[] = [];
  for (const product of products) {
    items.push({ productId: product.productId, sellingUnitId: null });
    for (const unit of activeSellUnits(product)) {
      items.push({ productId: product.productId, sellingUnitId: unit.unitId });
    }
  }
  return items.slice(0, MAX_OFFLINE_PRICE_AUTHORITIES_PER_REQUEST);
}

/**
 * Issues and caches leases for the products just browsed. Failure is silent by design: a lease
 * that could not be issued simply is not there, and the offline confirm gate then refuses the sale
 * instead of guessing a price.
 */
export async function refreshPriceAuthoritiesForProducts(
  db: OfflineDb,
  workspace: PosWorkspaceScope,
  products: ReadonlyArray<PosCatalogProductDto>,
  signal?: AbortSignal,
): Promise<number> {
  const items = priceAuthorityRequestsFor(products);
  if (items.length === 0) {
    return 0;
  }

  const issued = await issueOfflinePriceAuthorities(workspace, items, signal);
  if (signal?.aborted) {
    return 0;
  }
  await putPriceAuthorities(db, issued.authorities);
  await pruneExpiredPriceAuthorities(db);
  return issued.authorities.length;
}

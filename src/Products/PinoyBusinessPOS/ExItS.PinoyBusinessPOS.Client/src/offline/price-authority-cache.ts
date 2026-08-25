import type { OfflinePriceAuthority } from "@/api/pos/pos-offline-price-authority-client";
import type { OfflineDb } from "@/offline/db";
import type { CachedOfflinePriceAuthorityRecord } from "@/offline/types";

/**
 * Offline price lease cache (RMAP-21 Review Repair 01).
 *
 * Write-through from a successful online issue only. A lease is never minted, extended, or
 * repriced here — the client can only keep one, hand it back, or find it unusable and refuse to
 * sell. Reads fail closed to "no lease" so a broken cache blocks an offline sale instead of
 * letting one through at a price nobody signed.
 */

/** One lease per sellable line shape: the base unit and a pack are different prices. */
export function priceAuthorityLeaseKey(
  productId: string,
  sellingUnitId: string | null | undefined,
): string {
  return `${productId}::${sellingUnitId ?? "base"}`;
}

export async function putPriceAuthorities(
  db: OfflineDb,
  authorities: ReadonlyArray<OfflinePriceAuthority>,
): Promise<void> {
  if (authorities.length === 0) {
    return;
  }

  const cachedAtUtc = new Date().toISOString();
  const tx = db.transaction("priceAuthorities", "readwrite");
  for (const authority of authorities) {
    const sellingUnitId = authority.sellingUnitId ?? null;
    const record: CachedOfflinePriceAuthorityRecord = {
      leaseKey: priceAuthorityLeaseKey(authority.productId, sellingUnitId),
      productId: authority.productId,
      sellingUnitId,
      cachedAtUtc,
      expiresAtUtc: authority.expiresAtUtc,
      authority,
    };
    await tx.store.put(record);
  }
  await tx.done;
}

export async function listCachedPriceAuthorities(
  db: OfflineDb,
): Promise<CachedOfflinePriceAuthorityRecord[]> {
  try {
    return await db.getAll("priceAuthorities");
  } catch {
    return [];
  }
}

export async function getCachedPriceAuthority(
  db: OfflineDb,
  productId: string,
  sellingUnitId: string | null | undefined,
): Promise<OfflinePriceAuthority | null> {
  try {
    const row = await db.get("priceAuthorities", priceAuthorityLeaseKey(productId, sellingUnitId));
    return row?.authority ?? null;
  } catch {
    return null;
  }
}

/**
 * A lease is usable while the server's own validity window is still open. The device clock is the
 * only clock available offline, so this is deliberately strict: a clock that has drifted forward
 * refuses to sell rather than selling on a lease the server will reject at sync.
 */
export function isPriceAuthorityUsable(
  authority: Pick<OfflinePriceAuthority, "issuedAtUtc" | "expiresAtUtc">,
  now: Date = new Date(),
): boolean {
  const issued = Date.parse(authority.issuedAtUtc);
  const expires = Date.parse(authority.expiresAtUtc);
  if (!Number.isFinite(issued) || !Number.isFinite(expires) || expires <= issued) {
    return false;
  }
  return now.getTime() <= expires;
}

/** Drops leases whose window has closed, so a stale price cannot linger on the device. */
export async function pruneExpiredPriceAuthorities(
  db: OfflineDb,
  now: Date = new Date(),
): Promise<number> {
  try {
    const rows = await db.getAll("priceAuthorities");
    const stale = rows.filter((row) => !isPriceAuthorityUsable(row.authority, now));
    if (stale.length === 0) {
      return 0;
    }
    const tx = db.transaction("priceAuthorities", "readwrite");
    for (const row of stale) {
      await tx.store.delete(row.leaseKey);
    }
    await tx.done;
    return stale.length;
  } catch {
    return 0;
  }
}

export type PriceAuthorityLookup = ReadonlyMap<string, OfflinePriceAuthority>;

/** Loads every usable lease as a map keyed the way a cart line looks one up. */
export async function loadUsablePriceAuthorities(
  db: OfflineDb,
  now: Date = new Date(),
): Promise<PriceAuthorityLookup> {
  const rows = await listCachedPriceAuthorities(db);
  const map = new Map<string, OfflinePriceAuthority>();
  for (const row of rows) {
    if (isPriceAuthorityUsable(row.authority, now)) {
      map.set(row.leaseKey, row.authority);
    }
  }
  return map;
}

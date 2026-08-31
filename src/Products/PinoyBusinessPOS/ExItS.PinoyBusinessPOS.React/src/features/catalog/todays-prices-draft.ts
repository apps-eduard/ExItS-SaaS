import type { PosCatalogProductDto } from "@/api/pos/pos-catalog-types";

/** Practical JS-safe upper bound (server max is larger; server remains authoritative). */
export const SELLING_PRICE_MAX = 9_999_999_999.99;

export type PriceDraft = {
  productId: string;
  name: string;
  brandName?: string | null;
  scope?: string | null;
  currentPrice: number;
  draftPrice: string;
  expectedUpdatedAtUtc: string;
  rowError: string | null;
};

export type ParseDraftPriceResult =
  | { ok: true; value: number }
  | { ok: false; reason: "empty" | "invalid" };

export function toPriceDraft(product: PosCatalogProductDto): PriceDraft {
  return {
    productId: product.productId,
    name: product.name,
    brandName: product.brandName,
    scope: product.scope ?? null,
    currentPrice: product.sellingPrice,
    draftPrice: String(product.sellingPrice),
    expectedUpdatedAtUtc: product.updatedAtUtc,
    rowError: null,
  };
}

export function parseDraftPrice(raw: string): ParseDraftPriceResult {
  const trimmed = raw.trim();
  if (trimmed === "") {
    return { ok: false, reason: "empty" };
  }
  if (!/^\d+(\.\d{1,2})?$/.test(trimmed)) {
    return { ok: false, reason: "invalid" };
  }
  const value = Number(trimmed);
  if (!Number.isFinite(value)) {
    return { ok: false, reason: "invalid" };
  }
  if (value < 0 || value > SELLING_PRICE_MAX) {
    return { ok: false, reason: "invalid" };
  }
  // Reject more than 2 dp via string rule above; round for float noise.
  const cents = Math.round(value * 100);
  return { ok: true, value: cents / 100 };
}

export function pricesEqual(a: number, b: number): boolean {
  return Math.round(a * 100) === Math.round(b * 100);
}

export function isPriceDraftDirty(row: PriceDraft): boolean {
  const parsed = parseDraftPrice(row.draftPrice);
  if (!parsed.ok) {
    return row.draftPrice.trim() !== String(row.currentPrice);
  }
  return !pricesEqual(parsed.value, row.currentPrice);
}

export function canSavePriceDraft(row: PriceDraft): boolean {
  const parsed = parseDraftPrice(row.draftPrice);
  if (!parsed.ok) {
    return false;
  }
  return !pricesEqual(parsed.value, row.currentPrice);
}

/**
 * Merge server products into local drafts.
 * Dirty rows keep draftPrice + concurrency token; non-dirty rows refresh fully.
 * Products not in the response are retained in the map (search hide/show).
 */
export function mergePriceDraftMap(
  previous: Record<string, PriceDraft>,
  products: PosCatalogProductDto[],
): Record<string, PriceDraft> {
  const next: Record<string, PriceDraft> = { ...previous };
  for (const product of products) {
    const existing = previous[product.productId];
    if (!existing) {
      next[product.productId] = toPriceDraft(product);
      continue;
    }
    if (isPriceDraftDirty(existing)) {
      next[product.productId] = {
        ...existing,
        name: product.name,
        brandName: product.brandName,
        currentPrice: product.sellingPrice,
      };
    } else {
      next[product.productId] = toPriceDraft(product);
    }
  }
  return next;
}

export function applySuccessfulPriceSave(
  row: PriceDraft,
  sellingPrice: number,
  updatedAtUtc: string,
): PriceDraft {
  return {
    ...row,
    currentPrice: sellingPrice,
    draftPrice: String(sellingPrice),
    expectedUpdatedAtUtc: updatedAtUtc,
    rowError: null,
  };
}

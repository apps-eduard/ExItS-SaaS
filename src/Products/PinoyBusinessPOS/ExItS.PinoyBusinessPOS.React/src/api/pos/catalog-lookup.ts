import {
  listCatalogProducts,
  lookupCatalogProductByBarcode,
  lookupCatalogProductBySku,
  type ListCatalogProductsOptions,
} from "@/api/pos/pos-catalog-client";
import type { PosCatalogProductDto } from "@/api/pos/pos-catalog-types";
import { PosApiError } from "@/api/pos/pos-http";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";

export type CatalogLookupResult =
  | { kind: "exact"; product: PosCatalogProductDto; matchedBy: "barcode" | "sku" }
  | { kind: "search"; products: PosCatalogProductDto[]; unknownBarcode?: boolean }
  | { kind: "empty"; unknownBarcode?: boolean };

/** GS1-style scan: long digit-only payload. Name search must not hit /by-barcode. */
export function looksLikeBarcodeScan(value: string): boolean {
  const trimmed = value.trim();
  return /^\d{8,}$/.test(trimmed);
}

function isLookupMiss(error: unknown): boolean {
  return (
    error instanceof PosApiError &&
    (error.status === 404 ||
      error.status === 400 ||
      error.errorCode === "pos.product.barcode.invalid" ||
      error.errorCode === "pos.product.sku.invalid")
  );
}

export async function resolveCatalogLookup(
  workspace: PosWorkspaceScope,
  rawTerm: string,
  listOptions: Omit<ListCatalogProductsOptions, "search"> = {},
  signal?: AbortSignal,
): Promise<CatalogLookupResult> {
  const term = rawTerm.trim();
  if (!term) {
    return { kind: "empty" };
  }

  let unknownBarcode = false;

  // Only call barcode for digit scans. Typed letters like "s" return HTTP 400
  // ("Barcode must contain digits only") and must fall through to name search.
  if (looksLikeBarcodeScan(term)) {
    try {
      const product = await lookupCatalogProductByBarcode(workspace, term, signal, {
        commerciallyOffered: listOptions.commerciallyOffered,
      });
      return { kind: "exact", product, matchedBy: "barcode" };
    } catch (error) {
      if (isLookupMiss(error)) {
        // 404 or validation 400 — still a scan miss, not a connection failure.
        unknownBarcode = true;
      } else {
        throw error;
      }
    }
  }

  try {
    const product = await lookupCatalogProductBySku(workspace, term, signal, {
      commerciallyOffered: listOptions.commerciallyOffered,
    });
    return { kind: "exact", product, matchedBy: "sku" };
  } catch (error) {
    if (!isLookupMiss(error)) {
      throw error;
    }
  }

  if (unknownBarcode && looksLikeBarcodeScan(term)) {
    return { kind: "empty", unknownBarcode: true };
  }

  const page = await listCatalogProducts(
    workspace,
    {
      ...listOptions,
      search: term,
      canBeSold: listOptions.canBeSold ?? true,
      commerciallyOffered: listOptions.commerciallyOffered,
    },
    signal,
  );
  return {
    kind: "search",
    products: page.items.filter((item) => item.canBeSold !== false),
    unknownBarcode: unknownBarcode || undefined,
  };
}

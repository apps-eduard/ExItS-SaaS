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

export function looksLikeBarcodeScan(value: string): boolean {
  const trimmed = value.trim();
  return /^\d{8,}$/.test(trimmed);
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

  try {
    const product = await lookupCatalogProductByBarcode(workspace, term, signal);
    return { kind: "exact", product, matchedBy: "barcode" };
  } catch (error) {
    if (error instanceof PosApiError && error.status === 404 && looksLikeBarcodeScan(term)) {
      unknownBarcode = true;
    } else if (!(error instanceof PosApiError) || error.status !== 404) {
      throw error;
    }
  }

  try {
    const product = await lookupCatalogProductBySku(workspace, term, signal);
    return { kind: "exact", product, matchedBy: "sku" };
  } catch (error) {
    if (!(error instanceof PosApiError) || error.status !== 404) {
      throw error;
    }
  }

  if (unknownBarcode && looksLikeBarcodeScan(term)) {
    return { kind: "empty", unknownBarcode: true };
  }

  const page = await listCatalogProducts(workspace, { ...listOptions, search: term }, signal);
  return {
    kind: "search",
    products: page.items.filter((item) => item.canBeSold !== false),
    unknownBarcode: unknownBarcode || undefined,
  };
}

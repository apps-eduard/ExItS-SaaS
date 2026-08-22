import { mapCatalogProduct } from "@/api/catalog/product-catalog-client";
import type { CatalogProduct } from "@/api/catalog/product-catalog-client";
import { commercialMutationRequest } from "@/api/commercial/commercial-http";

function productPath(productId: string, suffix = ""): string {
  return `/api/v1/platform/catalog/products/${productId}${suffix}`;
}

function requireProduct(payload: unknown): CatalogProduct {
  const mapped = mapCatalogProduct(payload);
  if (!mapped) {
    throw new Error("Invalid catalog product.");
  }
  return mapped;
}

export type RenameProductBody = {
  displayName: string;
  expectedUpdatedAtUtc?: string | null;
};

export function renameProduct(
  baseUrl: string,
  productId: string,
  body: RenameProductBody,
  signal?: AbortSignal,
): Promise<CatalogProduct> {
  return commercialMutationRequest<unknown>(baseUrl, {
    method: "PATCH",
    path: productPath(productId, "/rename"),
    body,
    signal,
  }).then(requireProduct);
}

export function activateProduct(
  baseUrl: string,
  productId: string,
  signal?: AbortSignal,
): Promise<CatalogProduct> {
  return commercialMutationRequest<unknown>(baseUrl, {
    method: "POST",
    path: productPath(productId, "/activate"),
    signal,
  }).then(requireProduct);
}

export function deactivateProduct(
  baseUrl: string,
  productId: string,
  signal?: AbortSignal,
): Promise<CatalogProduct> {
  return commercialMutationRequest<unknown>(baseUrl, {
    method: "POST",
    path: productPath(productId, "/deactivate"),
    signal,
  }).then(requireProduct);
}

export function retireProduct(
  baseUrl: string,
  productId: string,
  signal?: AbortSignal,
): Promise<CatalogProduct> {
  return commercialMutationRequest<unknown>(baseUrl, {
    method: "POST",
    path: productPath(productId, "/retire"),
    signal,
  }).then(requireProduct);
}

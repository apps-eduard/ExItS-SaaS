import type {
  PosCatalogProductDto,
  PosCatalogProductPagedResult,
  PosProductCategoryPagedResult,
} from "@/api/pos/pos-catalog-types";
import { posRequest, type PosWorkspaceScope } from "@/api/pos/pos-http";

const CATEGORIES_PATH = "/api/v1/pos/catalog/categories";
const PRODUCTS_PATH = "/api/v1/pos/catalog/products";

export const CATALOG_BROWSE_PAGE_SIZE = 24;

export type ListCatalogProductsOptions = {
  search?: string;
  status?: string;
  categoryId?: string;
  page?: number;
  pageSize?: number;
};

function appendQuery(path: string, params: Record<string, string | number | undefined>): string {
  const query = new URLSearchParams();
  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== "") {
      query.set(key, String(value));
    }
  }
  const serialized = query.toString();
  return serialized ? `${path}?${serialized}` : path;
}

export function listCatalogCategories(
  workspace: PosWorkspaceScope,
  options: { search?: string; status?: string; page?: number; pageSize?: number } = {},
  signal?: AbortSignal,
): Promise<PosProductCategoryPagedResult> {
  return posRequest({
    method: "GET",
    workspace,
    signal,
    path: appendQuery(CATEGORIES_PATH, {
      search: options.search,
      status: options.status ?? "Active",
      page: options.page ?? 1,
      pageSize: options.pageSize ?? 50,
    }),
  });
}

export function listCatalogProducts(
  workspace: PosWorkspaceScope,
  options: ListCatalogProductsOptions = {},
  signal?: AbortSignal,
): Promise<PosCatalogProductPagedResult> {
  return posRequest({
    method: "GET",
    workspace,
    signal,
    path: appendQuery(PRODUCTS_PATH, {
      search: options.search,
      status: options.status ?? "Active",
      categoryId: options.categoryId,
      page: options.page ?? 1,
      pageSize: options.pageSize ?? CATALOG_BROWSE_PAGE_SIZE,
    }),
  });
}

export function lookupCatalogProductBySku(
  workspace: PosWorkspaceScope,
  sku: string,
  signal?: AbortSignal,
): Promise<PosCatalogProductDto> {
  const encoded = encodeURIComponent(sku.trim());
  return posRequest({
    method: "GET",
    workspace,
    signal,
    path: `${PRODUCTS_PATH}/by-sku/${encoded}`,
  });
}

export function lookupCatalogProductByBarcode(
  workspace: PosWorkspaceScope,
  barcode: string,
  signal?: AbortSignal,
): Promise<PosCatalogProductDto> {
  const encoded = encodeURIComponent(barcode.trim());
  return posRequest({
    method: "GET",
    workspace,
    signal,
    path: `${PRODUCTS_PATH}/by-barcode/${encoded}`,
  });
}

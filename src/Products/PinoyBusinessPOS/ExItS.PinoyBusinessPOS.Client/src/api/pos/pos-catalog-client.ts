import type {
  CatalogProductImageVariant,
  CreatePosCatalogProductRequest,
  CreatePosProductCategoryRequest,
  PosCatalogProductDto,
  PosCatalogProductPagedResult,
  PosProductCategoryDto,
  PosProductCategoryPagedResult,
  UpdatePosCatalogProductRequest,
  UpdatePosProductCategoryRequest,
} from "@/api/pos/pos-catalog-types";
import { posRequest, posRequestBlob, type PosWorkspaceScope } from "@/api/pos/pos-http";

const CATEGORIES_PATH = "/api/v1/pos/catalog/categories";
const PRODUCTS_PATH = "/api/v1/pos/catalog/products";

export const CATALOG_BROWSE_PAGE_SIZE = 24;
export const CATALOG_ADMIN_PAGE_SIZE = 50;

export type ListCatalogProductsOptions = {
  search?: string;
  status?: string;
  categoryId?: string;
  page?: number;
  pageSize?: number;
};

export type ListCatalogCategoriesOptions = {
  search?: string;
  status?: string;
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
  options: ListCatalogCategoriesOptions = {},
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
      pageSize: options.pageSize ?? CATALOG_ADMIN_PAGE_SIZE,
    }),
  });
}

export function getCatalogCategory(
  workspace: PosWorkspaceScope,
  categoryId: string,
  signal?: AbortSignal,
): Promise<PosProductCategoryDto> {
  return posRequest({
    method: "GET",
    workspace,
    signal,
    path: `${CATEGORIES_PATH}/${categoryId}`,
  });
}

export function createCatalogCategory(
  workspace: PosWorkspaceScope,
  body: CreatePosProductCategoryRequest,
  signal?: AbortSignal,
): Promise<PosProductCategoryDto> {
  return posRequest({
    method: "POST",
    workspace,
    signal,
    path: CATEGORIES_PATH,
    body,
  });
}

export function updateCatalogCategory(
  workspace: PosWorkspaceScope,
  categoryId: string,
  body: UpdatePosProductCategoryRequest,
  signal?: AbortSignal,
): Promise<PosProductCategoryDto> {
  return posRequest({
    method: "PUT",
    workspace,
    signal,
    path: `${CATEGORIES_PATH}/${categoryId}`,
    body,
  });
}

export function deactivateCatalogCategory(
  workspace: PosWorkspaceScope,
  categoryId: string,
  signal?: AbortSignal,
): Promise<PosProductCategoryDto> {
  return posRequest({
    method: "POST",
    workspace,
    signal,
    path: `${CATEGORIES_PATH}/${categoryId}/deactivate`,
  });
}

export function reactivateCatalogCategory(
  workspace: PosWorkspaceScope,
  categoryId: string,
  signal?: AbortSignal,
): Promise<PosProductCategoryDto> {
  return posRequest({
    method: "POST",
    workspace,
    signal,
    path: `${CATEGORIES_PATH}/${categoryId}/reactivate`,
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

export function getCatalogProduct(
  workspace: PosWorkspaceScope,
  productId: string,
  signal?: AbortSignal,
): Promise<PosCatalogProductDto> {
  return posRequest({
    method: "GET",
    workspace,
    signal,
    path: `${PRODUCTS_PATH}/${productId}`,
  });
}

export function createCatalogProduct(
  workspace: PosWorkspaceScope,
  body: CreatePosCatalogProductRequest,
  signal?: AbortSignal,
): Promise<PosCatalogProductDto> {
  return posRequest({
    method: "POST",
    workspace,
    signal,
    path: PRODUCTS_PATH,
    body,
  });
}

export function updateCatalogProduct(
  workspace: PosWorkspaceScope,
  productId: string,
  body: UpdatePosCatalogProductRequest,
  signal?: AbortSignal,
): Promise<PosCatalogProductDto> {
  return posRequest({
    method: "PUT",
    workspace,
    signal,
    path: `${PRODUCTS_PATH}/${productId}`,
    body,
  });
}

export function deactivateCatalogProduct(
  workspace: PosWorkspaceScope,
  productId: string,
  signal?: AbortSignal,
): Promise<PosCatalogProductDto> {
  return posRequest({
    method: "POST",
    workspace,
    signal,
    path: `${PRODUCTS_PATH}/${productId}/deactivate`,
  });
}

export function reactivateCatalogProduct(
  workspace: PosWorkspaceScope,
  productId: string,
  signal?: AbortSignal,
): Promise<PosCatalogProductDto> {
  return posRequest({
    method: "POST",
    workspace,
    signal,
    path: `${PRODUCTS_PATH}/${productId}/reactivate`,
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

/** Multipart field name must be `file` (CatalogEndpoints). */
export function uploadCatalogProductImage(
  workspace: PosWorkspaceScope,
  productId: string,
  file: Blob,
  fileName = "product-image.jpg",
  signal?: AbortSignal,
): Promise<PosCatalogProductDto> {
  const formData = new FormData();
  formData.append("file", file, fileName);
  return posRequest({
    method: "PUT",
    workspace,
    signal,
    path: `${PRODUCTS_PATH}/${productId}/image`,
    formData,
  });
}

export function removeCatalogProductImage(
  workspace: PosWorkspaceScope,
  productId: string,
  signal?: AbortSignal,
): Promise<void> {
  return posRequest({
    method: "DELETE",
    workspace,
    signal,
    path: `${PRODUCTS_PATH}/${productId}/image`,
  });
}

export function getCatalogProductImage(
  workspace: PosWorkspaceScope,
  productId: string,
  variant: CatalogProductImageVariant = "thumb",
  signal?: AbortSignal,
): Promise<Blob> {
  return posRequestBlob({
    method: "GET",
    workspace,
    signal,
    path: `${PRODUCTS_PATH}/${productId}/image/${variant}`,
  });
}

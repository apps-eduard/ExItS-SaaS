import { platformRequest, PLATFORM_API_BASE_PATH } from "@/api/platform/platform-http";
import type {
  PlatformMerchantCatalogTemplate,
  PlatformMerchantCatalogTemplateSummary,
  PlatformMerchantGlobalCategory,
  PlatformMerchantGlobalProduct,
  PlatformPagedResult,
} from "@/api/platform/merchant-catalog-types";

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

export type ListPublishedTemplatesOptions = {
  search?: string;
  businessTypeCode?: string;
  businessTypeId?: string;
  page?: number;
  pageSize?: number;
};

export type SearchActiveProductsOptions = {
  search?: string;
  categoryId?: string;
  businessTypeCode?: string;
  barcode?: string;
  sku?: string;
  page?: number;
  pageSize?: number;
};

export type ListActiveCategoriesOptions = {
  search?: string;
  businessTypeCode?: string;
  parentId?: string;
  page?: number;
  pageSize?: number;
};

/** GET /api/v1/catalog/templates — published merchant templates. */
export function listPublishedTemplates(
  options: ListPublishedTemplatesOptions = {},
  signal?: AbortSignal,
): Promise<PlatformPagedResult<PlatformMerchantCatalogTemplateSummary>> {
  return platformRequest({
    path: appendQuery("/api/v1/catalog/templates", {
      page: options.page ?? 1,
      pageSize: options.pageSize ?? 20,
      search: options.search,
      businessTypeCode: options.businessTypeCode,
      businessTypeId: options.businessTypeId,
    }),
    signal,
  });
}

/** GET /api/v1/catalog/templates/{id} */
export function getPublishedTemplate(
  templateId: string,
  signal?: AbortSignal,
): Promise<PlatformMerchantCatalogTemplate> {
  return platformRequest({
    path: `/api/v1/catalog/templates/${templateId}`,
    signal,
  });
}

/** GET /api/v1/catalog/products/search */
export function searchActiveGlobalProducts(
  options: SearchActiveProductsOptions = {},
  signal?: AbortSignal,
): Promise<PlatformPagedResult<PlatformMerchantGlobalProduct>> {
  return platformRequest({
    path: appendQuery("/api/v1/catalog/products/search", {
      page: options.page ?? 1,
      pageSize: options.pageSize ?? 20,
      q: options.search,
      categoryId: options.categoryId,
      businessTypeCode: options.businessTypeCode,
      barcode: options.barcode,
      sku: options.sku,
    }),
    signal,
  });
}

/** GET /api/v1/catalog/categories */
export function listActiveGlobalCategories(
  options: ListActiveCategoriesOptions = {},
  signal?: AbortSignal,
): Promise<PlatformPagedResult<PlatformMerchantGlobalCategory>> {
  return platformRequest({
    path: appendQuery("/api/v1/catalog/categories", {
      page: options.page ?? 1,
      pageSize: options.pageSize ?? 100,
      search: options.search,
      businessTypeCode: options.businessTypeCode,
      parentId: options.parentId,
    }),
    signal,
  });
}

/** Relative image URL for a published global product (credentials via same-origin proxy). */
export function globalProductImageUrl(
  productId: string,
  variant: "thumb" | "medium" | "large" = "thumb",
  imageVersion?: number | null,
): string {
  const base = `${PLATFORM_API_BASE_PATH}/api/v1/catalog/products/${productId}/image/${variant}`;
  return imageVersion != null ? `${base}?v=${imageVersion}` : base;
}

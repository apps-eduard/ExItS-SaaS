import {
  GLOBAL_PRODUCT_LIST_PAGE_SIZE,
  GLOBAL_PRODUCT_LIST_SORT_BY,
  GLOBAL_PRODUCT_STATUSES,
  type GlobalProductListQuery,
  type GlobalProductListSortBy,
  type GlobalProductStatus,
} from "@/api/global-catalog/global-catalog-types";
import { withQuery } from "@/lib/http/query-string";

export type GlobalProductListUrlState = {
  page: number;
  search: string;
  status: GlobalProductStatus | "";
  categoryId: string;
  businessTypeId: string;
  barcode: string;
  sku: string;
  sortBy: GlobalProductListSortBy;
  sortDesc: boolean;
};

const DEFAULT_SORT: GlobalProductListSortBy = "Name";

export function isGlobalProductStatus(value: string): value is GlobalProductStatus {
  return (GLOBAL_PRODUCT_STATUSES as readonly string[]).includes(value);
}

export function isGlobalProductListSortBy(value: string): value is GlobalProductListSortBy {
  return (GLOBAL_PRODUCT_LIST_SORT_BY as readonly string[]).includes(value);
}

export function parseGlobalProductListSearchParams(
  params: URLSearchParams,
): GlobalProductListUrlState {
  const pageRaw = Number(params.get("page") ?? "1");
  const page = Number.isFinite(pageRaw) && pageRaw >= 1 ? Math.floor(pageRaw) : 1;
  const statusRaw = params.get("status") ?? "";
  const sortRaw = params.get("sortBy") ?? DEFAULT_SORT;
  return {
    page,
    search: params.get("search")?.trim() ?? "",
    status: isGlobalProductStatus(statusRaw) ? statusRaw : "",
    categoryId: params.get("categoryId")?.trim() ?? "",
    businessTypeId: params.get("businessTypeId")?.trim() ?? "",
    barcode: params.get("barcode")?.trim() ?? "",
    sku: params.get("sku")?.trim() ?? "",
    sortBy: isGlobalProductListSortBy(sortRaw) ? sortRaw : DEFAULT_SORT,
    sortDesc: params.get("sortDesc") === "true",
  };
}

export function globalProductListSearchParams(state: GlobalProductListUrlState): URLSearchParams {
  const params = new URLSearchParams();
  if (state.search) {
    params.set("search", state.search);
  }
  if (state.status) {
    params.set("status", state.status);
  }
  if (state.categoryId) {
    params.set("categoryId", state.categoryId);
  }
  if (state.businessTypeId) {
    params.set("businessTypeId", state.businessTypeId);
  }
  if (state.barcode) {
    params.set("barcode", state.barcode);
  }
  if (state.sku) {
    params.set("sku", state.sku);
  }
  if (state.sortBy !== DEFAULT_SORT) {
    params.set("sortBy", state.sortBy);
  }
  if (state.sortDesc) {
    params.set("sortDesc", "true");
  }
  if (state.page > 1) {
    params.set("page", String(state.page));
  }
  return params;
}

export function globalProductsListRequestPath(query: GlobalProductListQuery): string {
  return withQuery("/api/v1/platform/global-catalog/products", {
    page: query.page ?? 1,
    pageSize: query.pageSize ?? GLOBAL_PRODUCT_LIST_PAGE_SIZE,
    status: query.status,
    categoryId: query.categoryId,
    businessTypeId: query.businessTypeId,
    businessTypeCode: query.businessTypeCode,
    search: query.search,
    barcode: query.barcode,
    sku: query.sku,
    sortBy: query.sortBy,
    sortDesc: query.sortDesc === true ? true : undefined,
  });
}

export function hasActiveGlobalProductFilters(state: GlobalProductListUrlState): boolean {
  return Boolean(
    state.search ||
      state.status ||
      state.categoryId ||
      state.businessTypeId ||
      state.barcode ||
      state.sku,
  );
}

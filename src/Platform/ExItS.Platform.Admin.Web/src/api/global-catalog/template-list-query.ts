import {
  GLOBAL_CATALOG_TEMPLATE_AVAILABLE_PRODUCTS_PAGE_SIZE,
  GLOBAL_CATALOG_TEMPLATE_LIST_PAGE_SIZE,
  GLOBAL_CATALOG_TEMPLATE_LIST_SORT_BY,
  GLOBAL_CATALOG_TEMPLATE_STATUSES,
  type GlobalCatalogTemplateAvailableProductsQuery,
  type GlobalCatalogTemplateListQuery,
  type GlobalCatalogTemplateListSortBy,
  type GlobalCatalogTemplateStatus,
} from "@/api/global-catalog/global-catalog-types";
import { withQuery } from "@/lib/http/query-string";

export type GlobalCatalogTemplateListUrlState = {
  page: number;
  search: string;
  status: GlobalCatalogTemplateStatus | "";
  primaryBusinessTypeId: string;
  sortBy: GlobalCatalogTemplateListSortBy;
  sortDesc: boolean;
};

const DEFAULT_SORT: GlobalCatalogTemplateListSortBy = "Name";

export function isGlobalCatalogTemplateStatus(value: string): value is GlobalCatalogTemplateStatus {
  return (GLOBAL_CATALOG_TEMPLATE_STATUSES as readonly string[]).includes(value);
}

export function isGlobalCatalogTemplateListSortBy(
  value: string,
): value is GlobalCatalogTemplateListSortBy {
  return (GLOBAL_CATALOG_TEMPLATE_LIST_SORT_BY as readonly string[]).includes(value);
}

export function parseGlobalCatalogTemplateListSearchParams(
  params: URLSearchParams,
): GlobalCatalogTemplateListUrlState {
  const pageRaw = Number(params.get("page") ?? "1");
  const page = Number.isFinite(pageRaw) && pageRaw >= 1 ? Math.floor(pageRaw) : 1;
  const statusRaw = params.get("status") ?? "";
  const sortRaw = params.get("sortBy") ?? DEFAULT_SORT;
  return {
    page,
    search: params.get("search")?.trim() ?? "",
    status: isGlobalCatalogTemplateStatus(statusRaw) ? statusRaw : "",
    primaryBusinessTypeId: params.get("primaryBusinessTypeId")?.trim() ?? "",
    sortBy: isGlobalCatalogTemplateListSortBy(sortRaw) ? sortRaw : DEFAULT_SORT,
    sortDesc: params.get("sortDesc") === "true",
  };
}

export function globalCatalogTemplateListSearchParams(
  state: GlobalCatalogTemplateListUrlState,
): URLSearchParams {
  const params = new URLSearchParams();
  if (state.search) {
    params.set("search", state.search);
  }
  if (state.status) {
    params.set("status", state.status);
  }
  if (state.primaryBusinessTypeId) {
    params.set("primaryBusinessTypeId", state.primaryBusinessTypeId);
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

export function globalCatalogTemplatesListRequestPath(
  query: GlobalCatalogTemplateListQuery,
): string {
  return withQuery("/api/v1/platform/global-catalog/templates", {
    page: query.page ?? 1,
    pageSize: query.pageSize ?? GLOBAL_CATALOG_TEMPLATE_LIST_PAGE_SIZE,
    status: query.status,
    primaryBusinessTypeId: query.primaryBusinessTypeId,
    primaryBusinessTypeCode: query.primaryBusinessTypeCode,
    search: query.search,
    sortBy: query.sortBy,
    sortDesc: query.sortDesc === true ? true : undefined,
  });
}

export function globalCatalogTemplateAvailableProductsRequestPath(
  templateId: string,
  query: GlobalCatalogTemplateAvailableProductsQuery,
): string {
  return withQuery(`/api/v1/platform/global-catalog/templates/${templateId}/available-products`, {
    page: query.page ?? 1,
    pageSize: query.pageSize ?? GLOBAL_CATALOG_TEMPLATE_AVAILABLE_PRODUCTS_PAGE_SIZE,
    status: query.status ?? "Active",
    categoryId: query.categoryId,
    search: query.search,
    barcode: query.barcode,
    sku: query.sku,
    sortBy: query.sortBy,
    sortDesc: query.sortDesc === true ? true : undefined,
  });
}

export function hasActiveGlobalCatalogTemplateFilters(
  state: GlobalCatalogTemplateListUrlState,
): boolean {
  return Boolean(state.search || state.status || state.primaryBusinessTypeId);
}

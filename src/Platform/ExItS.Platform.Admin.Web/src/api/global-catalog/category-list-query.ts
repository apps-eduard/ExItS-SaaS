import {
  GLOBAL_CATEGORY_LIST_PAGE_SIZE,
  GLOBAL_CATEGORY_LIST_SORT_BY,
  GLOBAL_CATEGORY_STATUSES,
  type GlobalCategoryListQuery,
  type GlobalCategoryListSortBy,
  type GlobalCategoryStatus,
} from "@/api/global-catalog/global-catalog-types";
import { withQuery } from "@/lib/http/query-string";

export type GlobalCategoryListUrlState = {
  page: number;
  search: string;
  status: GlobalCategoryStatus | "";
  parentId: string;
  businessTypeId: string;
  sortBy: GlobalCategoryListSortBy;
  sortDesc: boolean;
};

const DEFAULT_SORT: GlobalCategoryListSortBy = "SortOrder";

export function isGlobalCategoryStatus(value: string): value is GlobalCategoryStatus {
  return (GLOBAL_CATEGORY_STATUSES as readonly string[]).includes(value);
}

export function isGlobalCategoryListSortBy(value: string): value is GlobalCategoryListSortBy {
  return (GLOBAL_CATEGORY_LIST_SORT_BY as readonly string[]).includes(value);
}

export function parseGlobalCategoryListSearchParams(
  params: URLSearchParams,
): GlobalCategoryListUrlState {
  const pageRaw = Number(params.get("page") ?? "1");
  const page = Number.isFinite(pageRaw) && pageRaw >= 1 ? Math.floor(pageRaw) : 1;
  const statusRaw = params.get("status") ?? "";
  const sortRaw = params.get("sortBy") ?? DEFAULT_SORT;
  return {
    page,
    search: params.get("search")?.trim() ?? "",
    status: isGlobalCategoryStatus(statusRaw) ? statusRaw : "",
    parentId: params.get("parentId")?.trim() ?? "",
    businessTypeId: params.get("businessTypeId")?.trim() ?? "",
    sortBy: isGlobalCategoryListSortBy(sortRaw) ? sortRaw : DEFAULT_SORT,
    sortDesc: params.get("sortDesc") === "true",
  };
}

export function globalCategoryListSearchParams(
  state: GlobalCategoryListUrlState,
): URLSearchParams {
  const params = new URLSearchParams();
  if (state.search) {
    params.set("search", state.search);
  }
  if (state.status) {
    params.set("status", state.status);
  }
  if (state.parentId) {
    params.set("parentId", state.parentId);
  }
  if (state.businessTypeId) {
    params.set("businessTypeId", state.businessTypeId);
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

export function globalCategoriesListRequestPath(query: GlobalCategoryListQuery): string {
  return withQuery("/api/v1/platform/global-catalog/categories", {
    page: query.page ?? 1,
    pageSize: query.pageSize ?? GLOBAL_CATEGORY_LIST_PAGE_SIZE,
    status: query.status,
    parentId: query.parentId,
    businessTypeId: query.businessTypeId,
    businessTypeCode: query.businessTypeCode,
    search: query.search,
    sortBy: query.sortBy,
    sortDesc: query.sortDesc === true ? true : undefined,
  });
}

export function hasActiveGlobalCategoryFilters(state: GlobalCategoryListUrlState): boolean {
  return Boolean(state.search || state.status || state.parentId || state.businessTypeId);
}

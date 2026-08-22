import {
  GLOBAL_BUSINESS_TYPE_LIST_PAGE_SIZE,
  GLOBAL_BUSINESS_TYPE_LIST_SORT_BY,
  GLOBAL_BUSINESS_TYPE_STATUSES,
  type GlobalBusinessTypeListQuery,
  type GlobalBusinessTypeListSortBy,
  type GlobalBusinessTypeStatus,
} from "@/api/global-catalog/global-catalog-types";
import { withQuery } from "@/lib/http/query-string";

export type GlobalBusinessTypeListUrlState = {
  page: number;
  search: string;
  status: GlobalBusinessTypeStatus | "";
  sortBy: GlobalBusinessTypeListSortBy;
  sortDesc: boolean;
};

const DEFAULT_SORT: GlobalBusinessTypeListSortBy = "SortOrder";

export function isGlobalBusinessTypeStatus(value: string): value is GlobalBusinessTypeStatus {
  return (GLOBAL_BUSINESS_TYPE_STATUSES as readonly string[]).includes(value);
}

export function isGlobalBusinessTypeListSortBy(
  value: string,
): value is GlobalBusinessTypeListSortBy {
  return (GLOBAL_BUSINESS_TYPE_LIST_SORT_BY as readonly string[]).includes(value);
}

export function parseGlobalBusinessTypeListSearchParams(
  params: URLSearchParams,
): GlobalBusinessTypeListUrlState {
  const pageRaw = Number(params.get("page") ?? "1");
  const page = Number.isFinite(pageRaw) && pageRaw >= 1 ? Math.floor(pageRaw) : 1;
  const statusRaw = params.get("status") ?? "";
  const sortRaw = params.get("sortBy") ?? DEFAULT_SORT;
  return {
    page,
    search: params.get("search")?.trim() ?? "",
    status: isGlobalBusinessTypeStatus(statusRaw) ? statusRaw : "",
    sortBy: isGlobalBusinessTypeListSortBy(sortRaw) ? sortRaw : DEFAULT_SORT,
    sortDesc: params.get("sortDesc") === "true",
  };
}

export function globalBusinessTypeListSearchParams(
  state: GlobalBusinessTypeListUrlState,
): URLSearchParams {
  const params = new URLSearchParams();
  if (state.search) {
    params.set("search", state.search);
  }
  if (state.status) {
    params.set("status", state.status);
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

export function globalBusinessTypesListRequestPath(query: GlobalBusinessTypeListQuery): string {
  return withQuery("/api/v1/platform/global-catalog/business-types", {
    page: query.page ?? 1,
    pageSize: query.pageSize ?? GLOBAL_BUSINESS_TYPE_LIST_PAGE_SIZE,
    status: query.status,
    search: query.search,
    sortBy: query.sortBy,
    sortDesc: query.sortDesc === true ? true : undefined,
  });
}

export function hasActiveGlobalBusinessTypeFilters(state: GlobalBusinessTypeListUrlState): boolean {
  return Boolean(state.search || state.status);
}

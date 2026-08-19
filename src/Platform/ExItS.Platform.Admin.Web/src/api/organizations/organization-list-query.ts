import {
  ORGANIZATION_LIST_PAGE_SIZE,
  ORGANIZATION_LIST_SORT_BY,
  ORGANIZATION_STATUSES,
  type OrganizationListQuery,
  type OrganizationListSortBy,
  type OrganizationStatus,
} from "@/api/organizations/organization-types";
import { withQuery } from "@/lib/http/query-string";

export type OrganizationListUrlState = {
  page: number;
  search: string;
  status: OrganizationStatus | "";
  sortBy: OrganizationListSortBy;
  sortDesc: boolean;
};

const DEFAULT_SORT: OrganizationListSortBy = "DisplayName";

export function isOrganizationStatus(value: string): value is OrganizationStatus {
  return (ORGANIZATION_STATUSES as readonly string[]).includes(value);
}

export function isOrganizationSortBy(value: string): value is OrganizationListSortBy {
  return (ORGANIZATION_LIST_SORT_BY as readonly string[]).includes(value);
}

export function parseOrganizationListSearchParams(
  params: URLSearchParams,
): OrganizationListUrlState {
  const pageRaw = Number(params.get("page") ?? "1");
  const page = Number.isFinite(pageRaw) && pageRaw >= 1 ? Math.floor(pageRaw) : 1;
  const statusRaw = params.get("status") ?? "";
  const sortRaw = params.get("sortBy") ?? DEFAULT_SORT;
  return {
    page,
    search: params.get("search")?.trim() ?? "",
    status: isOrganizationStatus(statusRaw) ? statusRaw : "",
    sortBy: isOrganizationSortBy(sortRaw) ? sortRaw : DEFAULT_SORT,
    sortDesc: params.get("sortDesc") === "true",
  };
}

export function organizationListSearchParams(state: OrganizationListUrlState): URLSearchParams {
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

export function organizationsListRequestPath(query: OrganizationListQuery): string {
  return withQuery("/api/v1/platform/organizations", {
    page: query.page ?? 1,
    pageSize: query.pageSize ?? ORGANIZATION_LIST_PAGE_SIZE,
    status: query.status,
    search: query.search,
    sortBy: query.sortBy,
    sortDesc: query.sortDesc === true ? true : undefined,
  });
}

export function hasActiveOrganizationFilters(state: OrganizationListUrlState): boolean {
  return Boolean(state.search || state.status);
}

import {
  USER_DIRECTORY_FILTERS,
  USER_LIST_PAGE_SIZE,
  USER_LIST_SORT_BY,
  ACCOUNT_STATUSES,
  type AccountStatus,
  type UserDirectoryFilter,
  type UserListQuery,
  type UserListSortBy,
} from "@/api/users/user-types";
import { withQuery } from "@/lib/http/query-string";

export type UserListUrlState = {
  page: number;
  search: string;
  status: AccountStatus | "";
  directory: UserDirectoryFilter | "";
  sortBy: UserListSortBy;
  sortDesc: boolean;
};

const DEFAULT_SORT: UserListSortBy = "Username";

const DIRECTORY_ALIASES: Record<string, UserDirectoryFilter> = {
  platform: "PlatformStaff",
  platformstaff: "PlatformStaff",
  organization: "Organization",
  personal: "Personal",
  unassigned: "Unassigned",
  "needs-review": "Unassigned",
};

export function isAccountStatus(value: string): value is AccountStatus {
  return (ACCOUNT_STATUSES as readonly string[]).includes(value);
}

export function isUserListSortBy(value: string): value is UserListSortBy {
  return (USER_LIST_SORT_BY as readonly string[]).includes(value);
}

export function sanitizeUserDirectory(value: string): UserDirectoryFilter | null {
  if (!value) {
    return null;
  }
  const exact = USER_DIRECTORY_FILTERS.find((item) => item === value);
  if (exact) {
    return exact;
  }
  return DIRECTORY_ALIASES[value.trim().toLowerCase()] ?? null;
}

export function parseUserListSearchParams(params: URLSearchParams): UserListUrlState {
  const pageRaw = Number(params.get("page") ?? "1");
  const page = Number.isFinite(pageRaw) && pageRaw >= 1 ? Math.floor(pageRaw) : 1;
  const statusRaw = params.get("status") ?? "";
  const sortRaw = params.get("sortBy") ?? DEFAULT_SORT;
  const directoryRaw = params.get("directory") ?? "";
  const directoryFromStatusAlias =
    statusRaw.toLowerCase() === "needs-review" ? ("Unassigned" as const) : null;
  const directory = sanitizeUserDirectory(directoryRaw) ?? directoryFromStatusAlias;
  return {
    page,
    search: params.get("search")?.trim() ?? "",
    status: isAccountStatus(statusRaw) ? statusRaw : "",
    directory: directory ?? "",
    sortBy: isUserListSortBy(sortRaw) ? sortRaw : DEFAULT_SORT,
    sortDesc: params.get("sortDesc") === "true",
  };
}

export function userListSearchParams(state: UserListUrlState): URLSearchParams {
  const params = new URLSearchParams();
  if (state.search) {
    params.set("search", state.search);
  }
  if (state.status) {
    params.set("status", state.status);
  }
  if (state.directory) {
    params.set("directory", state.directory);
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

export function usersListRequestPath(query: UserListQuery): string {
  return withQuery("/api/v1/platform/users", {
    page: query.page ?? 1,
    pageSize: query.pageSize ?? USER_LIST_PAGE_SIZE,
    status: query.status,
    search: query.search,
    directory: query.directory,
    sortBy: query.sortBy,
    sortDesc: query.sortDesc === true ? true : undefined,
  });
}

export function hasActiveUserFilters(state: UserListUrlState): boolean {
  return Boolean(state.search || state.status || state.directory);
}

export function isUnrecognizedDirectoryParam(params: URLSearchParams): boolean {
  const raw = params.get("directory")?.trim() ?? "";
  if (!raw) {
    return false;
  }
  return sanitizeUserDirectory(raw) == null;
}

import { withQuery } from "@/lib/http/query-string";
import {
  PLATFORM_ROLE_KINDS,
  PLATFORM_ROLE_PAGE_SIZE,
  PLATFORM_ROLE_STATUSES,
  type PlatformRoleKindFilter,
  type PlatformRoleStatusFilter,
} from "@/api/platform-roles/platform-roles-types";

export type PlatformRolesUrlState = {
  search: string;
  kind: PlatformRoleKindFilter;
  status: PlatformRoleStatusFilter;
  page: number;
};

function parsePage(raw: string | null): number {
  const value = Number(raw ?? "1");
  return Number.isFinite(value) && value >= 1 ? Math.floor(value) : 1;
}

function isKind(value: string): value is Exclude<PlatformRoleKindFilter, ""> {
  return (PLATFORM_ROLE_KINDS as readonly string[]).includes(value);
}

function isStatus(value: string): value is Exclude<PlatformRoleStatusFilter, ""> {
  return (PLATFORM_ROLE_STATUSES as readonly string[]).includes(value);
}

export function parsePlatformRolesSearchParams(params: URLSearchParams): PlatformRolesUrlState {
  const kindRaw = params.get("kind") ?? "";
  const statusRaw = params.get("status") ?? "";
  return {
    search: params.get("search")?.trim() ?? "",
    kind: isKind(kindRaw) ? kindRaw : "",
    status: isStatus(statusRaw) ? statusRaw : "",
    page: parsePage(params.get("page")),
  };
}

export function platformRolesSearchParams(state: PlatformRolesUrlState): URLSearchParams {
  const params = new URLSearchParams();
  if (state.search) {
    params.set("search", state.search);
  }
  if (state.kind) {
    params.set("kind", state.kind);
  }
  if (state.status) {
    params.set("status", state.status);
  }
  if (state.page > 1) {
    params.set("page", String(state.page));
  }
  return params;
}

export function hasActivePlatformRolesFilters(state: PlatformRolesUrlState): boolean {
  return Boolean(state.search || state.kind || state.status);
}

export function platformRolesListPath(state: PlatformRolesUrlState): string {
  return withQuery("/api/v1/platform/authorization/role-definitions", {
    page: state.page,
    pageSize: PLATFORM_ROLE_PAGE_SIZE,
    kind: state.kind || undefined,
    status: state.status || undefined,
    search: state.search || undefined,
  });
}

const GUID_PATTERN =
  /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/;

export function parsePlatformRoleId(raw: string | undefined): string | null {
  if (!raw || !GUID_PATTERN.test(raw)) {
    return null;
  }
  return raw;
}

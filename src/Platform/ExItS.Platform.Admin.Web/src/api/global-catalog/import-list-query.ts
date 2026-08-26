import {
  GLOBAL_CATALOG_IMPORT_LIST_PAGE_SIZE,
  GLOBAL_CATALOG_IMPORT_STATUSES,
  type GlobalCatalogImportListQuery,
  type GlobalCatalogImportStatus,
} from "@/api/global-catalog/global-catalog-types";
import { withQuery } from "@/lib/http/query-string";

export type GlobalCatalogImportListUrlState = {
  page: number;
  status: GlobalCatalogImportStatus | "";
};

export function isGlobalCatalogImportStatus(value: string): value is GlobalCatalogImportStatus {
  return (GLOBAL_CATALOG_IMPORT_STATUSES as readonly string[]).includes(value);
}

export function parseGlobalCatalogImportListSearchParams(
  params: URLSearchParams,
): GlobalCatalogImportListUrlState {
  const pageRaw = Number(params.get("page") ?? "1");
  const page = Number.isFinite(pageRaw) && pageRaw >= 1 ? Math.floor(pageRaw) : 1;
  const statusRaw = params.get("status") ?? "";
  return {
    page,
    status: isGlobalCatalogImportStatus(statusRaw) ? statusRaw : "",
  };
}

export function globalCatalogImportListSearchParams(
  state: GlobalCatalogImportListUrlState,
): URLSearchParams {
  const params = new URLSearchParams();
  if (state.status) {
    params.set("status", state.status);
  }
  if (state.page > 1) {
    params.set("page", String(state.page));
  }
  return params;
}

export function globalCatalogImportsListRequestPath(query: GlobalCatalogImportListQuery): string {
  return withQuery("/api/v1/platform/global-catalog/products/imports", {
    page: query.page ?? 1,
    pageSize: query.pageSize ?? GLOBAL_CATALOG_IMPORT_LIST_PAGE_SIZE,
    status: query.status,
  });
}

export function hasActiveGlobalCatalogImportFilters(state: GlobalCatalogImportListUrlState): boolean {
  return Boolean(state.status);
}

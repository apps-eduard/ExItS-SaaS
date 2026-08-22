import { useQuery } from "@tanstack/react-query";

import {
  getGlobalCatalogImport,
  listGlobalCatalogImportErrors,
  listGlobalCatalogImports,
} from "@/api/global-catalog/global-catalog-client";
import {
  GLOBAL_CATALOG_IMPORT_ERRORS_PAGE_SIZE,
  GLOBAL_CATALOG_IMPORT_LIST_PAGE_SIZE,
  type GlobalCatalogImportErrorsQuery,
  type GlobalCatalogImportListQuery,
  type GlobalCatalogImportStatus,
} from "@/api/global-catalog/global-catalog-types";
import { globalCatalogQueryKeys } from "@/api/global-catalog/global-catalog-query-keys";
import { env } from "@/lib/env";

const ACTIVE_IMPORT_STATUSES: GlobalCatalogImportStatus[] = ["Queued", "Processing"];

export function importListQueryKey(query: GlobalCatalogImportListQuery) {
  return globalCatalogQueryKeys.imports.list({
    page: query.page ?? 1,
    pageSize: query.pageSize ?? GLOBAL_CATALOG_IMPORT_LIST_PAGE_SIZE,
    status: query.status ?? "",
  });
}

export function useGlobalCatalogImportListQuery(
  query: GlobalCatalogImportListQuery,
  enabled: boolean,
) {
  return useQuery({
    queryKey: importListQueryKey(query),
    enabled,
    queryFn: ({ signal }) =>
      listGlobalCatalogImports(env.platformApiBaseUrl, {
        ...query,
        pageSize: query.pageSize ?? GLOBAL_CATALOG_IMPORT_LIST_PAGE_SIZE,
        signal,
      }),
  });
}

export function useGlobalCatalogImportDetailQuery(jobId: string, enabled: boolean) {
  return useQuery({
    queryKey: globalCatalogQueryKeys.imports.detail(jobId),
    enabled: enabled && jobId.length > 0,
    queryFn: ({ signal }) => getGlobalCatalogImport(env.platformApiBaseUrl, jobId, signal),
    refetchInterval: (query) => {
      const status = query.state.data?.status;
      if (status && ACTIVE_IMPORT_STATUSES.includes(status)) {
        return 3000;
      }
      return false;
    },
  });
}

export function useGlobalCatalogImportErrorsQuery(
  jobId: string,
  query: GlobalCatalogImportErrorsQuery,
  enabled: boolean,
) {
  return useQuery({
    queryKey: globalCatalogQueryKeys.imports.errors(jobId, {
      page: query.page ?? 1,
      pageSize: query.pageSize ?? GLOBAL_CATALOG_IMPORT_ERRORS_PAGE_SIZE,
    }),
    enabled: enabled && jobId.length > 0,
    queryFn: ({ signal }) =>
      listGlobalCatalogImportErrors(env.platformApiBaseUrl, jobId, {
        ...query,
        pageSize: query.pageSize ?? GLOBAL_CATALOG_IMPORT_ERRORS_PAGE_SIZE,
        signal,
      }),
  });
}

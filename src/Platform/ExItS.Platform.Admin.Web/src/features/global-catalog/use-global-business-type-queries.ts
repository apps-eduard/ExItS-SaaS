import { useQuery } from "@tanstack/react-query";
import {
  getGlobalBusinessType,
  listGlobalBusinessTypes,
} from "@/api/global-catalog/global-catalog-client";
import {
  GLOBAL_BUSINESS_TYPE_LIST_PAGE_SIZE,
  type GlobalBusinessTypeListQuery,
} from "@/api/global-catalog/global-catalog-types";
import { globalCatalogQueryKeys } from "@/api/global-catalog/global-catalog-query-keys";
import { env } from "@/lib/env";

export function businessTypeListQueryKey(query: GlobalBusinessTypeListQuery) {
  return globalCatalogQueryKeys.businessTypes.list({
    page: query.page ?? 1,
    pageSize: query.pageSize ?? GLOBAL_BUSINESS_TYPE_LIST_PAGE_SIZE,
    status: query.status ?? "",
    search: query.search ?? "",
    sortBy: query.sortBy ?? "SortOrder",
    sortDesc: query.sortDesc === true,
  });
}

export function useGlobalBusinessTypeListQuery(query: GlobalBusinessTypeListQuery, enabled: boolean) {
  return useQuery({
    queryKey: businessTypeListQueryKey(query),
    enabled,
    queryFn: ({ signal }) =>
      listGlobalBusinessTypes(env.platformApiBaseUrl, {
        ...query,
        pageSize: query.pageSize ?? GLOBAL_BUSINESS_TYPE_LIST_PAGE_SIZE,
        signal,
      }),
  });
}

export function useGlobalBusinessTypeDetailQuery(businessTypeId: string, enabled: boolean) {
  return useQuery({
    queryKey: globalCatalogQueryKeys.businessTypes.detail(businessTypeId),
    enabled: enabled && businessTypeId.length > 0,
    queryFn: ({ signal }) =>
      getGlobalBusinessType(env.platformApiBaseUrl, businessTypeId, signal),
  });
}

import { useQuery } from "@tanstack/react-query";
import {
  getGlobalCategory,
  listGlobalCategories,
} from "@/api/global-catalog/global-catalog-client";
import {
  GLOBAL_CATALOG_LOOKUP_PAGE_SIZE,
  GLOBAL_CATEGORY_LIST_PAGE_SIZE,
  type GlobalCategoryListQuery,
} from "@/api/global-catalog/global-catalog-types";
import { globalCatalogQueryKeys } from "@/api/global-catalog/global-catalog-query-keys";
import { env } from "@/lib/env";

export function categoryListQueryKey(query: GlobalCategoryListQuery) {
  return globalCatalogQueryKeys.categories.list({
    page: query.page ?? 1,
    pageSize: query.pageSize ?? GLOBAL_CATEGORY_LIST_PAGE_SIZE,
    status: query.status ?? "",
    parentId: query.parentId ?? "",
    businessTypeId: query.businessTypeId ?? "",
    businessTypeCode: query.businessTypeCode ?? "",
    search: query.search ?? "",
    sortBy: query.sortBy ?? "SortOrder",
    sortDesc: query.sortDesc === true,
  });
}

export function useGlobalCategoryListQuery(query: GlobalCategoryListQuery, enabled: boolean) {
  return useQuery({
    queryKey: categoryListQueryKey(query),
    enabled,
    queryFn: ({ signal }) =>
      listGlobalCategories(env.platformApiBaseUrl, {
        ...query,
        pageSize: query.pageSize ?? GLOBAL_CATEGORY_LIST_PAGE_SIZE,
        signal,
      }),
  });
}

export function useGlobalCategoryDetailQuery(categoryId: string, enabled: boolean) {
  return useQuery({
    queryKey: globalCatalogQueryKeys.categories.detail(categoryId),
    enabled: enabled && categoryId.length > 0,
    queryFn: ({ signal }) => getGlobalCategory(env.platformApiBaseUrl, categoryId, signal),
  });
}

export function useGlobalCategoryLookupQuery(enabled: boolean) {
  return useQuery({
    queryKey: globalCatalogQueryKeys.categories.lookup,
    enabled,
    queryFn: ({ signal }) =>
      listGlobalCategories(env.platformApiBaseUrl, {
        page: 1,
        pageSize: GLOBAL_CATALOG_LOOKUP_PAGE_SIZE,
        signal,
      }),
  });
}

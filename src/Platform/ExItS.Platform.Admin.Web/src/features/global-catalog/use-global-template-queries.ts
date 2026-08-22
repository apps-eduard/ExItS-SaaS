import { useQuery } from "@tanstack/react-query";
import {
  getGlobalCatalogTemplate,
  listGlobalCatalogTemplateAvailableProducts,
  listGlobalCatalogTemplates,
} from "@/api/global-catalog/global-catalog-client";
import {
  GLOBAL_CATALOG_TEMPLATE_AVAILABLE_PRODUCTS_PAGE_SIZE,
  GLOBAL_CATALOG_TEMPLATE_LIST_PAGE_SIZE,
  type GlobalCatalogTemplateAvailableProductsQuery,
  type GlobalCatalogTemplateListQuery,
} from "@/api/global-catalog/global-catalog-types";
import { globalCatalogQueryKeys } from "@/api/global-catalog/global-catalog-query-keys";
import { env } from "@/lib/env";

export function templateListQueryKey(query: GlobalCatalogTemplateListQuery) {
  return globalCatalogQueryKeys.templates.list({
    page: query.page ?? 1,
    pageSize: query.pageSize ?? GLOBAL_CATALOG_TEMPLATE_LIST_PAGE_SIZE,
    status: query.status ?? "",
    primaryBusinessTypeId: query.primaryBusinessTypeId ?? "",
    primaryBusinessTypeCode: query.primaryBusinessTypeCode ?? "",
    search: query.search ?? "",
    sortBy: query.sortBy ?? "Name",
    sortDesc: query.sortDesc === true,
  });
}

export function useGlobalCatalogTemplateListQuery(
  query: GlobalCatalogTemplateListQuery,
  enabled: boolean,
) {
  return useQuery({
    queryKey: templateListQueryKey(query),
    enabled,
    queryFn: ({ signal }) =>
      listGlobalCatalogTemplates(env.platformApiBaseUrl, {
        ...query,
        pageSize: query.pageSize ?? GLOBAL_CATALOG_TEMPLATE_LIST_PAGE_SIZE,
        signal,
      }),
  });
}

export function useGlobalCatalogTemplateDetailQuery(templateId: string, enabled: boolean) {
  return useQuery({
    queryKey: globalCatalogQueryKeys.templates.detail(templateId),
    enabled: enabled && templateId.length > 0,
    queryFn: ({ signal }) => getGlobalCatalogTemplate(env.platformApiBaseUrl, templateId, signal),
  });
}

export function useGlobalCatalogTemplateAvailableProductsQuery(
  templateId: string,
  query: GlobalCatalogTemplateAvailableProductsQuery,
  enabled: boolean,
) {
  return useQuery({
    queryKey: globalCatalogQueryKeys.templates.availableProducts(templateId, {
      page: query.page ?? 1,
      pageSize: query.pageSize ?? GLOBAL_CATALOG_TEMPLATE_AVAILABLE_PRODUCTS_PAGE_SIZE,
      search: query.search ?? "",
      status: query.status ?? "Active",
    }),
    enabled: enabled && templateId.length > 0,
    queryFn: ({ signal }) =>
      listGlobalCatalogTemplateAvailableProducts(env.platformApiBaseUrl, templateId, {
        ...query,
        pageSize: query.pageSize ?? GLOBAL_CATALOG_TEMPLATE_AVAILABLE_PRODUCTS_PAGE_SIZE,
        signal,
      }),
  });
}

import { useQuery } from "@tanstack/react-query";
import { getGlobalProduct, listGlobalProducts } from "@/api/global-catalog/global-catalog-client";
import {
  GLOBAL_PRODUCT_LIST_PAGE_SIZE,
  type GlobalProductListQuery,
} from "@/api/global-catalog/global-catalog-types";
import { globalCatalogQueryKeys } from "@/api/global-catalog/global-catalog-query-keys";
import { env } from "@/lib/env";

export function productListQueryKey(query: GlobalProductListQuery) {
  return globalCatalogQueryKeys.products.list({
    page: query.page ?? 1,
    pageSize: query.pageSize ?? GLOBAL_PRODUCT_LIST_PAGE_SIZE,
    status: query.status ?? "",
    categoryId: query.categoryId ?? "",
    businessTypeId: query.businessTypeId ?? "",
    businessTypeCode: query.businessTypeCode ?? "",
    search: query.search ?? "",
    barcode: query.barcode ?? "",
    sku: query.sku ?? "",
    sortBy: query.sortBy ?? "Name",
    sortDesc: query.sortDesc === true,
  });
}

export function useGlobalProductListQuery(query: GlobalProductListQuery, enabled: boolean) {
  return useQuery({
    queryKey: productListQueryKey(query),
    enabled,
    queryFn: ({ signal }) =>
      listGlobalProducts(env.platformApiBaseUrl, {
        ...query,
        pageSize: query.pageSize ?? GLOBAL_PRODUCT_LIST_PAGE_SIZE,
        signal,
      }),
  });
}

export function useGlobalProductDetailQuery(productId: string, enabled: boolean) {
  return useQuery({
    queryKey: globalCatalogQueryKeys.products.detail(productId),
    enabled: enabled && productId.length > 0,
    queryFn: ({ signal }) => getGlobalProduct(env.platformApiBaseUrl, productId, signal),
  });
}

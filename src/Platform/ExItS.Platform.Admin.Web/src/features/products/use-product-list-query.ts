import { useQuery } from "@tanstack/react-query";
import { listCatalogProductsPage } from "@/api/catalog/product-catalog-client";
import {
  PRODUCT_LIST_PAGE_SIZE,
  type ProductListQuery,
} from "@/api/catalog/product-list-types";
import { env } from "@/lib/env";

export const productListQueryKey = (query: ProductListQuery) =>
  [
    "platform-catalog-products",
    "list",
    query.page ?? 1,
    query.pageSize ?? PRODUCT_LIST_PAGE_SIZE,
    query.status ?? "",
    query.search ?? "",
    query.sortBy ?? "DisplayName",
    query.sortDesc === true,
  ] as const;

export function useProductListQuery(query: ProductListQuery, enabled: boolean) {
  return useQuery({
    queryKey: productListQueryKey(query),
    enabled,
    queryFn: ({ signal }) =>
      listCatalogProductsPage(env.platformApiBaseUrl, {
        ...query,
        pageSize: query.pageSize ?? PRODUCT_LIST_PAGE_SIZE,
        signal,
      }),
  });
}

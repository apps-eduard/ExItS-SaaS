import { useQuery } from "@tanstack/react-query";
import {
  getCatalogPlanById,
  listCatalogPlansByProductCode,
  listCatalogPlansPage,
} from "@/api/catalog/plan-catalog-client";
import { PLAN_LIST_PAGE_SIZE, type PlanListQuery } from "@/api/catalog/plan-catalog-types";
import { getCatalogProductById } from "@/api/catalog/product-catalog-client";
import { env } from "@/lib/env";

export const catalogProductDetailQueryKey = (productId: string) =>
  ["catalog-products", "detail", productId] as const;

export const catalogProductPlansQueryKey = (productCode: string) =>
  ["catalog-products", "plans", productCode] as const;

export const catalogPlanDetailQueryKey = (planId: string) =>
  ["catalog-plans", "detail", planId] as const;

export const catalogPlanListQueryKey = (query: PlanListQuery) =>
  [
    "catalog-plans",
    "list",
    query.page ?? 1,
    query.pageSize ?? PLAN_LIST_PAGE_SIZE,
    query.productCode ?? "",
    query.status ?? "",
    query.search ?? "",
    query.sortBy ?? "DisplayName",
    query.sortDesc === true,
  ] as const;

export function useCatalogProductDetailQuery(productId: string | null) {
  return useQuery({
    queryKey: catalogProductDetailQueryKey(productId ?? ""),
    enabled: productId != null,
    queryFn: ({ signal }) => getCatalogProductById(env.platformApiBaseUrl, productId!, signal),
  });
}

export function useCatalogProductPlansQuery(productCode: string | null) {
  return useQuery({
    queryKey: catalogProductPlansQueryKey(productCode ?? ""),
    enabled: productCode != null && productCode.length > 0,
    queryFn: ({ signal }) =>
      listCatalogPlansByProductCode(env.platformApiBaseUrl, productCode!, signal),
  });
}

export function useCatalogPlanDetailQuery(planId: string | null) {
  return useQuery({
    queryKey: catalogPlanDetailQueryKey(planId ?? ""),
    enabled: planId != null,
    queryFn: ({ signal }) => getCatalogPlanById(env.platformApiBaseUrl, planId!, signal),
  });
}

export function useCatalogPlanListQuery(query: PlanListQuery, enabled: boolean) {
  return useQuery({
    queryKey: catalogPlanListQueryKey(query),
    enabled,
    queryFn: ({ signal }) =>
      listCatalogPlansPage(env.platformApiBaseUrl, {
        ...query,
        pageSize: query.pageSize ?? PLAN_LIST_PAGE_SIZE,
        signal,
      }),
  });
}

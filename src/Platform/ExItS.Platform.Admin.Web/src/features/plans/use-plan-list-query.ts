import { useQuery } from "@tanstack/react-query";
import { listCatalogPlansPage } from "@/api/catalog/plan-catalog-client";
import { PLAN_LIST_PAGE_SIZE, type PlanListQuery } from "@/api/catalog/plan-catalog-types";
import { env } from "@/lib/env";

export const planListQueryKey = (query: PlanListQuery) =>
  [
    "platform-catalog-plans",
    "list",
    query.page ?? 1,
    query.pageSize ?? PLAN_LIST_PAGE_SIZE,
    query.productCode ?? "",
    query.status ?? "",
    query.search ?? "",
    query.sortBy ?? "DisplayName",
    query.sortDesc === true,
  ] as const;

export function usePlanListQuery(query: PlanListQuery, enabled: boolean) {
  return useQuery({
    queryKey: planListQueryKey(query),
    enabled,
    queryFn: ({ signal }) =>
      listCatalogPlansPage(env.platformApiBaseUrl, {
        ...query,
        pageSize: query.pageSize ?? PLAN_LIST_PAGE_SIZE,
        signal,
      }),
  });
}

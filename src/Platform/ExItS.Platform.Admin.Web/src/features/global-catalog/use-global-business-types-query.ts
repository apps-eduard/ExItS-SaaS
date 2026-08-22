import { useQuery } from "@tanstack/react-query";
import { listGlobalBusinessTypes } from "@/api/global-catalog/global-catalog-client";
import { globalCatalogQueryKeys } from "@/api/global-catalog/global-catalog-query-keys";
import { env } from "@/lib/env";

export function useGlobalBusinessTypesQuery(enabled: boolean) {
  return useQuery({
    queryKey: globalCatalogQueryKeys.businessTypes,
    enabled,
    queryFn: ({ signal }) => listGlobalBusinessTypes(env.platformApiBaseUrl, signal),
  });
}

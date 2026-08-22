import { useQuery } from "@tanstack/react-query";
import { listActiveGlobalBusinessTypes } from "@/api/global-catalog/global-catalog-client";
import { globalCatalogQueryKeys } from "@/api/global-catalog/global-catalog-query-keys";
import { env } from "@/lib/env";

export function useGlobalBusinessTypesQuery(enabled: boolean) {
  return useQuery({
    queryKey: globalCatalogQueryKeys.businessTypes.lookup,
    enabled,
    queryFn: ({ signal }) => listActiveGlobalBusinessTypes(env.platformApiBaseUrl, signal),
  });
}

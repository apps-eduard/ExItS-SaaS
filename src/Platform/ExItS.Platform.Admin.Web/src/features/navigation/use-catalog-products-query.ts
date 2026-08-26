import { useQuery } from "@tanstack/react-query";
import { listCatalogProducts } from "@/api/catalog/product-catalog-client";
import { PLATFORM_PERMISSIONS } from "@/api/authorization/authorization-types";
import { useAuthorization } from "@/hooks/use-authorization";
import { env } from "@/lib/env";

export function useAuthorizedCatalogProductsQuery() {
  const authorization = useAuthorization();
  const canView = authorization.hasAnyPermission([
    PLATFORM_PERMISSIONS.viewPortfolio,
    PLATFORM_PERMISSIONS.manageOrganizations,
  ]);

  return useQuery({
    queryKey: ["platform-catalog-products", "nav"],
    enabled: authorization.status === "loaded" && canView,
    queryFn: ({ signal }) => listCatalogProducts(env.platformApiBaseUrl, { signal }),
    staleTime: 60_000,
    retry: false,
  });
}

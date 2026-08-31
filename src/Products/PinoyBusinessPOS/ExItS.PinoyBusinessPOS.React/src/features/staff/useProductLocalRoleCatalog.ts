import { useQuery } from "@tanstack/react-query";
import { listProductLocalRoleDefinitions } from "@/api/platform/product-local-role-definitions-client";

export function useProductLocalRoleCatalog(organizationId: string | null | undefined) {
  return useQuery({
    queryKey: ["product-local-role-definitions", organizationId],
    enabled: Boolean(organizationId),
    queryFn: async () => {
      const result = await listProductLocalRoleDefinitions(organizationId!);
      if (!result.ok) {
        throw new Error(result.body?.detail ?? "Failed to load role catalog.");
      }
      return result.roles.filter((role) => role.isAssignable);
    },
    staleTime: 60_000,
  });
}

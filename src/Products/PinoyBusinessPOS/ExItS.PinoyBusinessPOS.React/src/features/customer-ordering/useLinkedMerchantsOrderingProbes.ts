import { useMemo } from "react";
import { useQueries } from "@tanstack/react-query";
import { probeSellerCustomerOrderingCapability } from "@/api/pos/pos-customer-orders-client";

export type MerchantOrderingProbe = {
  canCustomerOrder: boolean;
  canCustomerDelivery: boolean;
  pending: boolean;
  resolved: boolean;
};

export function useLinkedMerchantsOrderingProbes(
  organizationIds: string[],
  enabled: boolean,
) {
  const uniqueIds = useMemo(
    () => [...new Set(organizationIds.filter(Boolean))],
    [organizationIds],
  );

  const queries = useQueries({
    queries: uniqueIds.map((organizationId) => ({
      queryKey: ["personal", "merchant-ordering-probe", organizationId] as const,
      enabled: enabled && Boolean(organizationId),
      staleTime: 60_000,
      retry: 1,
      meta: { suppressGlobalError: true, operation: "probe merchant ordering" },
      queryFn: ({ signal }: { signal?: AbortSignal }) =>
        probeSellerCustomerOrderingCapability(organizationId, signal),
    })),
  });

  const byOrganizationId = useMemo(() => {
    const map = new Map<string, MerchantOrderingProbe>();
    uniqueIds.forEach((organizationId, index) => {
      const query = queries[index];
      if (!query) {
        return;
      }

      if (query.isPending) {
        map.set(organizationId, {
          canCustomerOrder: false,
          canCustomerDelivery: false,
          pending: true,
          resolved: false,
        });
        return;
      }

      if (query.isSuccess && query.data) {
        map.set(organizationId, {
          ...query.data,
          pending: false,
          resolved: true,
        });
        return;
      }

      map.set(organizationId, {
        canCustomerOrder: false,
        canCustomerDelivery: false,
        pending: false,
        resolved: true,
      });
    });
    return map;
  }, [queries, uniqueIds]);

  const anyPending = queries.some((query) => query.isPending);

  return { byOrganizationId, anyPending };
}

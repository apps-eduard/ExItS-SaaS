import { useMemo } from "react";
import { useQueries } from "@tanstack/react-query";
import { probeSellerCustomerOrderingCapability } from "@/api/pos/pos-customer-orders-client";

export type MerchantOrderingProbe = {
  canCustomerOrder: boolean;
  canCustomerDelivery: boolean;
  pending: boolean;
  resolved: boolean;
};

export type MerchantOrderingProbeTarget = {
  organizationId: string;
  platformBusinessCustomerId?: string | null;
};

/**
 * Probes seller storefront shopping authorization per linked merchant.
 * Pass platformBusinessCustomerId so per-customer access (Blocked) and
 * Personal link gates are included — org-only probes are not authoritative.
 */
export function useLinkedMerchantsOrderingProbes(
  targets: MerchantOrderingProbeTarget[] | string[],
  enabled: boolean,
) {
  const normalized = useMemo(() => {
    const mapped = targets.map((entry) =>
      typeof entry === "string"
        ? { organizationId: entry, platformBusinessCustomerId: null as string | null }
        : {
            organizationId: entry.organizationId,
            platformBusinessCustomerId: entry.platformBusinessCustomerId ?? null,
          },
    );
    const seen = new Set<string>();
    return mapped.filter((row) => {
      if (!row.organizationId) {
        return false;
      }
      const key = `${row.organizationId}:${row.platformBusinessCustomerId ?? ""}`;
      if (seen.has(key)) {
        return false;
      }
      seen.add(key);
      return true;
    });
  }, [targets]);

  const queries = useQueries({
    queries: normalized.map((target) => ({
      queryKey: [
        "personal",
        "merchant-ordering-probe",
        target.organizationId,
        target.platformBusinessCustomerId ?? "",
      ] as const,
      enabled: enabled && Boolean(target.organizationId),
      staleTime: 60_000,
      retry: 1,
      meta: { suppressGlobalError: true, operation: "probe merchant ordering" },
      queryFn: ({ signal }: { signal?: AbortSignal }) =>
        probeSellerCustomerOrderingCapability(
          target.organizationId,
          signal,
          target.platformBusinessCustomerId ?? undefined,
        ),
    })),
  });

  const byOrganizationId = useMemo(() => {
    const map = new Map<string, MerchantOrderingProbe>();
    normalized.forEach((target, index) => {
      const query = queries[index];
      if (!query) {
        return;
      }

      // Prefer org+customer probe; if multiple customers share an org, last write wins
      // for the org-keyed map used by list/statement chips.
      if (query.isPending) {
        map.set(target.organizationId, {
          canCustomerOrder: false,
          canCustomerDelivery: false,
          pending: true,
          resolved: false,
        });
        return;
      }

      if (query.isSuccess && query.data) {
        map.set(target.organizationId, {
          ...query.data,
          pending: false,
          resolved: true,
        });
        return;
      }

      map.set(target.organizationId, {
        canCustomerOrder: false,
        canCustomerDelivery: false,
        pending: false,
        resolved: true,
      });
    });
    return map;
  }, [queries, normalized]);

  const anyPending = queries.some((query) => query.isPending);

  return { byOrganizationId, anyPending };
}

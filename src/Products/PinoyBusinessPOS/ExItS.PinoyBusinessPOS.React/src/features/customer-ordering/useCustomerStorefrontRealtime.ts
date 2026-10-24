import { useEffect } from "react";
import { HubConnectionBuilder } from "@microsoft/signalr";
import { useQueryClient } from "@tanstack/react-query";
import { platformApiUrl } from "@/api/platform/browser-session";

type StorefrontAvailabilityPayload = {
  organizationId?: string;
  branchId?: string;
};

/**
 * Personal customers: subscribe to Platform SignalR when watching a seller shop.
 * Store hours / pause / fulfillment saves push StorefrontAvailabilityChanged so
 * storefront + ordering-probe queries refresh without a manual reload.
 */
export function useCustomerStorefrontRealtime(organizationId: string | undefined | null): void {
  const queryClient = useQueryClient();

  useEffect(() => {
    if (!organizationId || import.meta.env.MODE === "test") {
      return;
    }

    const connection = new HubConnectionBuilder()
      .withUrl(platformApiUrl("/hubs/customer-storefront"), { withCredentials: true })
      .withAutomaticReconnect()
      .build();

    const invalidate = (payload?: StorefrontAvailabilityPayload) => {
      const orgId = payload?.organizationId ?? organizationId;
      if (payload?.organizationId && payload.organizationId !== organizationId) {
        return;
      }
      // Shop uses ["storefront", orgId, …]; checkout uses ["storefront", "checkout", orgId, …].
      void queryClient.invalidateQueries({
        predicate: (query) =>
          Array.isArray(query.queryKey) &&
          query.queryKey[0] === "storefront" &&
          query.queryKey.includes(orgId),
      });
      void queryClient.invalidateQueries({
        predicate: (query) =>
          Array.isArray(query.queryKey) &&
          query.queryKey[0] === "personal" &&
          query.queryKey[1] === "merchant-ordering-probe" &&
          query.queryKey.includes(orgId),
      });
    };

    connection.on("StorefrontAvailabilityChanged", invalidate);

    void connection
      .start()
      .then(() => connection.invoke("JoinSeller", organizationId))
      .catch(() => undefined);

    return () => {
      void connection.invoke("LeaveSeller", organizationId).catch(() => undefined);
      connection.off("StorefrontAvailabilityChanged", invalidate);
      void connection.stop();
    };
  }, [organizationId, queryClient]);
}

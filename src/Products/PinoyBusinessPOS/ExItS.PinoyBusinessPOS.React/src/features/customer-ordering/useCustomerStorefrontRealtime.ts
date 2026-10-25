import { useEffect } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { exitsRealtimeConnection } from "@/realtime/exits-realtime-connection";
import { RealtimeEventTypes } from "@/realtime/realtime-event-types";
import { applyRealtimeEventToQueries } from "@/realtime/realtime-query-map";

/**
 * Personal customers: join seller storefront group on the shared Platform realtime hub.
 * Hours / pause / fulfillment saves push StorefrontAvailabilityChanged (+ envelope).
 */
export function useCustomerStorefrontRealtime(organizationId: string | undefined | null): void {
  const queryClient = useQueryClient();

  useEffect(() => {
    if (!organizationId || import.meta.env.MODE === "test") {
      return;
    }

    void exitsRealtimeConnection.joinStorefrontSeller(organizationId);

    const unsubscribe = exitsRealtimeConnection.subscribe((envelope) => {
      if (
        envelope.eventType !== RealtimeEventTypes.StorefrontAvailabilityChanged
        && envelope.eventType !== RealtimeEventTypes.BranchOperatingHoursChanged
        && envelope.eventType !== RealtimeEventTypes.BranchFulfillmentSettingsChanged
      ) {
        return;
      }
      const orgId = envelope.data?.organizationId ?? envelope.scopeId ?? null;
      if (orgId && orgId !== organizationId) {
        return;
      }
      applyRealtimeEventToQueries(queryClient, {
        ...envelope,
        data: { ...(envelope.data ?? {}), organizationId },
      });
    });

    return () => {
      unsubscribe();
      void exitsRealtimeConnection.leaveStorefrontSeller(organizationId);
    };
  }, [organizationId, queryClient]);
}

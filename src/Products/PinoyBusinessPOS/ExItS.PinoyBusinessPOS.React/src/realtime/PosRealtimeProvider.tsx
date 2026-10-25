import { useEffect, type ReactNode } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { posRealtimeConnection } from "@/realtime/pos-realtime-connection";
import { applyPosRealtimeEventToQueries } from "@/realtime/pos-realtime-query-map";
import { useOptionalWorkspace } from "@/workspace/WorkspaceProvider";

/**
 * Mount once under QueryClientProvider + WorkspaceProvider when available.
 * Subscribes to POS /hubs/pos-realtime and maps envelopes to targeted query invalidation.
 */
export function PosRealtimeProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient();
  const workspace = useOptionalWorkspace();
  const organizationId = workspace?.boundWorkspace?.organizationId ?? null;

  useEffect(() => {
    if (import.meta.env.MODE === "test") {
      return;
    }

    const unsubscribe = posRealtimeConnection.subscribe((envelope) => {
      applyPosRealtimeEventToQueries(queryClient, envelope);
    });

    return unsubscribe;
  }, [queryClient]);

  useEffect(() => {
    if (!organizationId || import.meta.env.MODE === "test") {
      posRealtimeConnection.setOrganizationHeader(null);
      return;
    }
    void posRealtimeConnection.joinOrganization(organizationId);
    return () => {
      void posRealtimeConnection.leaveOrganization(organizationId);
    };
  }, [organizationId]);

  return <>{children}</>;
}

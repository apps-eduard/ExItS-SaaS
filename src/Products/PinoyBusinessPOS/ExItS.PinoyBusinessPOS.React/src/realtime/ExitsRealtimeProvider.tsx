import { useEffect, type ReactNode } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { exitsRealtimeConnection } from "@/realtime/exits-realtime-connection";
import { applyRealtimeEventToQueries } from "@/realtime/realtime-query-map";
import { useOptionalWorkspace } from "@/workspace/WorkspaceProvider";

/**
 * Mount once under QueryClientProvider + WorkspaceProvider when available.
 * Subscribes to Platform /hubs/exits-realtime and maps envelopes to targeted query invalidation.
 */
export function ExitsRealtimeProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient();
  const workspace = useOptionalWorkspace();
  const organizationId = workspace?.boundWorkspace?.organizationId ?? null;

  useEffect(() => {
    if (import.meta.env.MODE === "test") {
      return;
    }

    const unsubscribe = exitsRealtimeConnection.subscribe((envelope) => {
      applyRealtimeEventToQueries(queryClient, envelope);
    });
    const unsubReconnect = exitsRealtimeConnection.onReconnected(() => {
      // Missed-event recovery: refetch active org/personal settings and storefront probes.
      void queryClient.invalidateQueries({
        predicate: (q) =>
          Array.isArray(q.queryKey)
          && (q.queryKey[0] === "personal"
            || q.queryKey[0] === "storefront"
            || q.queryKey[0] === "branch-fulfillment"
            || q.queryKey[0] === "session"
            || q.queryKey[0] === "workspaces"),
      });
    });

    return () => {
      unsubscribe();
      unsubReconnect();
    };
  }, [queryClient]);

  useEffect(() => {
    if (!organizationId || import.meta.env.MODE === "test") {
      return;
    }
    void exitsRealtimeConnection.joinOrganization(organizationId);
    return () => {
      void exitsRealtimeConnection.leaveOrganization(organizationId);
    };
  }, [organizationId]);

  return <>{children}</>;
}

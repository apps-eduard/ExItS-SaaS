import { useEffect } from "react";
import { posRealtimeConnection } from "@/realtime/pos-realtime-connection";

/** Join a seller order detail group while the page is mounted. */
export function usePosRealtimeOrderWatch(
  sellerOrganizationId: string | undefined,
  orderId: string | undefined,
): void {
  useEffect(() => {
    if (!sellerOrganizationId || !orderId || import.meta.env.MODE === "test") {
      return;
    }
    void posRealtimeConnection.joinOrder(sellerOrganizationId, orderId);
    return () => {
      void posRealtimeConnection.leaveOrder(sellerOrganizationId, orderId);
    };
  }, [sellerOrganizationId, orderId]);
}

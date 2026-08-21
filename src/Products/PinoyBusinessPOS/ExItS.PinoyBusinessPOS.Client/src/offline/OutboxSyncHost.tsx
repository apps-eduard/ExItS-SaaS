import { useEffect, useRef } from "react";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { drainOutbox } from "@/offline/outbox-processor";
import { useOfflineSync } from "@/offline/OfflineSyncProvider";

/**
 * Warm-session outbox drain on reconnect.
 * Does not unlock protected LocalStore on cold start — that remains DEFERRED_SECURITY_GAP.
 */
export function OutboxSyncHost() {
  const online = useBrowserOnline();
  const { activeDb, activeScopeBinding, refreshCounts, markSuccessfulSync } = useOfflineSync();
  const drainingRef = useRef(false);
  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => {
    if (!online || !activeDb || !activeScopeBinding) {
      return;
    }

    if (debounceRef.current) {
      clearTimeout(debounceRef.current);
    }

    debounceRef.current = setTimeout(() => {
      if (drainingRef.current) {
        return;
      }
      drainingRef.current = true;
      void drainOutbox(activeDb, activeScopeBinding)
        .then(async (result) => {
          await refreshCounts();
          if (result.succeeded > 0 && result.failed === 0) {
            await markSuccessfulSync();
          }
        })
        .finally(() => {
          drainingRef.current = false;
        });
    }, 400);

    return () => {
      if (debounceRef.current) {
        clearTimeout(debounceRef.current);
      }
    };
  }, [online, activeDb, activeScopeBinding, refreshCounts, markSuccessfulSync]);

  return null;
}

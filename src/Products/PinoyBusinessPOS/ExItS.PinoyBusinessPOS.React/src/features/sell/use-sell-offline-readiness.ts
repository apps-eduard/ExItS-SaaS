import { useEffect, useState } from "react";
import { useAppOnline } from "@/connectivity/ConnectivityProvider";
import { useShiftContext } from "@/features/shifts/ShiftContextProvider";
import {
  isSellReadinessSnapshotUsable,
  loadSellReadinessSnapshot,
  type SellReadinessSnapshot,
} from "@/offline/sell-readiness-snapshot";
import { saveSellReadinessSnapshotIfChanged } from "@/offline/sell-readiness-sync";
import { useSellOfflineContext, type SellOfflineContext } from "@/offline/sell-offline-context";
import { organizationWebAllowsOfflineBusinessReads } from "@/runtime/organization-web-runtime-policy";
import { isPosDeviceReadyForMoney } from "@/workspace/pos-device-context";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

/**
 * Sell readiness for Organization.
 *
 * Organization Web/PWA is online-only (ORG-PWA-ONLINE-ONLY-01): snapshot fallback and
 * offline LocalStore continuation are disabled. The preserved snapshot write-through still
 * runs while online so future Capacitor can reuse the engine.
 */
export type SellOfflineReadiness = {
  online: boolean;
  deviceReady: boolean;
  moneyPostReady: boolean;
  shiftGateReady: boolean;
  shiftId: string | null;
  openShiftNumber: string | null;
  /** True when the values above came from the snapshot instead of live server state. */
  fromSnapshot: boolean;
  offlineContext: SellOfflineContext | null;
};

export function useSellOfflineReadiness(): SellOfflineReadiness {
  const online = useAppOnline();
  const { posDevice, deviceEnforcementEnabled } = useWorkspace();
  const { readiness, currentShift } = useShiftContext();
  const offlineContext = useSellOfflineContext();
  const [snapshot, setSnapshot] = useState<SellReadinessSnapshot | null>(null);
  const allowOfflineReads = organizationWebAllowsOfflineBusinessReads();

  const liveDeviceReady = isPosDeviceReadyForMoney(posDevice, {
    enforcementEnabled: deviceEnforcementEnabled,
  });
  const liveShiftId = readiness.shiftId;
  const liveOpenShiftNumber = currentShift?.shiftNumber ?? null;
  const liveReady =
    liveDeviceReady && readiness.moneyPostReady && readiness.shiftGateReady && liveShiftId != null;

  const db = offlineContext?.db ?? null;

  useEffect(() => {
    if (!online || !liveReady || !db || !liveShiftId) {
      return;
    }
    void saveSellReadinessSnapshotIfChanged(db, {
      deviceReady: true,
      moneyPostReady: true,
      shiftId: liveShiftId,
      openShiftNumber: liveOpenShiftNumber,
    }).catch(() => {
      // A snapshot write failure must never block an online sale.
    });
  }, [db, liveOpenShiftNumber, liveReady, liveShiftId, online]);

  useEffect(() => {
    if (!allowOfflineReads || online || liveReady || !db) {
      return;
    }
    let cancelled = false;
    void loadSellReadinessSnapshot(db).then((loaded) => {
      if (!cancelled) {
        setSnapshot(loaded);
      }
    });
    return () => {
      cancelled = true;
    };
  }, [allowOfflineReads, db, liveReady, online]);

  const useSnapshot =
    allowOfflineReads && !online && !liveReady && isSellReadinessSnapshotUsable(snapshot);

  if (useSnapshot && snapshot) {
    return {
      online,
      deviceReady: snapshot.deviceReady,
      moneyPostReady: snapshot.moneyPostReady,
      shiftGateReady: snapshot.shiftId != null,
      shiftId: snapshot.shiftId,
      openShiftNumber: snapshot.openShiftNumber,
      fromSnapshot: true,
      offlineContext,
    };
  }

  return {
    online,
    deviceReady: liveDeviceReady,
    moneyPostReady: readiness.moneyPostReady,
    shiftGateReady: readiness.shiftGateReady,
    shiftId: liveShiftId,
    openShiftNumber: liveOpenShiftNumber,
    fromSnapshot: false,
    offlineContext,
  };
}

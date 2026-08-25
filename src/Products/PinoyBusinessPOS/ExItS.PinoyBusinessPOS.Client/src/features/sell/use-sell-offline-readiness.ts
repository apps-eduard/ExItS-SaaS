import { useEffect, useState } from "react";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { useShiftContext } from "@/features/shifts/ShiftContextProvider";
import {
  isSellReadinessSnapshotUsable,
  loadSellReadinessSnapshot,
  saveSellReadinessSnapshot,
  type SellReadinessSnapshot,
} from "@/offline/sell-readiness-snapshot";
import { useSellOfflineContext, type SellOfflineContext } from "@/offline/sell-offline-context";
import { isPosDeviceReadyForMoney } from "@/workspace/pos-device-context";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

/**
 * Sell readiness that survives losing the network (RMAP-21D).
 *
 * While online and genuinely ready, the live device/shift state is written to the offline
 * snapshot. While offline, and only when the live hydrate could not confirm readiness, the
 * last-good snapshot stands in so a warm session keeps selling Cash on the same shift.
 * This is UX continuation, not authorization — the server still accepts or rejects the sale.
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
  const online = useBrowserOnline();
  const { posDevice, deviceEnforcementEnabled } = useWorkspace();
  const { readiness, currentShift } = useShiftContext();
  const offlineContext = useSellOfflineContext();
  const [snapshot, setSnapshot] = useState<SellReadinessSnapshot | null>(null);

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
    void saveSellReadinessSnapshot(db, {
      deviceReady: true,
      moneyPostReady: true,
      shiftId: liveShiftId,
      openShiftNumber: liveOpenShiftNumber,
    }).catch(() => {
      // A snapshot write failure must never block an online sale.
    });
  }, [db, liveOpenShiftNumber, liveReady, liveShiftId, online]);

  useEffect(() => {
    if (online || liveReady || !db) {
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
  }, [db, liveReady, online]);

  const useSnapshot = !online && !liveReady && isSellReadinessSnapshotUsable(snapshot);

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

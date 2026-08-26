import type { OfflineDb } from "@/offline/db";
import { SELL_READINESS_SNAPSHOT_KEY, type SellReadinessSnapshotRecord } from "@/offline/types";

/**
 * Last-good Sell readiness snapshot (RMAP-21D).
 * Captured only while online and genuinely ready, so an offline warm session can keep
 * selling Cash on the same device and shift. It is a UX continuation aid, never an
 * authorization claim: the server still decides whether the queued sale is accepted.
 */

export type SellReadinessSnapshot = {
  deviceReady: boolean;
  moneyPostReady: boolean;
  shiftId: string | null;
  openShiftNumber: string | null;
  capturedAt: string;
};

/** A snapshot older than one day cannot stand in for a live shift. */
export const SELL_READINESS_SNAPSHOT_MAX_AGE_MS = 24 * 60 * 60 * 1000;

export async function saveSellReadinessSnapshot(
  db: OfflineDb,
  input: Omit<SellReadinessSnapshot, "capturedAt"> & { capturedAt?: string },
): Promise<SellReadinessSnapshot> {
  const snapshot: SellReadinessSnapshot = {
    deviceReady: input.deviceReady,
    moneyPostReady: input.moneyPostReady,
    shiftId: input.shiftId,
    openShiftNumber: input.openShiftNumber,
    capturedAt: input.capturedAt ?? new Date().toISOString(),
  };
  const record: SellReadinessSnapshotRecord = {
    key: SELL_READINESS_SNAPSHOT_KEY,
    ...snapshot,
  };
  await db.put("sellReadiness", record);
  return snapshot;
}

export async function loadSellReadinessSnapshot(
  db: OfflineDb,
): Promise<SellReadinessSnapshot | null> {
  try {
    const row = await db.get("sellReadiness", SELL_READINESS_SNAPSHOT_KEY);
    if (!row) {
      return null;
    }
    return {
      deviceReady: row.deviceReady === true,
      moneyPostReady: row.moneyPostReady === true,
      shiftId: row.shiftId ?? null,
      openShiftNumber: row.openShiftNumber ?? null,
      capturedAt: row.capturedAt,
    };
  } catch {
    return null;
  }
}

export async function clearSellReadinessSnapshot(db: OfflineDb): Promise<void> {
  await db.delete("sellReadiness", SELL_READINESS_SNAPSHOT_KEY);
}

/** Fail closed: an unparseable or stale snapshot is not usable readiness. */
export function isSellReadinessSnapshotUsable(
  snapshot: SellReadinessSnapshot | null,
  now: number = Date.now(),
  maxAgeMs: number = SELL_READINESS_SNAPSHOT_MAX_AGE_MS,
): boolean {
  if (!snapshot || !snapshot.deviceReady || !snapshot.moneyPostReady || !snapshot.shiftId) {
    return false;
  }
  const capturedAt = Date.parse(snapshot.capturedAt);
  if (!Number.isFinite(capturedAt)) {
    return false;
  }
  return now - capturedAt <= maxAgeMs && capturedAt - now <= maxAgeMs;
}

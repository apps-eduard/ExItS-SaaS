import type { OfflineDb } from "@/offline/db";
import {
  saveSellReadinessSnapshot,
  type SellReadinessSnapshot,
} from "@/offline/sell-readiness-snapshot";

type SnapshotInput = Omit<SellReadinessSnapshot, "capturedAt">;

function snapshotFingerprint(input: SnapshotInput): string {
  return [
    input.deviceReady,
    input.moneyPostReady,
    input.shiftId ?? "",
    input.openShiftNumber ?? "",
  ].join("::");
}

type SnapshotState = {
  lastFingerprint: string | null;
  inFlight: Promise<SellReadinessSnapshot> | null;
  inFlightFingerprint: string | null;
  writeCount: number;
};

const snapshotByDb = new Map<string, SnapshotState>();

function dbKey(db: OfflineDb): string {
  return db.name;
}

function stateFor(db: OfflineDb): SnapshotState {
  const key = dbKey(db);
  let state = snapshotByDb.get(key);
  if (!state) {
    state = {
      lastFingerprint: null,
      inFlight: null,
      inFlightFingerprint: null,
      writeCount: 0,
    };
    snapshotByDb.set(key, state);
  }
  return state;
}

export function resetSellReadinessSyncStateForTests(): void {
  snapshotByDb.clear();
}

export function getSellReadinessSyncStats(db: OfflineDb): {
  writeCount: number;
  lastFingerprint: string | null;
  inFlight: boolean;
} {
  const state = stateFor(db);
  return {
    writeCount: state.writeCount,
    lastFingerprint: state.lastFingerprint,
    inFlight: state.inFlight !== null,
  };
}

/** Persist readiness only when live shift/device facts actually changed. */
export async function saveSellReadinessSnapshotIfChanged(
  db: OfflineDb,
  input: SnapshotInput,
): Promise<void> {
  const fingerprint = snapshotFingerprint(input);
  const state = stateFor(db);

  if (fingerprint === state.lastFingerprint) {
    return;
  }

  if (state.inFlight && state.inFlightFingerprint === fingerprint) {
    await state.inFlight;
    return;
  }

  if (state.inFlight) {
    await state.inFlight.catch(() => {});
    if (fingerprint === state.lastFingerprint) {
      return;
    }
  }

  state.inFlightFingerprint = fingerprint;
  state.inFlight = (async () => {
    try {
      const saved = await saveSellReadinessSnapshot(db, input);
      state.lastFingerprint = fingerprint;
      state.writeCount += 1;
      return saved;
    } finally {
      state.inFlight = null;
      state.inFlightFingerprint = null;
    }
  })();

  await state.inFlight;
}

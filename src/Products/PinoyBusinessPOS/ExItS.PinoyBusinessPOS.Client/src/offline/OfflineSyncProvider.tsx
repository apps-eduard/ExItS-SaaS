import { createContext, useCallback, useContext, useMemo, useState, type ReactNode } from "react";
import type { OfflineDb } from "@/offline/db";
import { getMeta, putMeta } from "@/offline/db";
import { drainOutbox } from "@/offline/outbox-processor";
import { getOutboxCounts, recoverAbandonedSyncing } from "@/offline/outbox";
import {
  isFullySynced,
  needsAttentionCount,
  waitingSyncCount,
  type OfflineQueueCounts,
} from "@/offline/types";

const emptyCounts: OfflineQueueCounts = {
  pending: 0,
  syncing: 0,
  succeeded: 0,
  retryableFailure: 0,
  permanentFailure: 0,
  conflict: 0,
  blockedByAccess: 0,
};

type OfflineSyncContextValue = {
  counts: OfflineQueueCounts;
  lastSuccessfulSyncAt: string | null;
  activeDb: OfflineDb | null;
  activeScopeBinding: string | null;
  bindDatabase: (db: OfflineDb | null, scopeBinding?: string | null) => Promise<void>;
  refreshCounts: () => Promise<void>;
  markSuccessfulSync: () => Promise<void>;
  /** Recover abandoned Syncing rows, then drain the outbox when online. */
  retrySync: () => Promise<void>;
  /** @deprecated use retrySync — kept for older call sites during Master Run. */
  retrySyncPreparation: () => Promise<void>;
};

const OfflineSyncContext = createContext<OfflineSyncContextValue | null>(null);

export function OfflineSyncProvider({ children }: { children: ReactNode }) {
  const [activeDb, setActiveDb] = useState<OfflineDb | null>(null);
  const [activeScopeBinding, setActiveScopeBinding] = useState<string | null>(null);
  const [counts, setCounts] = useState<OfflineQueueCounts>(emptyCounts);
  const [lastSuccessfulSyncAt, setLastSuccessfulSyncAt] = useState<string | null>(null);

  const refreshCounts = useCallback(async () => {
    if (!activeDb) {
      setCounts(emptyCounts);
      return;
    }
    setCounts(await getOutboxCounts(activeDb));
    setLastSuccessfulSyncAt(await getMeta(activeDb, "lastSuccessfulSyncAt"));
  }, [activeDb]);

  const bindDatabase = useCallback(async (db: OfflineDb | null, scopeBinding?: string | null) => {
    setActiveDb(db);
    setActiveScopeBinding(db ? (scopeBinding ?? null) : null);
    if (!db) {
      setCounts(emptyCounts);
      setLastSuccessfulSyncAt(null);
      return;
    }
    await recoverAbandonedSyncing(db);
    setCounts(await getOutboxCounts(db));
    setLastSuccessfulSyncAt(await getMeta(db, "lastSuccessfulSyncAt"));
  }, []);

  const markSuccessfulSync = useCallback(async () => {
    if (!activeDb) {
      return;
    }
    const at = new Date().toISOString();
    await putMeta(activeDb, "lastSuccessfulSyncAt", at);
    setLastSuccessfulSyncAt(at);
    setCounts(await getOutboxCounts(activeDb));
  }, [activeDb]);

  const retrySync = useCallback(async () => {
    if (!activeDb || !activeScopeBinding) {
      return;
    }
    const result = await drainOutbox(activeDb, activeScopeBinding);
    setCounts(await getOutboxCounts(activeDb));
    if (result.succeeded > 0) {
      const at = new Date().toISOString();
      await putMeta(activeDb, "lastSuccessfulSyncAt", at);
      setLastSuccessfulSyncAt(at);
    }
  }, [activeDb, activeScopeBinding]);

  const value = useMemo(
    () => ({
      counts,
      lastSuccessfulSyncAt,
      activeDb,
      activeScopeBinding,
      bindDatabase,
      refreshCounts,
      markSuccessfulSync,
      retrySync,
      retrySyncPreparation: retrySync,
    }),
    [
      counts,
      lastSuccessfulSyncAt,
      activeDb,
      activeScopeBinding,
      bindDatabase,
      refreshCounts,
      markSuccessfulSync,
      retrySync,
    ],
  );

  return <OfflineSyncContext.Provider value={value}>{children}</OfflineSyncContext.Provider>;
}

export function useOfflineSync(): OfflineSyncContextValue {
  const ctx = useContext(OfflineSyncContext);
  if (!ctx) {
    throw new Error("useOfflineSync requires OfflineSyncProvider");
  }
  return ctx;
}

export function describeSyncSummary(counts: OfflineQueueCounts): {
  kind: "synced" | "waiting" | "syncing" | "attention" | "access";
  waiting: number;
  attention: number;
} {
  if (counts.blockedByAccess > 0) {
    return {
      kind: "access",
      waiting: waitingSyncCount(counts),
      attention: needsAttentionCount(counts),
    };
  }
  if (needsAttentionCount(counts) > 0) {
    return {
      kind: "attention",
      waiting: waitingSyncCount(counts),
      attention: needsAttentionCount(counts),
    };
  }
  if (counts.syncing > 0) {
    return { kind: "syncing", waiting: waitingSyncCount(counts), attention: 0 };
  }
  if (!isFullySynced(counts) || waitingSyncCount(counts) > 0) {
    return { kind: "waiting", waiting: waitingSyncCount(counts), attention: 0 };
  }
  return { kind: "synced", waiting: 0, attention: 0 };
}

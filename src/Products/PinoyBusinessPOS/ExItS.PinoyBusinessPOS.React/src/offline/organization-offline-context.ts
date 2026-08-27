import { useAppOnline } from "@/connectivity/ConnectivityProvider";
import type { OfflineDb } from "@/offline/db";
import { openSharedOfflineDatabase, organizationScopeKey } from "@/offline/db";
import { maybeMigrateLegacyLocalStoreWhenReady } from "@/offline/local-store-migration";
import { useOfflineSync } from "@/offline/OfflineSyncProvider";
import {
  organizationWebAllowsOfflineBusinessReads,
  organizationWebAllowsOfflineSession,
} from "@/runtime/organization-web-runtime-policy";
import { isAuthenticatedOrColdStartOffline, useSession } from "@/session/SessionProvider";
import { peekDurableInstallationDeviceId } from "@/workspace/browser-installation-identity";
import { useWorkspace } from "@/workspace/WorkspaceProvider";
import { useEffect, useMemo, useState } from "react";

/**
 * Organization-scoped offline store, shared by every Business surface that caches or queues work.
 * The database is keyed by user + organization + branch + installation device, so a different
 * cashier, branch, or browser never reads another scope's cached customers or queued money.
 *
 * Organization Web/PWA (ORG-PWA-ONLINE-ONLY-01): while offline, this hook returns null so
 * LocalStore is not treated as an active operating session. While online, the store may still
 * open so legacy pending outbox rows can drain and write-through cache can warm for future native.
 */
export type OrganizationOfflineContext = {
  db: OfflineDb;
  /** Envelope key material — the same organization scope key the database is named after. */
  scopeBinding: string;
  userId: string;
  organizationId: string;
  branchId: string;
  installationDeviceId: string;
  posDeviceId: string | null;
};

export function useOrganizationOfflineContext(): OrganizationOfflineContext | null {
  const { session, status: sessionStatus } = useSession();
  const { boundWorkspace, posDevice } = useWorkspace();
  const { bindDatabase } = useOfflineSync();
  const online = useAppOnline();
  const [opened, setOpened] = useState<{ db: OfflineDb; scopeBinding: string } | null>(null);

  const userId = session?.userId ?? null;
  const organizationId = boundWorkspace?.organizationId ?? null;
  const branchId = boundWorkspace?.branchId ?? null;
  const installationDeviceId = posDevice.installationDeviceId ?? peekDurableInstallationDeviceId();
  const posDeviceId = posDevice.posDeviceId ?? null;

  const allowOfflineReads = organizationWebAllowsOfflineBusinessReads();
  const allowOfflineSession = organizationWebAllowsOfflineSession();
  /** Open LocalStore only when online (legacy drain / warm) unless offline reads are enabled. */
  const mayOpenStore = allowOfflineReads || online;

  useEffect(() => {
    if (!userId || !organizationId || !branchId || !installationDeviceId || !mayOpenStore) {
      setOpened(null);
      return;
    }

    const scopeBinding = organizationScopeKey({
      userId,
      organizationId,
      branchId,
      installationDeviceId,
    });

    let cancelled = false;
    void openSharedOfflineDatabase("Organization", scopeBinding)
      .then(async (db) => {
        if (cancelled) {
          return;
        }
        setOpened({ db, scopeBinding });
        await bindDatabase(db, scopeBinding);
      })
      .catch(() => {
        if (!cancelled) {
          setOpened(null);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [bindDatabase, branchId, installationDeviceId, mayOpenStore, organizationId, userId]);

  useEffect(() => {
    if (!opened || !userId || !isAuthenticatedOrColdStartOffline(sessionStatus)) {
      return;
    }
    if (!online) {
      return;
    }
    void maybeMigrateLegacyLocalStoreWhenReady(opened.db, opened.scopeBinding, userId, {
      online: true,
      trustedSession: true,
    });
  }, [opened, online, sessionStatus, userId]);

  return useMemo(() => {
    if (!opened || !userId || !organizationId || !branchId || !installationDeviceId) {
      return null;
    }
    // Web online-only: never expose LocalStore as an offline operating context.
    if (!allowOfflineSession && !allowOfflineReads && !online) {
      return null;
    }
    return {
      db: opened.db,
      scopeBinding: opened.scopeBinding,
      userId,
      organizationId,
      branchId,
      installationDeviceId,
      posDeviceId,
    };
  }, [
    allowOfflineReads,
    allowOfflineSession,
    branchId,
    installationDeviceId,
    online,
    opened,
    organizationId,
    posDeviceId,
    userId,
  ]);
}

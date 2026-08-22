import { useEffect, useMemo, useState } from "react";
import type { OfflineDb } from "@/offline/db";
import { openSharedOfflineDatabase, organizationScopeKey } from "@/offline/db";
import { maybeMigrateLegacyLocalStoreWhenReady } from "@/offline/local-store-migration";
import { useOfflineSync } from "@/offline/OfflineSyncProvider";
import { isAuthenticatedOrColdStartOffline, useSession } from "@/session/SessionProvider";
import { peekDurableInstallationDeviceId } from "@/workspace/browser-installation-identity";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

/**
 * Organization-scoped offline store, shared by every Business surface that caches or queues work.
 * The database is keyed by user + organization + branch + installation device, so a different
 * cashier, branch, or browser never reads another scope's cached customers or queued money.
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
  const [opened, setOpened] = useState<{ db: OfflineDb; scopeBinding: string } | null>(null);

  const userId = session?.userId ?? null;
  const organizationId = boundWorkspace?.organizationId ?? null;
  const branchId = boundWorkspace?.branchId ?? null;
  // Durable installation identity is local, so it survives an offline device authorize failure.
  const installationDeviceId = posDevice.installationDeviceId ?? peekDurableInstallationDeviceId();
  const posDeviceId = posDevice.posDeviceId ?? null;

  useEffect(() => {
    if (!userId || !organizationId || !branchId || !installationDeviceId) {
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
  }, [bindDatabase, branchId, installationDeviceId, organizationId, userId]);

  useEffect(() => {
    if (!opened || !userId || !isAuthenticatedOrColdStartOffline(sessionStatus)) {
      return;
    }
    if (typeof navigator !== "undefined" && navigator.onLine === false) {
      return;
    }
    void maybeMigrateLegacyLocalStoreWhenReady(opened.db, opened.scopeBinding, userId, {
      online: true,
      trustedSession: true,
    });
  }, [opened, sessionStatus, userId]);

  return useMemo(() => {
    if (!opened || !userId || !organizationId || !branchId || !installationDeviceId) {
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
  }, [branchId, installationDeviceId, opened, organizationId, posDeviceId, userId]);
}

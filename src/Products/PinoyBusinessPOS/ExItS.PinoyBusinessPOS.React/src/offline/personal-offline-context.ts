import { useAppOnline } from "@/connectivity/ConnectivityProvider";
import { useEffect, useMemo, useState } from "react";
import type { BrowserSessionSnapshot } from "@/api/platform/browser-session";
import type { OfflineDb } from "@/offline/db";
import { openSharedOfflineDatabase, personalScopeKey } from "@/offline/db";
import { useOfflineSync } from "@/offline/OfflineSyncProvider";
import {
  personalWebAllowsOfflineBusinessReads,
  personalWebAllowsOfflineSession,
} from "@/runtime/personal-web-runtime-policy";
import { isOrganizationContextLocked, sessionAccountClass } from "@/session/account-class";
import { useSession } from "@/session/SessionProvider";

/**
 * Personal-scoped offline store (RMAP-21F).
 *
 * A Personal LocalStore holds who owes the signed-in person money — the most sensitive data in the
 * app and none of the organization's business. It is a separate database from every Organization
 * store, keyed by the Personal user id, and it is opened only for a principal the Platform already
 * calls Personal. Organization staff are locked to their home organization and have no Personal
 * profile at all, so they must never reach this database, not even to create it.
 *
 * Personal Web/PWA (PERS-WEB-ONLINE-ONLY-01): while offline, this hook returns null so LocalStore
 * is not treated as an active operating session. While online, the store may still open so legacy
 * pending outbox rows can drain and write-through cache can warm for future native.
 */
export type PersonalOfflineContext = {
  db: OfflineDb;
  /** Envelope key material — the same Personal scope key the database is named after. */
  scopeBinding: string;
  userId: string;
};

export type PersonalOfflineEligibility =
  | { eligible: true; userId: string; scopeBinding: string }
  | { eligible: false; reason: "no-session" | "staff-locked" | "not-personal" };

/**
 * Fail closed: only an unlocked Personal principal may open a Personal store. Anything unknown or
 * organization-shaped is refused rather than given an empty Personal database of its own.
 */
export function personalOfflineEligibility(
  session: BrowserSessionSnapshot | null | undefined,
): PersonalOfflineEligibility {
  const userId = session?.userId;
  if (!userId) {
    return { eligible: false, reason: "no-session" };
  }
  if (isOrganizationContextLocked(session)) {
    return { eligible: false, reason: "staff-locked" };
  }
  if (sessionAccountClass(session) !== "Personal") {
    return { eligible: false, reason: "not-personal" };
  }
  return { eligible: true, userId, scopeBinding: personalScopeKey(userId) };
}

export function usePersonalOfflineContext(): PersonalOfflineContext | null {
  const { session } = useSession();
  const { bindDatabase } = useOfflineSync();
  const online = useAppOnline();
  const [opened, setOpened] = useState<{ db: OfflineDb; scopeBinding: string } | null>(null);

  const eligibility = personalOfflineEligibility(session);
  const scopeBinding = eligibility.eligible ? eligibility.scopeBinding : null;
  const userId = eligibility.eligible ? eligibility.userId : null;

  const allowOfflineReads = personalWebAllowsOfflineBusinessReads();
  const allowOfflineSession = personalWebAllowsOfflineSession();
  /** Open LocalStore only when online (legacy drain / warm) unless offline reads are enabled. */
  const mayOpenStore = allowOfflineReads || online;

  useEffect(() => {
    if (!scopeBinding || !mayOpenStore) {
      setOpened(null);
      return;
    }

    let cancelled = false;
    void openSharedOfflineDatabase("Personal", scopeBinding)
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
  }, [bindDatabase, mayOpenStore, scopeBinding]);

  return useMemo(() => {
    if (!opened || !userId || opened.scopeBinding !== scopeBinding) {
      return null;
    }
    // Web online-only: never expose LocalStore as an offline operating context.
    if (!allowOfflineSession && !allowOfflineReads && !online) {
      return null;
    }
    return { db: opened.db, scopeBinding: opened.scopeBinding, userId };
  }, [
    allowOfflineReads,
    allowOfflineSession,
    online,
    opened,
    scopeBinding,
    userId,
  ]);
}

/**
 * Non-secret marker: local sign-out completed but Platform logout could not be reached.
 * Prevents silent session restoration on reconnect until remote logout succeeds.
 */

export const PENDING_REMOTE_LOGOUT_STORE_KEY = "exits.pos-client.pending-remote-logout.v1";

export type PendingRemoteLogoutMarker = {
  version: 1;
  markedAtUtc: string;
};

export function hasPendingRemoteLogout(): boolean {
  if (typeof window === "undefined" || typeof window.localStorage === "undefined") {
    return false;
  }
  try {
    const raw = window.localStorage.getItem(PENDING_REMOTE_LOGOUT_STORE_KEY);
    if (!raw) {
      return false;
    }
    const parsed = JSON.parse(raw) as Partial<PendingRemoteLogoutMarker>;
    return parsed?.version === 1 && typeof parsed.markedAtUtc === "string";
  } catch {
    return false;
  }
}

export function markPendingRemoteLogout(now: Date = new Date()): void {
  if (typeof window === "undefined" || typeof window.localStorage === "undefined") {
    return;
  }
  const marker: PendingRemoteLogoutMarker = {
    version: 1,
    markedAtUtc: now.toISOString(),
  };
  try {
    window.localStorage.setItem(PENDING_REMOTE_LOGOUT_STORE_KEY, JSON.stringify(marker));
  } catch {
    // ignore quota errors
  }
}

export function clearPendingRemoteLogout(): void {
  if (typeof window === "undefined" || typeof window.localStorage === "undefined") {
    return;
  }
  try {
    window.localStorage.removeItem(PENDING_REMOTE_LOGOUT_STORE_KEY);
  } catch {
    // ignore
  }
}

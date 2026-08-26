import {
  clearPendingRemoteLogout,
  hasPendingRemoteLogout,
  markPendingRemoteLogout,
} from "@/session/pending-remote-logout";
import { logoutSession } from "@/api/platform/platform-auth-client";

/**
 * Completes a deferred Platform logout after local sign-out while offline.
 * Returns true when no pending marker remains.
 */
export async function completePendingRemoteLogoutIfNeeded(): Promise<boolean> {
  if (!hasPendingRemoteLogout()) {
    return true;
  }

  try {
    await logoutSession();
    clearPendingRemoteLogout();
    return true;
  } catch {
    markPendingRemoteLogout();
    return false;
  }
}

export { clearPendingRemoteLogout, hasPendingRemoteLogout, markPendingRemoteLogout };

/**
 * Browser/OS network reachability only (`navigator.onLine`).
 * This is NOT ExItS API health, sync state, or operational Online/Offline.
 */
export function getBrowserNetworkReachability(): boolean | "unknown" {
  if (typeof navigator === "undefined" || typeof navigator.onLine !== "boolean") {
    return "unknown";
  }
  return navigator.onLine;
}

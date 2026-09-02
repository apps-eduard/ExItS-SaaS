import { useSyncExternalStore } from "react";

/**
 * Lightweight chrome flag so Sell cart/checkout can hide the org bottom nav
 * without restructuring RootLayout providers.
 */
let hideOrgBottomNav = false;
const listeners = new Set<() => void>();

function emit() {
  for (const listener of listeners) {
    listener();
  }
}

export function setOrgBottomNavHidden(hidden: boolean): void {
  if (hideOrgBottomNav === hidden) {
    return;
  }
  hideOrgBottomNav = hidden;
  emit();
}

export function getOrgBottomNavHidden(): boolean {
  return hideOrgBottomNav;
}

export function subscribeOrgBottomNavHidden(listener: () => void): () => void {
  listeners.add(listener);
  return () => {
    listeners.delete(listener);
  };
}

export function useOrgBottomNavHidden(): boolean {
  return useSyncExternalStore(subscribeOrgBottomNavHidden, getOrgBottomNavHidden, () => false);
}

/** True while Sell checkout / payment / summary routes own the bottom of the screen. */
export function isSellTransactionPath(pathname: string): boolean {
  return (
    pathname.startsWith("/sell/checkout") ||
    pathname.startsWith("/sell/offline-queued") ||
    pathname.startsWith("/sell/sales/")
  );
}

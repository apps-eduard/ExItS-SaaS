/**
 * Legacy Personal/Business Home surface preference.
 * Kept for future POS activation; V2 Personal-only Home ignores this store.
 */
export type HomeSurface = "personal" | "business";

const KEY_PREFIX = "exits.v2.home-surface.v1:";

/** Business Home UI switch is disabled while Personal-only V2 scope is active. */
export const HOME_BUSINESS_SURFACE_ENABLED = false;

export function readHomeSurface(
  userId: string | undefined,
  fallback: HomeSurface,
): HomeSurface {
  if (!HOME_BUSINESS_SURFACE_ENABLED) return "personal";
  if (!userId || typeof window === "undefined") return fallback;
  try {
    const value = window.sessionStorage.getItem(`${KEY_PREFIX}${userId}`);
    if (value === "personal" || value === "business") return value;
  } catch {
    // ignore
  }
  return fallback;
}

export function writeHomeSurface(
  userId: string | undefined,
  surface: HomeSurface,
): void {
  if (!HOME_BUSINESS_SURFACE_ENABLED) return;
  if (!userId || typeof window === "undefined") return;
  try {
    window.sessionStorage.setItem(`${KEY_PREFIX}${userId}`, surface);
  } catch {
    // ignore
  }
}

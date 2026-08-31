/** Map tile / center helpers for the branch delivery location picker. */

export const PHILIPPINES_DEFAULT_CENTER = {
  latitude: 12.8797,
  longitude: 121.774,
} as const;

export const DEFAULT_MAP_ZOOM = 15;

export function resolveMapTilesUrl(
  env: Record<string, string | undefined> = import.meta.env as Record<string, string | undefined>,
): string | null {
  const tile = env.VITE_MAP_TILES_URL?.trim();
  return tile || null;
}

export function resolveMapTilesAttribution(
  env: Record<string, string | undefined> = import.meta.env as Record<string, string | undefined>,
): string {
  const attribution = env.VITE_MAP_TILES_ATTRIBUTION?.trim();
  if (attribution) {
    return attribution;
  }
  return '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>';
}

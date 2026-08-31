/** WGS84 latitude/longitude validation and parsing (authoritative for fulfillment). */

export const LATITUDE_MIN = -90;
export const LATITUDE_MAX = 90;
export const LONGITUDE_MIN = -180;
export const LONGITUDE_MAX = 180;

export function isValidLatitude(value: number): boolean {
  return Number.isFinite(value) && value >= LATITUDE_MIN && value <= LATITUDE_MAX;
}

export function isValidLongitude(value: number): boolean {
  return Number.isFinite(value) && value >= LONGITUDE_MIN && value <= LONGITUDE_MAX;
}

export function isValidCoordinatePair(
  latitude: number | null | undefined,
  longitude: number | null | undefined,
): boolean {
  if (latitude == null || longitude == null) {
    return false;
  }
  return isValidLatitude(latitude) && isValidLongitude(longitude);
}

export function parseCoordinateInput(raw: string): number | null {
  const trimmed = raw.trim();
  if (!trimmed) {
    return null;
  }
  const n = Number(trimmed);
  return Number.isFinite(n) ? n : Number.NaN;
}

export type CoordinateParseResult =
  | { ok: true; latitude: number | null; longitude: number | null; clearCoordinates: boolean }
  | { ok: false; error: "invalid_latitude" | "invalid_longitude" | "pair_incomplete" };

/**
 * Parse optional lat/lng text fields for UpdateBranchRequest.
 * Empty+empty → clearCoordinates. One filled without the other → pair_incomplete.
 */
export function parseOptionalCoordinatePair(
  latitudeText: string,
  longitudeText: string,
): CoordinateParseResult {
  const latRaw = latitudeText.trim();
  const lngRaw = longitudeText.trim();

  if (!latRaw && !lngRaw) {
    return { ok: true, latitude: null, longitude: null, clearCoordinates: true };
  }

  if (!latRaw || !lngRaw) {
    return { ok: false, error: "pair_incomplete" };
  }

  const latitude = parseCoordinateInput(latRaw);
  const longitude = parseCoordinateInput(lngRaw);

  if (latitude == null || Number.isNaN(latitude) || !isValidLatitude(latitude)) {
    return { ok: false, error: "invalid_latitude" };
  }
  if (longitude == null || Number.isNaN(longitude) || !isValidLongitude(longitude)) {
    return { ok: false, error: "invalid_longitude" };
  }

  return { ok: true, latitude, longitude, clearCoordinates: false };
}

/** True when a map tile / embed provider URL is configured for this build. */
export function isMapProviderConfigured(
  env: Record<string, string | undefined> = import.meta.env as Record<string, string | undefined>,
): boolean {
  const tile = env.VITE_MAP_TILES_URL?.trim();
  const embed = env.VITE_MAP_EMBED_URL?.trim();
  return Boolean(tile || embed);
}

export function formatCoordinate(value: number | null | undefined): string {
  if (value == null || !Number.isFinite(value)) {
    return "";
  }
  return String(value);
}

/** Display helper (~6 decimal places). Does not reduce persisted precision. */
export function formatCoordinateDisplay(value: number | null | undefined): string {
  if (value == null || !Number.isFinite(value)) {
    return "";
  }
  return value.toFixed(6);
}

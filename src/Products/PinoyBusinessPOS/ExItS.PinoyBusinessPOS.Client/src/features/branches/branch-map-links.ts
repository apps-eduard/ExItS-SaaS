import { isValidCoordinatePair } from "@/features/branches/branch-coordinates";

export function googleMapsUrl(latitude: number, longitude: number): string {
  return `https://www.google.com/maps?q=${encodeURIComponent(`${latitude},${longitude}`)}`;
}

export function openStreetMapUrl(latitude: number, longitude: number): string {
  return `https://www.openstreetmap.org/?mlat=${encodeURIComponent(String(latitude))}&mlon=${encodeURIComponent(String(longitude))}#map=17/${encodeURIComponent(String(latitude))}/${encodeURIComponent(String(longitude))}`;
}

export function externalMapLinks(
  latitude: number | null | undefined,
  longitude: number | null | undefined,
): { google: string; osm: string } | null {
  if (!isValidCoordinatePair(latitude, longitude)) {
    return null;
  }
  return {
    google: googleMapsUrl(latitude!, longitude!),
    osm: openStreetMapUrl(latitude!, longitude!),
  };
}

export type GpsAssistResult =
  | { ok: true; latitude: number; longitude: number }
  | { ok: false; error: "unsupported" | "denied" | "unavailable" | "timeout" };

/**
 * One-shot browser geolocation assist. Never starts continuous watch.
 * No Capacitor — browser navigator.geolocation only.
 */
export function requestGpsAssistOnce(
  geolocation: Geolocation | undefined = typeof navigator !== "undefined"
    ? navigator.geolocation
    : undefined,
): Promise<GpsAssistResult> {
  if (!geolocation?.getCurrentPosition) {
    return Promise.resolve({ ok: false, error: "unsupported" });
  }

  return new Promise((resolve) => {
    geolocation.getCurrentPosition(
      (position) => {
        resolve({
          ok: true,
          latitude: position.coords.latitude,
          longitude: position.coords.longitude,
        });
      },
      (error) => {
        if (error.code === error.PERMISSION_DENIED) {
          resolve({ ok: false, error: "denied" });
          return;
        }
        if (error.code === error.TIMEOUT) {
          resolve({ ok: false, error: "timeout" });
          return;
        }
        resolve({ ok: false, error: "unavailable" });
      },
      {
        enableHighAccuracy: true,
        maximumAge: 0,
        timeout: 15_000,
      },
    );
  });
}

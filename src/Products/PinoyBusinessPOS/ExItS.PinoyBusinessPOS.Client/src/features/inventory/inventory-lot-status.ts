/** Pure helpers for expiration-aware inventory lot UI labels. */

export type LotExpiryLabel =
  | { kind: "expired" }
  | { kind: "expiresToday" }
  | { kind: "expiresInDays"; days: number }
  | { kind: "ok" }
  | { kind: "unknown"; status: string };

/** Parse `yyyy-MM-dd` (or ISO date prefix) as a UTC calendar day. */
export function parseBusinessDate(value: string): Date | null {
  const match = /^(\d{4})-(\d{2})-(\d{2})/.exec(value.trim());
  if (!match) {
    return null;
  }
  const year = Number(match[1]);
  const month = Number(match[2]);
  const day = Number(match[3]);
  if (!year || !month || !day) {
    return null;
  }
  return new Date(Date.UTC(year, month - 1, day));
}

export function calendarDaysBetween(from: Date, to: Date): number {
  const msPerDay = 24 * 60 * 60 * 1000;
  const fromUtc = Date.UTC(from.getUTCFullYear(), from.getUTCMonth(), from.getUTCDate());
  const toUtc = Date.UTC(to.getUTCFullYear(), to.getUTCMonth(), to.getUTCDate());
  return Math.round((toUtc - fromUtc) / msPerDay);
}

export function daysUntilExpiration(
  expirationDate: string,
  today: Date = new Date(),
): number | null {
  const expiry = parseBusinessDate(expirationDate);
  if (!expiry) {
    return null;
  }
  return calendarDaysBetween(today, expiry);
}

/**
 * Friendly lot status from API `expiryStatus` (+ days for near-expiry wording).
 * API codes: Expired | ExpiresToday | NearExpiry | Ok
 */
export function resolveLotExpiryLabel(
  expiryStatus: string,
  expirationDate: string,
  today: Date = new Date(),
): LotExpiryLabel {
  const status = expiryStatus.trim();
  if (status.localeCompare("Expired", undefined, { sensitivity: "accent" }) === 0) {
    return { kind: "expired" };
  }
  if (status.localeCompare("ExpiresToday", undefined, { sensitivity: "accent" }) === 0) {
    return { kind: "expiresToday" };
  }
  if (status.localeCompare("NearExpiry", undefined, { sensitivity: "accent" }) === 0) {
    const days = daysUntilExpiration(expirationDate, today);
    if (days != null && days > 0) {
      return { kind: "expiresInDays", days };
    }
    if (days === 0) {
      return { kind: "expiresToday" };
    }
    return { kind: "expiresInDays", days: Math.max(days ?? 1, 1) };
  }
  if (status.localeCompare("Ok", undefined, { sensitivity: "accent" }) === 0) {
    return { kind: "ok" };
  }
  return { kind: "unknown", status: status || "Unknown" };
}

export function requiresOpeningExpirationDate(
  tracksExpiration: boolean,
  openingQuantity: number | null | undefined,
): boolean {
  if (!tracksExpiration) {
    return false;
  }
  return openingQuantity != null && !Number.isNaN(openingQuantity) && openingQuantity > 0;
}

export const EXPIRY_WINDOWS = ["Expired", "Days7", "Days14", "Days30"] as const;
export type ExpiryWindowCode = (typeof EXPIRY_WINDOWS)[number];

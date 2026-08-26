/**
 * DEVELOPMENT-ONLY Offline PIN storage for insecure HTTP (Tailscale, etc.).
 *
 * This is intentionally NOT encrypted and is NOT compatible with the secure
 * PBKDF2 verifier / wrapped-DEK stores. Production must never read or write here.
 */

import {
  isValidOfflinePinFormat,
  OFFLINE_PIN_LOCKOUT_MINUTES,
  OFFLINE_PIN_MAX_FAILED_ATTEMPTS,
  type OfflinePinVerifyResult,
} from "@/offline/offline-pin";
import { isInsecureOfflinePinFallbackAllowed } from "@/offline/insecure-offline-pin-gate";

export const INSECURE_OFFLINE_PIN_STORE_KEY = "exits.pos-client.offline-pin-insecure-dev.v1";
export const INSECURE_OFFLINE_DEK_MARKER_STORE_KEY =
  "exits.pos-client.wrapped-dek-insecure-dev.v1";

export const INSECURE_OFFLINE_PIN_MODE = "dev-insecure-plaintext" as const;
export const INSECURE_OFFLINE_DEK_MODE = "dev-insecure-no-crypto" as const;

type InsecurePinRecord = {
  mode: typeof INSECURE_OFFLINE_PIN_MODE;
  /** Plaintext PIN — development convenience only; never treat as encrypted. */
  pin: string;
  userId: string;
  failedAttempts: number;
  lockedUntilUtc: string | null;
};

type InsecurePinStoreDocument = {
  version: 1;
  records: Record<string, InsecurePinRecord>;
};

type InsecureDekMarker = {
  mode: typeof INSECURE_OFFLINE_DEK_MODE;
  configured: true;
  userId: string;
};

type InsecureDekStoreDocument = {
  version: 1;
  records: Record<string, InsecureDekMarker>;
};

/** In-memory unlock flag for insecure DEV sessions (no CryptoKey without subtle). */
const unlockedInsecureUsers = new Set<string>();

function canUseLocalStorage(): boolean {
  if (typeof window === "undefined" || typeof window.localStorage === "undefined") {
    return false;
  }
  try {
    const probe = `${INSECURE_OFFLINE_PIN_STORE_KEY}.probe`;
    window.localStorage.setItem(probe, "1");
    window.localStorage.removeItem(probe);
    return true;
  } catch {
    return false;
  }
}

function readPinStore(): InsecurePinStoreDocument {
  if (!canUseLocalStorage()) {
    return { version: 1, records: {} };
  }
  try {
    const raw = window.localStorage.getItem(INSECURE_OFFLINE_PIN_STORE_KEY);
    if (!raw) {
      return { version: 1, records: {} };
    }
    const parsed = JSON.parse(raw) as Partial<InsecurePinStoreDocument>;
    if (parsed?.version !== 1 || typeof parsed.records !== "object" || parsed.records === null) {
      return { version: 1, records: {} };
    }
    return { version: 1, records: parsed.records as Record<string, InsecurePinRecord> };
  } catch {
    return { version: 1, records: {} };
  }
}

function writePinStore(document: InsecurePinStoreDocument): boolean {
  if (!canUseLocalStorage()) {
    return false;
  }
  try {
    window.localStorage.setItem(INSECURE_OFFLINE_PIN_STORE_KEY, JSON.stringify(document));
    return true;
  } catch {
    return false;
  }
}

function readDekStore(): InsecureDekStoreDocument {
  if (!canUseLocalStorage()) {
    return { version: 1, records: {} };
  }
  try {
    const raw = window.localStorage.getItem(INSECURE_OFFLINE_DEK_MARKER_STORE_KEY);
    if (!raw) {
      return { version: 1, records: {} };
    }
    const parsed = JSON.parse(raw) as Partial<InsecureDekStoreDocument>;
    if (parsed?.version !== 1 || typeof parsed.records !== "object" || parsed.records === null) {
      return { version: 1, records: {} };
    }
    return { version: 1, records: parsed.records as Record<string, InsecureDekMarker> };
  } catch {
    return { version: 1, records: {} };
  }
}

function writeDekStore(document: InsecureDekStoreDocument): boolean {
  if (!canUseLocalStorage()) {
    return false;
  }
  try {
    window.localStorage.setItem(INSECURE_OFFLINE_DEK_MARKER_STORE_KEY, JSON.stringify(document));
    return true;
  } catch {
    return false;
  }
}

function assertDevFallbackActive(): void {
  if (!isInsecureOfflinePinFallbackAllowed()) {
    throw new Error("Insecure offline PIN fallback is not allowed in this build/context.");
  }
}

export function hasInsecureOfflinePinConfigured(userId: string): boolean {
  if (!isInsecureOfflinePinFallbackAllowed()) {
    return false;
  }
  const record = readPinStore().records[userId];
  return record?.mode === INSECURE_OFFLINE_PIN_MODE && typeof record.pin === "string";
}

export function hasInsecureOfflineDekMarker(userId: string): boolean {
  if (!isInsecureOfflinePinFallbackAllowed()) {
    return false;
  }
  const marker = readDekStore().records[userId];
  return marker?.mode === INSECURE_OFFLINE_DEK_MODE && marker.configured === true;
}

export function isInsecureOfflinePinAndDekConfigured(userId: string): boolean {
  return hasInsecureOfflinePinConfigured(userId) && hasInsecureOfflineDekMarker(userId);
}

export function clearInsecureOfflinePinForUser(userId: string): void {
  const pins = readPinStore();
  if (pins.records[userId]) {
    delete pins.records[userId];
    writePinStore(pins);
  }
  const deks = readDekStore();
  if (deks.records[userId]) {
    delete deks.records[userId];
    writeDekStore(deks);
  }
  unlockedInsecureUsers.delete(userId);
}

export function clearAllInsecureOfflinePinDevState(): void {
  if (!canUseLocalStorage()) {
    unlockedInsecureUsers.clear();
    return;
  }
  try {
    window.localStorage.removeItem(INSECURE_OFFLINE_PIN_STORE_KEY);
    window.localStorage.removeItem(INSECURE_OFFLINE_DEK_MARKER_STORE_KEY);
  } catch {
    // ignore
  }
  unlockedInsecureUsers.clear();
}

export function enrollInsecureOfflinePinAndDek(userId: string, pin: string): boolean {
  assertDevFallbackActive();
  if (!isValidOfflinePinFormat(pin)) {
    return false;
  }
  const pins = readPinStore();
  pins.records[userId] = {
    mode: INSECURE_OFFLINE_PIN_MODE,
    pin,
    userId,
    failedAttempts: 0,
    lockedUntilUtc: null,
  };
  if (!writePinStore(pins)) {
    return false;
  }
  const deks = readDekStore();
  deks.records[userId] = {
    mode: INSECURE_OFFLINE_DEK_MODE,
    configured: true,
    userId,
  };
  if (!writeDekStore(deks)) {
    return false;
  }
  unlockedInsecureUsers.add(userId);
  return true;
}

function isLocked(record: InsecurePinRecord, now: Date): boolean {
  if (!record.lockedUntilUtc) {
    return false;
  }
  const until = Date.parse(record.lockedUntilUtc);
  return Number.isFinite(until) && now.getTime() < until;
}

export function verifyInsecureOfflinePin(
  userId: string,
  pin: string,
  now: Date = new Date(),
): OfflinePinVerifyResult {
  if (!isInsecureOfflinePinFallbackAllowed()) {
    return { ok: false, reason: "not_configured" };
  }
  if (!isValidOfflinePinFormat(pin)) {
    return { ok: false, reason: "invalid_format" };
  }
  const pins = readPinStore();
  const record = pins.records[userId];
  if (!record || record.mode !== INSECURE_OFFLINE_PIN_MODE) {
    return { ok: false, reason: "not_configured" };
  }
  if (isLocked(record, now)) {
    return { ok: false, reason: "locked", lockedUntilUtc: record.lockedUntilUtc ?? undefined };
  }
  if (record.pin !== pin) {
    const failedAttempts = record.failedAttempts + 1;
    const next: InsecurePinRecord = {
      ...record,
      failedAttempts,
      lockedUntilUtc:
        failedAttempts >= OFFLINE_PIN_MAX_FAILED_ATTEMPTS
          ? new Date(now.getTime() + OFFLINE_PIN_LOCKOUT_MINUTES * 60 * 1000).toISOString()
          : record.lockedUntilUtc,
    };
    pins.records[userId] = next;
    writePinStore(pins);
    if (failedAttempts >= OFFLINE_PIN_MAX_FAILED_ATTEMPTS) {
      return { ok: false, reason: "locked", lockedUntilUtc: next.lockedUntilUtc ?? undefined };
    }
    return { ok: false, reason: "wrong_pin" };
  }
  if (record.failedAttempts > 0 || record.lockedUntilUtc) {
    pins.records[userId] = {
      ...record,
      failedAttempts: 0,
      lockedUntilUtc: null,
    };
    writePinStore(pins);
  }
  return { ok: true };
}

export function unlockInsecureOfflineSession(userId: string): void {
  assertDevFallbackActive();
  unlockedInsecureUsers.add(userId);
}

export function isInsecureOfflineSessionUnlocked(userId?: string | null): boolean {
  if (!isInsecureOfflinePinFallbackAllowed()) {
    return false;
  }
  if (!userId) {
    return unlockedInsecureUsers.size > 0;
  }
  return unlockedInsecureUsers.has(userId);
}

export function clearInsecureOfflineSessionUnlock(userId?: string | null): void {
  if (userId) {
    unlockedInsecureUsers.delete(userId);
    return;
  }
  unlockedInsecureUsers.clear();
}

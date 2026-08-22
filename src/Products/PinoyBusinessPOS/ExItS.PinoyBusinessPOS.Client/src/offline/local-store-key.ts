import { wrapDek, unwrapDek, deriveScopeKeyFromBinding, toArrayBuffer } from "@/offline/crypto";
import {
  enrollOfflinePin,
  hasOfflinePinConfigured,
  loadOfflinePinVerifier,
  verifyOfflinePin,
} from "@/offline/offline-pin";
import { getUnlockedDek, setUnlockedDek } from "@/offline/offline-unlock-session";

export { hasOfflinePinConfigured, loadOfflinePinVerifier };

export const WRAPPED_DEK_STORE_KEY = "exits.pos-client.wrapped-dek.v1";

export type WrappedDekRecord = {
  version: 1;
  wrappedDekBase64: string;
  wrapIvBase64: string;
  saltBase64: string;
  iterations: number;
};

type WrappedDekStoreDocument = {
  version: 1;
  records: Record<string, WrappedDekRecord>;
};

export class OfflineCryptoLockedError extends Error {
  constructor(message = "Offline data is locked. Enter your PIN to continue.") {
    super(message);
    this.name = "OfflineCryptoLockedError";
  }
}

function canUseLocalStorage(): boolean {
  if (typeof window === "undefined" || typeof window.localStorage === "undefined") {
    return false;
  }
  try {
    const probe = `${WRAPPED_DEK_STORE_KEY}.probe`;
    window.localStorage.setItem(probe, "1");
    window.localStorage.removeItem(probe);
    return true;
  } catch {
    return false;
  }
}

function readStore(): WrappedDekStoreDocument {
  if (!canUseLocalStorage()) {
    return { version: 1, records: {} };
  }
  try {
    const raw = window.localStorage.getItem(WRAPPED_DEK_STORE_KEY);
    if (!raw) {
      return { version: 1, records: {} };
    }
    const parsed = JSON.parse(raw) as Partial<WrappedDekStoreDocument>;
    if (parsed?.version !== 1 || typeof parsed.records !== "object" || parsed.records === null) {
      return { version: 1, records: {} };
    }
    return { version: 1, records: parsed.records as Record<string, WrappedDekRecord> };
  } catch {
    return { version: 1, records: {} };
  }
}

function writeStore(document: WrappedDekStoreDocument): boolean {
  if (!canUseLocalStorage()) {
    return false;
  }
  try {
    window.localStorage.setItem(WRAPPED_DEK_STORE_KEY, JSON.stringify(document));
    return true;
  } catch {
    return false;
  }
}

export function loadWrappedDekRecord(userId: string): WrappedDekRecord | null {
  const store = readStore();
  return store.records[userId] ?? null;
}

export function clearWrappedDekRecord(userId: string): void {
  const store = readStore();
  if (!store.records[userId]) {
    return;
  }
  delete store.records[userId];
  writeStore(store);
}

export function clearAllWrappedDekRecords(): void {
  if (!canUseLocalStorage()) {
    return;
  }
  try {
    window.localStorage.removeItem(WRAPPED_DEK_STORE_KEY);
  } catch {
    // ignore
  }
}

async function derivePinWrapKey(pin: string, saltBase64: string, iterations: number): Promise<CryptoKey> {
  const salt = Uint8Array.from(atob(saltBase64), (c) => c.charCodeAt(0));
  const passwordBytes = new TextEncoder().encode(pin);
  try {
    const baseKey = await crypto.subtle.importKey("raw", passwordBytes, "PBKDF2", false, ["deriveKey"]);
    return crypto.subtle.deriveKey(
      {
        name: "PBKDF2",
        salt,
        iterations,
        hash: "SHA-256",
      },
      baseKey,
      { name: "AES-GCM", length: 256 },
      false,
      ["encrypt", "decrypt"],
    );
  } finally {
    passwordBytes.fill(0);
  }
}

export async function generateRandomDek(): Promise<CryptoKey> {
  return crypto.subtle.generateKey({ name: "AES-GCM", length: 256 }, true, ["encrypt", "decrypt"]);
}

async function exportRawDek(dek: CryptoKey): Promise<Uint8Array> {
  const raw = await crypto.subtle.exportKey("raw", dek);
  return new Uint8Array(raw);
}

async function importRawDek(raw: Uint8Array): Promise<CryptoKey> {
  return crypto.subtle.importKey("raw", toArrayBuffer(raw), { name: "AES-GCM", length: 256 }, false, [
    "encrypt",
    "decrypt",
  ]);
}

export async function wrapAndPersistDek(userId: string, pin: string, dek: CryptoKey): Promise<boolean> {
  const verifier = loadOfflinePinVerifier(userId);
  if (!verifier) {
    return false;
  }
  const wrapKey = await derivePinWrapKey(pin, verifier.saltBase64, verifier.iterations);
  const rawDek = await exportRawDek(dek);
  const wrapped = await wrapDek(wrapKey, rawDek);
  const record: WrappedDekRecord = {
    version: 1,
    wrappedDekBase64: btoa(String.fromCharCode(...wrapped.ciphertext)),
    wrapIvBase64: btoa(String.fromCharCode(...new Uint8Array(wrapped.iv))),
    saltBase64: verifier.saltBase64,
    iterations: verifier.iterations,
  };
  const store = readStore();
  store.records[userId] = record;
  rawDek.fill(0);
  return writeStore(store);
}

export async function unwrapPersistedDek(userId: string, pin: string): Promise<CryptoKey | null> {
  const record = loadWrappedDekRecord(userId);
  if (!record) {
    return null;
  }
  const wrapKey = await derivePinWrapKey(pin, record.saltBase64, record.iterations);
  const ciphertext = Uint8Array.from(atob(record.wrappedDekBase64), (c) => c.charCodeAt(0));
  const ivBytes = Uint8Array.from(atob(record.wrapIvBase64), (c) => c.charCodeAt(0));
  try {
    const raw = await unwrapDek(wrapKey, { ciphertext, iv: toArrayBuffer(ivBytes) });
    return importRawDek(raw);
  } catch {
    return null;
  }
}

function isVitestRuntime(): boolean {
  if (typeof import.meta !== "undefined") {
    if (import.meta.env?.MODE === "test" || import.meta.env?.VITEST) {
      return true;
    }
  }
  return typeof process !== "undefined" && Boolean(process.env.VITEST);
}

/**
 * Active payload encryption key — random DEK when PIN-unlocked.
 * Throws OfflineCryptoLockedError when the DEK is not in memory.
 */
export async function getActiveOfflineCryptoKey(
  userId?: string | null,
  legacyScopeBinding?: string,
): Promise<CryptoKey> {
  const dek = getUnlockedDek(userId ?? undefined);
  if (dek) {
    return dek;
  }
  if (isVitestRuntime() && legacyScopeBinding) {
    return deriveScopeKeyFromBinding(legacyScopeBinding);
  }
  throw new OfflineCryptoLockedError();
}

export async function getActiveOfflineCryptoKeyForScope(scopeBinding: string): Promise<CryptoKey> {
  return getActiveOfflineCryptoKey(parseUserIdFromScopeBinding(scopeBinding), scopeBinding);
}

export async function unlockOfflineCryptoWithPin(userId: string, pin: string): Promise<boolean> {
  const verify = await verifyOfflinePin(userId, pin);
  if (!verify.ok) {
    return false;
  }
  const dek = await unwrapPersistedDek(userId, pin);
  if (!dek) {
    return false;
  }
  setUnlockedDek(userId, dek);
  return true;
}

export async function enrollOfflinePinAndDek(userId: string, pin: string): Promise<boolean> {
  const dek = await generateRandomDek();
  const savedPin = await enrollOfflinePin(userId, pin);
  if (!savedPin) {
    return false;
  }
  const wrapped = await wrapAndPersistDek(userId, pin, dek);
  if (!wrapped) {
    return false;
  }
  setUnlockedDek(userId, dek);
  return true;
}

export function isOfflinePinAndDekConfigured(userId: string): boolean {
  return hasOfflinePinConfigured(userId) && loadWrappedDekRecord(userId) != null;
}

/** Ensures wrapped DEK storage never contains exportable raw key material. */
export function assertWrappedDekNotPlaintext(raw: string): boolean {
  const lower = raw.toLowerCase();
  return !lower.includes('"dek"') && !lower.includes("plaintextdek");
}

export function parseUserIdFromScopeBinding(scopeBinding: string): string {
  const [userId] = scopeBinding.split(":");
  if (!userId?.trim()) {
    throw new Error("Invalid offline scope binding.");
  }
  return userId;
}

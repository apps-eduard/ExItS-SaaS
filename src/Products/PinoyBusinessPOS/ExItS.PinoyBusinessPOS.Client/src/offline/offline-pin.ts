/**
 * PBKDF2-SHA256 offline PIN verifier — matches MAUI OfflinePinHasher.
 * Never stores or logs the raw PIN.
 */

export const OFFLINE_PIN_ALGORITHM = "PBKDF2-SHA256";
export const OFFLINE_PIN_SALT_SIZE_BYTES = 16;
export const OFFLINE_PIN_HASH_SIZE_BYTES = 32;
export const OFFLINE_PIN_MIN_LENGTH = 6;
export const OFFLINE_PIN_HASH_ITERATIONS = 100_000;
export const OFFLINE_PIN_MAX_FAILED_ATTEMPTS = 5;
export const OFFLINE_PIN_LOCKOUT_MINUTES = 15;

export const OFFLINE_PIN_VERIFIER_STORE_KEY = "exits.pos-client.offline-pin-verifier.v1";

export type OfflinePinVerifier = {
  algorithm: typeof OFFLINE_PIN_ALGORITHM;
  iterations: number;
  saltBase64: string;
  hashBase64: string;
  failedAttempts: number;
  lockedUntilUtc: string | null;
  userId: string;
};

type PinVerifierStoreDocument = {
  version: 1;
  verifiers: Record<string, OfflinePinVerifier>;
};

function canUseLocalStorage(): boolean {
  if (typeof window === "undefined" || typeof window.localStorage === "undefined") {
    return false;
  }
  try {
    const probe = `${OFFLINE_PIN_VERIFIER_STORE_KEY}.probe`;
    window.localStorage.setItem(probe, "1");
    window.localStorage.removeItem(probe);
    return true;
  } catch {
    return false;
  }
}

function readStore(): PinVerifierStoreDocument {
  if (!canUseLocalStorage()) {
    return { version: 1, verifiers: {} };
  }
  try {
    const raw = window.localStorage.getItem(OFFLINE_PIN_VERIFIER_STORE_KEY);
    if (!raw) {
      return { version: 1, verifiers: {} };
    }
    const parsed = JSON.parse(raw) as Partial<PinVerifierStoreDocument>;
    if (parsed?.version !== 1 || typeof parsed.verifiers !== "object" || parsed.verifiers === null) {
      return { version: 1, verifiers: {} };
    }
    return { version: 1, verifiers: parsed.verifiers as Record<string, OfflinePinVerifier> };
  } catch {
    return { version: 1, verifiers: {} };
  }
}

function writeStore(document: PinVerifierStoreDocument): boolean {
  if (!canUseLocalStorage()) {
    return false;
  }
  try {
    window.localStorage.setItem(OFFLINE_PIN_VERIFIER_STORE_KEY, JSON.stringify(document));
    return true;
  } catch {
    return false;
  }
}

export function isValidOfflinePinFormat(pin: string | null | undefined): boolean {
  if (!pin || pin.length < OFFLINE_PIN_MIN_LENGTH) {
    return false;
  }
  for (const ch of pin) {
    if (ch < "0" || ch > "9") {
      return false;
    }
  }
  return true;
}

function toArrayBuffer(view: Uint8Array): ArrayBuffer {
  return view.buffer.slice(view.byteOffset, view.byteOffset + view.byteLength) as ArrayBuffer;
}

async function pbkdf2Sha256(pin: string, salt: Uint8Array, iterations: number): Promise<Uint8Array> {
  const passwordBytes = new TextEncoder().encode(pin);
  try {
    const baseKey = await crypto.subtle.importKey("raw", toArrayBuffer(passwordBytes), "PBKDF2", false, [
      "deriveBits",
    ]);
    const bits = await crypto.subtle.deriveBits(
      {
        name: "PBKDF2",
        salt: toArrayBuffer(salt),
        iterations,
        hash: "SHA-256",
      },
      baseKey,
      OFFLINE_PIN_HASH_SIZE_BYTES * 8,
    );
    return new Uint8Array(bits);
  } finally {
    passwordBytes.fill(0);
  }
}

function fixedTimeEqual(left: Uint8Array, right: Uint8Array): boolean {
  if (left.length !== right.length) {
    return false;
  }
  let diff = 0;
  for (let i = 0; i < left.length; i += 1) {
    diff |= left[i]! ^ right[i]!;
  }
  return diff === 0;
}

export function loadOfflinePinVerifier(userId: string): OfflinePinVerifier | null {
  const store = readStore();
  return store.verifiers[userId] ?? null;
}

export function hasOfflinePinConfigured(userId: string): boolean {
  return loadOfflinePinVerifier(userId) != null;
}

export function clearOfflinePinVerifier(userId: string): void {
  const store = readStore();
  if (!store.verifiers[userId]) {
    return;
  }
  delete store.verifiers[userId];
  writeStore(store);
}

export function clearAllOfflinePinVerifiers(): void {
  if (!canUseLocalStorage()) {
    return;
  }
  try {
    window.localStorage.removeItem(OFFLINE_PIN_VERIFIER_STORE_KEY);
  } catch {
    // ignore
  }
}

export async function createOfflinePinVerifier(userId: string, pin: string): Promise<OfflinePinVerifier> {
  if (!isValidOfflinePinFormat(pin)) {
    throw new Error("Invalid offline PIN format.");
  }
  const salt = crypto.getRandomValues(new Uint8Array(OFFLINE_PIN_SALT_SIZE_BYTES));
  const hash = await pbkdf2Sha256(pin, salt, OFFLINE_PIN_HASH_ITERATIONS);
  return {
    algorithm: OFFLINE_PIN_ALGORITHM,
    iterations: OFFLINE_PIN_HASH_ITERATIONS,
    saltBase64: btoa(String.fromCharCode(...salt)),
    hashBase64: btoa(String.fromCharCode(...hash)),
    failedAttempts: 0,
    lockedUntilUtc: null,
    userId,
  };
}

export async function saveOfflinePinVerifier(verifier: OfflinePinVerifier): Promise<boolean> {
  const store = readStore();
  store.verifiers[verifier.userId] = verifier;
  return writeStore(store);
}

function isLocked(verifier: OfflinePinVerifier, now: Date = new Date()): boolean {
  if (!verifier.lockedUntilUtc) {
    return false;
  }
  const until = Date.parse(verifier.lockedUntilUtc);
  return Number.isFinite(until) && now.getTime() < until;
}

export type OfflinePinVerifyResult =
  | { ok: true }
  | { ok: false; reason: "invalid_format" | "not_configured" | "locked" | "wrong_pin"; lockedUntilUtc?: string };

export async function verifyOfflinePin(
  userId: string,
  pin: string,
  now: Date = new Date(),
): Promise<OfflinePinVerifyResult> {
  if (!isValidOfflinePinFormat(pin)) {
    return { ok: false, reason: "invalid_format" };
  }

  const verifier = loadOfflinePinVerifier(userId);
  if (!verifier) {
    return { ok: false, reason: "not_configured" };
  }

  if (isLocked(verifier, now)) {
    return { ok: false, reason: "locked", lockedUntilUtc: verifier.lockedUntilUtc ?? undefined };
  }

  let salt: Uint8Array;
  let expected: Uint8Array;
  try {
    salt = Uint8Array.from(atob(verifier.saltBase64), (c) => c.charCodeAt(0));
    expected = Uint8Array.from(atob(verifier.hashBase64), (c) => c.charCodeAt(0));
  } catch {
    return { ok: false, reason: "not_configured" };
  }

  const actual = await pbkdf2Sha256(pin, salt, verifier.iterations);
  if (!fixedTimeEqual(actual, expected)) {
    const failedAttempts = verifier.failedAttempts + 1;
    const next: OfflinePinVerifier = {
      ...verifier,
      failedAttempts,
      lockedUntilUtc:
        failedAttempts >= OFFLINE_PIN_MAX_FAILED_ATTEMPTS
          ? new Date(now.getTime() + OFFLINE_PIN_LOCKOUT_MINUTES * 60 * 1000).toISOString()
          : verifier.lockedUntilUtc,
    };
    await saveOfflinePinVerifier(next);
    if (failedAttempts >= OFFLINE_PIN_MAX_FAILED_ATTEMPTS) {
      return { ok: false, reason: "locked", lockedUntilUtc: next.lockedUntilUtc ?? undefined };
    }
    return { ok: false, reason: "wrong_pin" };
  }

  if (verifier.failedAttempts > 0 || verifier.lockedUntilUtc) {
    await saveOfflinePinVerifier({
      ...verifier,
      failedAttempts: 0,
      lockedUntilUtc: null,
    });
  }

  return { ok: true };
}

export async function enrollOfflinePin(userId: string, pin: string): Promise<boolean> {
  const verifier = await createOfflinePinVerifier(userId, pin);
  return saveOfflinePinVerifier(verifier);
}

/** Ensures the stored verifier document never contains a plaintext PIN. */
export function assertPinVerifierHasNoPlaintextPin(raw: string): boolean {
  return !/\b\d{6,}\b/.test(raw);
}

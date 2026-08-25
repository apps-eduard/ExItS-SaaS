/**
 * Durable browser/PWA POS installation identity (RMAP-10b).
 * Survives logout, user switch, and org switch. Never invent an ephemeral id for register.
 */

export const INSTALLATION_DEVICE_ID_STORAGE_KEY = "exits.pos-client.installation-device-id.v1";

/** RFC 4122 UUID (versions 1–8; variant 8/9/a/b). */
const UUID_PATTERN = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-8][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;

export type DurableInstallationIdentityFailureReason =
  "storage_unavailable" | "crypto_unavailable" | "ssr_unavailable";

export type DurableInstallationIdentityResult =
  | { ok: true; installationDeviceId: string; created: boolean }
  | { ok: false; reason: DurableInstallationIdentityFailureReason };

export function isValidInstallationDeviceId(value: string | null | undefined): boolean {
  if (!value) {
    return false;
  }
  return UUID_PATTERN.test(value.trim());
}

function canUseLocalStorage(): boolean {
  if (typeof window === "undefined" || typeof window.localStorage === "undefined") {
    return false;
  }
  try {
    const probeKey = `${INSTALLATION_DEVICE_ID_STORAGE_KEY}.probe`;
    window.localStorage.setItem(probeKey, "1");
    window.localStorage.removeItem(probeKey);
    return true;
  } catch {
    return false;
  }
}

function canCreateUuid(): boolean {
  return typeof crypto !== "undefined" && typeof crypto.randomUUID === "function";
}

/**
 * Load or create the durable installation device id.
 * Fail-closed when storage or crypto is unavailable — callers must not invent a substitute.
 */
export function getDurableInstallationDeviceId(): DurableInstallationIdentityResult {
  if (typeof window === "undefined") {
    return { ok: false, reason: "ssr_unavailable" };
  }
  if (!canUseLocalStorage()) {
    return { ok: false, reason: "storage_unavailable" };
  }
  if (!canCreateUuid()) {
    return { ok: false, reason: "crypto_unavailable" };
  }

  let stored: string | null = null;
  try {
    stored = window.localStorage.getItem(INSTALLATION_DEVICE_ID_STORAGE_KEY);
  } catch {
    return { ok: false, reason: "storage_unavailable" };
  }

  if (isValidInstallationDeviceId(stored)) {
    return { ok: true, installationDeviceId: stored!.trim(), created: false };
  }

  const next = crypto.randomUUID();
  try {
    window.localStorage.setItem(INSTALLATION_DEVICE_ID_STORAGE_KEY, next);
  } catch {
    return { ok: false, reason: "storage_unavailable" };
  }

  return { ok: true, installationDeviceId: next, created: true };
}

/**
 * Read current durable id without creating one. Returns null when missing, invalid, or unavailable.
 * Does not clear or invent values.
 */
export function peekDurableInstallationDeviceId(): string | null {
  if (!canUseLocalStorage()) {
    return null;
  }
  try {
    const stored = window.localStorage.getItem(INSTALLATION_DEVICE_ID_STORAGE_KEY);
    return isValidInstallationDeviceId(stored) ? stored!.trim() : null;
  } catch {
    return null;
  }
}

/**
 * Intentionally empty — logout / user / org switch must never clear installation identity.
 */
export function clearDurableInstallationDeviceIdOnLogout(): void {
  // no-op by design (RMAP-10b)
}

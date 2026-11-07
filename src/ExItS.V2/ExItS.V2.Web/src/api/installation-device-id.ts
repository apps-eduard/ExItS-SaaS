const STORAGE_KEY = "exits.pos-client.installation-device-id.v1";
const UUID_PATTERN =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[1-8][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;

/** Durable POS installation id — fail closed; never invent an ephemeral substitute. */
export function getDurableInstallationDeviceId():
  | { ok: true; installationDeviceId: string }
  | { ok: false } {
  if (typeof window === "undefined" || typeof crypto?.randomUUID !== "function") {
    return { ok: false };
  }
  try {
    const stored = window.localStorage.getItem(STORAGE_KEY);
    if (stored && UUID_PATTERN.test(stored.trim())) {
      return { ok: true, installationDeviceId: stored.trim() };
    }
    const next = crypto.randomUUID();
    window.localStorage.setItem(STORAGE_KEY, next);
    return { ok: true, installationDeviceId: next };
  } catch {
    return { ok: false };
  }
}

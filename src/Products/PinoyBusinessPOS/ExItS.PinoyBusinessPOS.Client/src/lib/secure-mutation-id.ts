/**
 * Secure client mutation identity for POS idempotent POSTs (e.g. sale return ReturnId).
 * Prefer crypto.randomUUID; otherwise RFC4122 UUID v4 via getRandomValues.
 * Fail closed when secure randomness is unavailable — never use a constant GUID.
 */

export type SecureMutationIdResult =
  { ok: true; id: string } | { ok: false; reason: "secure_randomness_unavailable" };

const UUID_V4_PATTERN = /^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;

export function isUuidV4(value: string): boolean {
  return UUID_V4_PATTERN.test(value.trim());
}

function bytesToUuidV4(bytes: Uint8Array): string {
  // RFC 4122 version 4 + variant 10xx
  bytes[6] = (bytes[6]! & 0x0f) | 0x40;
  bytes[8] = (bytes[8]! & 0x3f) | 0x80;
  const hex = Array.from(bytes, (b) => b.toString(16).padStart(2, "0")).join("");
  return `${hex.slice(0, 8)}-${hex.slice(8, 12)}-${hex.slice(12, 16)}-${hex.slice(16, 20)}-${hex.slice(20, 32)}`;
}

/**
 * Create a cryptographically random UUID suitable as a client mutation id.
 */
export function createSecureMutationId(): SecureMutationIdResult {
  if (typeof crypto !== "undefined" && typeof crypto.randomUUID === "function") {
    return { ok: true, id: crypto.randomUUID() };
  }

  if (typeof crypto !== "undefined" && typeof crypto.getRandomValues === "function") {
    const bytes = new Uint8Array(16);
    crypto.getRandomValues(bytes);
    return { ok: true, id: bytesToUuidV4(bytes) };
  }

  return { ok: false, reason: "secure_randomness_unavailable" };
}

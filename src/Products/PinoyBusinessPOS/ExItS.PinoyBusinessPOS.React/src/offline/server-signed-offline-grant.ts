/**
 * Server-signed offline operating grant verification (RMAP-21-FIX02).
 * Canonical format matches C# OfflineOperatingGrantSigning pipe-delimited v1 fields.
 */

import { toArrayBuffer } from "@/offline/crypto";
import {
  DEVELOPMENT_OFFLINE_OPERATING_GRANT_PUBLIC_KEY_PEM,
  resolveOfflineOperatingGrantVerificationPublicKeyPem,
} from "@/offline/offline-grant-verification-key";

/** Documented development verification key — use resolveOfflineOperatingGrantVerificationPublicKeyPem() at runtime. */
export const OFFLINE_OPERATING_GRANT_SIGNING_PUBLIC_KEY_PEM =
  DEVELOPMENT_OFFLINE_OPERATING_GRANT_PUBLIC_KEY_PEM;

export const OFFLINE_OPERATING_GRANT_CANONICAL_VERSION = "v1";
const ABSENT = "-";

export type OfflineGrantScopeKindNumeric = 0 | 1;

export function scopeKindToNumeric(scopeKind: "Organization" | "Personal"): OfflineGrantScopeKindNumeric {
  return scopeKind === "Personal" ? 1 : 0;
}

export function numericToScopeKind(value: number): "Organization" | "Personal" | null {
  if (value === 0) {
    return "Organization";
  }
  if (value === 1) {
    return "Personal";
  }
  return null;
}

export type CanonicalGrantFields = {
  grantId: string;
  schemaVersion: number;
  userId: string;
  scopeKind: OfflineGrantScopeKindNumeric;
  organizationId: string | null;
  organizationDisplayName: string;
  branchId: string | null;
  branchName: string | null;
  installationDeviceId: string;
  posDeviceId: string | null;
  roleCode: string | null;
  displayName: string | null;
  username: string | null;
  issuedAtUtc: string;
  lastOnlineValidatedAtUtc: string;
  expiresAtUtc: string;
};

function toUnixSeconds(isoUtc: string): number {
  const ms = Date.parse(isoUtc);
  if (!Number.isFinite(ms)) {
    return 0;
  }
  return Math.floor(ms / 1000);
}

export function canonicalizeOfflineOperatingGrant(fields: CanonicalGrantFields): string {
  const parts = [
    OFFLINE_OPERATING_GRANT_CANONICAL_VERSION,
    fields.grantId,
    String(fields.schemaVersion),
    fields.userId,
    String(fields.scopeKind),
    fields.organizationId ?? ABSENT,
    fields.organizationDisplayName ?? "",
    fields.branchId ?? ABSENT,
    fields.branchName ?? "",
    fields.installationDeviceId ?? "",
    fields.posDeviceId ?? ABSENT,
    fields.roleCode ?? "",
    fields.displayName ?? "",
    fields.username ?? "",
    String(toUnixSeconds(fields.issuedAtUtc)),
    String(toUnixSeconds(fields.lastOnlineValidatedAtUtc)),
    String(toUnixSeconds(fields.expiresAtUtc)),
  ];
  return parts.join("|");
}

function pemToSpkiDer(pem: string): ArrayBuffer {
  const b64 = pem
    .replace(/-----BEGIN PUBLIC KEY-----/g, "")
    .replace(/-----END PUBLIC KEY-----/g, "")
    .replace(/\s/g, "");
  const bytes = Uint8Array.from(atob(b64), (c) => c.charCodeAt(0));
  return bytes.buffer.slice(bytes.byteOffset, bytes.byteOffset + bytes.byteLength);
}

function hexToBytes(hex: string): Uint8Array {
  const normalized = hex.trim().toLowerCase();
  if (normalized.length % 2 !== 0) {
    throw new Error("Invalid signature hex length.");
  }
  const out = new Uint8Array(normalized.length / 2);
  for (let i = 0; i < out.length; i += 1) {
    out[i] = Number.parseInt(normalized.slice(i * 2, i * 2 + 2), 16);
  }
  return out;
}

let cachedVerifyKey: CryptoKey | null = null;
let cachedVerifyKeyPem: string | null = null;

async function importVerifyPublicKey(publicKeyPem: string): Promise<CryptoKey> {
  if (cachedVerifyKey && cachedVerifyKeyPem === publicKeyPem) {
    return cachedVerifyKey;
  }
  cachedVerifyKeyPem = publicKeyPem;
  cachedVerifyKey = await crypto.subtle.importKey(
    "spki",
    pemToSpkiDer(publicKeyPem),
    { name: "ECDSA", namedCurve: "P-256" },
    false,
    ["verify"],
  );
  return cachedVerifyKey;
}

export async function verifyOfflineOperatingGrantSignature(
  canonical: string,
  signatureHex: string | null | undefined,
  publicKeyPem?: string,
): Promise<boolean> {
  if (!signatureHex?.trim() || !canonical.trim()) {
    return false;
  }

  let signature: Uint8Array;
  try {
    signature = hexToBytes(signatureHex);
  } catch {
    return false;
  }

  let pem: string;
  try {
    pem = publicKeyPem?.trim() || resolveOfflineOperatingGrantVerificationPublicKeyPem();
  } catch {
    return false;
  }

  const key = await importVerifyPublicKey(pem);

  return crypto.subtle.verify(
    { name: "ECDSA", hash: "SHA-256" },
    key,
    toArrayBuffer(signature),
    new TextEncoder().encode(canonical),
  );
}

/** Test-only: sign a canonical payload with a dev private key PEM. */
export async function signOfflineOperatingGrantForTests(
  canonical: string,
  privateKeyPem: string,
): Promise<string> {
  const pemBody = privateKeyPem
    .replace(/-----BEGIN PRIVATE KEY-----/g, "")
    .replace(/-----END PRIVATE KEY-----/g, "")
    .replace(/\s/g, "");
  const der = Uint8Array.from(atob(pemBody), (c) => c.charCodeAt(0));
  const key = await crypto.subtle.importKey(
    "pkcs8",
    der.buffer.slice(der.byteOffset, der.byteOffset + der.byteLength),
    { name: "ECDSA", namedCurve: "P-256" },
    false,
    ["sign"],
  );
  const signature = await crypto.subtle.sign(
    { name: "ECDSA", hash: "SHA-256" },
    key,
    new TextEncoder().encode(canonical),
  );
  return [...new Uint8Array(signature)].map((b) => b.toString(16).padStart(2, "0")).join("");
}

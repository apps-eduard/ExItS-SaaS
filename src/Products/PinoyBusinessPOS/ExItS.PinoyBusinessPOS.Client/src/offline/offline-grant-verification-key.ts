/**
 * Offline operating grant verification public key resolution (RMAP-21-FIX03).
 * Public keys are not secrets; Production must not fall back to the documented dev key.
 */

export const DEVELOPMENT_OFFLINE_OPERATING_GRANT_PUBLIC_KEY_PEM = `-----BEGIN PUBLIC KEY-----
MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEkld6WGOTRLooj2ArP2UV2S+nTVtA
yfFYSN1+JNozH4BKAVf5/c1MwCGTLCel38wB0fnM9/1cYKEGKrh9xldC7Q==
-----END PUBLIC KEY-----`;

export class OfflineGrantVerificationKeyError extends Error {
  constructor(message: string) {
    super(message);
    this.name = "OfflineGrantVerificationKeyError";
  }
}

function readConfiguredPublicKeyPem(): string | null {
  const raw = import.meta.env.VITE_OFFLINE_OPERATING_GRANT_PUBLIC_KEY_PEM;
  if (typeof raw !== "string") {
    return null;
  }
  const trimmed = raw.trim();
  return trimmed.length > 0 ? trimmed : null;
}

function isProductionRuntime(): boolean {
  return import.meta.env.PROD && import.meta.env.MODE === "production";
}

/**
 * Returns the PEM public key used to verify server-signed offline operating grants.
 * Development/LocalValidation may use the documented dev key when unset.
 * Production fails closed when unset.
 */
export function resolveOfflineOperatingGrantVerificationPublicKeyPem(): string {
  const configured = readConfiguredPublicKeyPem();
  if (configured) {
    return configured;
  }

  if (isProductionRuntime()) {
    throw new OfflineGrantVerificationKeyError(
      "Production requires VITE_OFFLINE_OPERATING_GRANT_PUBLIC_KEY_PEM matching the server signing key pair.",
    );
  }

  return DEVELOPMENT_OFFLINE_OPERATING_GRANT_PUBLIC_KEY_PEM;
}

/** Test-only override for production-key fail-closed behavior. */
export function resolveOfflineOperatingGrantVerificationPublicKeyPemForTests(options?: {
  configuredPem?: string | null;
  production?: boolean;
}): string {
  const configured = options?.configuredPem?.trim();
  if (configured) {
    return configured;
  }
  if (options?.production) {
    throw new OfflineGrantVerificationKeyError(
      "Production requires VITE_OFFLINE_OPERATING_GRANT_PUBLIC_KEY_PEM matching the server signing key pair.",
    );
  }
  return DEVELOPMENT_OFFLINE_OPERATING_GRANT_PUBLIC_KEY_PEM;
}

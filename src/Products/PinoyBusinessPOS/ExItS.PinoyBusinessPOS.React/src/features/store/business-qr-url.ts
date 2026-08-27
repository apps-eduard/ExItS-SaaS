/**
 * Safe Business QR acquisition URL helpers (EXITS-V1-CLOSURE-01).
 * PublicOrganizationId is the v1 routing identifier; friendly slugs remain deferred.
 */

const PUBLIC_ORG_ID_PATTERN = /^ORG\d{6}$/i;

export function normalizePublicOrganizationId(value: string | null | undefined): string | null {
  if (!value) {
    return null;
  }
  const trimmed = value.trim().toUpperCase();
  if (!PUBLIC_ORG_ID_PATTERN.test(trimmed)) {
    return null;
  }
  return trimmed;
}

export function buildPublicStorePath(publicOrganizationId: string): string {
  const normalized = normalizePublicOrganizationId(publicOrganizationId);
  if (!normalized) {
    throw new Error("Invalid public organization id for store path.");
  }
  return `/store/${normalized}`;
}

export function buildPublicStoreAbsoluteUrl(
  publicOrganizationId: string,
  origin: string = typeof window !== "undefined" ? window.location.origin : "",
): string {
  const path = buildPublicStorePath(publicOrganizationId);
  const base = origin.replace(/\/$/, "");
  return `${base}${path}`;
}

/**
 * QR payload shown on Business QR page — HTTPS acquisition URL for phone cameras.
 * Legacy exits:// organization payloads remain resolvable server-side.
 */
export function buildBusinessQrAcquisitionPayload(
  publicOrganizationId: string,
  origin?: string,
): string {
  return buildPublicStoreAbsoluteUrl(publicOrganizationId, origin);
}

export function isLegacyOrganizationQrPayload(payload: string): boolean {
  return /^exits:\/\/qr\/v1\/organization\//i.test(payload.trim());
}

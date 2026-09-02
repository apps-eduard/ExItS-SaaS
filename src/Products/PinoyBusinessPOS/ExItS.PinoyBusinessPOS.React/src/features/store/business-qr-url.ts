/**
 * Safe Business / branch QR acquisition URL helpers.
 * PublicOrganizationId is the v1 org routing identifier; friendly slugs remain deferred.
 * Branch identity uses the stable Platform branch GUID (rename-safe).
 */

const PUBLIC_ORG_ID_PATTERN = /^ORG\d{6}$/i;
const BRANCH_ID_PATTERN =
  /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/;

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

export function normalizePublicBranchId(value: string | null | undefined): string | null {
  if (!value) {
    return null;
  }
  const trimmed = value.trim();
  if (!BRANCH_ID_PATTERN.test(trimmed)) {
    return null;
  }
  return trimmed.toLowerCase();
}

export function buildPublicStorePath(publicOrganizationId: string): string {
  const normalized = normalizePublicOrganizationId(publicOrganizationId);
  if (!normalized) {
    throw new Error("Invalid public organization id for store path.");
  }
  return `/store/${normalized}`;
}

/** Exact-branch public path: /store/ORG######/b/{branchGuid} */
export function buildPublicBranchStorePath(
  publicOrganizationId: string,
  branchId: string,
): string {
  const org = normalizePublicOrganizationId(publicOrganizationId);
  const branch = normalizePublicBranchId(branchId);
  if (!org || !branch) {
    throw new Error("Invalid public organization/branch id for store path.");
  }
  return `/store/${org}/b/${branch}`;
}

export function buildPublicStoreAbsoluteUrl(
  publicOrganizationId: string,
  origin: string = typeof window !== "undefined" ? window.location.origin : "",
): string {
  const path = buildPublicStorePath(publicOrganizationId);
  const base = origin.replace(/\/$/, "");
  return `${base}${path}`;
}

export function buildPublicBranchStoreAbsoluteUrl(
  publicOrganizationId: string,
  branchId: string,
  origin: string = typeof window !== "undefined" ? window.location.origin : "",
): string {
  const path = buildPublicBranchStorePath(publicOrganizationId, branchId);
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

export function buildBranchStoreQrAcquisitionPayload(
  publicOrganizationId: string,
  branchId: string,
  origin?: string,
): string {
  return buildPublicBranchStoreAbsoluteUrl(publicOrganizationId, branchId, origin);
}

export function isLegacyOrganizationQrPayload(payload: string): boolean {
  return /^exits:\/\/qr\/v1\/organization\//i.test(payload.trim());
}

/** Suggested download filename for a branch QR PNG. */
export function buildBranchQrDownloadFilename(
  organizationDisplayName: string,
  branchName: string,
): string {
  const slug = (value: string) =>
    value
      .trim()
      .toLowerCase()
      .replace(/[^a-z0-9]+/g, "-")
      .replace(/^-+|-+$/g, "")
      .slice(0, 40) || "branch";
  return `${slug(organizationDisplayName)}-${slug(branchName)}-qr.png`;
}

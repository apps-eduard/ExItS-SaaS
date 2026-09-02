/**
 * Auth continuation for Business QR / public store acquisition.
 * Only internal validated routes are allowed (open-redirect protection).
 */

import {
  buildPublicBranchStorePath,
  buildPublicStorePath,
  normalizePublicBranchId,
  normalizePublicOrganizationId,
} from "@/features/store/business-qr-url";

export const STORE_ACQUISITION_STORAGE_KEY = "exits.acquisition.storeIntent";

export type StoreAcquisitionIntent = {
  publicOrganizationId: string;
  /** When set, shop must open this exact branch (branch QR). */
  branchId?: string | null;
  intendedAction: "open-store";
};

const ABSOLUTE_OR_PROTOCOL = /^(?:[a-z][a-z0-9+.-]*:)?\/\//i;

function isInternalRelativePath(path: string): boolean {
  if (!path.startsWith("/") || path.startsWith("//")) {
    return false;
  }
  if (ABSOLUTE_OR_PROTOCOL.test(path)) {
    return false;
  }
  if (path.toLowerCase().startsWith("/javascript:") || path.toLowerCase().startsWith("/data:")) {
    return false;
  }
  return true;
}

/** Allowed post-auth continuation targets for acquisition. */
export function isSafeAuthContinuePath(path: string | null | undefined): path is string {
  if (!path || !isInternalRelativePath(path)) {
    return false;
  }
  const [pathname, search = ""] = path.split(/[?#]/);
  if (!pathname) {
    return false;
  }
  const storeMatch = pathname.match(/^\/store\/(ORG\d{6})$/i);
  if (storeMatch) {
    return normalizePublicOrganizationId(storeMatch[1]) !== null;
  }
  const branchStoreMatch = pathname.match(
    /^\/store\/(ORG\d{6})\/b\/([0-9a-fA-F-]{36})$/i,
  );
  if (branchStoreMatch) {
    return (
      normalizePublicOrganizationId(branchStoreMatch[1]) !== null &&
      normalizePublicBranchId(branchStoreMatch[2]) !== null
    );
  }
  if (pathname === "/personal/linked-merchants") {
    return true;
  }
  if (/^\/personal\/linked-merchants\/[0-9a-fA-F-]{36}\/shop(?:\/checkout)?$/.test(pathname)) {
    if (!search) {
      return true;
    }
    const params = new URLSearchParams(search.startsWith("?") ? search.slice(1) : search);
    const branch = params.get("branchId");
    return branch == null || normalizePublicBranchId(branch) !== null;
  }
  return false;
}

export function buildStoreContinuePath(
  publicOrganizationId: string,
  branchId?: string | null,
): string {
  const branch = normalizePublicBranchId(branchId);
  if (branch) {
    return buildPublicBranchStorePath(publicOrganizationId, branch);
  }
  return buildPublicStorePath(publicOrganizationId);
}

export function buildMerchantShopPath(
  organizationId: string,
  branchId?: string | null,
): string {
  const base = `/personal/linked-merchants/${organizationId}/shop`;
  const branch = normalizePublicBranchId(branchId);
  return branch ? `${base}?branchId=${encodeURIComponent(branch)}` : base;
}

export function rememberStoreAcquisitionIntent(
  publicOrganizationId: string,
  branchId?: string | null,
): void {
  const normalized = normalizePublicOrganizationId(publicOrganizationId);
  if (!normalized || typeof sessionStorage === "undefined") {
    return;
  }
  const branch = normalizePublicBranchId(branchId);
  const intent: StoreAcquisitionIntent = {
    publicOrganizationId: normalized,
    branchId: branch,
    intendedAction: "open-store",
  };
  try {
    sessionStorage.setItem(STORE_ACQUISITION_STORAGE_KEY, JSON.stringify(intent));
  } catch {
    /* ignore quota */
  }
}

export function peekStoreAcquisitionIntent(): StoreAcquisitionIntent | null {
  if (typeof sessionStorage === "undefined") {
    return null;
  }
  try {
    const raw = sessionStorage.getItem(STORE_ACQUISITION_STORAGE_KEY);
    if (!raw) {
      return null;
    }
    const parsed = JSON.parse(raw) as Partial<StoreAcquisitionIntent>;
    const id = normalizePublicOrganizationId(parsed.publicOrganizationId);
    if (!id || parsed.intendedAction !== "open-store") {
      return null;
    }
    return {
      publicOrganizationId: id,
      branchId: normalizePublicBranchId(parsed.branchId),
      intendedAction: "open-store",
    };
  } catch {
    return null;
  }
}

export function clearStoreAcquisitionIntent(): void {
  if (typeof sessionStorage === "undefined") {
    return;
  }
  try {
    sessionStorage.removeItem(STORE_ACQUISITION_STORAGE_KEY);
  } catch {
    /* ignore */
  }
}

/**
 * Resolve continue target from URL query and/or stored acquisition intent.
 * Query `continue` wins when safe; otherwise fall back to stored store intent.
 */
export function resolveAuthContinuePath(
  continueParam: string | null | undefined,
): string | null {
  if (isSafeAuthContinuePath(continueParam)) {
    return continueParam;
  }
  const intent = peekStoreAcquisitionIntent();
  if (intent) {
    return buildStoreContinuePath(intent.publicOrganizationId, intent.branchId);
  }
  return null;
}

export function buildSignInHrefForStore(
  publicOrganizationId: string,
  branchId?: string | null,
): string {
  const continuePath = buildStoreContinuePath(publicOrganizationId, branchId);
  return `/sign-in?continue=${encodeURIComponent(continuePath)}`;
}

export function buildSignUpHrefForStore(
  publicOrganizationId: string,
  branchId?: string | null,
): string {
  const continuePath = buildStoreContinuePath(publicOrganizationId, branchId);
  return `/sign-in?tab=sign-up&continue=${encodeURIComponent(continuePath)}`;
}

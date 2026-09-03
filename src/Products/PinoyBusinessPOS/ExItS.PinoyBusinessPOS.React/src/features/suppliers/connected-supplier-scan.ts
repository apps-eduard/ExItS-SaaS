import {
  normalizePublicBranchId,
  normalizePublicOrganizationId,
} from "@/features/store/business-qr-url";
import { assertExItsQrPurpose, ExItsQrParseError } from "@/lib/exits-qr/envelope";

export type ConnectedSupplierScanResolution = {
  /** Public ORG###### used for Platform resolve + location listing. */
  publicOrganizationId: string;
  /** Exact branch from branch storefront QR when present. */
  supplierBranchId: string | null;
  /** Source kind for UX copy. */
  source: "organization" | "branch-storefront" | "public-id";
};

const BRANCH_STORE_PATH =
  /(?:^|\/\/)[^/]*\/store\/(ORG\d{6})\/b\/([0-9a-fA-F-]{36})(?:[/?#]|$)/i;
const ORG_STORE_PATH = /(?:^|\/\/)[^/]*\/store\/(ORG\d{6})(?:\/?(?:[?#]|$))/i;

/**
 * Parses Business QR / ORG id / optional branch storefront URL for supplier connect.
 * Branch storefront URLs are accepted only as connect-scan input (not customer navigation).
 */
export function parseConnectedSupplierScanPayload(
  raw: string,
): ConnectedSupplierScanResolution {
  const trimmed = raw.trim();
  if (!trimmed) {
    throw new ExItsQrParseError("empty", "Payload is empty.");
  }

  const branchMatch = trimmed.match(BRANCH_STORE_PATH);
  if (branchMatch) {
    const org = normalizePublicOrganizationId(branchMatch[1]);
    const branch = normalizePublicBranchId(branchMatch[2]);
    if (org && branch) {
      return {
        publicOrganizationId: org,
        supplierBranchId: branch,
        source: "branch-storefront",
      };
    }
  }

  const storeMatch = trimmed.match(ORG_STORE_PATH);
  if (storeMatch) {
    const org = normalizePublicOrganizationId(storeMatch[1]);
    if (org) {
      return {
        publicOrganizationId: org,
        supplierBranchId: null,
        source: "organization",
      };
    }
  }

  const bare = normalizePublicOrganizationId(trimmed);
  if (bare) {
    return {
      publicOrganizationId: bare,
      supplierBranchId: null,
      source: "public-id",
    };
  }

  const parsed = assertExItsQrPurpose(trimmed, "organization");
  const orgFromEnvelope = normalizePublicOrganizationId(parsed.subject);
  if (!orgFromEnvelope) {
    throw new ExItsQrParseError("invalid_subject", "Organization subject is invalid.");
  }

  return {
    publicOrganizationId: orgFromEnvelope,
    supplierBranchId: null,
    source: "organization",
  };
}

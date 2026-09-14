import { platformRequest } from "@/api/platform/platform-http";
import {
  formatPublicBusinessAddress,
  type OrganizationB2bPublicProfile,
} from "@/api/platform/organization-b2b-public-profile-client";
import type { LinkedCustomerSaleReceipt } from "@/api/pos/pos-linked-customers-client";
import type { BusinessIdentity, DocumentHeaderVisibility } from "@/components/exits/business-document";
import {
  personalStoreDisplayName,
  stripPersonalRunStamp,
} from "@/features/customer-ordering/format-personal-store-label";
import { DEFAULT_DOCUMENT_SETTINGS } from "@/features/documents/document-settings";

function pick(raw: Record<string, unknown>, camel: string, pascal: string): unknown {
  return raw[camel] ?? raw[pascal];
}

export type CustomerSellerDocumentIdentitySnap = {
  businessName?: string | null;
  publicOrganizationId?: string | null;
  logoUrl?: string | null;
  address?: string | null;
  phone?: string | null;
  email?: string | null;
  branchName?: string | null;
  branchAddress?: string | null;
  showLogo?: boolean;
  showBusinessName?: boolean;
  showBusinessAddress?: boolean;
  showBusinessPhone?: boolean;
  showBusinessEmail?: boolean;
  showBranchName?: boolean;
  showBranchAddress?: boolean;
  identitySource?: string | null;
};

export async function getOrganizationDocumentPublicIdentity(
  organizationId: string,
  signal?: AbortSignal,
): Promise<OrganizationB2bPublicProfile | null> {
  try {
    const raw = await platformRequest<unknown>({
      path: `/api/v1/platform/organizations/${organizationId}/document-public-identity`,
      signal,
    });
    if (!raw || typeof raw !== "object") {
      return null;
    }
    const r = raw as Record<string, unknown>;
    return {
      organizationId: String(pick(r, "organizationId", "OrganizationId") ?? organizationId),
      displayName: String(pick(r, "displayName", "DisplayName") ?? ""),
      publicOrganizationId: (pick(r, "publicOrganizationId", "PublicOrganizationId") as string | null) ?? null,
      logoUrl: (pick(r, "logoUrl", "LogoUrl") as string | null) ?? null,
      businessPhone: (pick(r, "businessPhone", "BusinessPhone") as string | null) ?? null,
      businessEmail: (pick(r, "businessEmail", "BusinessEmail") as string | null) ?? null,
      addressLine1: (pick(r, "addressLine1", "AddressLine1") as string | null) ?? null,
      addressLine2: (pick(r, "addressLine2", "AddressLine2") as string | null) ?? null,
      city: (pick(r, "city", "City") as string | null) ?? null,
      region: (pick(r, "region", "Region") as string | null) ?? null,
      postalCode: (pick(r, "postalCode", "PostalCode") as string | null) ?? null,
      countryCode: (pick(r, "countryCode", "CountryCode") as string | null) ?? null,
    };
  } catch {
    return null;
  }
}

/**
 * Resolve seller document identity for customer-facing purchase summaries.
 * Prefer sale-time snapshot fields → public document identity → merchant snapshot → "Store".
 * Empty snap fields (common when checkout raced org-profile load) must still gap-fill.
 */
export function resolveCustomerSellerIdentityParts(args: {
  sellerDocumentIdentity?: CustomerSellerDocumentIdentitySnap | null;
  merchantDisplayName?: string | null;
  branchDisplayName?: string | null;
  publicProfile?: OrganizationB2bPublicProfile | null;
}): { identity: BusinessIdentity; headerVisibility: DocumentHeaderVisibility } {
  const { sellerDocumentIdentity: snap, merchantDisplayName, branchDisplayName, publicProfile } =
    args;
  const defaults = DEFAULT_DOCUMENT_SETTINGS.header;

  const fromSnapName =
    personalStoreDisplayName(snap?.businessName) || snap?.businessName?.trim() || null;
  const fromMerchant =
    personalStoreDisplayName(merchantDisplayName) || merchantDisplayName?.trim() || null;
  const fromPublic = publicProfile?.displayName?.trim() || null;

  const businessName = fromSnapName || fromPublic || fromMerchant || "Store";

  const address =
    snap?.address?.trim() ||
    (publicProfile ? formatPublicBusinessAddress(publicProfile) : null) ||
    null;
  const phone = snap?.phone?.trim() || publicProfile?.businessPhone?.trim() || null;
  const email = snap?.email?.trim() || publicProfile?.businessEmail?.trim() || null;
  const logoUrl = snap?.logoUrl?.trim() || publicProfile?.logoUrl?.trim() || null;
  const branchName = snap?.branchName?.trim() || branchDisplayName?.trim() || null;
  const branchAddress = snap?.branchAddress?.trim() || null;

  // Visibility: prefer snap flags when the snap carried durable branding; otherwise defaults
  // so a branch-only / empty snap cannot hide public-profile email/address.
  const snapHasDurable =
    Boolean(fromSnapName) ||
    Boolean(snap?.address?.trim()) ||
    Boolean(snap?.phone?.trim()) ||
    Boolean(snap?.email?.trim()) ||
    Boolean(snap?.logoUrl?.trim()) ||
    Boolean(snap?.publicOrganizationId?.trim());

  return {
    identity: {
      businessName,
      logoUrl,
      address,
      phone,
      email,
      website: null,
      branchName,
      branchAddress,
    },
    headerVisibility: {
      showLogo: snapHasDurable ? (snap?.showLogo ?? defaults.showLogo) : defaults.showLogo,
      showBusinessName: true,
      showBusinessAddress: snapHasDurable
        ? (snap?.showBusinessAddress ?? defaults.showBusinessAddress)
        : defaults.showBusinessAddress,
      showBusinessPhone: snapHasDurable
        ? (snap?.showBusinessPhone ?? defaults.showBusinessPhone)
        : defaults.showBusinessPhone,
      showBusinessEmail: snapHasDurable
        ? (snap?.showBusinessEmail ?? defaults.showBusinessEmail)
        : defaults.showBusinessEmail,
      showWebsite: defaults.showWebsite,
      showBranchName: snap?.showBranchName ?? defaults.showBranchName,
      showBranchAddress: snap?.showBranchAddress ?? defaults.showBranchAddress,
    },
  };
}

export function resolveCustomerSellerIdentity(args: {
  receipt: LinkedCustomerSaleReceipt;
  publicProfile?: OrganizationB2bPublicProfile | null;
}): { identity: BusinessIdentity; headerVisibility: DocumentHeaderVisibility } {
  return resolveCustomerSellerIdentityParts({
    sellerDocumentIdentity: args.receipt.sellerDocumentIdentity,
    merchantDisplayName: args.receipt.merchantDisplayName,
    branchDisplayName: args.receipt.branchDisplayName,
    publicProfile: args.publicProfile,
  });
}

export function resolveCustomerDisplayName(receipt: LinkedCustomerSaleReceipt): string | null {
  return stripPersonalRunStamp(receipt.customerDisplayName ?? "") || null;
}

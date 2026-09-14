import { useQuery } from "@tanstack/react-query";
import { getOrganization } from "@/api/platform/organization-profile-client";
import {
  formatOrganizationAddress,
  type DocumentHeaderSettings,
} from "@/features/documents/document-settings";
import type {
  BusinessIdentity,
  DocumentHeaderVisibility,
} from "@/components/exits/business-document";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

export function useBusinessDocumentIdentity(organizationId: string | null | undefined): {
  identity: BusinessIdentity;
  headerVisibility: (settings: DocumentHeaderSettings) => DocumentHeaderVisibility;
  isLoading: boolean;
} {
  const { boundWorkspace } = useWorkspace();
  const query = useQuery({
    queryKey: ["organization-profile", organizationId],
    enabled: Boolean(organizationId),
    queryFn: ({ signal }) => getOrganization(organizationId!, signal),
    staleTime: 60_000,
  });

  const org = query.data;
  const address = org
    ? formatOrganizationAddress({
        addressLine1: org.profile.addressLine1,
        addressLine2: org.profile.addressLine2,
        city: org.profile.city,
        region: org.profile.region,
        postalCode: org.profile.postalCode,
        countryCode: org.profile.countryCode,
      })
    : null;

  const identity: BusinessIdentity = {
    businessName:
      org?.displayName?.trim() ||
      org?.branding.brandDisplayName?.trim() ||
      "Business",
    logoUrl: org?.branding.logoUrl ?? null,
    address,
    phone: org?.profile.contactPhone ?? null,
    email: org?.profile.contactEmail ?? null,
    website: null,
    branchName: boundWorkspace?.branchName ?? null,
    branchAddress: null,
  };

  return {
    identity,
    isLoading: query.isLoading,
    headerVisibility: (settings) => ({
      showLogo: settings.showLogo,
      showBusinessName: true,
      showBusinessAddress: settings.showBusinessAddress,
      showBusinessPhone: settings.showBusinessPhone,
      showBusinessEmail: settings.showBusinessEmail,
      showWebsite: settings.showWebsite,
      showBranchName: settings.showBranchName,
      showBranchAddress: settings.showBranchAddress,
    }),
  };
}

export function formatMoney(amount: number): string {
  return `₱${amount.toLocaleString(undefined, {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  })}`;
}

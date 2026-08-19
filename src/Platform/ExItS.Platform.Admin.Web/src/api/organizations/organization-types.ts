export const ORGANIZATION_STATUSES = ["Active", "Suspended", "Closed"] as const;
export type OrganizationStatus = (typeof ORGANIZATION_STATUSES)[number];

export const ORGANIZATION_LIST_SORT_BY = [
  "DisplayName",
  "Slug",
  "Status",
  "CreatedAtUtc",
  "UpdatedAtUtc",
] as const;
export type OrganizationListSortBy = (typeof ORGANIZATION_LIST_SORT_BY)[number];

export const ORGANIZATION_LIST_PAGE_SIZE = 20;

export type OrganizationListItem = {
  id: string;
  displayName: string;
  slug: string;
  status: string;
  createdAtUtc?: string;
  updatedAtUtc?: string;
};

export type OrganizationProfile = {
  legalName?: string;
  contactEmail?: string;
  contactPhone?: string;
  addressLine1?: string;
  addressLine2?: string;
  city?: string;
  region?: string;
  postalCode?: string;
  countryCode?: string;
  timeZoneId?: string;
  locale?: string;
  currencyCode?: string;
};

export type OrganizationBranding = {
  brandDisplayName?: string;
  primaryColor?: string;
  accentColor?: string;
};

export type OrganizationDetail = OrganizationListItem & {
  profile: OrganizationProfile;
  branding: OrganizationBranding;
};

export type CommercialSubscriptionRecord = {
  id: string;
  productCode: string;
  status: string;
};

export type CommercialPaymentRecord = {
  id: string;
  productCode: string;
  status: string;
  paidAtUtc?: string;
};

export type CommercialEntitlementRecord = {
  id: string;
  productCode: string;
  subscriptionStatus: string;
  generatedAtUtc?: string;
};

export type OrganizationCommercialSummary = {
  subscriptions: CommercialSubscriptionRecord[];
  payments: CommercialPaymentRecord[];
  latestEntitlements: CommercialEntitlementRecord[];
};

export type OrganizationListQuery = {
  page?: number;
  pageSize?: number;
  status?: string;
  search?: string;
  sortBy?: string;
  sortDesc?: boolean;
};

export type OrganizationBranch = {
  id: string;
  organizationId: string;
  code: string;
  name: string;
  status: string;
  isPrimary: boolean;
  addressLine1?: string;
  addressLine2?: string;
  city?: string;
  region?: string;
  postalCode?: string;
  countryCode?: string;
  contactPhone?: string;
  timeZoneId?: string;
  createdAtUtc?: string;
  updatedAtUtc?: string;
};

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
  logoUrl?: string;
  primaryColor?: string;
  accentColor?: string;
};

export const ORGANIZATION_MEMBER_ROLES = ["OrganizationOwner", "OrganizationMember"] as const;
export type OrganizationMemberRole = (typeof ORGANIZATION_MEMBER_ROLES)[number];

export type ProductAccessAssignment = {
  id: string;
  userId: string;
  organizationId: string;
  membershipId: string;
  productCode: string;
  status: string;
  grantedAtUtc?: string;
  grantedByActor?: string;
  revokedAtUtc?: string;
  revokedByActor?: string;
  reason?: string;
  createdAtUtc?: string;
  updatedAtUtc?: string;
};

export type EnabledProduct = {
  productCode: string;
  displayName: string;
  entitlementActive: boolean;
  productAccessAssigned: boolean;
  productLocalRoleGranted: boolean;
  canLaunch: boolean;
  productLocalRoleCode?: string;
  mappedPosRoleCode?: string;
  subscriptionStatus?: string;
  reasonCode?: string;
  productId?: string;
  productKey?: string;
  productDisplayName?: string;
  entitlementStatus?: string;
  provisioningStatus?: string;
  organizationRole?: string;
  productRole?: string;
  denialReasonCode?: string;
  denialReasonDisplay?: string;
};

export type ProductLocalRoleGrant = {
  id: string;
  organizationId: string;
  userIdentityId: string;
  productCode: string;
  roleCode: string;
  mappedPosRoleCode: string;
  status: string;
  grantedAtUtc?: string;
  grantedByUserIdentityId?: string;
  source?: string;
  revokedAtUtc?: string;
  userDisplayName?: string;
  productDisplayName?: string;
  roleDisplay?: string;
  productKey?: string;
};

export type OrganizationRoleDefinition = {
  id: string;
  organizationId: string;
  code: string;
  name: string;
  description?: string;
  status: string;
  permissions: string[];
  createdAtUtc?: string;
  updatedAtUtc?: string;
  version?: number;
};

export const PRODUCT_ACCESS_PAGE_SIZE = 20;
export const ORGANIZATION_ROLES_PAGE_SIZE = 20;

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
  productDisplayName?: string;
  snapshotVersion?: number;
  schemaVersion?: number;
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
  productCode?: string;
};

export const MEMBERSHIP_STATUSES = ["Active", "Suspended", "Removed"] as const;
export type MembershipStatus = (typeof MEMBERSHIP_STATUSES)[number];

export const INVITATION_STATUSES = ["Pending", "Accepted", "Revoked", "Expired"] as const;
export type InvitationStatus = (typeof INVITATION_STATUSES)[number];

export const ORGANIZATION_PEOPLE_PAGE_SIZE = 20;

export type OrganizationMember = {
  id: string;
  organizationId: string;
  userId: string;
  role: string;
  status: string;
  createdAtUtc?: string;
  updatedAtUtc?: string;
  suspendedAtUtc?: string;
  removedAtUtc?: string;
  username?: string;
  displayName?: string;
  email?: string;
  roleDisplay?: string;
  accountStatus?: string;
  employeeCode?: string;
};

export type OrganizationInvitation = {
  id: string;
  organizationId: string;
  email: string;
  role: string;
  status: string;
  createdAtUtc?: string;
  updatedAtUtc?: string;
  expiresAtUtc?: string;
  acceptedAtUtc?: string;
  revokedAtUtc?: string;
  roleDisplay?: string;
  inviteeDisplayName?: string;
  invitationStatus?: string;
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

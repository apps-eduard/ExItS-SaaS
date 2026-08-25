export type PersonalContactDto = {
  id: string;
  displayName: string;
  phone?: string | null;
  email?: string | null;
  linkedUserIdentityId?: string | null;
  resolvedUserIdentityId?: string | null;
  resolvedPublicUserId?: string | null;
  connectedAtUtc?: string | null;
  blockedAtUtc?: string | null;
  status: string;
  createdAtUtc: string;
};

export type CreatePersonalContactRequest = {
  displayName: string;
  phone?: string | null;
  email?: string | null;
  resolvedUserIdentityId?: string | null;
  resolvedPublicUserId?: string | null;
};

export type PersonalConnectionRequestDto = {
  id: string;
  requesterUserIdentityId: string;
  targetUserIdentityId: string;
  requesterContactId: string;
  requesterDisplayName: string;
  requesterPublicUserId?: string | null;
  targetPublicUserId?: string | null;
  status: string;
  createdAtUtc: string;
  updatedAtUtc: string;
  expiresAtUtc: string;
  acceptedAtUtc?: string | null;
  declinedAtUtc?: string | null;
  revokedAtUtc?: string | null;
  direction: string;
};

export type PersonalDebtRelationshipSummaryDto = {
  id: string;
  perspective: string;
  creditorUserIdentityId?: string | null;
  creditorContactId?: string | null;
  debtorUserIdentityId?: string | null;
  debtorContactId?: string | null;
  currencyCode: string;
  currentBalance: number;
  dueDateUtc?: string | null;
  status: string;
  version: number;
  updatedAtUtc: string;
};

export type CreatePersonalDebtRelationshipRequest = {
  creditorUserIdentityId?: string | null;
  creditorContactId?: string | null;
  debtorUserIdentityId?: string | null;
  debtorContactId?: string | null;
  currencyCode?: string | null;
  dueDateUtc?: string | null;
  initialLoanAmount?: number | null;
  initialLoanNotes?: string | null;
};

export type PersonalUtangInvitationDto = {
  id: string;
  debtRelationshipId: string;
  inviteeContactId: string;
  invitedByUserIdentityId: string;
  inviteTargetEmailMasked?: string | null;
  status: string;
  createdAtUtc: string;
  updatedAtUtc: string;
  expiresAtUtc: string;
  acceptedAtUtc?: string | null;
  declinedAtUtc?: string | null;
  revokedAtUtc?: string | null;
  acceptedByUserIdentityId?: string | null;
  acceptToken?: string | null;
};

export type PersonalUtangInvitationAcceptResultDto = {
  invitationId: string;
  debtRelationshipId: string;
  linkedContactId: string;
  linkedUserIdentityId: string;
  createdOrganizationMembership: boolean;
  grantedProductRole: boolean;
};

export type PersonalInAppNotificationDto = {
  id: string;
  title: string;
  preview: string;
  relatedType: string;
  relatedId?: string | null;
  isRead: boolean;
  createdAtUtc: string;
  readAtUtc?: string | null;
};

export type ResolvedPublicUserDto = {
  publicUserId: string;
  userIdentityId: string;
  displayName: string;
  maskedEmail?: string | null;
  status: string;
  isSelf: boolean;
};

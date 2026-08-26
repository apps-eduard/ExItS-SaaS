import {
  mapOrganizationInvitation,
  mapOrganizationMember,
} from "@/api/organizations/organization-client";
import type {
  OrganizationInvitation,
  OrganizationMember,
} from "@/api/organizations/organization-types";
import { commercialMutationRequest } from "@/api/commercial/commercial-http";

export type CreateInvitationBody = {
  email: string;
  role: string;
  firstName?: string | null;
  lastName?: string | null;
  displayName?: string | null;
  phone?: string | null;
  employeeCode?: string | null;
  branch?: string | null;
  requireEmailVerification?: boolean;
};

export type AddMemberBody = {
  userId: string;
  role: string;
  reason?: string | null;
};

export type ChangeMembershipRoleBody = {
  role: string;
  actorReference?: string | null;
};

export type MembershipLifecycleBody = {
  reason?: string | null;
  actorReference?: string | null;
};

export function createOrganizationInvitation(
  baseUrl: string,
  organizationId: string,
  body: CreateInvitationBody,
  signal?: AbortSignal,
): Promise<OrganizationInvitation> {
  return commercialMutationRequest<unknown>(baseUrl, {
    method: "POST",
    path: `/api/v1/platform/organizations/${organizationId}/invitations`,
    body,
    signal,
  }).then(mapOrganizationInvitation);
}

export function resendOrganizationInvitation(
  baseUrl: string,
  invitationId: string,
  signal?: AbortSignal,
): Promise<OrganizationInvitation> {
  return commercialMutationRequest<unknown>(baseUrl, {
    method: "POST",
    path: `/api/v1/platform/invitations/${invitationId}/resend`,
    signal,
  }).then(mapOrganizationInvitation);
}

export function revokeOrganizationInvitation(
  baseUrl: string,
  invitationId: string,
  signal?: AbortSignal,
): Promise<OrganizationInvitation> {
  return commercialMutationRequest<unknown>(baseUrl, {
    method: "POST",
    path: `/api/v1/platform/invitations/${invitationId}/revoke`,
    signal,
  }).then(mapOrganizationInvitation);
}

export function addOrganizationMember(
  baseUrl: string,
  organizationId: string,
  body: AddMemberBody,
  signal?: AbortSignal,
): Promise<OrganizationMember> {
  return commercialMutationRequest<unknown>(baseUrl, {
    method: "POST",
    path: `/api/v1/platform/organizations/${organizationId}/members`,
    body,
    signal,
  }).then(mapOrganizationMember);
}

export function changeMembershipRole(
  baseUrl: string,
  membershipId: string,
  body: ChangeMembershipRoleBody,
  signal?: AbortSignal,
): Promise<OrganizationMember> {
  return commercialMutationRequest<unknown>(baseUrl, {
    method: "PUT",
    path: `/api/v1/platform/memberships/${membershipId}/role`,
    body,
    signal,
  }).then(mapOrganizationMember);
}

export function suspendMembership(
  baseUrl: string,
  membershipId: string,
  body: MembershipLifecycleBody,
  signal?: AbortSignal,
): Promise<OrganizationMember> {
  return commercialMutationRequest<unknown>(baseUrl, {
    method: "POST",
    path: `/api/v1/platform/memberships/${membershipId}/suspend`,
    body,
    signal,
  }).then(mapOrganizationMember);
}

export function reactivateMembership(
  baseUrl: string,
  membershipId: string,
  body: MembershipLifecycleBody,
  signal?: AbortSignal,
): Promise<OrganizationMember> {
  return commercialMutationRequest<unknown>(baseUrl, {
    method: "POST",
    path: `/api/v1/platform/memberships/${membershipId}/reactivate`,
    body,
    signal,
  }).then(mapOrganizationMember);
}

export function revokeMembership(
  baseUrl: string,
  membershipId: string,
  body: MembershipLifecycleBody,
  signal?: AbortSignal,
): Promise<OrganizationMember> {
  return commercialMutationRequest<unknown>(baseUrl, {
    method: "POST",
    path: `/api/v1/platform/memberships/${membershipId}/revoke`,
    body,
    signal,
  }).then(mapOrganizationMember);
}

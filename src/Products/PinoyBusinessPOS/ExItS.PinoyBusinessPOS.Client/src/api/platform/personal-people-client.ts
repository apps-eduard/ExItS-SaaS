import { platformRequest } from "@/api/http";
import type {
  CreatePersonalContactRequest,
  CreatePersonalDebtRelationshipRequest,
  PersonalContactDto,
  PersonalDebtRelationshipSummaryDto,
  PersonalInAppNotificationDto,
  PersonalUtangInvitationAcceptResultDto,
  PersonalUtangInvitationDto,
  ResolvedPublicUserDto,
} from "@/api/platform/personal-types";

export const personalPeopleKeys = {
  all: ["personal-people"] as const,
  contacts: () => [...personalPeopleKeys.all, "contacts"] as const,
  invitations: () => [...personalPeopleKeys.all, "invitations"] as const,
  notifications: () => [...personalPeopleKeys.all, "notifications"] as const,
  lent: () => [...personalPeopleKeys.all, "lent"] as const,
  borrowed: () => [...personalPeopleKeys.all, "borrowed"] as const,
};

export async function listPersonalContacts(signal?: AbortSignal): Promise<PersonalContactDto[]> {
  return platformRequest<PersonalContactDto[]>({
    path: "/api/v1/personal/utang/contacts",
    signal,
  });
}

export async function createPersonalContact(
  body: CreatePersonalContactRequest,
  signal?: AbortSignal,
): Promise<PersonalContactDto> {
  return platformRequest<PersonalContactDto>({
    method: "POST",
    path: "/api/v1/personal/utang/contacts",
    body,
    signal,
  });
}

export async function resolvePublicUserId(
  publicUserIdOrQrPayload: string,
  purpose = "utang-people",
  signal?: AbortSignal,
): Promise<ResolvedPublicUserDto> {
  return platformRequest<ResolvedPublicUserDto>({
    method: "POST",
    path: "/api/v1/users/resolve-public-id",
    body: { publicUserIdOrQrPayload, purpose },
    signal,
  });
}

export async function listPersonalInvitations(
  signal?: AbortSignal,
): Promise<PersonalUtangInvitationDto[]> {
  return platformRequest<PersonalUtangInvitationDto[]>({
    path: "/api/v1/personal/utang/invitations",
    signal,
  });
}

export async function acceptPersonalInvitation(
  token: string,
  signal?: AbortSignal,
): Promise<PersonalUtangInvitationAcceptResultDto> {
  return platformRequest<PersonalUtangInvitationAcceptResultDto>({
    method: "POST",
    path: "/api/v1/personal/utang/invitations/accept",
    body: { token },
    signal,
  });
}

export async function declinePersonalInvitation(
  token: string,
  signal?: AbortSignal,
): Promise<PersonalUtangInvitationDto> {
  return platformRequest<PersonalUtangInvitationDto>({
    method: "POST",
    path: "/api/v1/personal/utang/invitations/decline",
    body: { token },
    signal,
  });
}

export async function resendPersonalInvitation(
  invitationId: string,
  signal?: AbortSignal,
): Promise<PersonalUtangInvitationDto> {
  return platformRequest<PersonalUtangInvitationDto>({
    method: "POST",
    path: `/api/v1/personal/utang/invitations/${invitationId}/resend`,
    signal,
  });
}

export async function revokePersonalInvitation(
  invitationId: string,
  signal?: AbortSignal,
): Promise<PersonalUtangInvitationDto> {
  return platformRequest<PersonalUtangInvitationDto>({
    method: "POST",
    path: `/api/v1/personal/utang/invitations/${invitationId}/revoke`,
    signal,
  });
}

export async function listPersonalNotifications(
  signal?: AbortSignal,
): Promise<PersonalInAppNotificationDto[]> {
  return platformRequest<PersonalInAppNotificationDto[]>({
    path: "/api/v1/personal/notifications",
    signal,
  });
}

export async function markPersonalNotificationRead(
  notificationId: string,
  signal?: AbortSignal,
): Promise<PersonalInAppNotificationDto> {
  return platformRequest<PersonalInAppNotificationDto>({
    method: "POST",
    path: `/api/v1/personal/notifications/${notificationId}/read`,
    signal,
  });
}

export async function listLentRelationships(
  signal?: AbortSignal,
): Promise<PersonalDebtRelationshipSummaryDto[]> {
  return platformRequest<PersonalDebtRelationshipSummaryDto[]>({
    path: "/api/v1/personal/utang/relationships/lent",
    signal,
  });
}

export async function listBorrowedRelationships(
  signal?: AbortSignal,
): Promise<PersonalDebtRelationshipSummaryDto[]> {
  return platformRequest<PersonalDebtRelationshipSummaryDto[]>({
    path: "/api/v1/personal/utang/relationships/borrowed",
    signal,
  });
}

export async function createPersonalDebtRelationship(
  body: CreatePersonalDebtRelationshipRequest,
  signal?: AbortSignal,
): Promise<PersonalDebtRelationshipSummaryDto> {
  return platformRequest<PersonalDebtRelationshipSummaryDto>({
    method: "POST",
    path: "/api/v1/personal/utang/relationships",
    body,
    signal,
  });
}

export async function createPersonalInvitation(
  relationshipId: string,
  inviteeContactId: string,
  signal?: AbortSignal,
): Promise<PersonalUtangInvitationDto> {
  return platformRequest<PersonalUtangInvitationDto>({
    method: "POST",
    path: `/api/v1/personal/utang/relationships/${relationshipId}/invitations`,
    body: { inviteeContactId },
    signal,
  });
}

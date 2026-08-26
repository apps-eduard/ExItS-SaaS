import { platformRequest } from "@/api/platform/platform-http";
import type {
  CreatePersonalContactRequest,
  CreatePersonalDebtRelationshipRequest,
  PersonalConnectionRequestDto,
  PersonalContactDto,
  PersonalDebtRelationshipSummaryDto,
  PersonalInAppNotificationDto,
  PersonalNotificationPageDto,
  PersonalNotificationUnreadCountDto,
  PersonalUtangInvitationAcceptResultDto,
  PersonalUtangInvitationDto,
  ResolvedPublicUserDto,
} from "@/api/platform/personal-types";

export const personalPeopleKeys = {
  all: ["personal-people"] as const,
  contacts: () => [...personalPeopleKeys.all, "contacts"] as const,
  connections: () => [...personalPeopleKeys.all, "connections"] as const,
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

export async function listPersonalConnectionRequests(
  signal?: AbortSignal,
): Promise<PersonalConnectionRequestDto[]> {
  return platformRequest<PersonalConnectionRequestDto[]>({
    path: "/api/v1/personal/connections",
    signal,
  });
}

export async function requestPersonalConnection(
  contactId: string,
  signal?: AbortSignal,
): Promise<PersonalConnectionRequestDto> {
  return platformRequest<PersonalConnectionRequestDto>({
    method: "POST",
    path: `/api/v1/personal/people/${contactId}/connection-request`,
    signal,
  });
}

export async function acceptPersonalConnectionRequest(
  requestId: string,
  signal?: AbortSignal,
): Promise<PersonalConnectionRequestDto> {
  return platformRequest<PersonalConnectionRequestDto>({
    method: "POST",
    path: `/api/v1/personal/connections/${requestId}/accept`,
    signal,
  });
}

export async function declinePersonalConnectionRequest(
  requestId: string,
  signal?: AbortSignal,
): Promise<PersonalConnectionRequestDto> {
  return platformRequest<PersonalConnectionRequestDto>({
    method: "POST",
    path: `/api/v1/personal/connections/${requestId}/decline`,
    signal,
  });
}

export async function revokePersonalConnectionRequest(
  requestId: string,
  signal?: AbortSignal,
): Promise<PersonalConnectionRequestDto> {
  return platformRequest<PersonalConnectionRequestDto>({
    method: "POST",
    path: `/api/v1/personal/connections/${requestId}/revoke`,
    signal,
  });
}

export async function unlinkPersonalContact(
  contactId: string,
  signal?: AbortSignal,
): Promise<PersonalContactDto> {
  return platformRequest<PersonalContactDto>({
    method: "POST",
    path: `/api/v1/personal/people/${contactId}/unlink`,
    signal,
  });
}

export async function blockPersonalContact(
  contactId: string,
  signal?: AbortSignal,
): Promise<PersonalContactDto> {
  return platformRequest<PersonalContactDto>({
    method: "POST",
    path: `/api/v1/personal/people/${contactId}/block`,
    signal,
  });
}

export async function unblockPersonalContact(
  contactId: string,
  signal?: AbortSignal,
): Promise<PersonalContactDto> {
  return platformRequest<PersonalContactDto>({
    method: "POST",
    path: `/api/v1/personal/people/${contactId}/unblock`,
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
  options?: { unreadOnly?: boolean },
): Promise<PersonalInAppNotificationDto[]> {
  const params = new URLSearchParams({ scope: "recent" });
  if (options?.unreadOnly) {
    params.set("unreadOnly", "true");
  }
  const raw = await platformRequest<PersonalInAppNotificationDto[] | PersonalNotificationPageDto>({
    path: `/api/v1/personal/notifications?${params.toString()}`,
    signal,
  });
  if (Array.isArray(raw)) {
    return raw;
  }
  return raw?.items ?? [];
}

export async function listArchivedPersonalNotifications(
  page: number,
  pageSize = 30,
  options?: { unreadOnly?: boolean; signal?: AbortSignal },
): Promise<PersonalNotificationPageDto> {
  const params = new URLSearchParams({
    scope: "archived",
    page: String(Math.max(page, 1)),
    pageSize: String(pageSize),
  });
  if (options?.unreadOnly) {
    params.set("unreadOnly", "true");
  }
  const raw = await platformRequest<PersonalNotificationPageDto>({
    path: `/api/v1/personal/notifications?${params.toString()}`,
    signal: options?.signal,
  });
  return {
    items: raw.items ?? [],
    totalCount: raw.totalCount ?? 0,
    page: raw.page ?? page,
    pageSize: raw.pageSize ?? pageSize,
  };
}

export async function getPersonalNotificationUnreadCount(
  signal?: AbortSignal,
): Promise<number> {
  const raw = await platformRequest<PersonalNotificationUnreadCountDto>({
    path: "/api/v1/personal/notifications/unread-count",
    signal,
  });
  return raw.unreadCount ?? 0;
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

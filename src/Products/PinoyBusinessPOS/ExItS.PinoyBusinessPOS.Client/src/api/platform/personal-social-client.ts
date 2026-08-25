import { z } from "zod";
import { platformRequest } from "@/api/platform/platform-http";

const guidSchema = z
  .string()
  .regex(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/);

function pick(raw: Record<string, unknown>, camel: string, pascal: string): unknown {
  return raw[camel] ?? raw[pascal];
}

export const personalUtangInvitationSchema = z.object({
  id: guidSchema,
  debtRelationshipId: guidSchema,
  inviteeContactId: guidSchema,
  invitedByUserIdentityId: guidSchema,
  inviteTargetEmailMasked: z.string().nullable().optional().default(null),
  status: z.string(),
  createdAtUtc: z.string(),
  updatedAtUtc: z.string(),
  expiresAtUtc: z.string(),
  acceptedAtUtc: z.string().nullable().optional().default(null),
  declinedAtUtc: z.string().nullable().optional().default(null),
  revokedAtUtc: z.string().nullable().optional().default(null),
  acceptedByUserIdentityId: guidSchema.nullable().optional().default(null),
  acceptToken: z.string().nullable().optional().default(null),
});

export const personalReminderSchema = z.object({
  id: guidSchema,
  debtRelationshipId: guidSchema,
  createdByUserIdentityId: guidSchema,
  scheduleType: z.string(),
  message: z.string().nullable().optional().default(null),
  scheduledForUtc: z.string(),
  nextDeliveryAtUtc: z.string().nullable().optional().default(null),
  status: z.string(),
  deliveryAttemptCount: z.number().int(),
  createdAtUtc: z.string(),
  deliveredAtUtc: z.string().nullable().optional().default(null),
});

export const personalInAppNotificationSchema = z.object({
  id: guidSchema,
  title: z.string(),
  preview: z.string(),
  relatedType: z.string(),
  relatedId: z.string().nullable().optional().default(null),
  isRead: z.boolean(),
  createdAtUtc: z.string(),
  readAtUtc: z.string().nullable().optional().default(null),
});

export const publicIdentitySchema = z.object({
  publicUserId: z.string(),
  qrPayload: z.string().nullable().optional().default(null),
  displayName: z.string().optional().default(""),
  status: z.string().optional().default("Active"),
});

export type PersonalUtangInvitationDto = z.infer<typeof personalUtangInvitationSchema>;
export type PersonalReminderDto = z.infer<typeof personalReminderSchema>;
export type PersonalInAppNotificationDto = z.infer<typeof personalInAppNotificationSchema>;
export type PublicIdentityDto = z.infer<typeof publicIdentitySchema>;

function normalizeInvitation(raw: unknown): unknown {
  if (!raw || typeof raw !== "object") return raw;
  const r = raw as Record<string, unknown>;
  return {
    id: pick(r, "id", "Id"),
    debtRelationshipId: pick(r, "debtRelationshipId", "DebtRelationshipId"),
    inviteeContactId: pick(r, "inviteeContactId", "InviteeContactId"),
    invitedByUserIdentityId: pick(r, "invitedByUserIdentityId", "InvitedByUserIdentityId"),
    inviteTargetEmailMasked: pick(r, "inviteTargetEmailMasked", "InviteTargetEmailMasked") ?? null,
    status: pick(r, "status", "Status"),
    createdAtUtc: pick(r, "createdAtUtc", "CreatedAtUtc"),
    updatedAtUtc: pick(r, "updatedAtUtc", "UpdatedAtUtc"),
    expiresAtUtc: pick(r, "expiresAtUtc", "ExpiresAtUtc"),
    acceptedAtUtc: pick(r, "acceptedAtUtc", "AcceptedAtUtc") ?? null,
    declinedAtUtc: pick(r, "declinedAtUtc", "DeclinedAtUtc") ?? null,
    revokedAtUtc: pick(r, "revokedAtUtc", "RevokedAtUtc") ?? null,
    acceptedByUserIdentityId:
      pick(r, "acceptedByUserIdentityId", "AcceptedByUserIdentityId") ?? null,
    acceptToken: pick(r, "acceptToken", "AcceptToken") ?? null,
  };
}

function normalizeReminder(raw: unknown): unknown {
  if (!raw || typeof raw !== "object") return raw;
  const r = raw as Record<string, unknown>;
  return {
    id: pick(r, "id", "Id"),
    debtRelationshipId: pick(r, "debtRelationshipId", "DebtRelationshipId"),
    createdByUserIdentityId: pick(r, "createdByUserIdentityId", "CreatedByUserIdentityId"),
    scheduleType: pick(r, "scheduleType", "ScheduleType"),
    message: pick(r, "message", "Message") ?? null,
    scheduledForUtc: pick(r, "scheduledForUtc", "ScheduledForUtc"),
    nextDeliveryAtUtc: pick(r, "nextDeliveryAtUtc", "NextDeliveryAtUtc") ?? null,
    status: pick(r, "status", "Status"),
    deliveryAttemptCount: Number(pick(r, "deliveryAttemptCount", "DeliveryAttemptCount") ?? 0),
    createdAtUtc: pick(r, "createdAtUtc", "CreatedAtUtc"),
    deliveredAtUtc: pick(r, "deliveredAtUtc", "DeliveredAtUtc") ?? null,
  };
}

function normalizeNotification(raw: unknown): unknown {
  if (!raw || typeof raw !== "object") return raw;
  const r = raw as Record<string, unknown>;
  return {
    id: pick(r, "id", "Id"),
    title: pick(r, "title", "Title"),
    preview: pick(r, "preview", "Preview"),
    relatedType: pick(r, "relatedType", "RelatedType"),
    relatedId: pick(r, "relatedId", "RelatedId") ?? null,
    isRead: Boolean(pick(r, "isRead", "IsRead")),
    createdAtUtc: pick(r, "createdAtUtc", "CreatedAtUtc"),
    readAtUtc: pick(r, "readAtUtc", "ReadAtUtc") ?? null,
  };
}

const UTANG = "/api/v1/personal/utang";

export async function listPersonalUtangInvitations(
  signal?: AbortSignal,
): Promise<PersonalUtangInvitationDto[]> {
  const raw = await platformRequest<unknown>({ path: `${UTANG}/invitations`, signal });
  return (Array.isArray(raw) ? raw : []).map((item) =>
    personalUtangInvitationSchema.parse(normalizeInvitation(item)),
  );
}

export async function createPersonalUtangInvitation(
  relationshipId: string,
  inviteeContactId: string,
  signal?: AbortSignal,
): Promise<PersonalUtangInvitationDto> {
  const raw = await platformRequest<unknown>({
    method: "POST",
    path: `${UTANG}/relationships/${relationshipId}/invitations`,
    body: { inviteeContactId },
    signal,
  });
  return personalUtangInvitationSchema.parse(normalizeInvitation(raw));
}

export async function acceptPersonalUtangInvitation(
  token: string,
  signal?: AbortSignal,
): Promise<unknown> {
  return platformRequest({
    method: "POST",
    path: `${UTANG}/invitations/accept`,
    body: { token },
    signal,
  });
}

export async function declinePersonalUtangInvitation(
  token: string,
  signal?: AbortSignal,
): Promise<unknown> {
  return platformRequest({
    method: "POST",
    path: `${UTANG}/invitations/decline`,
    body: { token },
    signal,
  });
}

export async function resendPersonalUtangInvitation(
  invitationId: string,
  signal?: AbortSignal,
): Promise<PersonalUtangInvitationDto> {
  const raw = await platformRequest<unknown>({
    method: "POST",
    path: `${UTANG}/invitations/${invitationId}/resend`,
    signal,
  });
  return personalUtangInvitationSchema.parse(normalizeInvitation(raw));
}

export async function revokePersonalUtangInvitation(
  invitationId: string,
  signal?: AbortSignal,
): Promise<PersonalUtangInvitationDto> {
  const raw = await platformRequest<unknown>({
    method: "POST",
    path: `${UTANG}/invitations/${invitationId}/revoke`,
    signal,
  });
  return personalUtangInvitationSchema.parse(normalizeInvitation(raw));
}

export async function listRelationshipReminders(
  relationshipId: string,
  signal?: AbortSignal,
): Promise<PersonalReminderDto[]> {
  const raw = await platformRequest<unknown>({
    path: `${UTANG}/relationships/${relationshipId}/reminders`,
    signal,
  });
  return (Array.isArray(raw) ? raw : []).map((item) =>
    personalReminderSchema.parse(normalizeReminder(item)),
  );
}

export async function createRelationshipReminder(
  relationshipId: string,
  body: { scheduleType: string; scheduledForUtc: string; message?: string | null },
  signal?: AbortSignal,
): Promise<PersonalReminderDto> {
  const raw = await platformRequest<unknown>({
    method: "POST",
    path: `${UTANG}/relationships/${relationshipId}/reminders`,
    body,
    signal,
  });
  return personalReminderSchema.parse(normalizeReminder(raw));
}

export async function cancelPersonalReminder(
  reminderId: string,
  signal?: AbortSignal,
): Promise<PersonalReminderDto> {
  const raw = await platformRequest<unknown>({
    method: "POST",
    path: `${UTANG}/reminders/${reminderId}/cancel`,
    signal,
  });
  return personalReminderSchema.parse(normalizeReminder(raw));
}

export async function listPersonalNotifications(
  signal?: AbortSignal,
): Promise<PersonalInAppNotificationDto[]> {
  const raw = await platformRequest<unknown>({ path: "/api/v1/personal/notifications", signal });
  return (Array.isArray(raw) ? raw : []).map((item) =>
    personalInAppNotificationSchema.parse(normalizeNotification(item)),
  );
}

export async function markPersonalNotificationRead(
  notificationId: string,
  signal?: AbortSignal,
): Promise<PersonalInAppNotificationDto> {
  const raw = await platformRequest<unknown>({
    method: "POST",
    path: `/api/v1/personal/notifications/${notificationId}/read`,
    signal,
  });
  return personalInAppNotificationSchema.parse(normalizeNotification(raw));
}

export async function getMyPublicIdentity(signal?: AbortSignal): Promise<PublicIdentityDto> {
  const raw = await platformRequest<unknown>({ path: "/api/v1/me/public-identity", signal });
  const r = (raw ?? {}) as Record<string, unknown>;
  return publicIdentitySchema.parse({
    publicUserId: pick(r, "publicUserId", "PublicUserId"),
    qrPayload:
      pick(r, "qrPayload", "QrPayload") ?? pick(r, "qrCodePayload", "QrCodePayload") ?? null,
    displayName: pick(r, "displayName", "DisplayName") ?? "",
    status: pick(r, "status", "Status") ?? "Active",
  });
}

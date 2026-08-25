import { z } from "zod";
import { platformRequest } from "@/api/platform/platform-http";

const guidSchema = z
  .string()
  .regex(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/);

function pick(raw: Record<string, unknown>, camel: string, pascal: string): unknown {
  return raw[camel] ?? raw[pascal];
}

export const organizationInAppNotificationSchema = z.object({
  id: guidSchema,
  organizationId: guidSchema,
  recipientUserIdentityId: guidSchema,
  title: z.string(),
  preview: z.string(),
  relatedType: z.string(),
  relatedId: z.string().nullable().optional().default(null),
  isRead: z.boolean(),
  createdAtUtc: z.string(),
  readAtUtc: z.string().nullable().optional().default(null),
});

export type OrganizationInAppNotificationDto = z.infer<
  typeof organizationInAppNotificationSchema
>;

function normalizeNotification(raw: unknown): unknown {
  if (!raw || typeof raw !== "object") return raw;
  const r = raw as Record<string, unknown>;
  return {
    id: pick(r, "id", "Id"),
    organizationId: pick(r, "organizationId", "OrganizationId"),
    recipientUserIdentityId: pick(r, "recipientUserIdentityId", "RecipientUserIdentityId"),
    title: pick(r, "title", "Title"),
    preview: pick(r, "preview", "Preview"),
    relatedType: pick(r, "relatedType", "RelatedType"),
    relatedId: pick(r, "relatedId", "RelatedId") ?? null,
    isRead: pick(r, "isRead", "IsRead"),
    createdAtUtc: pick(r, "createdAtUtc", "CreatedAtUtc"),
    readAtUtc: pick(r, "readAtUtc", "ReadAtUtc") ?? null,
  };
}

export async function listOrganizationNotifications(
  organizationId: string,
  signal?: AbortSignal,
): Promise<OrganizationInAppNotificationDto[]> {
  const raw = await platformRequest<unknown>({
    path: `/api/v1/organizations/${organizationId}/notifications`,
    signal,
  });
  return (Array.isArray(raw) ? raw : []).map((item) =>
    organizationInAppNotificationSchema.parse(normalizeNotification(item)),
  );
}

export async function markOrganizationNotificationRead(
  organizationId: string,
  notificationId: string,
  signal?: AbortSignal,
): Promise<OrganizationInAppNotificationDto> {
  const raw = await platformRequest<unknown>({
    method: "POST",
    path: `/api/v1/organizations/${organizationId}/notifications/${notificationId}/read`,
    signal,
  });
  return organizationInAppNotificationSchema.parse(normalizeNotification(raw));
}

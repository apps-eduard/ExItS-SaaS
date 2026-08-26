import { z } from "zod";
import { platformRequest } from "@/api/platform/platform-http";

const guidSchema = z
  .string()
  .regex(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/);

function pick(raw: Record<string, unknown>, camel: string, pascal: string): unknown {
  return raw[camel] ?? raw[pascal];
}

/**
 * Authoritative Platform customer-link status for an Organization BusinessCustomer.
 * Route id is Platform BusinessCustomerId — not the POS customer id.
 */
export const customerLinkStatusSchema = z.object({
  businessCustomerId: guidSchema,
  organizationId: guidSchema,
  status: z.string(),
  linkedUserIdentityId: guidSchema.nullable().optional().default(null),
  latestLinkRequestId: guidSchema.nullable().optional().default(null),
  latestLinkRequestStatus: z.string().nullable().optional().default(null),
  reminderCount: z.number().int().nonnegative().optional().default(0),
  lastRemindedAtUtc: z.string().nullable().optional().default(null),
  nextReminderEligibleAtUtc: z.string().nullable().optional().default(null),
  invitationSentAtUtc: z.string().nullable().optional().default(null),
});

export type CustomerLinkStatusDto = z.infer<typeof customerLinkStatusSchema>;

export async function getCustomerLinkStatus(
  organizationId: string,
  platformBusinessCustomerId: string,
  signal?: AbortSignal,
): Promise<CustomerLinkStatusDto> {
  const raw = await platformRequest<unknown>({
    path: `/api/v1/organizations/${organizationId}/customers/${platformBusinessCustomerId}/link-status`,
    signal,
  });
  const r = (raw ?? {}) as Record<string, unknown>;
  return customerLinkStatusSchema.parse({
    businessCustomerId: pick(r, "businessCustomerId", "BusinessCustomerId"),
    organizationId: pick(r, "organizationId", "OrganizationId"),
    status: pick(r, "status", "Status"),
    linkedUserIdentityId: pick(r, "linkedUserIdentityId", "LinkedUserIdentityId") ?? null,
    latestLinkRequestId: pick(r, "latestLinkRequestId", "LatestLinkRequestId") ?? null,
    latestLinkRequestStatus:
      pick(r, "latestLinkRequestStatus", "LatestLinkRequestStatus") ?? null,
    reminderCount: pick(r, "reminderCount", "ReminderCount") ?? 0,
    lastRemindedAtUtc: pick(r, "lastRemindedAtUtc", "LastRemindedAtUtc") ?? null,
    nextReminderEligibleAtUtc:
      pick(r, "nextReminderEligibleAtUtc", "NextReminderEligibleAtUtc") ?? null,
    invitationSentAtUtc: pick(r, "invitationSentAtUtc", "InvitationSentAtUtc") ?? null,
  });
}

const reminderSchema = z.object({
  requestId: guidSchema,
  organizationId: guidSchema,
  businessCustomerId: guidSchema,
  status: z.string(),
  reminderCount: z.number().int().nonnegative(),
  lastRemindedAtUtc: z.string().nullable().optional().default(null),
  nextReminderEligibleAtUtc: z.string().nullable().optional().default(null),
});

export type CustomerLinkReminderDto = z.infer<typeof reminderSchema>;

export async function remindCustomerLinkRequest(
  organizationId: string,
  requestId: string,
): Promise<CustomerLinkReminderDto> {
  const raw = await platformRequest<unknown>({
    path: `/api/v1/organizations/${organizationId}/customer-link-requests/${requestId}/remind`,
    method: "POST",
  });
  const r = (raw ?? {}) as Record<string, unknown>;
  return reminderSchema.parse({
    requestId: pick(r, "requestId", "RequestId"),
    organizationId: pick(r, "organizationId", "OrganizationId"),
    businessCustomerId: pick(r, "businessCustomerId", "BusinessCustomerId"),
    status: pick(r, "status", "Status"),
    reminderCount: pick(r, "reminderCount", "ReminderCount"),
    lastRemindedAtUtc: pick(r, "lastRemindedAtUtc", "LastRemindedAtUtc") ?? null,
    nextReminderEligibleAtUtc:
      pick(r, "nextReminderEligibleAtUtc", "NextReminderEligibleAtUtc") ?? null,
  });
}

export async function revokeCustomerLinkRequest(
  organizationId: string,
  requestId: string,
): Promise<void> {
  await platformRequest<unknown>({
    path: `/api/v1/organizations/${organizationId}/customer-link-requests/${requestId}/revoke`,
    method: "POST",
  });
}

export async function createCustomerLinkRequestForCustomer(input: {
  organizationId: string;
  platformBusinessCustomerId: string;
  publicUserId?: string | null;
  email?: string | null;
}): Promise<void> {
  await platformRequest<unknown>({
    path: `/api/v1/organizations/${input.organizationId}/customers/${input.platformBusinessCustomerId}/link-requests`,
    method: "POST",
    body: {
      publicUserId: input.publicUserId ?? null,
      email: input.email ?? null,
    },
  });
}

/**
 * Compact history row from GET .../customers/{platformBusinessCustomerId}/link-requests.
 * Full CustomerLinkRequestDto has more fields; UI only needs id/status/createdAtUtc.
 */
export const customerLinkRequestHistoryItemSchema = z.object({
  id: guidSchema,
  status: z.string(),
  createdAtUtc: z.string(),
  resolvedAtUtc: z.string().nullable().optional().default(null),
});

export type CustomerLinkRequestHistoryItemDto = z.infer<
  typeof customerLinkRequestHistoryItemSchema
>;

function normalizeHistoryItem(raw: unknown): unknown {
  if (!raw || typeof raw !== "object") {
    return raw;
  }
  const r = raw as Record<string, unknown>;
  return {
    id: pick(r, "id", "Id"),
    status: pick(r, "status", "Status"),
    createdAtUtc: pick(r, "createdAtUtc", "CreatedAtUtc"),
    resolvedAtUtc: pick(r, "resolvedAtUtc", "ResolvedAtUtc") ?? null,
  };
}

export async function listCustomerLinkRequestHistory(
  organizationId: string,
  platformBusinessCustomerId: string,
  signal?: AbortSignal,
): Promise<CustomerLinkRequestHistoryItemDto[]> {
  const raw = await platformRequest<unknown>({
    path: `/api/v1/organizations/${organizationId}/customers/${platformBusinessCustomerId}/link-requests`,
    signal,
  });
  const list = Array.isArray(raw) ? raw : [];
  return list.map((item) =>
    customerLinkRequestHistoryItemSchema.parse(normalizeHistoryItem(item)),
  );
}

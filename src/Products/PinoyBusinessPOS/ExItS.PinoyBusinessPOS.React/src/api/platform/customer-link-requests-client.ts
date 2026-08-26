import { z } from "zod";
import { platformRequest } from "@/api/platform/platform-http";

const guidSchema = z
  .string()
  .regex(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/);

function pick(raw: Record<string, unknown>, camel: string, pascal: string): unknown {
  return raw[camel] ?? raw[pascal];
}

export const pendingCustomerLinkRequestSchema = z.object({
  id: guidSchema,
  organizationId: guidSchema,
  organizationDisplayName: z.string(),
  businessCustomerId: guidSchema,
  status: z.string(),
  createdAtUtc: z.string(),
  expiresAtUtc: z.string(),
  targetPublicUserId: z.string().nullable().optional().default(null),
});

export type PendingCustomerLinkRequestDto = z.infer<typeof pendingCustomerLinkRequestSchema>;

function normalizeRequest(raw: unknown): unknown {
  if (!raw || typeof raw !== "object") {
    return raw;
  }
  const r = raw as Record<string, unknown>;
  return {
    id: pick(r, "id", "Id"),
    organizationId: pick(r, "organizationId", "OrganizationId"),
    organizationDisplayName: pick(r, "organizationDisplayName", "OrganizationDisplayName"),
    businessCustomerId: pick(r, "businessCustomerId", "BusinessCustomerId"),
    status: pick(r, "status", "Status"),
    createdAtUtc: pick(r, "createdAtUtc", "CreatedAtUtc"),
    expiresAtUtc: pick(r, "expiresAtUtc", "ExpiresAtUtc"),
    targetPublicUserId: pick(r, "targetPublicUserId", "TargetPublicUserId") ?? null,
  };
}

const BASE = "/api/v1/personal/customer-link-requests";

export async function listPendingCustomerLinkRequests(
  signal?: AbortSignal,
): Promise<PendingCustomerLinkRequestDto[]> {
  const raw = await platformRequest<unknown>({ path: BASE, signal });
  const list = Array.isArray(raw) ? raw : [];
  return list.map((item) => pendingCustomerLinkRequestSchema.parse(normalizeRequest(item)));
}

export async function listResolvedCustomerLinkRequests(
  signal?: AbortSignal,
): Promise<PendingCustomerLinkRequestDto[]> {
  const raw = await platformRequest<unknown>({ path: `${BASE}/history`, signal });
  const list = Array.isArray(raw) ? raw : [];
  return list.map((item) => pendingCustomerLinkRequestSchema.parse(normalizeRequest(item)));
}

export async function acceptCustomerLinkRequest(requestId: string): Promise<void> {
  await platformRequest<unknown>({
    method: "POST",
    path: `${BASE}/${requestId}/accept`,
  });
}

export async function declineCustomerLinkRequest(requestId: string): Promise<void> {
  await platformRequest<unknown>({
    method: "POST",
    path: `${BASE}/${requestId}/decline`,
  });
}

export async function blockBusinessFromCustomerLinkRequest(requestId: string): Promise<void> {
  await platformRequest<unknown>({
    method: "POST",
    path: `${BASE}/${requestId}/block-business`,
  });
}

const blockedBusinessSchema = z.object({
  organizationId: guidSchema,
  organizationDisplayName: z.string(),
  blockedAtUtc: z.string(),
});

export type PersonalBlockedBusinessDto = z.infer<typeof blockedBusinessSchema>;

export async function listPersonalBlockedBusinesses(
  signal?: AbortSignal,
): Promise<PersonalBlockedBusinessDto[]> {
  const raw = await platformRequest<unknown>({
    path: "/api/v1/personal/blocked-businesses",
    signal,
  });
  const list = Array.isArray(raw) ? raw : [];
  return list.map((item) => {
    const r = (item ?? {}) as Record<string, unknown>;
    return blockedBusinessSchema.parse({
      organizationId: pick(r, "organizationId", "OrganizationId"),
      organizationDisplayName: pick(r, "organizationDisplayName", "OrganizationDisplayName"),
      blockedAtUtc: pick(r, "blockedAtUtc", "BlockedAtUtc"),
    });
  });
}

export async function unblockPersonalBusiness(organizationId: string): Promise<void> {
  await platformRequest<unknown>({
    method: "POST",
    path: `/api/v1/personal/blocked-businesses/${organizationId}/unblock`,
  });
}

export async function disconnectLinkedMerchant(organizationId: string): Promise<void> {
  await platformRequest<unknown>({
    method: "POST",
    path: `/api/v1/personal/linked-merchants/by-organization/${organizationId}/disconnect`,
  });
}

export async function disconnectAndBlockLinkedMerchant(organizationId: string): Promise<void> {
  await platformRequest<unknown>({
    method: "POST",
    path: `/api/v1/personal/linked-merchants/by-organization/${organizationId}/disconnect-and-block`,
  });
}

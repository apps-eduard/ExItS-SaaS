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
  });
}

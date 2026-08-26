import { z } from "zod";
import { platformRequest } from "@/api/platform/platform-http";

const guidSchema = z
  .string()
  .regex(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/);

export const linkedMerchantSchema = z.object({
  linkedCustomerId: guidSchema,
  businessCustomerId: guidSchema,
  organizationId: guidSchema,
  organizationDisplayName: z.string(),
  customerDisplayName: z.string(),
  linkStatus: z.string(),
  linkedAtUtc: z.string(),
  canCustomerOrder: z.boolean().optional().default(false),
  canCustomerDelivery: z.boolean().optional().default(false),
});

export const linkedMerchantPagedSchema = z.object({
  items: z.array(linkedMerchantSchema),
  totalCount: z.number(),
  page: z.number(),
  pageSize: z.number(),
});

export const linkedMerchantOrderingCapabilitySchema = z.object({
  organizationId: guidSchema,
  canCustomerOrder: z.boolean(),
  canCustomerDelivery: z.boolean(),
  organizationDisplayName: z.string().optional().default(""),
});

export type LinkedMerchantDto = z.infer<typeof linkedMerchantSchema>;
export type LinkedMerchantPagedResult = z.infer<typeof linkedMerchantPagedSchema>;
export type LinkedMerchantOrderingCapabilityDto = z.infer<
  typeof linkedMerchantOrderingCapabilitySchema
>;

function normalizePaged(raw: unknown): unknown {
  if (!raw || typeof raw !== "object") {
    return raw;
  }
  const r = raw as Record<string, unknown>;
  return {
    items: r.items ?? r.Items ?? [],
    totalCount: r.totalCount ?? r.TotalCount ?? 0,
    page: r.page ?? r.Page ?? 1,
    pageSize: r.pageSize ?? r.PageSize ?? 20,
  };
}

function normalizeMerchant(raw: unknown): unknown {
  if (!raw || typeof raw !== "object") {
    return raw;
  }
  const r = raw as Record<string, unknown>;
  return {
    linkedCustomerId: r.linkedCustomerId ?? r.LinkedCustomerId,
    businessCustomerId: r.businessCustomerId ?? r.BusinessCustomerId,
    organizationId: r.organizationId ?? r.OrganizationId,
    organizationDisplayName: r.organizationDisplayName ?? r.OrganizationDisplayName,
    customerDisplayName: r.customerDisplayName ?? r.CustomerDisplayName,
    linkStatus: r.linkStatus ?? r.LinkStatus,
    linkedAtUtc: r.linkedAtUtc ?? r.LinkedAtUtc,
    canCustomerOrder: r.canCustomerOrder ?? r.CanCustomerOrder ?? false,
    canCustomerDelivery: r.canCustomerDelivery ?? r.CanCustomerDelivery ?? false,
  };
}

export async function listLinkedMerchants(
  page = 1,
  pageSize = 20,
  signal?: AbortSignal,
): Promise<LinkedMerchantPagedResult> {
  const raw = await platformRequest<unknown>({
    path: `/api/v1/personal/linked-merchants?page=${page}&pageSize=${pageSize}`,
    signal,
  });
  const normalized = normalizePaged(raw) as { items: unknown[] } & Record<string, unknown>;
  return linkedMerchantPagedSchema.parse({
    ...normalized,
    items: (normalized.items ?? []).map(normalizeMerchant),
  });
}

export async function getLinkedMerchantOrderingCapability(
  organizationId: string,
  signal?: AbortSignal,
): Promise<LinkedMerchantOrderingCapabilityDto> {
  const raw = await platformRequest<unknown>({
    path: `/api/v1/personal/linked-merchants/${organizationId}/ordering-capability`,
    signal,
  });
  const r = (raw ?? {}) as Record<string, unknown>;
  return linkedMerchantOrderingCapabilitySchema.parse({
    organizationId: r.organizationId ?? r.OrganizationId,
    canCustomerOrder: r.canCustomerOrder ?? r.CanCustomerOrder,
    canCustomerDelivery: r.canCustomerDelivery ?? r.CanCustomerDelivery,
    organizationDisplayName: r.organizationDisplayName ?? r.OrganizationDisplayName ?? "",
  });
}

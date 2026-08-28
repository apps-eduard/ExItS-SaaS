import { z } from "zod";
import { platformRequest } from "@/api/platform/platform-http";

const guidSchema = z
  .string()
  .regex(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/);

function pick(raw: Record<string, unknown>, camel: string, pascal: string): unknown {
  return raw[camel] ?? raw[pascal];
}

export const publicIdentitySchema = z.object({
  publicUserId: z.string(),
  qrPayload: z.string(),
  displayName: z.string(),
  status: z.string().optional().default("Active"),
});

export const organizationPublicIdentitySchema = z.object({
  publicOrganizationId: z.string(),
  qrPayload: z.string(),
  displayName: z.string(),
});

export const resolvedPublicUserSchema = z.object({
  publicUserId: z.string(),
  userIdentityId: guidSchema,
  displayName: z.string(),
  maskedEmail: z.string().nullable().optional().default(null),
  status: z.string(),
  isSelf: z.boolean(),
});

export const resolvedPublicOrganizationSchema = z.object({
  publicOrganizationId: z.string(),
  organizationId: guidSchema,
  displayName: z.string(),
  status: z.string(),
});

export const businessCustomerLinkResultSchema = z.object({
  customerId: guidSchema,
  linkRequestId: guidSchema.nullable().optional().default(null),
  linkStatus: z.string().nullable().optional().default(null),
});

export type PublicIdentityDto = z.infer<typeof publicIdentitySchema>;
export type OrganizationPublicIdentityDto = z.infer<typeof organizationPublicIdentitySchema>;
export type ResolvedPublicUserDto = z.infer<typeof resolvedPublicUserSchema>;
export type ResolvedPublicOrganizationDto = z.infer<typeof resolvedPublicOrganizationSchema>;
export type BusinessCustomerLinkResultDto = z.infer<typeof businessCustomerLinkResultSchema>;

export const customerLinkEligibilityStatuses = [
  "Eligible",
  "OwnerOfOrganization",
  "OrganizationStaff",
  "AlreadyLinked",
  "PendingInvitation",
  "BlockedOrUnavailable",
  "InvalidTarget",
] as const;

export type CustomerLinkEligibilityStatus = (typeof customerLinkEligibilityStatuses)[number];

export const customerLinkEligibilitySchema = z.object({
  status: z.enum(customerLinkEligibilityStatuses),
  message: z.string(),
  publicUserId: z.string().nullable().optional().default(null),
  displayName: z.string().nullable().optional().default(null),
  userIdentityId: guidSchema.nullable().optional().default(null),
  existingBusinessCustomerId: guidSchema.nullable().optional().default(null),
  existingPendingRequestId: guidSchema.nullable().optional().default(null),
});

export type CustomerLinkEligibilityDto = z.infer<typeof customerLinkEligibilitySchema>;

export async function getMyPublicIdentity(signal?: AbortSignal): Promise<PublicIdentityDto> {
  const raw = await platformRequest<unknown>({ path: "/api/v1/me/public-identity", signal });
  const r = (raw ?? {}) as Record<string, unknown>;
  return publicIdentitySchema.parse({
    publicUserId: pick(r, "publicUserId", "PublicUserId"),
    qrPayload: pick(r, "qrPayload", "QrPayload") ?? pick(r, "qrCodePayload", "QrCodePayload"),
    displayName: pick(r, "displayName", "DisplayName") ?? "",
    status: pick(r, "status", "Status") ?? "Active",
  });
}

export async function getOrganizationPublicIdentity(
  organizationId: string,
  signal?: AbortSignal,
): Promise<OrganizationPublicIdentityDto> {
  const raw = await platformRequest<unknown>({
    path: `/api/v1/organizations/${organizationId}/public-identity`,
    signal,
  });
  const r = (raw ?? {}) as Record<string, unknown>;
  return organizationPublicIdentitySchema.parse({
    publicOrganizationId: pick(r, "publicOrganizationId", "PublicOrganizationId"),
    qrPayload: pick(r, "qrPayload", "QrPayload") ?? pick(r, "qrCodePayload", "QrCodePayload"),
    displayName: pick(r, "displayName", "DisplayName") ?? "",
  });
}

export async function resolvePublicUserId(
  publicUserIdOrQrPayload: string,
  purpose?: string,
  signal?: AbortSignal,
): Promise<ResolvedPublicUserDto> {
  const raw = await platformRequest<unknown>({
    method: "POST",
    path: "/api/v1/users/resolve-public-id",
    body: { publicUserIdOrQrPayload, purpose: purpose ?? null },
    signal,
  });
  const r = (raw ?? {}) as Record<string, unknown>;
  return resolvedPublicUserSchema.parse({
    publicUserId: pick(r, "publicUserId", "PublicUserId"),
    userIdentityId: pick(r, "userIdentityId", "UserIdentityId"),
    displayName: pick(r, "displayName", "DisplayName"),
    maskedEmail: pick(r, "maskedEmail", "MaskedEmail") ?? null,
    status: pick(r, "status", "Status"),
    isSelf: Boolean(pick(r, "isSelf", "IsSelf")),
  });
}

export async function evaluateCustomerLinkEligibility(
  organizationId: string,
  body: {
    publicUserIdOrQrPayload: string;
    businessCustomerId?: string | null;
  },
  signal?: AbortSignal,
): Promise<CustomerLinkEligibilityDto> {
  const raw = await platformRequest<unknown>({
    method: "POST",
    path: `/api/v1/organizations/${organizationId}/customers/link-eligibility`,
    body: {
      publicUserIdOrQrPayload: body.publicUserIdOrQrPayload,
      businessCustomerId: body.businessCustomerId ?? null,
    },
    signal,
  });
  const r = (raw ?? {}) as Record<string, unknown>;
  return customerLinkEligibilitySchema.parse({
    status: pick(r, "status", "Status"),
    message: pick(r, "message", "Message") ?? "",
    publicUserId: pick(r, "publicUserId", "PublicUserId") ?? null,
    displayName: pick(r, "displayName", "DisplayName") ?? null,
    userIdentityId: pick(r, "userIdentityId", "UserIdentityId") ?? null,
    existingBusinessCustomerId:
      pick(r, "existingBusinessCustomerId", "ExistingBusinessCustomerId") ?? null,
    existingPendingRequestId:
      pick(r, "existingPendingRequestId", "ExistingPendingRequestId") ?? null,
  });
}

export async function resolvePublicOrganizationId(
  publicOrganizationIdOrQrPayload: string,
  purpose?: string,
  signal?: AbortSignal,
): Promise<ResolvedPublicOrganizationDto> {
  const raw = await platformRequest<unknown>({
    method: "POST",
    path: "/api/v1/organizations/resolve-public-id",
    body: { publicOrganizationIdOrQrPayload, purpose: purpose ?? null },
    signal,
  });
  const r = (raw ?? {}) as Record<string, unknown>;
  return resolvedPublicOrganizationSchema.parse({
    publicOrganizationId: pick(r, "publicOrganizationId", "PublicOrganizationId"),
    organizationId: pick(r, "organizationId", "OrganizationId"),
    displayName: pick(r, "displayName", "DisplayName"),
    status: pick(r, "status", "Status"),
  });
}

export async function createBusinessCustomerWithPersonalLink(
  organizationId: string,
  body: {
    displayName: string;
    email?: string | null;
    phone?: string | null;
    notes?: string | null;
    owningProductCode?: string | null;
    publicUserId?: string | null;
    targetUserIdentityId?: string | null;
  },
  signal?: AbortSignal,
): Promise<BusinessCustomerLinkResultDto> {
  const raw = await platformRequest<unknown>({
    method: "POST",
    path: `/api/v1/organizations/${organizationId}/customers/with-personal-link`,
    body: {
      displayName: body.displayName,
      email: body.email ?? null,
      phone: body.phone ?? null,
      notes: body.notes ?? null,
      owningProductCode: body.owningProductCode ?? "PinoyBusinessPOS",
      publicUserId: body.publicUserId ?? null,
      targetUserIdentityId: body.targetUserIdentityId ?? null,
    },
    signal,
  });
  const r = (raw ?? {}) as Record<string, unknown>;
  const customer = (pick(r, "customer", "Customer") ?? r) as Record<string, unknown>;
  const linkRequest = (pick(r, "linkRequest", "LinkRequest") ?? null) as Record<
    string,
    unknown
  > | null;
  return businessCustomerLinkResultSchema.parse({
    customerId: pick(customer, "id", "Id") ?? pick(r, "customerId", "CustomerId"),
    linkRequestId: linkRequest
      ? (pick(linkRequest, "id", "Id") ?? null)
      : (pick(r, "linkRequestId", "LinkRequestId") ?? null),
    linkStatus: linkRequest
      ? (pick(linkRequest, "status", "Status") ?? null)
      : (pick(r, "linkStatus", "LinkStatus") ?? null),
  });
}

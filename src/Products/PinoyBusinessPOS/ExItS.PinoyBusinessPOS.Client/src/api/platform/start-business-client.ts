import { z } from "zod";
import { POS_PRODUCT_CODE } from "@/api/platform/browser-session";
import { platformRequest } from "@/api/platform/platform-http";

const guidSchema = z
  .string()
  .regex(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/);

function pick(raw: Record<string, unknown>, camel: string, pascal: string): unknown {
  return raw[camel] ?? raw[pascal];
}

export const onboardingBusinessTypeSchema = z.object({
  id: guidSchema,
  code: z.string(),
  name: z.string(),
  description: z.string().nullable().optional().default(null),
  status: z.string(),
  sortOrder: z.number().int(),
});

export type OnboardingBusinessTypeDto = z.infer<typeof onboardingBusinessTypeSchema>;

export const personalProfileSchema = z.object({
  userIdentityId: guidSchema,
  accountProfileId: guidSchema,
  username: z.string(),
  displayName: z.string(),
  email: z.string(),
  accountClass: z.string(),
  status: z.string(),
  publicUserId: z.string().nullable().optional().default(null),
  qrPayload: z.string().nullable().optional().default(null),
  phone: z.string().nullable().optional().default(null),
});

export type PersonalProfileDto = z.infer<typeof personalProfileSchema>;

/** Safe client result — never retains SessionToken in app state. */
export const startBusinessResultSchema = z.object({
  organizationId: guidSchema,
  membershipId: guidSchema,
  organizationAccountProfileId: guidSchema,
  sessionId: guidSchema,
  accountClass: z.string(),
  allowedScope: z.string(),
  selectedOrganizationId: guidSchema.nullable().optional().default(null),
  subscriptionId: guidSchema.nullable().optional().default(null),
  entitlementSnapshotVersion: z.number().int().nullable().optional().default(null),
  productAccessAssignmentId: guidSchema.nullable().optional().default(null),
  productLocalRoleGrantId: guidSchema.nullable().optional().default(null),
  productLocalRoleCode: z.string().nullable().optional().default(null),
  organizationOwnerGranted: z.boolean(),
  posEntitlementActivated: z.boolean(),
  posOwnerRoleGranted: z.boolean(),
  productCode: z.string(),
  primaryBusinessTypeId: guidSchema.nullable().optional().default(null),
  primaryBranchId: guidSchema.nullable().optional().default(null),
});

export type StartBusinessResultDto = z.infer<typeof startBusinessResultSchema>;

export type StartBusinessRequest = {
  displayName: string;
  slug: string;
  primaryBusinessTypeId: string;
  productCode?: string;
  planKey?: string | null;
  billingCycle?: "Monthly" | "Annual";
  startAsTrial?: boolean;
  payNow?: boolean;
  activatePosEntitlement?: boolean;
  activateProductAccess?: boolean;
  assignPosOwnerRole?: boolean;
  useMyContactDetails?: boolean;
  contactEmail?: string | null;
  contactPhone?: string | null;
  addressLine1?: string | null;
  city?: string | null;
  region?: string | null;
  postalCode?: string | null;
  countryCode?: string | null;
};

function normalizeBusinessType(raw: unknown): unknown {
  if (!raw || typeof raw !== "object") return raw;
  const r = raw as Record<string, unknown>;
  return {
    id: pick(r, "id", "Id"),
    code: pick(r, "code", "Code"),
    name: pick(r, "name", "Name"),
    description: pick(r, "description", "Description") ?? null,
    status: pick(r, "status", "Status"),
    sortOrder: Number(pick(r, "sortOrder", "SortOrder") ?? 0),
  };
}

function normalizeProfile(raw: unknown): unknown {
  if (!raw || typeof raw !== "object") return raw;
  const r = raw as Record<string, unknown>;
  return {
    userIdentityId: pick(r, "userIdentityId", "UserIdentityId"),
    accountProfileId: pick(r, "accountProfileId", "AccountProfileId"),
    username: pick(r, "username", "Username"),
    displayName: pick(r, "displayName", "DisplayName"),
    email: pick(r, "email", "Email"),
    accountClass: pick(r, "accountClass", "AccountClass"),
    status: pick(r, "status", "Status"),
    publicUserId: pick(r, "publicUserId", "PublicUserId") ?? null,
    qrPayload: pick(r, "qrPayload", "QrPayload") ?? null,
    phone: pick(r, "phone", "Phone") ?? null,
  };
}

function normalizeStartBusinessResult(raw: unknown): unknown {
  if (!raw || typeof raw !== "object") return raw;
  const r = raw as Record<string, unknown>;
  // Intentionally drop SessionToken — cookie is set by Platform; do not keep token in JS.
  return {
    organizationId: pick(r, "organizationId", "OrganizationId"),
    membershipId: pick(r, "membershipId", "MembershipId"),
    organizationAccountProfileId: pick(
      r,
      "organizationAccountProfileId",
      "OrganizationAccountProfileId",
    ),
    sessionId: pick(r, "sessionId", "SessionId"),
    accountClass: pick(r, "accountClass", "AccountClass"),
    allowedScope: pick(r, "allowedScope", "AllowedScope"),
    selectedOrganizationId: pick(r, "selectedOrganizationId", "SelectedOrganizationId") ?? null,
    subscriptionId: pick(r, "subscriptionId", "SubscriptionId") ?? null,
    entitlementSnapshotVersion:
      pick(r, "entitlementSnapshotVersion", "EntitlementSnapshotVersion") ?? null,
    productAccessAssignmentId:
      pick(r, "productAccessAssignmentId", "ProductAccessAssignmentId") ?? null,
    productLocalRoleGrantId: pick(r, "productLocalRoleGrantId", "ProductLocalRoleGrantId") ?? null,
    productLocalRoleCode: pick(r, "productLocalRoleCode", "ProductLocalRoleCode") ?? null,
    organizationOwnerGranted: Boolean(
      pick(r, "organizationOwnerGranted", "OrganizationOwnerGranted"),
    ),
    posEntitlementActivated: Boolean(pick(r, "posEntitlementActivated", "PosEntitlementActivated")),
    posOwnerRoleGranted: Boolean(pick(r, "posOwnerRoleGranted", "PosOwnerRoleGranted")),
    productCode: pick(r, "productCode", "ProductCode"),
    primaryBusinessTypeId: pick(r, "primaryBusinessTypeId", "PrimaryBusinessTypeId") ?? null,
    primaryBranchId: pick(r, "primaryBranchId", "PrimaryBranchId") ?? null,
  };
}

export async function listOnboardingBusinessTypes(
  signal?: AbortSignal,
): Promise<OnboardingBusinessTypeDto[]> {
  const raw = await platformRequest<unknown>({
    path: "/api/v1/personal/onboarding/business-types",
    signal,
  });
  const items = Array.isArray(raw) ? raw : [];
  return items
    .map((item) => onboardingBusinessTypeSchema.parse(normalizeBusinessType(item)))
    .filter((t) => t.status.localeCompare("Active", undefined, { sensitivity: "accent" }) === 0)
    .sort((a, b) => a.sortOrder - b.sortOrder || a.name.localeCompare(b.name));
}

export async function getPersonalProfile(signal?: AbortSignal): Promise<PersonalProfileDto> {
  const raw = await platformRequest<unknown>({
    path: "/api/v1/personal/profile",
    signal,
  });
  return personalProfileSchema.parse(normalizeProfile(raw));
}

export async function startBusiness(
  request: StartBusinessRequest,
  signal?: AbortSignal,
): Promise<StartBusinessResultDto> {
  const body = {
    displayName: request.displayName,
    slug: request.slug,
    primaryBusinessTypeId: request.primaryBusinessTypeId,
    productCode: request.productCode ?? POS_PRODUCT_CODE,
    planKey: request.planKey ?? null,
    billingCycle: request.billingCycle ?? "Monthly",
    startAsTrial: request.startAsTrial ?? true,
    payNow: request.payNow ?? false,
    activatePosEntitlement: request.activatePosEntitlement ?? true,
    activateProductAccess: request.activateProductAccess ?? true,
    assignPosOwnerRole: request.assignPosOwnerRole ?? true,
    useMyContactDetails: request.useMyContactDetails ?? false,
    contactEmail: request.contactEmail ?? null,
    contactPhone: request.contactPhone ?? null,
    addressLine1: request.addressLine1 ?? null,
    city: request.city ?? null,
    region: request.region ?? null,
    postalCode: request.postalCode ?? null,
    countryCode: request.countryCode ?? null,
  };

  const raw = await platformRequest<unknown>({
    method: "POST",
    path: "/api/v1/personal/start-business",
    body,
    signal,
  });
  return startBusinessResultSchema.parse(normalizeStartBusinessResult(raw));
}

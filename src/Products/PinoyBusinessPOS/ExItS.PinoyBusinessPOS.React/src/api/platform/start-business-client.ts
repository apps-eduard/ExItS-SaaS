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

export const personalAddressSchema = z.object({
  id: guidSchema,
  addressType: z.string(),
  country: z.string().nullable().optional().default(null),
  addressLine1: z.string().nullable().optional().default(null),
  addressLine2: z.string().nullable().optional().default(null),
  barangay: z.string().nullable().optional().default(null),
  cityMunicipality: z.string().nullable().optional().default(null),
  provinceState: z.string().nullable().optional().default(null),
  postalCode: z.string().nullable().optional().default(null),
  isPrimary: z.boolean().optional().default(false),
  countryCode: z.string().nullable().optional().default(null),
});

export type PersonalAddressDto = z.infer<typeof personalAddressSchema>;

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
  firstName: z.string().nullable().optional().default(null),
  middleName: z.string().nullable().optional().default(null),
  lastName: z.string().nullable().optional().default(null),
  dateOfBirth: z.string().nullable().optional().default(null),
  gender: z.string().nullable().optional().default(null),
  nationality: z.string().nullable().optional().default(null),
  profilePhotoUrl: z.string().nullable().optional().default(null),
  alternativeMobile: z.string().nullable().optional().default(null),
  country: z.string().nullable().optional().default(null),
  addressLine1: z.string().nullable().optional().default(null),
  addressLine2: z.string().nullable().optional().default(null),
  barangay: z.string().nullable().optional().default(null),
  cityMunicipality: z.string().nullable().optional().default(null),
  province: z.string().nullable().optional().default(null),
  provinceState: z.string().nullable().optional().default(null),
  postalCode: z.string().nullable().optional().default(null),
  isPrimary: z.boolean().optional().default(false),
  addresses: z.array(personalAddressSchema).optional().default([]),
  showProfilePhoto: z.string().optional().default("Private"),
  showDisplayName: z.string().optional().default("Connections"),
  showCity: z.string().optional().default("Private"),
  showMobile: z.string().optional().default("Private"),
  showEmail: z.string().optional().default("Private"),
  completionPercent: z.number().int().optional().default(0),
  missingForBase: z.array(z.string()).optional().default([]),
  missingForStaff: z.array(z.string()).optional().default([]),
  missingForCustomer: z.array(z.string()).optional().default([]),
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
  paymentTransactionId: guidSchema.nullable().optional().default(null),
  paymentReferenceNumber: z.string().nullable().optional().default(null),
  requiresCheckout: z.boolean().optional().default(false),
  reusedExistingOrganization: z.boolean().optional().default(false),
});

export type StartBusinessResultDto = z.infer<typeof startBusinessResultSchema>;

export type StartBusinessBillingCycle = "Monthly" | "Quarterly" | "SixMonths" | "Annual";

export type StartBusinessRequest = {
  displayName: string;
  slug: string;
  primaryBusinessTypeId: string;
  productCode?: string;
  planKey?: string | null;
  billingCycle?: StartBusinessBillingCycle;
  startAsTrial?: boolean;
  payNow?: boolean;
  paidPaymentTransactionId?: string | null;
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

function normalizeAddress(raw: unknown): unknown {
  if (!raw || typeof raw !== "object") return raw;
  const r = raw as Record<string, unknown>;
  return {
    id: pick(r, "id", "Id"),
    addressType: pick(r, "addressType", "AddressType"),
    country: pick(r, "country", "Country") ?? null,
    addressLine1: pick(r, "addressLine1", "AddressLine1") ?? null,
    addressLine2: pick(r, "addressLine2", "AddressLine2") ?? null,
    barangay: pick(r, "barangay", "Barangay") ?? null,
    cityMunicipality: pick(r, "cityMunicipality", "CityMunicipality") ?? null,
    provinceState: pick(r, "provinceState", "ProvinceState") ?? pick(r, "province", "Province") ?? null,
    postalCode: pick(r, "postalCode", "PostalCode") ?? null,
    isPrimary: Boolean(pick(r, "isPrimary", "IsPrimary") ?? false),
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
    firstName: pick(r, "firstName", "FirstName") ?? null,
    middleName: pick(r, "middleName", "MiddleName") ?? null,
    lastName: pick(r, "lastName", "LastName") ?? null,
    dateOfBirth: pick(r, "dateOfBirth", "DateOfBirth") ?? null,
    gender: pick(r, "gender", "Gender") ?? null,
    nationality: pick(r, "nationality", "Nationality") ?? null,
    profilePhotoUrl: pick(r, "profilePhotoUrl", "ProfilePhotoUrl") ?? null,
    alternativeMobile: pick(r, "alternativeMobile", "AlternativeMobile") ?? null,
    country: pick(r, "country", "Country") ?? null,
    addressLine1: pick(r, "addressLine1", "AddressLine1") ?? null,
    addressLine2: pick(r, "addressLine2", "AddressLine2") ?? null,
    barangay: pick(r, "barangay", "Barangay") ?? null,
    cityMunicipality: pick(r, "cityMunicipality", "CityMunicipality") ?? null,
    province: pick(r, "province", "Province") ?? pick(r, "provinceState", "ProvinceState") ?? null,
    provinceState: pick(r, "provinceState", "ProvinceState") ?? pick(r, "province", "Province") ?? null,
    postalCode: pick(r, "postalCode", "PostalCode") ?? null,
    isPrimary: Boolean(pick(r, "isPrimary", "IsPrimary") ?? pick(r, "isPrimaryAddress", "IsPrimaryAddress") ?? false),
    addresses: Array.isArray(pick(r, "addresses", "Addresses"))
      ? (pick(r, "addresses", "Addresses") as unknown[]).map((item) => normalizeAddress(item))
      : [],
    showProfilePhoto: pick(r, "showProfilePhoto", "ShowProfilePhoto") ?? "Private",
    showDisplayName: pick(r, "showDisplayName", "ShowDisplayName") ?? "Connections",
    showCity: pick(r, "showCity", "ShowCity") ?? "Private",
    showMobile: pick(r, "showMobile", "ShowMobile") ?? "Private",
    showEmail: pick(r, "showEmail", "ShowEmail") ?? "Private",
    completionPercent: Number(pick(r, "completionPercent", "CompletionPercent") ?? 0),
    missingForBase: pick(r, "missingForBase", "MissingForBase") ?? [],
    missingForStaff: pick(r, "missingForStaff", "MissingForStaff") ?? [],
    missingForCustomer: pick(r, "missingForCustomer", "MissingForCustomer") ?? [],
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
    paymentTransactionId: pick(r, "paymentTransactionId", "PaymentTransactionId") ?? null,
    paymentReferenceNumber: pick(r, "paymentReferenceNumber", "PaymentReferenceNumber") ?? null,
    requiresCheckout: Boolean(pick(r, "requiresCheckout", "RequiresCheckout") ?? false),
    reusedExistingOrganization: Boolean(
      pick(r, "reusedExistingOrganization", "ReusedExistingOrganization") ?? false,
    ),
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

export type UpdatePersonalProfileRequest = {
  displayName: string;
  firstName?: string | null;
  middleName?: string | null;
  lastName?: string | null;
  phone?: string | null;
  alternativeMobile?: string | null;
  dateOfBirth?: string | null;
  gender?: string | null;
  nationality?: string | null;
  profilePhotoUrl?: string | null;
  country?: string | null;
  addressLine1?: string | null;
  addressLine2?: string | null;
  barangay?: string | null;
  cityMunicipality?: string | null;
  province?: string | null;
  region?: string | null;
  postalCode?: string | null;
  cityPsgcCode?: string | null;
  showProfilePhoto?: string;
  showDisplayName?: string;
  showCity?: string;
  showMobile?: string;
  showEmail?: string;
  clearDateOfBirth?: boolean;
};

export type SavePersonalAddressRequest = {
  addressType: string;
  country?: string | null;
  addressLine1?: string | null;
  addressLine2?: string | null;
  barangay?: string | null;
  cityMunicipality?: string | null;
  provinceState?: string | null;
  postalCode?: string | null;
  isPrimary: boolean;
};

export async function savePersonalAddress(
  request: SavePersonalAddressRequest,
  addressId?: string,
  signal?: AbortSignal,
): Promise<PersonalProfileDto> {
  const raw = await platformRequest<unknown>({
    method: addressId ? "PUT" : "POST",
    path: addressId
      ? `/api/v1/personal/profile/addresses/${addressId}`
      : "/api/v1/personal/profile/addresses",
    body: request,
    signal,
  });
  return personalProfileSchema.parse(normalizeProfile(raw));
}

export async function deletePersonalAddress(
  addressId: string,
  signal?: AbortSignal,
): Promise<PersonalProfileDto> {
  const raw = await platformRequest<unknown>({
    method: "DELETE",
    path: `/api/v1/personal/profile/addresses/${addressId}`,
    signal,
  });
  return personalProfileSchema.parse(normalizeProfile(raw));
}

export async function setPersonalAddressPrimary(
  addressId: string,
  signal?: AbortSignal,
): Promise<PersonalProfileDto> {
  const raw = await platformRequest<unknown>({
    method: "POST",
    path: `/api/v1/personal/profile/addresses/${addressId}/primary`,
    signal,
  });
  return personalProfileSchema.parse(normalizeProfile(raw));
}

export async function uploadPersonalProfilePhoto(
  file: File,
  signal?: AbortSignal,
): Promise<PersonalProfileDto> {
  const formData = new FormData();
  formData.append("file", file);
  const raw = await platformRequest<unknown>({
    method: "POST",
    path: "/api/v1/personal/profile/photo",
    formData,
    signal,
  });
  return personalProfileSchema.parse(normalizeProfile(raw));
}

export async function updatePersonalProfile(
  request: UpdatePersonalProfileRequest,
  signal?: AbortSignal,
): Promise<PersonalProfileDto> {
  const raw = await platformRequest<unknown>({
    method: "PUT",
    path: "/api/v1/personal/profile",
    body: request,
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
    paidPaymentTransactionId: request.paidPaymentTransactionId ?? null,
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

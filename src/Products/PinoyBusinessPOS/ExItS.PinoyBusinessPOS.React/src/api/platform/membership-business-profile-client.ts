import { z } from "zod";
import { platformRequest } from "@/api/platform/platform-http";

const guidSchema = z
  .string()
  .regex(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/);

function pick(raw: Record<string, unknown>, camel: string, pascal: string): unknown {
  return raw[camel] ?? raw[pascal];
}

const membershipBusinessProfileSchema = z.object({
  membershipId: guidSchema,
  organizationId: guidSchema,
  userId: guidSchema,
  displayName: z.string(),
  role: z.string(),
  roleDisplay: z.string().nullable().optional().default(null),
  department: z.string().nullable().optional().default(null),
  jobTitle: z.string().nullable().optional().default(null),
  workPhone: z.string().nullable().optional().default(null),
  workEmail: z.string().nullable().optional().default(null),
  isBusinessContact: z.boolean(),
  staffId: z.string().nullable().optional().default(null),
  country: z.string().nullable().optional().default(null),
  addressLine1: z.string().nullable().optional().default(null),
  addressLine2: z.string().nullable().optional().default(null),
  barangay: z.string().nullable().optional().default(null),
  cityMunicipality: z.string().nullable().optional().default(null),
  provinceState: z.string().nullable().optional().default(null),
  postalCode: z.string().nullable().optional().default(null),
  profileDetailsCaptured: z.boolean().optional().default(false),
  firstName: z.string().nullable().optional().default(null),
  middleName: z.string().nullable().optional().default(null),
  lastName: z.string().nullable().optional().default(null),
  dateOfBirth: z.string().nullable().optional().default(null),
  gender: z.string().nullable().optional().default(null),
  nationality: z.string().nullable().optional().default(null),
  profilePhotoUrl: z.string().nullable().optional().default(null),
  mobileNumber: z.string().nullable().optional().default(null),
  email: z.string().nullable().optional().default(null),
  staffDisplayName: z.string().nullable().optional().default(null),
  personal: z.object({
    firstName: z.string().nullable().optional().default(null),
    middleName: z.string().nullable().optional().default(null),
    lastName: z.string().nullable().optional().default(null),
    dateOfBirth: z.string().nullable().optional().default(null),
    gender: z.string().nullable().optional().default(null),
    nationality: z.string().nullable().optional().default(null),
    profilePhotoUrl: z.string().nullable().optional().default(null),
    mobileNumber: z.string().nullable().optional().default(null),
    email: z.string().nullable().optional().default(null),
    country: z.string().nullable().optional().default(null),
    addressLine1: z.string().nullable().optional().default(null),
    addressLine2: z.string().nullable().optional().default(null),
    barangay: z.string().nullable().optional().default(null),
    cityMunicipality: z.string().nullable().optional().default(null),
    provinceState: z.string().nullable().optional().default(null),
    postalCode: z.string().nullable().optional().default(null),
  }).nullable().optional().default(null),
  updatedAtUtc: z.string(),
});

export type MembershipBusinessProfile = z.infer<typeof membershipBusinessProfileSchema>;

function normalizePersonal(raw: unknown): unknown {
  if (!raw || typeof raw !== "object") return raw ?? null;
  const r = raw as Record<string, unknown>;
  return {
    firstName: pick(r, "firstName", "FirstName") ?? null,
    middleName: pick(r, "middleName", "MiddleName") ?? null,
    lastName: pick(r, "lastName", "LastName") ?? null,
    dateOfBirth: pick(r, "dateOfBirth", "DateOfBirth") ?? null,
    gender: pick(r, "gender", "Gender") ?? null,
    nationality: pick(r, "nationality", "Nationality") ?? null,
    profilePhotoUrl: pick(r, "profilePhotoUrl", "ProfilePhotoUrl") ?? null,
    mobileNumber: pick(r, "mobileNumber", "MobileNumber") ?? null,
    email: pick(r, "email", "Email") ?? null,
    country: pick(r, "country", "Country") ?? null,
    addressLine1: pick(r, "addressLine1", "AddressLine1") ?? null,
    addressLine2: pick(r, "addressLine2", "AddressLine2") ?? null,
    barangay: pick(r, "barangay", "Barangay") ?? null,
    cityMunicipality: pick(r, "cityMunicipality", "CityMunicipality") ?? null,
    provinceState: pick(r, "provinceState", "ProvinceState") ?? null,
    postalCode: pick(r, "postalCode", "PostalCode") ?? null,
  };
}

function normalizeProfile(raw: unknown): unknown {
  if (!raw || typeof raw !== "object") return raw;
  const r = raw as Record<string, unknown>;
  return {
    membershipId: pick(r, "membershipId", "MembershipId"),
    organizationId: pick(r, "organizationId", "OrganizationId"),
    userId: pick(r, "userId", "UserId"),
    displayName: pick(r, "displayName", "DisplayName") ?? "",
    role: pick(r, "role", "Role") ?? "",
    roleDisplay: pick(r, "roleDisplay", "RoleDisplay") ?? null,
    department: pick(r, "department", "Department") ?? null,
    jobTitle: pick(r, "jobTitle", "JobTitle") ?? null,
    workPhone: pick(r, "workPhone", "WorkPhone") ?? null,
    workEmail: pick(r, "workEmail", "WorkEmail") ?? null,
    isBusinessContact: pick(r, "isBusinessContact", "IsBusinessContact") ?? false,
    staffId: pick(r, "staffId", "StaffId") ?? null,
    country: pick(r, "country", "Country") ?? null,
    addressLine1: pick(r, "addressLine1", "AddressLine1") ?? null,
    addressLine2: pick(r, "addressLine2", "AddressLine2") ?? null,
    barangay: pick(r, "barangay", "Barangay") ?? null,
    cityMunicipality: pick(r, "cityMunicipality", "CityMunicipality") ?? null,
    provinceState: pick(r, "provinceState", "ProvinceState") ?? null,
    postalCode: pick(r, "postalCode", "PostalCode") ?? null,
    profileDetailsCaptured: pick(r, "profileDetailsCaptured", "ProfileDetailsCaptured") ?? false,
    firstName: pick(r, "firstName", "FirstName") ?? null,
    middleName: pick(r, "middleName", "MiddleName") ?? null,
    lastName: pick(r, "lastName", "LastName") ?? null,
    dateOfBirth: pick(r, "dateOfBirth", "DateOfBirth") ?? null,
    gender: pick(r, "gender", "Gender") ?? null,
    nationality: pick(r, "nationality", "Nationality") ?? null,
    profilePhotoUrl: pick(r, "profilePhotoUrl", "ProfilePhotoUrl") ?? null,
    mobileNumber: pick(r, "mobileNumber", "MobileNumber") ?? null,
    email: pick(r, "email", "Email") ?? null,
    staffDisplayName: pick(r, "staffDisplayName", "StaffDisplayName") ?? null,
    personal: normalizePersonal(pick(r, "personal", "Personal")),
    updatedAtUtc: pick(r, "updatedAtUtc", "UpdatedAtUtc"),
  };
}

export async function getMembershipBusinessProfile(
  organizationId: string,
  membershipId: string,
  signal?: AbortSignal,
): Promise<MembershipBusinessProfile> {
  const raw = await platformRequest<unknown>({
    path: `/api/v1/platform/organizations/${organizationId}/members/${membershipId}/business-profile`,
    signal,
  });
  return membershipBusinessProfileSchema.parse(normalizeProfile(raw));
}

export type UpdateMembershipBusinessProfileRequest = {
  department?: string | null;
  jobTitle?: string | null;
  workPhone?: string | null;
  workEmail?: string | null;
  isBusinessContact: boolean;
  staffId?: string | null;
  country?: string | null;
  addressLine1?: string | null;
  addressLine2?: string | null;
  barangay?: string | null;
  cityMunicipality?: string | null;
  provinceState?: string | null;
  postalCode?: string | null;
  firstName?: string | null;
  middleName?: string | null;
  lastName?: string | null;
  dateOfBirth?: string | null;
  gender?: string | null;
  nationality?: string | null;
  profilePhotoUrl?: string | null;
  mobileNumber?: string | null;
  email?: string | null;
  staffDisplayName?: string | null;
};

export async function updateMembershipBusinessProfile(
  organizationId: string,
  membershipId: string,
  body: UpdateMembershipBusinessProfileRequest,
  signal?: AbortSignal,
): Promise<MembershipBusinessProfile> {
  const raw = await platformRequest<unknown>({
    method: "PUT",
    path: `/api/v1/platform/organizations/${organizationId}/members/${membershipId}/business-profile`,
    body,
    signal,
  });
  return membershipBusinessProfileSchema.parse(normalizeProfile(raw));
}

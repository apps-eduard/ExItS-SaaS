import { z } from "zod";
import { platformRequest } from "@/api/platform/platform-http";

const guidSchema = z
  .string()
  .regex(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/);

const optionalText = z.string().nullable().optional().default(null);

function pick(raw: Record<string, unknown>, camel: string, pascal: string): unknown {
  return raw[camel] ?? raw[pascal];
}

export const linkedCustomerPersonalProfileSchema = z.object({
  userIdentityId: guidSchema,
  firstName: optionalText,
  lastName: optionalText,
  mobileNumber: optionalText,
  email: optionalText,
  cityMunicipality: optionalText,
  provinceState: optionalText,
  country: optionalText,
  displayName: optionalText,
  addressLine1: optionalText,
  addressLine2: optionalText,
  barangay: optionalText,
  postalCode: optionalText,
  profilePhotoUrl: optionalText,
  gender: optionalText,
});

export type LinkedCustomerPersonalProfile = z.infer<typeof linkedCustomerPersonalProfileSchema>;

function normalize(raw: unknown): unknown {
  const record = (raw ?? {}) as Record<string, unknown>;
  return {
    userIdentityId: pick(record, "userIdentityId", "UserIdentityId"),
    firstName: pick(record, "firstName", "FirstName") ?? null,
    lastName: pick(record, "lastName", "LastName") ?? null,
    mobileNumber: pick(record, "mobileNumber", "MobileNumber") ?? null,
    email: pick(record, "email", "Email") ?? null,
    cityMunicipality: pick(record, "cityMunicipality", "CityMunicipality") ?? null,
    provinceState: pick(record, "provinceState", "ProvinceState") ?? null,
    country: pick(record, "country", "Country") ?? null,
    displayName: pick(record, "displayName", "DisplayName") ?? null,
    addressLine1: pick(record, "addressLine1", "AddressLine1") ?? null,
    addressLine2: pick(record, "addressLine2", "AddressLine2") ?? null,
    barangay: pick(record, "barangay", "Barangay") ?? null,
    postalCode: pick(record, "postalCode", "PostalCode") ?? null,
    profilePhotoUrl: pick(record, "profilePhotoUrl", "ProfilePhotoUrl") ?? null,
    gender: pick(record, "gender", "Gender") ?? null,
  };
}

export async function getLinkedCustomerPersonalProfile(
  organizationId: string,
  personalUserId: string,
  signal?: AbortSignal,
): Promise<LinkedCustomerPersonalProfile> {
  const raw = await platformRequest<unknown>({
    path: `/api/v1/platform/organizations/${organizationId}/personal-profiles/${personalUserId}/customer`,
    signal,
  });
  return linkedCustomerPersonalProfileSchema.parse(normalize(raw));
}

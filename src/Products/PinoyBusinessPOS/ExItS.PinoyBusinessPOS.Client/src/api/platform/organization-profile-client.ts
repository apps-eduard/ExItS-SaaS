import { z } from "zod";
import { platformRequest } from "@/api/platform/platform-http";

const guidSchema = z
  .string()
  .regex(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/);

function pick(raw: Record<string, unknown>, camel: string, pascal: string): unknown {
  return raw[camel] ?? raw[pascal];
}

const organizationProfileSchema = z.object({
  legalName: z.string().nullable().optional().default(null),
  contactEmail: z.string().nullable().optional().default(null),
  contactPhone: z.string().nullable().optional().default(null),
  addressLine1: z.string().nullable().optional().default(null),
  addressLine2: z.string().nullable().optional().default(null),
  city: z.string().nullable().optional().default(null),
  region: z.string().nullable().optional().default(null),
  postalCode: z.string().nullable().optional().default(null),
  countryCode: z.string().nullable().optional().default(null),
  timeZoneId: z.string().nullable().optional().default(null),
  locale: z.string().nullable().optional().default(null),
  currencyCode: z.string().nullable().optional().default(null),
});

export const platformOrganizationSchema = z.object({
  id: guidSchema,
  displayName: z.string(),
  slug: z.string(),
  status: z.string(),
  profile: organizationProfileSchema,
  updatedAtUtc: z.string(),
  createdAtUtc: z.string(),
});

export type PlatformOrganizationDto = z.infer<typeof platformOrganizationSchema>;

function normalizeProfile(raw: unknown) {
  if (!raw || typeof raw !== "object") {
    return organizationProfileSchema.parse({});
  }
  const r = raw as Record<string, unknown>;
  return organizationProfileSchema.parse({
    legalName: pick(r, "legalName", "LegalName") ?? null,
    contactEmail: pick(r, "contactEmail", "ContactEmail") ?? null,
    contactPhone: pick(r, "contactPhone", "ContactPhone") ?? null,
    addressLine1: pick(r, "addressLine1", "AddressLine1") ?? null,
    addressLine2: pick(r, "addressLine2", "AddressLine2") ?? null,
    city: pick(r, "city", "City") ?? null,
    region: pick(r, "region", "Region") ?? null,
    postalCode: pick(r, "postalCode", "PostalCode") ?? null,
    countryCode: pick(r, "countryCode", "CountryCode") ?? null,
    timeZoneId: pick(r, "timeZoneId", "TimeZoneId") ?? null,
    locale: pick(r, "locale", "Locale") ?? null,
    currencyCode: pick(r, "currencyCode", "CurrencyCode") ?? null,
  });
}

function normalizeOrganization(raw: unknown): unknown {
  if (!raw || typeof raw !== "object") return raw;
  const r = raw as Record<string, unknown>;
  return {
    id: pick(r, "id", "Id"),
    displayName: pick(r, "displayName", "DisplayName"),
    slug: pick(r, "slug", "Slug"),
    status: pick(r, "status", "Status"),
    profile: normalizeProfile(pick(r, "profile", "Profile")),
    updatedAtUtc: pick(r, "updatedAtUtc", "UpdatedAtUtc"),
    createdAtUtc: pick(r, "createdAtUtc", "CreatedAtUtc"),
  };
}

export async function getOrganization(
  organizationId: string,
  signal?: AbortSignal,
): Promise<PlatformOrganizationDto> {
  const raw = await platformRequest<unknown>({
    path: `/api/v1/platform/organizations/${organizationId}`,
    signal,
  });
  return platformOrganizationSchema.parse(normalizeOrganization(raw));
}

export type UpdateOrganizationProfileRequest = {
  displayName?: string | null;
  legalName?: string | null;
  contactEmail?: string | null;
  contactPhone?: string | null;
  addressLine1?: string | null;
  addressLine2?: string | null;
  city?: string | null;
  region?: string | null;
  postalCode?: string | null;
  countryCode?: string | null;
  expectedUpdatedAtUtc: string;
};

export async function updateOrganizationProfile(
  organizationId: string,
  body: UpdateOrganizationProfileRequest,
  signal?: AbortSignal,
): Promise<PlatformOrganizationDto> {
  const raw = await platformRequest<unknown>({
    method: "PUT",
    path: `/api/v1/platform/organizations/${organizationId}`,
    body,
    signal,
  });
  return platformOrganizationSchema.parse(normalizeOrganization(raw));
}

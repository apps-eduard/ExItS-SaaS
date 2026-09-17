import { z } from "zod";
import { platformRequest } from "@/api/platform/platform-http";

const guidSchema = z
  .string()
  .regex(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/);

function pick(raw: Record<string, unknown>, camel: string, pascal: string): unknown {
  return raw[camel] ?? raw[pascal];
}

const organizationB2bPublicProfileSchema = z.object({
  organizationId: guidSchema,
  displayName: z.string(),
  publicOrganizationId: z.string().nullable().optional().default(null),
  logoUrl: z.string().nullable().optional().default(null),
  businessPhone: z.string().nullable().optional().default(null),
  businessEmail: z.string().nullable().optional().default(null),
  addressLine1: z.string().nullable().optional().default(null),
  addressLine2: z.string().nullable().optional().default(null),
  city: z.string().nullable().optional().default(null),
  region: z.string().nullable().optional().default(null),
  postalCode: z.string().nullable().optional().default(null),
  countryCode: z.string().nullable().optional().default(null),
});

export type OrganizationB2bPublicProfile = z.infer<typeof organizationB2bPublicProfileSchema>;

function normalize(raw: unknown): unknown {
  if (!raw || typeof raw !== "object") return raw;
  const r = raw as Record<string, unknown>;
  return {
    organizationId: pick(r, "organizationId", "OrganizationId"),
    displayName: pick(r, "displayName", "DisplayName") ?? "",
    publicOrganizationId: pick(r, "publicOrganizationId", "PublicOrganizationId") ?? null,
    logoUrl: pick(r, "logoUrl", "LogoUrl") ?? null,
    businessPhone: pick(r, "businessPhone", "BusinessPhone") ?? null,
    businessEmail: pick(r, "businessEmail", "BusinessEmail") ?? null,
    addressLine1: pick(r, "addressLine1", "AddressLine1") ?? null,
    addressLine2: pick(r, "addressLine2", "AddressLine2") ?? null,
    city: pick(r, "city", "City") ?? null,
    region: pick(r, "region", "Region") ?? null,
    postalCode: pick(r, "postalCode", "PostalCode") ?? null,
    countryCode: pick(r, "countryCode", "CountryCode") ?? null,
  };
}

/** Connected-seller view of buyer org public/business fields. Never personal data. */
export async function getOrganizationB2bPublicProfile(
  buyerOrganizationId: string,
  requesterOrganizationId: string,
  signal?: AbortSignal,
): Promise<OrganizationB2bPublicProfile> {
  const raw = await platformRequest<unknown>({
    path: `/api/v1/platform/organizations/${buyerOrganizationId}/b2b-public-profile?requesterOrganizationId=${encodeURIComponent(requesterOrganizationId)}`,
    signal,
  });
  return organizationB2bPublicProfileSchema.parse(normalize(raw));
}

export function formatPublicBusinessAddress(profile: OrganizationB2bPublicProfile): string | null {
  const parts = [
    profile.addressLine1,
    profile.addressLine2,
    [profile.city, profile.region].filter(Boolean).join(", ") || null,
    profile.postalCode,
    profile.countryCode,
  ]
    .map((p) => p?.trim())
    .filter(Boolean);
  return parts.length > 0 ? parts.join(", ") : null;
}

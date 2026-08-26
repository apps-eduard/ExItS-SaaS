import type { OrganizationDetail, OrganizationProfile } from "@/api/organizations/organization-types";
import type {
  UpdateOrganizationBody,
  UpdateOrganizationBrandingBody,
} from "@/api/organizations/organization-mutations-client";
import type { CreateInvitationBody } from "@/api/organizations/people-mutations-client";

function nullIfBlank(value: string | undefined): string | null {
  const trimmed = value?.trim();
  return trimmed && trimmed.length > 0 ? trimmed : null;
}

export type OrganizationProfileFormValues = {
  displayName: string;
  slug: string;
  legalName: string;
  contactEmail: string;
  contactPhone: string;
  addressLine1: string;
  addressLine2: string;
  city: string;
  region: string;
  postalCode: string;
  countryCode: string;
  timeZoneId: string;
  locale: string;
  currencyCode: string;
};

export type OrganizationBrandingFormValues = {
  brandDisplayName: string;
  logoUrl: string;
  primaryColor: string;
  accentColor: string;
};

export function organizationProfileFormValues(organization: OrganizationDetail): OrganizationProfileFormValues {
  const profile = organization.profile;
  return {
    displayName: organization.displayName,
    slug: organization.slug,
    legalName: profile.legalName ?? "",
    contactEmail: profile.contactEmail ?? "",
    contactPhone: profile.contactPhone ?? "",
    addressLine1: profile.addressLine1 ?? "",
    addressLine2: profile.addressLine2 ?? "",
    city: profile.city ?? "",
    region: profile.region ?? "",
    postalCode: profile.postalCode ?? "",
    countryCode: profile.countryCode ?? "",
    timeZoneId: profile.timeZoneId ?? "",
    locale: profile.locale ?? "",
    currencyCode: profile.currencyCode ?? "",
  };
}

export function organizationBrandingFormValues(organization: OrganizationDetail): OrganizationBrandingFormValues {
  const branding = organization.branding;
  return {
    brandDisplayName: branding.brandDisplayName ?? "",
    logoUrl: branding.logoUrl ?? "",
    primaryColor: branding.primaryColor ?? "",
    accentColor: branding.accentColor ?? "",
  };
}

export function buildUpdateOrganizationBody(
  values: OrganizationProfileFormValues,
  organization: OrganizationDetail,
  options: { includeSlug: boolean },
): UpdateOrganizationBody {
  return {
    displayName: values.displayName.trim(),
    slug: options.includeSlug ? nullIfBlank(values.slug) : undefined,
    legalName: nullIfBlank(values.legalName),
    contactEmail: nullIfBlank(values.contactEmail),
    contactPhone: nullIfBlank(values.contactPhone),
    addressLine1: nullIfBlank(values.addressLine1),
    addressLine2: nullIfBlank(values.addressLine2),
    city: nullIfBlank(values.city),
    region: nullIfBlank(values.region),
    postalCode: nullIfBlank(values.postalCode),
    countryCode: nullIfBlank(values.countryCode),
    timeZoneId: nullIfBlank(values.timeZoneId),
    locale: nullIfBlank(values.locale),
    currencyCode: nullIfBlank(values.currencyCode),
    expectedUpdatedAtUtc: organization.updatedAtUtc ?? null,
  };
}

export function buildUpdateOrganizationBrandingBody(
  values: OrganizationBrandingFormValues,
  organization: OrganizationDetail,
): UpdateOrganizationBrandingBody {
  return {
    brandDisplayName: nullIfBlank(values.brandDisplayName),
    logoUrl: nullIfBlank(values.logoUrl),
    primaryColor: nullIfBlank(values.primaryColor),
    accentColor: nullIfBlank(values.accentColor),
    expectedUpdatedAtUtc: organization.updatedAtUtc ?? null,
  };
}

export type InviteMemberFormValues = {
  email: string;
  role: string;
  firstName: string;
  lastName: string;
  displayName: string;
  phone: string;
  employeeCode: string;
  branch: string;
};

export function buildCreateInvitationBody(values: InviteMemberFormValues): CreateInvitationBody {
  return {
    email: values.email.trim(),
    role: values.role,
    firstName: nullIfBlank(values.firstName),
    lastName: nullIfBlank(values.lastName),
    displayName: nullIfBlank(values.displayName),
    phone: nullIfBlank(values.phone),
    employeeCode: nullIfBlank(values.employeeCode),
    branch: nullIfBlank(values.branch),
    requireEmailVerification: true,
  };
}

export function profileFieldKeys(): Array<keyof OrganizationProfile> {
  return [
    "legalName",
    "contactEmail",
    "contactPhone",
    "addressLine1",
    "addressLine2",
    "city",
    "region",
    "postalCode",
    "countryCode",
    "timeZoneId",
    "locale",
    "currencyCode",
  ];
}

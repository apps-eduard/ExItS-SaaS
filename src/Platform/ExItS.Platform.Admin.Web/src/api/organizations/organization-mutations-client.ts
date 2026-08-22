import { mapOrganizationDetail } from "@/api/organizations/organization-client";
import type { OrganizationDetail } from "@/api/organizations/organization-types";
import { commercialMutationRequest } from "@/api/commercial/commercial-http";

function organizationPath(organizationId: string, suffix = ""): string {
  return `/api/v1/platform/organizations/${organizationId}${suffix}`;
}

function requireOrganization(payload: unknown): OrganizationDetail {
  return mapOrganizationDetail(payload);
}

export type UpdateOrganizationBody = {
  displayName?: string | null;
  slug?: string | null;
  legalName?: string | null;
  contactEmail?: string | null;
  contactPhone?: string | null;
  addressLine1?: string | null;
  addressLine2?: string | null;
  city?: string | null;
  region?: string | null;
  postalCode?: string | null;
  countryCode?: string | null;
  timeZoneId?: string | null;
  locale?: string | null;
  currencyCode?: string | null;
  expectedUpdatedAtUtc?: string | null;
};

export type UpdateOrganizationBrandingBody = {
  brandDisplayName?: string | null;
  logoUrl?: string | null;
  primaryColor?: string | null;
  accentColor?: string | null;
  expectedUpdatedAtUtc?: string | null;
};

export function updateOrganization(
  baseUrl: string,
  organizationId: string,
  body: UpdateOrganizationBody,
  signal?: AbortSignal,
): Promise<OrganizationDetail> {
  return commercialMutationRequest<unknown>(baseUrl, {
    method: "PUT",
    path: organizationPath(organizationId),
    body,
    signal,
  }).then(requireOrganization);
}

export function updateOrganizationBranding(
  baseUrl: string,
  organizationId: string,
  body: UpdateOrganizationBrandingBody,
  signal?: AbortSignal,
): Promise<OrganizationDetail> {
  return commercialMutationRequest<unknown>(baseUrl, {
    method: "PUT",
    path: organizationPath(organizationId, "/branding"),
    body,
    signal,
  }).then(requireOrganization);
}

export function suspendOrganization(
  baseUrl: string,
  organizationId: string,
  signal?: AbortSignal,
): Promise<OrganizationDetail> {
  return commercialMutationRequest<unknown>(baseUrl, {
    method: "POST",
    path: organizationPath(organizationId, "/suspend"),
    signal,
  }).then(requireOrganization);
}

export function reactivateOrganization(
  baseUrl: string,
  organizationId: string,
  signal?: AbortSignal,
): Promise<OrganizationDetail> {
  return commercialMutationRequest<unknown>(baseUrl, {
    method: "POST",
    path: organizationPath(organizationId, "/reactivate"),
    signal,
  }).then(requireOrganization);
}

export function closeOrganization(
  baseUrl: string,
  organizationId: string,
  signal?: AbortSignal,
): Promise<OrganizationDetail> {
  return commercialMutationRequest<unknown>(baseUrl, {
    method: "POST",
    path: organizationPath(organizationId, "/close"),
    signal,
  }).then(requireOrganization);
}

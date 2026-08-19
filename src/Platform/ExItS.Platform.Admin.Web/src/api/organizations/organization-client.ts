import { parsePagedResult, type PagedResult } from "@/api/platform/paged-result";
import { platformRequest } from "@/api/platform-http";
import { organizationsListPath } from "@/features/overview/dashboard-bounds";
import { organizationsListRequestPath } from "@/api/organizations/organization-list-query";
import type {
  OrganizationBranding,
  OrganizationCommercialSummary,
  OrganizationDetail,
  OrganizationListItem,
  OrganizationListQuery,
  OrganizationProfile,
} from "@/api/organizations/organization-types";

function readString(record: Record<string, unknown>, ...keys: string[]): string | undefined {
  for (const key of keys) {
    const value = record[key];
    if (typeof value === "string" && value.length > 0) {
      return value;
    }
  }
  return undefined;
}

export function mapOrganizationListItem(payload: unknown): OrganizationListItem {
  if (typeof payload !== "object" || payload === null) {
    throw new Error("Invalid organization list item.");
  }
  const record = payload as Record<string, unknown>;
  const id = readString(record, "id", "Id");
  const displayName = readString(record, "displayName", "DisplayName");
  const slug = readString(record, "slug", "Slug");
  const status = readString(record, "status", "Status");
  if (!id || !displayName || !slug || !status) {
    throw new Error("Invalid organization list item.");
  }
  return {
    id,
    displayName,
    slug,
    status,
    createdAtUtc: readString(record, "createdAtUtc", "CreatedAtUtc"),
    updatedAtUtc: readString(record, "updatedAtUtc", "UpdatedAtUtc"),
  };
}

function asRecord(payload: unknown): Record<string, unknown> | null {
  if (typeof payload !== "object" || payload === null) {
    return null;
  }
  return payload as Record<string, unknown>;
}

function mapProfile(payload: unknown): OrganizationProfile {
  const record = asRecord(payload);
  if (!record) {
    return {};
  }
  return {
    legalName: readString(record, "legalName", "LegalName"),
    contactEmail: readString(record, "contactEmail", "ContactEmail"),
    contactPhone: readString(record, "contactPhone", "ContactPhone"),
    addressLine1: readString(record, "addressLine1", "AddressLine1"),
    addressLine2: readString(record, "addressLine2", "AddressLine2"),
    city: readString(record, "city", "City"),
    region: readString(record, "region", "Region"),
    postalCode: readString(record, "postalCode", "PostalCode"),
    countryCode: readString(record, "countryCode", "CountryCode"),
    timeZoneId: readString(record, "timeZoneId", "TimeZoneId"),
    locale: readString(record, "locale", "Locale"),
    currencyCode: readString(record, "currencyCode", "CurrencyCode"),
  };
}

function mapBranding(payload: unknown): OrganizationBranding {
  const record = asRecord(payload);
  if (!record) {
    return {};
  }
  return {
    brandDisplayName: readString(record, "brandDisplayName", "BrandDisplayName"),
    primaryColor: readString(record, "primaryColor", "PrimaryColor"),
    accentColor: readString(record, "accentColor", "AccentColor"),
  };
}

export function mapOrganizationDetail(payload: unknown): OrganizationDetail {
  const item = mapOrganizationListItem(payload);
  const record = asRecord(payload) ?? {};
  return {
    ...item,
    profile: mapProfile(record.profile ?? record.Profile),
    branding: mapBranding(record.branding ?? record.Branding),
  };
}

function mapNamedRecords<T>(
  payload: unknown,
  mapItem: (record: Record<string, unknown>) => T | null,
): T[] {
  if (!Array.isArray(payload)) {
    return [];
  }
  return payload.flatMap((item) => {
    const record = asRecord(item);
    if (!record) {
      return [];
    }
    const mapped = mapItem(record);
    return mapped ? [mapped] : [];
  });
}

export function mapOrganizationCommercialSummary(payload: unknown): OrganizationCommercialSummary {
  const record = asRecord(payload);
  if (!record) {
    throw new Error("Invalid commercial summary.");
  }
  return {
    subscriptions: mapNamedRecords(record.subscriptions ?? record.Subscriptions, (item) => {
      const id = readString(item, "id", "Id");
      const productCode = readString(item, "productCode", "ProductCode");
      const status = readString(item, "status", "Status");
      if (!id || !productCode || !status) {
        return null;
      }
      return { id, productCode, status };
    }),
    payments: mapNamedRecords(record.payments ?? record.Payments, (item) => {
      const id = readString(item, "id", "Id");
      const productCode = readString(item, "productCode", "ProductCode");
      const status = readString(item, "status", "Status");
      if (!id || !productCode || !status) {
        return null;
      }
      return {
        id,
        productCode,
        status,
        paidAtUtc: readString(item, "paidAtUtc", "PaidAtUtc"),
      };
    }),
    latestEntitlements: mapNamedRecords(
      record.latestEntitlements ?? record.LatestEntitlements,
      (item) => {
        const id = readString(item, "id", "Id");
        const productCode = readString(item, "productCode", "ProductCode");
        const subscriptionStatus = readString(item, "subscriptionStatus", "SubscriptionStatus");
        if (!id || !productCode || !subscriptionStatus) {
          return null;
        }
        return {
          id,
          productCode,
          subscriptionStatus,
          generatedAtUtc: readString(item, "generatedAtUtc", "GeneratedAtUtc"),
        };
      },
    ),
  };
}

export function getOrganization(
  baseUrl: string,
  organizationId: string,
  signal?: AbortSignal,
): Promise<OrganizationDetail> {
  return platformRequest<unknown>(baseUrl, {
    path: `/api/v1/platform/organizations/${organizationId}`,
    signal,
  }).then(mapOrganizationDetail);
}

export function getOrganizationCommercialSummary(
  baseUrl: string,
  organizationId: string,
  signal?: AbortSignal,
): Promise<OrganizationCommercialSummary> {
  return platformRequest<unknown>(baseUrl, {
    path: `/api/v1/platform/admin/organizations/${organizationId}/commercial-summary`,
    signal,
  }).then(mapOrganizationCommercialSummary);
}

export function listOrganizations(
  baseUrl: string,
  options: OrganizationListQuery & { signal?: AbortSignal },
): Promise<PagedResult<OrganizationListItem>> {
  const dashboardShaped =
    options.search == null &&
    options.sortBy == null &&
    options.sortDesc == null &&
    (options.page == null || options.page === 1);
  const path = dashboardShaped
    ? organizationsListPath({
        status: options.status,
        pageSize: options.pageSize ?? 1,
      })
    : organizationsListRequestPath(options);

  return platformRequest<unknown>(baseUrl, {
    path,
    signal: options.signal,
  }).then((payload) => {
    const page = parsePagedResult<unknown>(payload);
    return {
      ...page,
      items: page.items.map(mapOrganizationListItem),
    };
  });
}

import type { CommercialEntitlementRecord } from "@/api/organizations/organization-types";
import { withQuery } from "@/lib/http/query-string";

export const ORGANIZATION_ENTITLEMENT_PAGE_SIZE = 20;

export type EntitlementGrant = {
  featureCode: string;
  enabled: boolean;
  numericLimit?: number;
  source?: string;
  effectiveAtUtc?: string;
  expiresAtUtc?: string;
};

export type EntitlementSnapshot = {
  id: string;
  organizationId: string;
  productCode: string;
  subscriptionId: string;
  planCode: string;
  planVersionNumber?: number;
  snapshotVersion: number;
  schemaVersion?: number;
  subscriptionStatus: string;
  inGracePeriod: boolean;
  generatedAtUtc?: string;
  effectiveAtUtc?: string;
  refreshByUtc?: string;
  expiresAtUtc?: string;
  sourceAggregateVersion?: number;
  grants: EntitlementGrant[];
};

export type FeatureOverride = {
  id: string;
  organizationId: string;
  productCode: string;
  featureCode: string;
  enabled: boolean;
  numericLimit?: number;
  reason?: string;
  effectiveFromUtc?: string;
  expiresAtUtc?: string;
  status: string;
  createdAtUtc?: string;
  createdByUserId?: string;
  updatedAtUtc?: string;
  revokedAtUtc?: string;
  revokedByUserId?: string;
  revocationReason?: string;
};

export type EntitlementProductOption = {
  productCode: string;
  productDisplayName?: string;
};

export type OrganizationEntitlementUrlState = {
  product: string;
  page: number;
};

function parsePage(raw: string | null): number {
  const value = Number(raw ?? "1");
  return Number.isFinite(value) && value >= 1 ? Math.floor(value) : 1;
}

export function uniqueEntitlementProductOptions(
  records: CommercialEntitlementRecord[],
): EntitlementProductOption[] {
  const seen = new Set<string>();
  const options: EntitlementProductOption[] = [];
  for (const record of records) {
    if (seen.has(record.productCode)) {
      continue;
    }
    seen.add(record.productCode);
    options.push({
      productCode: record.productCode,
      productDisplayName: record.productDisplayName,
    });
  }
  return options;
}

export function sanitizeEntitlementProduct(
  requested: string,
  options: EntitlementProductOption[],
): string | null {
  if (!requested) {
    return null;
  }
  return options.some((option) => option.productCode === requested) ? requested : null;
}

export function parseOrganizationEntitlementSearchParams(
  params: URLSearchParams,
): OrganizationEntitlementUrlState {
  return {
    product: params.get("product") ?? "",
    page: parsePage(params.get("page")),
  };
}

export function organizationEntitlementSearchParams(
  state: OrganizationEntitlementUrlState,
): URLSearchParams {
  const params = new URLSearchParams();
  if (state.product) {
    params.set("product", state.product);
  }
  if (state.page > 1) {
    params.set("page", String(state.page));
  }
  return params;
}

export function organizationEntitlementSnapshotsRequestPath(
  organizationId: string,
  productCode: string,
  query: { page: number; pageSize?: number },
): string {
  return withQuery(
    `/api/v1/platform/organizations/${organizationId}/products/${encodeURIComponent(productCode)}/entitlements/snapshots`,
    {
      page: query.page,
      pageSize: query.pageSize ?? ORGANIZATION_ENTITLEMENT_PAGE_SIZE,
    },
  );
}

export function organizationLatestEntitlementSnapshotRequestPath(
  organizationId: string,
  productCode: string,
): string {
  return `/api/v1/platform/organizations/${organizationId}/products/${encodeURIComponent(productCode)}/entitlements/snapshots/latest`;
}

export const ORGANIZATION_FEATURE_OVERRIDE_PAGE_SIZE = 20;

export type FeatureOverrideStatusFilter = "" | "Active" | "Revoked";

export function organizationFeatureOverridesRequestPath(
  organizationId: string,
  productCode: string,
  query: { page: number; pageSize?: number; status?: FeatureOverrideStatusFilter },
): string {
  return withQuery(
    `/api/v1/platform/organizations/${organizationId}/products/${encodeURIComponent(productCode)}/feature-overrides`,
    {
      page: query.page,
      pageSize: query.pageSize ?? ORGANIZATION_FEATURE_OVERRIDE_PAGE_SIZE,
      status: query.status || undefined,
    },
  );
}

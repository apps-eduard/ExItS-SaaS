import { platformRequest } from "@/api/platform-http";
import type { CatalogFeatureGrant } from "@/api/catalog/plan-catalog-types";
import type { CatalogProduct } from "@/api/catalog/product-catalog-client";
import { mapCatalogProduct } from "@/api/catalog/product-catalog-client";

function asRecord(value: unknown): Record<string, unknown> | null {
  return value && typeof value === "object" ? (value as Record<string, unknown>) : null;
}

function readString(record: Record<string, unknown>, ...keys: string[]): string | undefined {
  for (const key of keys) {
    const value = record[key];
    if (typeof value === "string" && value.length > 0) {
      return value;
    }
  }
  return undefined;
}

function readNumber(record: Record<string, unknown>, ...keys: string[]): number | undefined {
  for (const key of keys) {
    const value = record[key];
    if (typeof value === "number" && Number.isFinite(value)) {
      return value;
    }
  }
  return undefined;
}

function readBoolean(record: Record<string, unknown>, ...keys: string[]): boolean | undefined {
  for (const key of keys) {
    const value = record[key];
    if (typeof value === "boolean") {
      return value;
    }
  }
  return undefined;
}

export type PortfolioSummary = {
  activeProductCount: number;
  publishedPlanVersionCount: number;
  organizationCount: number;
  trialingSubscriptionCount: number;
  activeSubscriptionCount: number;
  gracePeriodSubscriptionCount: number;
  pastDueSubscriptionCount: number;
  suspendedSubscriptionCount: number;
  pendingManualPaymentCount: number;
  latestEntitlementSnapshotCount: number;
  partialFailures: string[];
};

export type ProductOverviewFeature = {
  code: string;
  displayName: string;
  description?: string;
  valueType?: string;
};

export type ProductOverviewPlan = {
  id: string;
  code: string;
  displayName: string;
  status: string;
  maxBranches?: number;
  maxActivePosDevices?: number;
};

export type ProductOverviewPlanVersion = {
  id: string;
  planId: string;
  versionNumber: number;
  status: string;
  grants: CatalogFeatureGrant[];
};

export type ProductOverviewTrial = {
  id: string;
  code: string;
  displayName: string;
  status: string;
  durationDays?: number;
};

export type ProductOverview = {
  product: CatalogProduct;
  features: ProductOverviewFeature[];
  plans: ProductOverviewPlan[];
  publishedPlanVersions: ProductOverviewPlanVersion[];
  trials: ProductOverviewTrial[];
};

export function mapPortfolioSummary(payload: unknown): PortfolioSummary {
  const record = asRecord(payload);
  if (!record) {
    throw new Error("Invalid portfolio summary.");
  }
  const partialFailuresRaw = record.partialFailures ?? record.PartialFailures;
  const partialFailures = Array.isArray(partialFailuresRaw)
    ? partialFailuresRaw.filter((item): item is string => typeof item === "string")
    : [];

  return {
    activeProductCount: readNumber(record, "activeProductCount", "ActiveProductCount") ?? 0,
    publishedPlanVersionCount:
      readNumber(record, "publishedPlanVersionCount", "PublishedPlanVersionCount") ?? 0,
    organizationCount: readNumber(record, "organizationCount", "OrganizationCount") ?? 0,
    trialingSubscriptionCount:
      readNumber(record, "trialingSubscriptionCount", "TrialingSubscriptionCount") ?? 0,
    activeSubscriptionCount:
      readNumber(record, "activeSubscriptionCount", "ActiveSubscriptionCount") ?? 0,
    gracePeriodSubscriptionCount:
      readNumber(record, "gracePeriodSubscriptionCount", "GracePeriodSubscriptionCount") ?? 0,
    pastDueSubscriptionCount:
      readNumber(record, "pastDueSubscriptionCount", "PastDueSubscriptionCount") ?? 0,
    suspendedSubscriptionCount:
      readNumber(record, "suspendedSubscriptionCount", "SuspendedSubscriptionCount") ?? 0,
    pendingManualPaymentCount:
      readNumber(record, "pendingManualPaymentCount", "PendingManualPaymentCount") ?? 0,
    latestEntitlementSnapshotCount:
      readNumber(record, "latestEntitlementSnapshotCount", "LatestEntitlementSnapshotCount") ??
      0,
    partialFailures,
  };
}

function mapFeatureGrant(payload: unknown): CatalogFeatureGrant | null {
  const record = asRecord(payload);
  if (!record) {
    return null;
  }
  const featureCode = readString(record, "featureCode", "FeatureCode");
  const enabled = readBoolean(record, "enabled", "Enabled");
  if (!featureCode || enabled === undefined) {
    return null;
  }
  return {
    featureCode,
    enabled,
    numericLimit: readNumber(record, "numericLimit", "NumericLimit"),
  };
}

export function mapProductOverview(payload: unknown): ProductOverview {
  const record = asRecord(payload);
  if (!record) {
    throw new Error("Invalid product overview.");
  }
  const productRaw = record.product ?? record.Product;
  const product = mapCatalogProduct(productRaw);
  if (!product) {
    throw new Error("Invalid product overview.");
  }

  const features = mapNamedRecords(record.features ?? record.Features, (item) => {
    const code = readString(item, "code", "Code");
    const displayName = readString(item, "displayName", "DisplayName");
    if (!code || !displayName) {
      return null;
    }
    return {
      code,
      displayName,
      description: readString(item, "description", "Description"),
      valueType: readString(item, "valueType", "ValueType"),
    };
  });

  const plans = mapNamedRecords(record.plans ?? record.Plans, (item) => {
    const id = readString(item, "id", "Id");
    const code = readString(item, "code", "Code");
    const displayName = readString(item, "displayName", "DisplayName");
    const status = readString(item, "status", "Status");
    if (!id || !code || !displayName || !status) {
      return null;
    }
    return {
      id,
      code,
      displayName,
      status,
      maxBranches: readNumber(item, "maxBranches", "MaxBranches"),
      maxActivePosDevices: readNumber(item, "maxActivePosDevices", "MaxActivePosDevices"),
    };
  });

  const publishedPlanVersions = mapNamedRecords(
    record.publishedPlanVersions ?? record.PublishedPlanVersions,
    (item) => {
      const id = readString(item, "id", "Id");
      const planId = readString(item, "planId", "PlanId");
      const versionNumber = readNumber(item, "versionNumber", "VersionNumber");
      const status = readString(item, "status", "Status");
      if (!id || !planId || versionNumber === undefined || !status) {
        return null;
      }
      const grantsRaw = item.grants ?? item.Grants;
      const grants = Array.isArray(grantsRaw)
        ? grantsRaw
            .map(mapFeatureGrant)
            .filter((grant): grant is CatalogFeatureGrant => grant != null)
        : [];
      return { id, planId, versionNumber, status, grants };
    },
  );

  const trials = mapNamedRecords(record.trials ?? record.Trials, (item) => {
    const id = readString(item, "id", "Id");
    const code = readString(item, "code", "Code");
    const displayName = readString(item, "displayName", "DisplayName");
    const status = readString(item, "status", "Status");
    if (!id || !code || !displayName || !status) {
      return null;
    }
    return {
      id,
      code,
      displayName,
      status,
      durationDays: readNumber(item, "durationDays", "DurationDays"),
    };
  });

  return { product, features, plans, publishedPlanVersions, trials };
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

export function getPortfolioSummary(
  baseUrl: string,
  signal?: AbortSignal,
): Promise<PortfolioSummary> {
  return platformRequest<unknown>(baseUrl, {
    path: "/api/v1/platform/admin/portfolio-summary",
    signal,
  }).then(mapPortfolioSummary);
}

export function getProductOverview(
  baseUrl: string,
  productCode: string,
  signal?: AbortSignal,
): Promise<ProductOverview> {
  return platformRequest<unknown>(baseUrl, {
    path: `/api/v1/platform/admin/products/${encodeURIComponent(productCode)}/overview`,
    signal,
  }).then(mapProductOverview);
}

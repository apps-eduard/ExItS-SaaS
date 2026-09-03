import { parsePagedResult, type PagedResult } from "@/api/platform/paged-result";
import { platformRequest } from "@/api/platform-http";
import { catalogPlansListRequestPath } from "@/api/catalog/plan-list-query";
import type { CatalogPlan, CatalogPlanVersion, PlanListQuery } from "@/api/catalog/plan-catalog-types";

function asRecord(value: unknown): Record<string, unknown> | null {
  return typeof value === "object" && value !== null ? (value as Record<string, unknown>) : null;
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

export function mapCatalogPlan(payload: unknown): CatalogPlan | null {
  const record = asRecord(payload);
  if (!record) {
    return null;
  }
  const id = readString(record, "id", "Id");
  const productCode = readString(record, "productCode", "ProductCode");
  const code = readString(record, "code", "Code");
  const displayName = readString(record, "displayName", "DisplayName");
  const status = readString(record, "status", "Status");
  if (!id || !productCode || !code || !displayName || !status) {
    return null;
  }
  return {
    id,
    productCode,
    code,
    displayName,
    status,
    createdAtUtc: readString(record, "createdAtUtc", "CreatedAtUtc"),
    updatedAtUtc: readString(record, "updatedAtUtc", "UpdatedAtUtc"),
    productId: readString(record, "productId", "ProductId"),
    productDisplayName: readString(record, "productDisplayName", "ProductDisplayName"),
    planKey: readString(record, "planKey", "PlanKey"),
    description: readString(record, "description", "Description"),
    maxBranches: readNumber(record, "maxBranches", "MaxBranches"),
    maxActiveStaff: readNumber(record, "maxActiveStaff", "MaxActiveStaff"),
    maxActivePosDevices: readNumber(record, "maxActivePosDevices", "MaxActivePosDevices"),
    maxActiveBusinessTypes: readNumber(record, "maxActiveBusinessTypes", "MaxActiveBusinessTypes"),
    maxAreas: readNumber(record, "maxAreas", "MaxAreas"),
    customerCreditEnabled: readBoolean(record, "customerCreditEnabled", "CustomerCreditEnabled"),
    advancedReportsEnabled: readBoolean(record, "advancedReportsEnabled", "AdvancedReportsEnabled"),
    exportEnabled: readBoolean(record, "exportEnabled", "ExportEnabled"),
    trialAllowed: readBoolean(record, "trialAllowed", "TrialAllowed"),
    defaultTrialDays: readNumber(record, "defaultTrialDays", "DefaultTrialDays"),
    sortOrder: readNumber(record, "sortOrder", "SortOrder"),
    monthlyPrice: readNumber(record, "monthlyPrice", "MonthlyPrice"),
    annualPrice: readNumber(record, "annualPrice", "AnnualPrice"),
    currencyCode: readString(record, "currencyCode", "CurrencyCode"),
  };
}

export function listCatalogPlansPage(
  baseUrl: string,
  options: PlanListQuery & { signal?: AbortSignal },
): Promise<PagedResult<CatalogPlan>> {
  return platformRequest<unknown>(baseUrl, {
    path: catalogPlansListRequestPath(options),
    signal: options.signal,
  }).then((payload) => {
    const page = parsePagedResult<unknown>(payload);
    return {
      ...page,
      items: page.items.flatMap((item) => {
        const mapped = mapCatalogPlan(item);
        return mapped ? [mapped] : [];
      }),
    };
  });
}

export function getCatalogPlanById(
  baseUrl: string,
  planId: string,
  signal?: AbortSignal,
): Promise<CatalogPlan> {
  return platformRequest<unknown>(baseUrl, {
    path: `/api/v1/platform/catalog/plans/${planId}`,
    signal,
  }).then((payload) => {
    const mapped = mapCatalogPlan(payload);
    if (!mapped) {
      throw new Error("Invalid catalog plan.");
    }
    return mapped;
  });
}

export function listCatalogPlansByProductCode(
  baseUrl: string,
  productCode: string,
  signal?: AbortSignal,
): Promise<CatalogPlan[]> {
  return platformRequest<unknown>(baseUrl, {
    path: `/api/v1/platform/catalog/products/${encodeURIComponent(productCode)}/plans`,
    signal,
  }).then((payload) => {
    if (!Array.isArray(payload)) {
      return [];
    }
    return payload.flatMap((item) => {
      const mapped = mapCatalogPlan(item);
      return mapped ? [mapped] : [];
    });
  });
}

function mapCatalogFeatureGrant(payload: unknown): CatalogPlanVersion["grants"][number] | null {
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

export function mapCatalogPlanVersion(payload: unknown): CatalogPlanVersion | null {
  const record = asRecord(payload);
  if (!record) {
    return null;
  }
  const id = readString(record, "id", "Id");
  const planId = readString(record, "planId", "PlanId");
  const productCode = readString(record, "productCode", "ProductCode");
  const versionNumber = readNumber(record, "versionNumber", "VersionNumber");
  const status = readString(record, "status", "Status");
  if (!id || !planId || !productCode || versionNumber === undefined || !status) {
    return null;
  }
  const grantsPayload = record.grants ?? record.Grants;
  return {
    id,
    planId,
    productCode,
    versionNumber,
    status,
    billingPeriod: readString(record, "billingPeriod", "BillingPeriod"),
    trialEligible: readBoolean(record, "trialEligible", "TrialEligible"),
    effectiveFromUtc: readString(record, "effectiveFromUtc", "EffectiveFromUtc"),
    effectiveToUtc: readString(record, "effectiveToUtc", "EffectiveToUtc"),
    createdAtUtc: readString(record, "createdAtUtc", "CreatedAtUtc"),
    updatedAtUtc: readString(record, "updatedAtUtc", "UpdatedAtUtc"),
    grants: Array.isArray(grantsPayload)
      ? grantsPayload.flatMap((item) => {
          const mapped = mapCatalogFeatureGrant(item);
          return mapped ? [mapped] : [];
        })
      : [],
  };
}

export function listCatalogPlanVersions(
  baseUrl: string,
  productCode: string,
  planId: string,
  signal?: AbortSignal,
): Promise<CatalogPlanVersion[]> {
  return platformRequest<unknown>(baseUrl, {
    path: `/api/v1/platform/catalog/products/${encodeURIComponent(productCode)}/plans/${planId}/versions`,
    signal,
  }).then((payload) => {
    if (!Array.isArray(payload)) {
      return [];
    }
    return payload.flatMap((item) => {
      const mapped = mapCatalogPlanVersion(item);
      return mapped ? [mapped] : [];
    });
  });
}

import { platformRequest } from "@/api/platform-http";
import {
  type CatalogFeatureDefinition,
  featureSupportsNumericLimit,
} from "@/api/catalog/feature-catalog-types";

function asRecord(payload: unknown): Record<string, unknown> | null {
  return typeof payload === "object" && payload !== null ? (payload as Record<string, unknown>) : null;
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

export function mapCatalogFeatureDefinition(payload: unknown): CatalogFeatureDefinition | null {
  const record = asRecord(payload);
  if (!record) {
    return null;
  }
  const productCode = readString(record, "productCode", "ProductCode");
  const featureCode = readString(record, "featureCode", "FeatureCode");
  const displayName = readString(record, "displayName", "DisplayName");
  const valueType = readString(record, "valueType", "ValueType");
  const status = readString(record, "status", "Status");
  if (!productCode || !featureCode || !displayName || !valueType || !status) {
    return null;
  }
  return {
    productCode,
    featureCode,
    displayName,
    valueType,
    status,
    createdAtUtc: readString(record, "createdAtUtc", "CreatedAtUtc"),
    updatedAtUtc: readString(record, "updatedAtUtc", "UpdatedAtUtc"),
  };
}

export function listCatalogFeaturesByProductCode(
  baseUrl: string,
  productCode: string,
  signal?: AbortSignal,
): Promise<CatalogFeatureDefinition[]> {
  return platformRequest<unknown>(baseUrl, {
    path: `/api/v1/platform/catalog/products/${encodeURIComponent(productCode)}/features`,
    signal,
  }).then((payload) => {
    if (!Array.isArray(payload)) {
      return [];
    }
    return payload.flatMap((item) => {
      const mapped = mapCatalogFeatureDefinition(item);
      return mapped ? [mapped] : [];
    });
  });
}

export { featureSupportsNumericLimit };

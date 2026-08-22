import { platformRequest } from "@/api/platform-http";

export type CatalogTrialDefinition = {
  id: string;
  productCode: string;
  displayName: string;
  status: string;
  planId?: string;
  durationIso?: string;
};

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

export function mapCatalogTrialDefinition(payload: unknown): CatalogTrialDefinition | null {
  const record = asRecord(payload);
  if (!record) {
    return null;
  }
  const id = readString(record, "id", "Id");
  const productCode = readString(record, "productCode", "ProductCode");
  const displayName = readString(record, "displayName", "DisplayName");
  const status = readString(record, "status", "Status");
  if (!id || !productCode || !displayName || !status) {
    return null;
  }
  return {
    id,
    productCode,
    displayName,
    status,
    planId: readString(record, "planId", "PlanId"),
    durationIso: readString(record, "durationIso", "DurationIso"),
  };
}

export function listCatalogTrialsByProductCode(
  baseUrl: string,
  productCode: string,
  signal?: AbortSignal,
): Promise<CatalogTrialDefinition[]> {
  return platformRequest<unknown>(baseUrl, {
    path: `/api/v1/platform/catalog/products/${encodeURIComponent(productCode)}/trials`,
    signal,
  }).then((payload) => {
    if (!Array.isArray(payload)) {
      return [];
    }
    return payload.flatMap((item) => {
      const mapped = mapCatalogTrialDefinition(item);
      return mapped ? [mapped] : [];
    });
  });
}

function isProductWideTrial(trial: CatalogTrialDefinition): boolean {
  return trial.planId == null || trial.planId.length === 0;
}

/**
 * Bind a trial definition to the selected plan.
 * Exact plan-specific trial, then canonical product-wide (null PlanId).
 * Never another plan's trial.
 */
export function selectActiveTrialDefinition(
  trials: CatalogTrialDefinition[],
  planId: string,
): CatalogTrialDefinition | undefined {
  if (!planId) {
    return undefined;
  }
  const active = trials.filter((trial) => trial.status === "Active");
  const exact = active.find((trial) => trial.planId === planId);
  if (exact) {
    return exact;
  }
  return active.find(isProductWideTrial);
}

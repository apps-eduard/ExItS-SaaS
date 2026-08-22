import { parsePagedResult, type PagedResult } from "@/api/platform/paged-result";
import { platformRequest } from "@/api/platform-http";
import { withQuery } from "@/lib/http/query-string";

export const ENTITLEMENT_PORTFOLIO_PAGE_SIZE = 20;

export type EntitlementLatestSummary = {
  id: string;
  organizationId: string;
  productCode: string;
  subscriptionId: string;
  subscriptionStatus: string;
  snapshotVersion: number;
  schemaVersion: number;
  generatedAtUtc: string;
  effectiveAtUtc: string;
  refreshByUtc: string;
  expiresAtUtc?: string;
  inGracePeriod: boolean;
  organizationDisplayName?: string;
  productDisplayName?: string;
};

export type EntitlementPortfolioUrlState = {
  page: number;
  pageSize: number;
  sortBy: string;
  sortDesc: boolean;
};

function asRecord(value: unknown): Record<string, unknown> | null {
  return value && typeof value === "object" ? (value as Record<string, unknown>) : null;
}

function readString(record: Record<string, unknown>, ...keys: string[]): string | undefined {
  for (const key of keys) {
    const value = record[key];
    if (typeof value === "string" && value.length > 0) return value;
  }
  return undefined;
}

function readNumber(record: Record<string, unknown>, ...keys: string[]): number | undefined {
  for (const key of keys) {
    const value = record[key];
    if (typeof value === "number" && Number.isFinite(value)) return value;
  }
  return undefined;
}

function readBoolean(record: Record<string, unknown>, ...keys: string[]): boolean {
  for (const key of keys) {
    const value = record[key];
    if (typeof value === "boolean") return value;
  }
  return false;
}

export function mapEntitlementLatestSummary(payload: unknown): EntitlementLatestSummary {
  const record = asRecord(payload);
  if (!record) {
    throw new Error("Invalid entitlement summary.");
  }
  const id = readString(record, "id", "Id");
  const organizationId = readString(record, "organizationId", "OrganizationId");
  const productCode = readString(record, "productCode", "ProductCode");
  const subscriptionId = readString(record, "subscriptionId", "SubscriptionId");
  const subscriptionStatus = readString(record, "subscriptionStatus", "SubscriptionStatus");
  const generatedAtUtc = readString(record, "generatedAtUtc", "GeneratedAtUtc");
  const effectiveAtUtc = readString(record, "effectiveAtUtc", "EffectiveAtUtc");
  const refreshByUtc = readString(record, "refreshByUtc", "RefreshByUtc");
  const snapshotVersion = readNumber(record, "snapshotVersion", "SnapshotVersion");
  const schemaVersion = readNumber(record, "schemaVersion", "SchemaVersion");
  if (
    !id ||
    !organizationId ||
    !productCode ||
    !subscriptionId ||
    !subscriptionStatus ||
    !generatedAtUtc ||
    !effectiveAtUtc ||
    !refreshByUtc ||
    snapshotVersion === undefined ||
    schemaVersion === undefined
  ) {
    throw new Error("Invalid entitlement summary.");
  }
  return {
    id,
    organizationId,
    productCode,
    subscriptionId,
    subscriptionStatus,
    snapshotVersion,
    schemaVersion,
    generatedAtUtc,
    effectiveAtUtc,
    refreshByUtc,
    expiresAtUtc: readString(record, "expiresAtUtc", "ExpiresAtUtc"),
    inGracePeriod: readBoolean(record, "inGracePeriod", "InGracePeriod"),
    organizationDisplayName: readString(record, "organizationDisplayName", "OrganizationDisplayName"),
    productDisplayName: readString(record, "productDisplayName", "ProductDisplayName"),
  };
}

export function parseEntitlementPortfolioSearchParams(
  params: URLSearchParams,
): EntitlementPortfolioUrlState {
  const pageRaw = Number(params.get("page") ?? "1");
  return {
    page: Number.isFinite(pageRaw) && pageRaw >= 1 ? Math.floor(pageRaw) : 1,
    pageSize: ENTITLEMENT_PORTFOLIO_PAGE_SIZE,
    sortBy: params.get("sortBy")?.trim() || "GeneratedAtUtc",
    sortDesc: params.get("sortDesc") !== "false",
  };
}

export function entitlementPortfolioSearchParams(
  state: EntitlementPortfolioUrlState,
): URLSearchParams {
  const params = new URLSearchParams();
  if (state.sortBy !== "GeneratedAtUtc") params.set("sortBy", state.sortBy);
  if (!state.sortDesc) params.set("sortDesc", "false");
  if (state.page > 1) params.set("page", String(state.page));
  return params;
}

export function listLatestEntitlements(
  baseUrl: string,
  state: EntitlementPortfolioUrlState,
  signal?: AbortSignal,
): Promise<PagedResult<EntitlementLatestSummary>> {
  return platformRequest<unknown>(baseUrl, {
    path: withQuery("/api/v1/platform/admin/entitlements/latest", {
      page: state.page,
      pageSize: state.pageSize,
      sortBy: state.sortBy,
      sortDesc: state.sortDesc,
    }),
    signal,
  }).then((payload) => {
    const page = parsePagedResult<unknown>(payload);
    return {
      ...page,
      items: page.items.map(mapEntitlementLatestSummary),
    };
  });
}

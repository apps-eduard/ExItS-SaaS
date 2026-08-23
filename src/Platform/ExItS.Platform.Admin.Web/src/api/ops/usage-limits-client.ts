import { parsePagedResult, type PagedResult } from "@/api/platform/paged-result";
import { platformRequest } from "@/api/platform-http";

export type UsageLimitUsageStatus = "Measured" | "NotInstrumented" | "Unavailable";

export type UsageLimitApiRow = {
  organizationId: string;
  organizationDisplayName: string;
  productCode: string;
  productDisplayName?: string | null;
  subscriptionId: string;
  subscriptionStatus: string;
  planDisplayName?: string | null;
  planKey?: string | null;
  featureCode: string;
  entitlementEnabled: boolean;
  numericLimit?: number | null;
  unlimited: boolean;
  usage?: number | null;
  usageStatus: UsageLimitUsageStatus;
  usagePercent?: number | null;
};

export type ListUsageLimitsOptions = {
  organizationId?: string;
  productCode?: string;
  page?: number;
  pageSize?: number;
  signal?: AbortSignal;
};

export const USAGE_LIMITS_PATH = "/api/v1/platform/operations/usage-limits";
export const USAGE_LIMITS_PAGE_SIZE = 25;

export function listUsageLimits(
  baseUrl: string,
  options: ListUsageLimitsOptions = {},
): Promise<PagedResult<UsageLimitApiRow>> {
  const params = new URLSearchParams();
  if (options.organizationId) {
    params.set("organizationId", options.organizationId);
  }
  if (options.productCode) {
    params.set("productCode", options.productCode);
  }
  if (options.page && options.page > 1) {
    params.set("page", String(options.page));
  }
  if (options.pageSize) {
    params.set("pageSize", String(options.pageSize));
  }

  const query = params.toString();
  return platformRequest<unknown>(baseUrl, {
    path: query ? `${USAGE_LIMITS_PATH}?${query}` : USAGE_LIMITS_PATH,
    signal: options.signal,
  }).then((payload) => parsePagedResult<UsageLimitApiRow>(payload));
}

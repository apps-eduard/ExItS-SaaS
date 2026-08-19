import { parsePagedResult, type PagedResult } from "@/api/platform/paged-result";
import { platformRequest } from "@/api/platform-http";
import {
  assertDashboardPageSize,
  subscriptionsListPath,
} from "@/features/overview/dashboard-bounds";

export type SubscriptionListItem = {
  id: string;
  status: string;
  organizationDisplayName?: string | null;
  productDisplayName?: string | null;
};

export function listSubscriptions(
  baseUrl: string,
  options: { status?: string; pageSize: number; signal?: AbortSignal },
): Promise<PagedResult<SubscriptionListItem>> {
  assertDashboardPageSize(options.pageSize);
  return platformRequest<unknown>(baseUrl, {
    path: subscriptionsListPath({ status: options.status, pageSize: options.pageSize }),
    signal: options.signal,
  }).then((payload) => parsePagedResult<SubscriptionListItem>(payload));
}

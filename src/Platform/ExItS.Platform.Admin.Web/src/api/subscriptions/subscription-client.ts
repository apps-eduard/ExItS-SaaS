import { parsePagedResult, type PagedResult } from "@/api/platform/paged-result";
import { platformRequest } from "@/api/platform-http";
import { mapOrganizationSubscription } from "@/api/organizations/organization-client";
import type { OrganizationSubscription } from "@/api/organizations/subscription-list-query";
import {
  assertDashboardPageSize,
  subscriptionsListPath,
} from "@/features/overview/dashboard-bounds";
import { withQuery } from "@/lib/http/query-string";
import type { SubscriptionPortfolioUrlState } from "@/api/subscriptions/subscription-portfolio-query";

export type SubscriptionListItem = {
  id: string;
  status: string;
  organizationDisplayName?: string | null;
  productDisplayName?: string | null;
};

/** Bounded dashboard summary list (legacy dashboard widget). */
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

export function subscriptionPortfolioRequestPath(state: SubscriptionPortfolioUrlState): string {
  return withQuery("/api/v1/platform/subscriptions", {
    page: state.page,
    pageSize: state.pageSize,
    status: state.status || undefined,
    productCode: state.productCode || undefined,
    search: state.search || undefined,
    isTrial: state.isTrial || undefined,
    planId: state.planId || undefined,
    sortBy: state.sortBy,
    sortDesc: state.sortDesc,
  });
}

export function listSubscriptionPortfolio(
  baseUrl: string,
  state: SubscriptionPortfolioUrlState,
  signal?: AbortSignal,
  organizationId?: string,
): Promise<PagedResult<OrganizationSubscription>> {
  return platformRequest<unknown>(baseUrl, {
    path: withQuery(subscriptionPortfolioRequestPath(state), {
      organizationId: organizationId || undefined,
    }),
    signal,
  }).then((payload) => {
    const page = parsePagedResult<unknown>(payload);
    return {
      ...page,
      items: page.items.map(mapOrganizationSubscription),
    };
  });
}

export function getSubscription(
  baseUrl: string,
  subscriptionId: string,
  signal?: AbortSignal,
): Promise<OrganizationSubscription> {
  return platformRequest<unknown>(baseUrl, {
    path: `/api/v1/platform/subscriptions/${subscriptionId}`,
    signal,
  }).then(mapOrganizationSubscription);
}

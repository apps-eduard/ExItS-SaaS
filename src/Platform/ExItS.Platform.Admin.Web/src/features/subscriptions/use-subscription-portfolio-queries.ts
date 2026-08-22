import { useQuery } from "@tanstack/react-query";
import {
  getSubscription,
  listSubscriptionPortfolio,
} from "@/api/subscriptions/subscription-client";
import type { SubscriptionPortfolioUrlState } from "@/api/subscriptions/subscription-portfolio-query";
import { env } from "@/lib/env";

export const subscriptionPortfolioQueryKey = (state: SubscriptionPortfolioUrlState) =>
  [
    "subscriptions",
    "portfolio",
    state.page,
    state.pageSize,
    state.search,
    state.status,
    state.isTrial,
    state.productCode,
    state.planId,
    state.sortBy,
    state.sortDesc,
  ] as const;

export const subscriptionDetailQueryKey = (subscriptionId: string) =>
  ["subscriptions", "detail", subscriptionId] as const;

export function useSubscriptionPortfolioQuery(
  state: SubscriptionPortfolioUrlState,
  enabled: boolean,
) {
  return useQuery({
    queryKey: subscriptionPortfolioQueryKey(state),
    enabled,
    queryFn: ({ signal }) => listSubscriptionPortfolio(env.platformApiBaseUrl, state, signal),
  });
}

export function useSubscriptionDetailQuery(subscriptionId: string | null, enabled: boolean) {
  return useQuery({
    queryKey: subscriptionId ? subscriptionDetailQueryKey(subscriptionId) : ["subscriptions", "detail"],
    enabled: enabled && subscriptionId != null,
    queryFn: ({ signal }) => getSubscription(env.platformApiBaseUrl, subscriptionId!, signal),
  });
}

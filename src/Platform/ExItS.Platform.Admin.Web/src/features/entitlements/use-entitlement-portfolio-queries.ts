import { useQuery } from "@tanstack/react-query";
import {
  listLatestEntitlements,
  type EntitlementPortfolioUrlState,
} from "@/api/entitlements/entitlement-portfolio-client";
import { env } from "@/lib/env";

export const entitlementPortfolioQueryKey = (state: EntitlementPortfolioUrlState) =>
  [
    "entitlements",
    "portfolio",
    state.page,
    state.pageSize,
    state.sortBy,
    state.sortDesc,
  ] as const;

export function useEntitlementPortfolioQuery(
  state: EntitlementPortfolioUrlState,
  enabled: boolean,
) {
  return useQuery({
    queryKey: entitlementPortfolioQueryKey(state),
    enabled,
    queryFn: ({ signal }) => listLatestEntitlements(env.platformApiBaseUrl, state, signal),
  });
}

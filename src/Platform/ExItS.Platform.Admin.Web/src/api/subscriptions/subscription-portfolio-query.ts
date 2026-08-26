import {
  DEFAULT_SUBSCRIPTION_SORT,
  isOrganizationSubscriptionSortBy,
  isOrganizationSubscriptionStatus,
  ORGANIZATION_SUBSCRIPTION_PAGE_SIZE,
  ORGANIZATION_SUBSCRIPTION_SORT_BY,
  ORGANIZATION_SUBSCRIPTION_STATUSES,
  type OrganizationSubscriptionSortBy,
  type OrganizationSubscriptionStatus,
} from "@/api/organizations/subscription-list-query";

export const SUBSCRIPTION_PORTFOLIO_PAGE_SIZE = ORGANIZATION_SUBSCRIPTION_PAGE_SIZE;
export const SUBSCRIPTION_PORTFOLIO_STATUSES = ORGANIZATION_SUBSCRIPTION_STATUSES;
export const SUBSCRIPTION_PORTFOLIO_SORT_BY = ORGANIZATION_SUBSCRIPTION_SORT_BY;

export type SubscriptionPortfolioUrlState = {
  page: number;
  pageSize: number;
  search: string;
  status: OrganizationSubscriptionStatus | "";
  isTrial: "" | "true" | "false";
  productCode: string;
  planId: string;
  sortBy: OrganizationSubscriptionSortBy;
  sortDesc: boolean;
};

function parsePage(raw: string | null): number {
  const value = Number(raw ?? "1");
  return Number.isFinite(value) && value >= 1 ? Math.floor(value) : 1;
}

export function parseSubscriptionPortfolioSearchParams(
  params: URLSearchParams,
): SubscriptionPortfolioUrlState {
  const statusRaw = params.get("status") ?? "";
  const sortRaw = params.get("sortBy") ?? DEFAULT_SUBSCRIPTION_SORT;
  const isTrialRaw = params.get("isTrial") ?? "";
  return {
    page: parsePage(params.get("page")),
    pageSize: SUBSCRIPTION_PORTFOLIO_PAGE_SIZE,
    search: params.get("search")?.trim() ?? "",
    status: isOrganizationSubscriptionStatus(statusRaw) ? statusRaw : "",
    isTrial: isTrialRaw === "true" || isTrialRaw === "false" ? isTrialRaw : "",
    productCode: params.get("productCode")?.trim() ?? "",
    planId: params.get("planId")?.trim() ?? "",
    sortBy: isOrganizationSubscriptionSortBy(sortRaw) ? sortRaw : DEFAULT_SUBSCRIPTION_SORT,
    sortDesc: params.get("sortDesc") !== "false",
  };
}

export function subscriptionPortfolioSearchParams(
  state: SubscriptionPortfolioUrlState,
): URLSearchParams {
  const params = new URLSearchParams();
  if (state.search) params.set("search", state.search);
  if (state.status) params.set("status", state.status);
  if (state.isTrial) params.set("isTrial", state.isTrial);
  if (state.productCode) params.set("productCode", state.productCode);
  if (state.planId) params.set("planId", state.planId);
  if (state.sortBy !== DEFAULT_SUBSCRIPTION_SORT) params.set("sortBy", state.sortBy);
  if (!state.sortDesc) params.set("sortDesc", "false");
  if (state.page > 1) params.set("page", String(state.page));
  return params;
}

export function hasActiveSubscriptionPortfolioFilters(state: SubscriptionPortfolioUrlState): boolean {
  return Boolean(state.search || state.status || state.isTrial || state.productCode || state.planId);
}

const GUID_PATTERN =
  /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/;

export function parseSubscriptionId(value: string | undefined): string | null {
  if (!value || !GUID_PATTERN.test(value)) {
    return null;
  }
  return value;
}

export function subscriptionDetailHref(subscriptionId: string): string {
  return `/admin/subscriptions/${subscriptionId}`;
}

export function subscriptionsListHref(): string {
  return "/admin/subscriptions";
}

import { withQuery } from "@/lib/http/query-string";

export const ORGANIZATION_SUBSCRIPTION_STATUSES = [
  "Trialing",
  "Active",
  "GracePeriod",
  "PastDue",
  "Suspended",
  "Cancelled",
  "Expired",
] as const;
export type OrganizationSubscriptionStatus = (typeof ORGANIZATION_SUBSCRIPTION_STATUSES)[number];

export const ORGANIZATION_SUBSCRIPTION_SORT_BY = [
  "UpdatedAtUtc",
  "CreatedAtUtc",
  "Status",
  "ProductCode",
  "TrialEndUtc",
  "PaidPeriodEndUtc",
  "ProductDisplayName",
  "PlanDisplayName",
] as const;
export type OrganizationSubscriptionSortBy = (typeof ORGANIZATION_SUBSCRIPTION_SORT_BY)[number];

export const ORGANIZATION_SUBSCRIPTION_PAGE_SIZE = 20;
export const DEFAULT_SUBSCRIPTION_SORT: OrganizationSubscriptionSortBy = "UpdatedAtUtc";

export type OrganizationSubscription = {
  id: string;
  organizationId: string;
  productCode: string;
  planId: string;
  status: string;
  createdAtUtc?: string;
  updatedAtUtc?: string;
  trialStartUtc?: string;
  trialEndUtc?: string;
  paidPeriodStartUtc?: string;
  paidPeriodEndUtc?: string;
  currentPeriodStartUtc?: string;
  currentPeriodEndUtc?: string;
  organizationDisplayName?: string;
  productDisplayName?: string;
  planDisplayName?: string;
  planKey?: string;
  planVersionId?: string;
  trialDefinitionId?: string;
  billingCycle?: string;
  agreedPrice?: number;
  currencyCode?: string;
  pendingPlanId?: string;
  pendingPlanEffectiveAtUtc?: string;
  gracePeriodEndUtc?: string;
  suspendedAtUtc?: string;
  pastDueAtUtc?: string;
  cancelledAtUtc?: string;
  expiredAtUtc?: string;
  version?: number;
};

export type OrganizationSubscriptionUrlState = {
  page: number;
  search: string;
  status: OrganizationSubscriptionStatus | "";
  isTrial: "" | "true" | "false";
  productCode: string;
  sortBy: OrganizationSubscriptionSortBy;
  sortDesc: boolean;
};

export function isOrganizationSubscriptionStatus(
  value: string,
): value is OrganizationSubscriptionStatus {
  return (ORGANIZATION_SUBSCRIPTION_STATUSES as readonly string[]).includes(value);
}

export function isOrganizationSubscriptionSortBy(
  value: string,
): value is OrganizationSubscriptionSortBy {
  return (ORGANIZATION_SUBSCRIPTION_SORT_BY as readonly string[]).includes(value);
}

function parsePage(raw: string | null): number {
  const value = Number(raw ?? "1");
  return Number.isFinite(value) && value >= 1 ? Math.floor(value) : 1;
}

export function parseOrganizationSubscriptionSearchParams(
  params: URLSearchParams,
): OrganizationSubscriptionUrlState {
  const statusRaw = params.get("status") ?? "";
  const sortRaw = params.get("sortBy") ?? DEFAULT_SUBSCRIPTION_SORT;
  const isTrialRaw = params.get("isTrial") ?? "";
  return {
    page: parsePage(params.get("page")),
    search: params.get("search")?.trim() ?? "",
    status: isOrganizationSubscriptionStatus(statusRaw) ? statusRaw : "",
    isTrial: isTrialRaw === "true" || isTrialRaw === "false" ? isTrialRaw : "",
    productCode: params.get("productCode")?.trim() ?? "",
    sortBy: isOrganizationSubscriptionSortBy(sortRaw) ? sortRaw : DEFAULT_SUBSCRIPTION_SORT,
    sortDesc: params.get("sortDesc") !== "false",
  };
}

export function organizationSubscriptionSearchParams(
  state: OrganizationSubscriptionUrlState,
): URLSearchParams {
  const params = new URLSearchParams();
  if (state.search) {
    params.set("search", state.search);
  }
  if (state.status) {
    params.set("status", state.status);
  }
  if (state.isTrial) {
    params.set("isTrial", state.isTrial);
  }
  if (state.productCode) {
    params.set("productCode", state.productCode);
  }
  if (state.sortBy !== DEFAULT_SUBSCRIPTION_SORT) {
    params.set("sortBy", state.sortBy);
  }
  if (!state.sortDesc) {
    params.set("sortDesc", "false");
  }
  if (state.page > 1) {
    params.set("page", String(state.page));
  }
  return params;
}

export function hasActiveSubscriptionFilters(state: OrganizationSubscriptionUrlState): boolean {
  return Boolean(state.search || state.status || state.isTrial || state.productCode);
}

export function organizationSubscriptionsRequestPath(
  organizationId: string,
  state: OrganizationSubscriptionUrlState,
): string {
  return withQuery(`/api/v1/platform/organizations/${organizationId}/subscriptions`, {
    status: state.status || undefined,
    search: state.search || undefined,
    isTrial: state.isTrial === "" ? undefined : state.isTrial === "true",
    productCode: state.productCode || undefined,
    sortBy: state.sortBy,
    sortDesc: state.sortDesc,
    page: state.page,
    pageSize: ORGANIZATION_SUBSCRIPTION_PAGE_SIZE,
  });
}

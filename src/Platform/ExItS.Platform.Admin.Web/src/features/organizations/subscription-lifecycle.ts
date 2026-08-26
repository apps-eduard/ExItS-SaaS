import type { CatalogPlan } from "@/api/catalog/plan-catalog-types";
import type { OrganizationSubscription } from "@/api/organizations/subscription-list-query";
import { normalizeOrganizationSubscriptionStatus } from "@/features/organizations/organization-subscription-status";

export const PINOY_BUSINESS_POS_PRODUCT_CODE = "pinoy-business-pos";

export const ACTIVE_LIKE_SUBSCRIPTION_STATUSES = [
  "Trialing",
  "Active",
  "GracePeriod",
  "PastDue",
  "Suspended",
] as const;

export type ActiveLikeSubscriptionStatus = (typeof ACTIVE_LIKE_SUBSCRIPTION_STATUSES)[number];

export type SubscriptionSupportAction = "gracePeriod" | "pastDue" | "expire";

export type SubscriptionLifecycleCapabilities = {
  changePlan: boolean;
  applyPending: boolean;
  convertTrial: boolean;
  suspend: boolean;
  reactivate: boolean;
  cancel: boolean;
  supportActions: SubscriptionSupportAction[];
};

const SUPPORT_EMPTY: SubscriptionSupportAction[] = [];

export function isActiveLikeSubscriptionStatus(status: string): boolean {
  const normalized = normalizeOrganizationSubscriptionStatus(status);
  return (ACTIVE_LIKE_SUBSCRIPTION_STATUSES as readonly string[]).includes(normalized);
}

export function isPinoyBusinessPosSubscription(item: {
  productCode: string;
  productDisplayName?: string;
}): boolean {
  if (item.productCode === PINOY_BUSINESS_POS_PRODUCT_CODE) {
    return true;
  }
  return (item.productDisplayName ?? "").toLowerCase().includes("pinoy business pos");
}

export function organizationHasPinoyBusinessPosSubscription(
  items: Array<{ productCode: string; productDisplayName?: string }>,
): boolean {
  return items.some(isPinoyBusinessPosSubscription);
}

export function subscriptionLifecycleCapabilities(
  status: string,
  pendingPlanId?: string | null,
): SubscriptionLifecycleCapabilities {
  const normalized = normalizeOrganizationSubscriptionStatus(status);
  const activeLike = isActiveLikeSubscriptionStatus(normalized);
  const supportActions: SubscriptionSupportAction[] = [];

  if (normalized === "Trialing" || normalized === "Active" || normalized === "PastDue") {
    supportActions.push("gracePeriod");
  }
  if (normalized === "Trialing" || normalized === "Active" || normalized === "GracePeriod") {
    supportActions.push("pastDue");
  }
  if (
    normalized === "Trialing" ||
    normalized === "Active" ||
    normalized === "GracePeriod" ||
    normalized === "PastDue" ||
    normalized === "Suspended"
  ) {
    supportActions.push("expire");
  }

  const planMutable = normalized === "Trialing" || normalized === "Active";

  return {
    changePlan: planMutable,
    applyPending: Boolean(pendingPlanId) && planMutable,
    convertTrial: normalized === "Trialing",
    suspend:
      normalized === "Trialing" ||
      normalized === "Active" ||
      normalized === "GracePeriod" ||
      normalized === "PastDue",
    reactivate: normalized === "Suspended",
    cancel: activeLike,
    supportActions: supportActions.length > 0 ? supportActions : SUPPORT_EMPTY,
  };
}

export function trialEligiblePlans(plans: CatalogPlan[]): CatalogPlan[] {
  return plans
    .filter((plan) => plan.status === "Active" && plan.trialAllowed === true)
    .slice()
    .sort((left, right) => (left.sortOrder ?? 100) - (right.sortOrder ?? 100));
}

export function publishedPlanVersionId(
  versions: Array<{ id: string; status: string }>,
): string | undefined {
  return versions.find((version) => version.status === "Published")?.id;
}

export function comparePlanCommercialRank(current: CatalogPlan, target: CatalogPlan): number {
  const deviceDelta = (target.maxActivePosDevices ?? 0) - (current.maxActivePosDevices ?? 0);
  if (deviceDelta !== 0) {
    return deviceDelta;
  }
  const priceDelta = (target.monthlyPrice ?? 0) - (current.monthlyPrice ?? 0);
  if (priceDelta !== 0) {
    return priceDelta;
  }
  return (target.sortOrder ?? 0) - (current.sortOrder ?? 0);
}

export function planChangeDirection(
  current: CatalogPlan | undefined,
  target: CatalogPlan | undefined,
): "upgrade" | "downgrade" | "same" {
  if (!current || !target || current.id === target.id) {
    return "same";
  }
  const rank = comparePlanCommercialRank(current, target);
  if (rank > 0) {
    return "upgrade";
  }
  if (rank < 0) {
    return "downgrade";
  }
  return "same";
}

export function findCatalogPlan(
  plans: CatalogPlan[],
  planId: string | undefined,
): CatalogPlan | undefined {
  if (!planId) {
    return undefined;
  }
  return plans.find((plan) => plan.id === planId);
}

export function subscriptionPeriodEnd(item: OrganizationSubscription): string | undefined {
  return item.currentPeriodEndUtc || item.paidPeriodEndUtc || item.trialEndUtc;
}

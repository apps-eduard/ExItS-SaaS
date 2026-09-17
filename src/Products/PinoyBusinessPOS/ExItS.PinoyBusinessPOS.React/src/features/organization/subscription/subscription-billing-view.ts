/**
 * Pure presentation mapping for Organization Subscription & Billing.
 * Limits and statuses always come from the Platform current-plan payload —
 * this module never invents plan limits, prices, or invoice records.
 */

import type {
  OrganizationPlanDto,
  OrganizationSubscriptionDto,
} from "@/api/platform/organization-current-plan-client";
import type { MessageKey } from "@/i18n/messages";

export const SUBSCRIPTION_TABS = ["overview", "plan", "billing", "invoices"] as const;

export type SubscriptionTabId = (typeof SUBSCRIPTION_TABS)[number];

export const SUBSCRIPTION_TAB_LABEL_KEYS: Record<SubscriptionTabId, MessageKey> = {
  overview: "orgSubscription.tab.overview",
  plan: "orgSubscription.tab.plan",
  billing: "orgSubscription.tab.billing",
  invoices: "orgSubscription.tab.invoices",
};

export function parseSubscriptionTab(raw: string | null | undefined): SubscriptionTabId {
  const value = raw?.trim().toLowerCase() ?? "";
  return (SUBSCRIPTION_TABS as ReadonlyArray<string>).includes(value)
    ? (value as SubscriptionTabId)
    : "overview";
}

export type SubscriptionHealth =
  | "active"
  | "trial"
  | "pastDue"
  | "suspended"
  | "ended"
  | "unknown";

const HEALTH_BY_STATUS: Record<string, SubscriptionHealth> = {
  active: "active",
  trialing: "trial",
  trial: "trial",
  pastdue: "pastDue",
  past_due: "pastDue",
  suspended: "suspended",
  cancelled: "ended",
  canceled: "ended",
  expired: "ended",
};

export function classifySubscriptionStatus(
  status: string | null | undefined,
): SubscriptionHealth {
  const key = status?.trim().toLowerCase().replace(/\s+/g, "") ?? "";
  return HEALTH_BY_STATUS[key] ?? "unknown";
}

export type SubscriptionStatusTone = "success" | "info" | "warning" | "danger" | "neutral";

export function resolveSubscriptionStatusTone(
  status: string | null | undefined,
): SubscriptionStatusTone {
  switch (classifySubscriptionStatus(status)) {
    case "active":
      return "success";
    case "trial":
      return "info";
    case "pastDue":
      return "warning";
    case "suspended":
      return "danger";
    case "ended":
      return "neutral";
    default:
      return "neutral";
  }
}

/** Usage is only actionable once the plan grants a positive allowance. */
export const CAPACITY_NEAR_LIMIT_RATIO = 0.8;

export type CapacityDimension = "branches" | "staff" | "devices" | "areas";

export const CAPACITY_DIMENSION_ORDER: CapacityDimension[] = [
  "branches",
  "staff",
  "devices",
  "areas",
];

export const CAPACITY_LABEL_KEYS: Record<CapacityDimension, MessageKey> = {
  branches: "admin.context.branches",
  staff: "org.glance.staff",
  devices: "admin.context.devices",
  areas: "admin.context.areas",
};

export type CapacityInput = { used: number; allowed: number } | null | undefined;

export type CapacityUsage = {
  dimension: CapacityDimension;
  used: number;
  allowed: number;
  percent: number;
  nearLimit: boolean;
  atLimit: boolean;
};

export function buildCapacityUsage(
  inputs: Partial<Record<CapacityDimension, CapacityInput>>,
): CapacityUsage[] {
  const rows: CapacityUsage[] = [];
  for (const dimension of CAPACITY_DIMENSION_ORDER) {
    const input = inputs[dimension];
    if (!input) continue;
    const used = Math.max(0, Math.trunc(input.used));
    const allowed = Math.max(0, Math.trunc(input.allowed));
    if (allowed <= 0) continue;
    const percent = Math.min(100, Math.round((used / allowed) * 100));
    const atLimit = used >= allowed;
    rows.push({
      dimension,
      used,
      allowed,
      percent,
      atLimit,
      nearLimit: !atLimit && used / allowed >= CAPACITY_NEAR_LIMIT_RATIO,
    });
  }
  return rows;
}

export function capacityAtLimitDimensions(rows: ReadonlyArray<CapacityUsage>): CapacityDimension[] {
  return rows.filter((row) => row.atLimit).map((row) => row.dimension);
}

export type PlanFeatureId = "customerCredit" | "advancedReports" | "export";

export const PLAN_FEATURE_LABEL_KEYS: Record<PlanFeatureId, MessageKey> = {
  customerCredit: "orgSubscription.feature.customerCredit",
  advancedReports: "orgSubscription.feature.advancedReports",
  export: "orgSubscription.feature.export",
};

export type PlanFeatureRow = { id: PlanFeatureId; included: boolean };

export function buildPlanFeatureRows(
  plan: OrganizationPlanDto | null | undefined,
): PlanFeatureRow[] {
  if (!plan) return [];
  return [
    { id: "customerCredit", included: plan.customerCreditEnabled },
    { id: "advancedReports", included: plan.advancedReportsEnabled },
    { id: "export", included: plan.exportEnabled },
  ];
}

/**
 * Next money date shown on Overview / Billing. Renewal wins; otherwise the end of the
 * current paid period, then the trial end. Returns null when Platform reports none.
 */
export function resolveNextPaymentDate(
  subscription: OrganizationSubscriptionDto | null | undefined,
): string | null {
  if (!subscription) return null;
  return (
    subscription.renewalDateUtc ??
    subscription.currentPeriodEndUtc ??
    subscription.paidPeriodEndUtc ??
    subscription.trialEndUtc ??
    null
  );
}

export function formatSubscriptionDate(iso: string | null | undefined): string | null {
  if (!iso) return null;
  const parsed = new Date(iso);
  if (Number.isNaN(parsed.getTime())) return null;
  return parsed.toLocaleDateString(undefined, {
    year: "numeric",
    month: "short",
    day: "numeric",
  });
}

export type PlanChangeDirection = "upgrade" | "downgrade" | "same" | "unknown";

export function comparePlanTier(
  current: OrganizationPlanDto | null | undefined,
  target: OrganizationPlanDto | null | undefined,
): PlanChangeDirection {
  if (!current || !target) return "unknown";
  if (current.id && target.id && current.id === target.id) return "same";
  if (current.sortOrder !== target.sortOrder) {
    return target.sortOrder > current.sortOrder ? "upgrade" : "downgrade";
  }
  if (current.monthlyPrice !== target.monthlyPrice) {
    return target.monthlyPrice > current.monthlyPrice ? "upgrade" : "downgrade";
  }
  return "same";
}

/** Plans the Owner may compare against — the current plan is shown separately. */
export function selectableAvailablePlans(
  availablePlans: ReadonlyArray<OrganizationPlanDto>,
  current: OrganizationPlanDto | null | undefined,
): OrganizationPlanDto[] {
  return availablePlans.filter((plan) => !(current?.id && plan.id === current.id));
}

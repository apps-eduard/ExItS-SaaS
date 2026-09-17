import {
  platformRequest,
  PlatformApiError,
  type PlatformProblemDetails,
} from "@/api/platform/platform-http";
import { POS_PRODUCT_CODE } from "@/api/platform/browser-session";

/** Commercial plan as returned inside the organization current-plan payload. */
export type OrganizationPlanDto = {
  id: string | null;
  planKey: string | null;
  code: string | null;
  displayName: string | null;
  description: string | null;
  status: string | null;
  maxBranches: number;
  maxActiveStaff: number;
  maxActivePosDevices: number;
  maxActiveBusinessTypes: number;
  maxAreas: number;
  customerCreditEnabled: boolean;
  advancedReportsEnabled: boolean;
  exportEnabled: boolean;
  trialAllowed: boolean;
  defaultTrialDays: number;
  sortOrder: number;
  monthlyPrice: number;
  annualPrice: number;
  currencyCode: string;
};

export type OrganizationSubscriptionDto = {
  id: string | null;
  status: string | null;
  billingCycle: string | null;
  agreedPrice: number | null;
  currencyCode: string | null;
  planId: string | null;
  planKey: string | null;
  planDisplayName: string | null;
  trialStartUtc: string | null;
  trialEndUtc: string | null;
  paidPeriodStartUtc: string | null;
  paidPeriodEndUtc: string | null;
  currentPeriodStartUtc: string | null;
  currentPeriodEndUtc: string | null;
  gracePeriodEndUtc: string | null;
  renewalDateUtc: string | null;
  suspendedAtUtc: string | null;
  pastDueAtUtc: string | null;
  cancelledAtUtc: string | null;
  expiredAtUtc: string | null;
};

export type OrganizationPendingPlanChangeDto = {
  planId: string | null;
  planKey: string | null;
  displayName: string | null;
  effectiveAtUtc: string | null;
};

export type OrganizationEntitlementSummaryDto = {
  subscriptionId: string | null;
  subscriptionStatus: string | null;
  inGracePeriod: boolean;
  generatedAtUtc: string | null;
  effectiveAtUtc: string | null;
  refreshByUtc: string | null;
  expiresAtUtc: string | null;
};

export type OrganizationCurrentPlanDto = {
  organizationId: string;
  productCode: string;
  currentSubscription: OrganizationSubscriptionDto | null;
  currentPlan: OrganizationPlanDto | null;
  pendingPlanChange: OrganizationPendingPlanChangeDto | null;
  availablePlans: OrganizationPlanDto[];
  entitlement: OrganizationEntitlementSummaryDto | null;
  productInstancePresent: boolean | null;
  /** Flat compatibility view used by the Manage Business overview. */
  planDisplayName: string | null;
  planKey: string | null;
  subscriptionStatus: string | null;
};

type OrganizationCurrentPlanClientResult<T> =
  | { ok: true; value: T }
  | { ok: false; status: number; body: PlatformProblemDetails | null };

function asRecord(value: unknown): Record<string, unknown> | null {
  return typeof value === "object" && value !== null ? (value as Record<string, unknown>) : null;
}

function pick(raw: Record<string, unknown>, camel: string, pascal: string): unknown {
  return raw[camel] ?? raw[pascal];
}

function pickString(raw: Record<string, unknown>, camel: string, pascal: string): string | null {
  const value = pick(raw, camel, pascal);
  if (typeof value !== "string") {
    return null;
  }
  const trimmed = value.trim();
  return trimmed.length > 0 ? trimmed : null;
}

/** Timestamps arrive as ISO strings; tolerate Date-like payloads from other serializers. */
function pickInstant(raw: Record<string, unknown>, camel: string, pascal: string): string | null {
  const value = pick(raw, camel, pascal);
  if (value == null) return null;
  if (typeof value === "string") {
    const trimmed = value.trim();
    return trimmed.length > 0 ? trimmed : null;
  }
  if (value instanceof Date) return value.toISOString();
  return null;
}

function pickNumber(
  raw: Record<string, unknown>,
  camel: string,
  pascal: string,
  fallback: number,
): number {
  const value = pick(raw, camel, pascal);
  if (value == null || value === "") return fallback;
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : fallback;
}

function pickNullableNumber(
  raw: Record<string, unknown>,
  camel: string,
  pascal: string,
): number | null {
  const value = pick(raw, camel, pascal);
  if (value == null || value === "") return null;
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : null;
}

function pickBoolean(raw: Record<string, unknown>, camel: string, pascal: string): boolean {
  return pick(raw, camel, pascal) === true;
}

function normalizePlan(raw: unknown): OrganizationPlanDto | null {
  const r = asRecord(raw);
  if (!r) return null;
  return {
    id: pickString(r, "id", "Id"),
    planKey: pickString(r, "planKey", "PlanKey"),
    code: pickString(r, "code", "Code"),
    displayName: pickString(r, "displayName", "DisplayName"),
    description: pickString(r, "description", "Description"),
    status: pickString(r, "status", "Status"),
    maxBranches: pickNumber(r, "maxBranches", "MaxBranches", 0),
    maxActiveStaff: pickNumber(r, "maxActiveStaff", "MaxActiveStaff", 0),
    maxActivePosDevices: pickNumber(r, "maxActivePosDevices", "MaxActivePosDevices", 0),
    maxActiveBusinessTypes: pickNumber(r, "maxActiveBusinessTypes", "MaxActiveBusinessTypes", 0),
    maxAreas: pickNumber(r, "maxAreas", "MaxAreas", 0),
    customerCreditEnabled: pickBoolean(r, "customerCreditEnabled", "CustomerCreditEnabled"),
    advancedReportsEnabled: pickBoolean(r, "advancedReportsEnabled", "AdvancedReportsEnabled"),
    exportEnabled: pickBoolean(r, "exportEnabled", "ExportEnabled"),
    trialAllowed: pickBoolean(r, "trialAllowed", "TrialAllowed"),
    defaultTrialDays: pickNumber(r, "defaultTrialDays", "DefaultTrialDays", 0),
    sortOrder: pickNumber(r, "sortOrder", "SortOrder", 100),
    monthlyPrice: pickNumber(r, "monthlyPrice", "MonthlyPrice", 0),
    annualPrice: pickNumber(r, "annualPrice", "AnnualPrice", 0),
    currencyCode: pickString(r, "currencyCode", "CurrencyCode") ?? "PHP",
  };
}

function normalizeSubscription(raw: unknown): OrganizationSubscriptionDto | null {
  const r = asRecord(raw);
  if (!r) return null;
  return {
    id: pickString(r, "id", "Id"),
    status: pickString(r, "status", "Status"),
    billingCycle: pickString(r, "billingCycle", "BillingCycle"),
    agreedPrice: pickNullableNumber(r, "agreedPrice", "AgreedPrice"),
    currencyCode: pickString(r, "currencyCode", "CurrencyCode"),
    planId: pickString(r, "planId", "PlanId"),
    planKey: pickString(r, "planKey", "PlanKey"),
    planDisplayName: pickString(r, "planDisplayName", "PlanDisplayName"),
    trialStartUtc: pickInstant(r, "trialStartUtc", "TrialStartUtc"),
    trialEndUtc: pickInstant(r, "trialEndUtc", "TrialEndUtc"),
    paidPeriodStartUtc: pickInstant(r, "paidPeriodStartUtc", "PaidPeriodStartUtc"),
    paidPeriodEndUtc: pickInstant(r, "paidPeriodEndUtc", "PaidPeriodEndUtc"),
    currentPeriodStartUtc: pickInstant(r, "currentPeriodStartUtc", "CurrentPeriodStartUtc"),
    currentPeriodEndUtc: pickInstant(r, "currentPeriodEndUtc", "CurrentPeriodEndUtc"),
    gracePeriodEndUtc: pickInstant(r, "gracePeriodEndUtc", "GracePeriodEndUtc"),
    renewalDateUtc: pickInstant(r, "renewalDateUtc", "RenewalDateUtc"),
    suspendedAtUtc: pickInstant(r, "suspendedAtUtc", "SuspendedAtUtc"),
    pastDueAtUtc: pickInstant(r, "pastDueAtUtc", "PastDueAtUtc"),
    cancelledAtUtc: pickInstant(r, "cancelledAtUtc", "CancelledAtUtc"),
    expiredAtUtc: pickInstant(r, "expiredAtUtc", "ExpiredAtUtc"),
  };
}

function normalizePendingPlanChange(raw: unknown): OrganizationPendingPlanChangeDto | null {
  const r = asRecord(raw);
  if (!r) return null;
  const planId = pickString(r, "planId", "PlanId");
  const planKey = pickString(r, "planKey", "PlanKey");
  const displayName = pickString(r, "displayName", "DisplayName");
  const effectiveAtUtc = pickInstant(r, "effectiveAtUtc", "EffectiveAtUtc");
  if (!planId && !planKey && !displayName) {
    return null;
  }
  return { planId, planKey, displayName, effectiveAtUtc };
}

function normalizeEntitlement(raw: unknown): OrganizationEntitlementSummaryDto | null {
  const r = asRecord(raw);
  if (!r) return null;
  return {
    subscriptionId: pickString(r, "subscriptionId", "SubscriptionId"),
    subscriptionStatus: pickString(r, "subscriptionStatus", "SubscriptionStatus"),
    inGracePeriod: pickBoolean(r, "inGracePeriod", "InGracePeriod"),
    generatedAtUtc: pickInstant(r, "generatedAtUtc", "GeneratedAtUtc"),
    effectiveAtUtc: pickInstant(r, "effectiveAtUtc", "EffectiveAtUtc"),
    refreshByUtc: pickInstant(r, "refreshByUtc", "RefreshByUtc"),
    expiresAtUtc: pickInstant(r, "expiresAtUtc", "ExpiresAtUtc"),
  };
}

function normalizeAvailablePlans(raw: unknown): OrganizationPlanDto[] {
  if (!Array.isArray(raw)) return [];
  return raw
    .map(normalizePlan)
    .filter((plan): plan is OrganizationPlanDto => plan != null)
    .sort(
      (a, b) =>
        a.sortOrder - b.sortOrder ||
        (a.displayName ?? "").localeCompare(b.displayName ?? ""),
    );
}

export function normalizeOrganizationCurrentPlan(
  raw: Record<string, unknown>,
): OrganizationCurrentPlanDto {
  const currentPlan = normalizePlan(pick(raw, "currentPlan", "CurrentPlan"));
  const currentSubscription = normalizeSubscription(
    pick(raw, "currentSubscription", "CurrentSubscription"),
  );
  const productInstancePresent = pick(raw, "productInstancePresent", "ProductInstancePresent");

  return {
    organizationId: String(raw.organizationId ?? raw.OrganizationId ?? ""),
    productCode: String(raw.productCode ?? raw.ProductCode ?? POS_PRODUCT_CODE),
    currentSubscription,
    currentPlan,
    pendingPlanChange: normalizePendingPlanChange(
      pick(raw, "pendingPlanChange", "PendingPlanChange"),
    ),
    availablePlans: normalizeAvailablePlans(pick(raw, "availablePlans", "AvailablePlans")),
    entitlement: normalizeEntitlement(pick(raw, "entitlement", "Entitlement")),
    productInstancePresent:
      typeof productInstancePresent === "boolean" ? productInstancePresent : null,
    planDisplayName: currentPlan?.displayName ?? currentSubscription?.planDisplayName ?? null,
    planKey: currentPlan?.planKey ?? currentSubscription?.planKey ?? null,
    subscriptionStatus: currentSubscription?.status ?? null,
  };
}

export async function getOrganizationCurrentPlan(
  organizationId: string,
  signal?: AbortSignal,
): Promise<OrganizationCurrentPlanClientResult<OrganizationCurrentPlanDto>> {
  try {
    const payload = await platformRequest<Record<string, unknown>>({
      method: "GET",
      path: `/api/v1/platform/organizations/${organizationId}/current-plan?productCode=${encodeURIComponent(POS_PRODUCT_CODE)}`,
      signal,
    });
    return { ok: true, value: normalizeOrganizationCurrentPlan(payload) };
  } catch (error) {
    if (error instanceof PlatformApiError) {
      return { ok: false, status: error.status, body: error.problem };
    }
    throw error;
  }
}

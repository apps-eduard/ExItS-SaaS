import { mapOrganizationSubscription } from "@/api/organizations/organization-client";
import type { OrganizationSubscription } from "@/api/organizations/subscription-list-query";
import { commercialMutationRequest } from "@/api/commercial/commercial-http";
import { platformRequest } from "@/api/platform-http";

function requireSubscription(payload: unknown): OrganizationSubscription {
  return mapOrganizationSubscription(payload);
}

function orgSubscriptionPath(organizationId: string, suffix: string): string {
  return `/api/v1/platform/organizations/${organizationId}/subscriptions${suffix}`;
}

function topLevelSubscriptionPath(subscriptionId: string, action: string): string {
  return `/api/v1/platform/subscriptions/${subscriptionId}/${action}`;
}

export type StartTrialBody = {
  planId: string;
  planVersionId: string;
  trialDefinitionId: string;
};

export type CreatePaidSubscriptionBody = {
  planId: string;
  planVersionId: string;
  periodStartUtc: string;
  periodEndUtc: string;
  paymentId: string;
  billingCycle?: string | null;
};

export type StartFromCatalogBody = {
  productCode?: string | null;
  planKey?: string | null;
  planId?: string | null;
  billingCycle?: string | null;
  startAsTrial?: boolean;
  payNow?: boolean;
  idempotencyKey?: string | null;
};

export type UpgradeSubscriptionBody = {
  planId?: string | null;
  planKey?: string | null;
  billingCycle?: string | null;
  idempotencyKey?: string | null;
};

export type DowngradeSubscriptionBody = {
  planId?: string | null;
  planKey?: string | null;
  effectiveAtUtc?: string | null;
  idempotencyKey?: string | null;
};

export type ConvertTrialBody = {
  planId?: string | null;
  planKey?: string | null;
  billingCycle?: string | null;
  idempotencyKey?: string | null;
  expectedVersion?: number | null;
};

export type SubscriptionLifecycleBody = {
  expectedVersion?: number | null;
};

export type GracePeriodBody = {
  gracePeriodEndUtc: string;
  expectedVersion?: number | null;
};

export type ReactivateSubscriptionBody = {
  periodStartUtc?: string | null;
  periodEndUtc?: string | null;
  expectedVersion?: number | null;
};

/**
 * Typed for completeness. Server ActivateSubscription always fails with
 * application.payment.required_for_paid_activation. Do not expose as UI Activate.
 */
export type ActivateSubscriptionBody = {
  periodStartUtc: string;
  periodEndUtc: string;
  expectedVersion?: number | null;
};

export function startTrialSubscription(
  baseUrl: string,
  organizationId: string,
  body: StartTrialBody,
  signal?: AbortSignal,
): Promise<OrganizationSubscription> {
  return commercialMutationRequest<unknown>(baseUrl, {
    method: "POST",
    path: orgSubscriptionPath(organizationId, "/trials"),
    body,
    signal,
  }).then(requireSubscription);
}

export function createPaidSubscription(
  baseUrl: string,
  organizationId: string,
  body: CreatePaidSubscriptionBody,
  signal?: AbortSignal,
): Promise<OrganizationSubscription> {
  return commercialMutationRequest<unknown>(baseUrl, {
    method: "POST",
    path: orgSubscriptionPath(organizationId, ""),
    body,
    signal,
  }).then(requireSubscription);
}

export function startSubscriptionFromCatalog(
  baseUrl: string,
  organizationId: string,
  body: StartFromCatalogBody,
  signal?: AbortSignal,
): Promise<OrganizationSubscription> {
  return commercialMutationRequest<unknown>(baseUrl, {
    method: "POST",
    path: orgSubscriptionPath(organizationId, "/from-catalog"),
    body,
    signal,
  }).then(requireSubscription);
}

export function upgradeOrganizationSubscription(
  baseUrl: string,
  organizationId: string,
  subscriptionId: string,
  body: UpgradeSubscriptionBody,
  signal?: AbortSignal,
): Promise<OrganizationSubscription> {
  return commercialMutationRequest<unknown>(baseUrl, {
    method: "POST",
    path: orgSubscriptionPath(organizationId, `/${subscriptionId}/upgrade`),
    body,
    signal,
  }).then(requireSubscription);
}

export function scheduleSubscriptionDowngrade(
  baseUrl: string,
  organizationId: string,
  subscriptionId: string,
  body: DowngradeSubscriptionBody,
  signal?: AbortSignal,
): Promise<OrganizationSubscription> {
  return commercialMutationRequest<unknown>(baseUrl, {
    method: "POST",
    path: orgSubscriptionPath(organizationId, `/${subscriptionId}/downgrade`),
    body,
    signal,
  }).then(requireSubscription);
}

export function convertTrialSubscription(
  baseUrl: string,
  organizationId: string,
  subscriptionId: string,
  body: ConvertTrialBody,
  signal?: AbortSignal,
): Promise<OrganizationSubscription> {
  return commercialMutationRequest<unknown>(baseUrl, {
    method: "POST",
    path: orgSubscriptionPath(organizationId, `/${subscriptionId}/convert-trial`),
    body,
    signal,
  }).then(requireSubscription);
}

export function applyPendingPlanChange(
  baseUrl: string,
  organizationId: string,
  subscriptionId: string,
  signal?: AbortSignal,
): Promise<OrganizationSubscription> {
  return commercialMutationRequest<unknown>(baseUrl, {
    method: "POST",
    path: orgSubscriptionPath(organizationId, `/${subscriptionId}/apply-pending-plan`),
    signal,
  }).then(requireSubscription);
}

export type PlanUsageConflict = {
  kind: string;
  currentUsage: number;
  targetLimit: number;
  message: string;
};

export type PlanChangePreview = {
  currentPlanId: string;
  currentPlanDisplayName: string;
  targetPlanId: string;
  targetPlanDisplayName: string;
  activeStaffCount?: number;
  activeBranchCount?: number;
  branchCountAvailable?: boolean;
  usageConflicts: PlanUsageConflict[];
  lostFeatures: string[];
  hasBlockingUsageConflicts: boolean;
};

function asPreviewRecord(payload: unknown): Record<string, unknown> | null {
  return typeof payload === "object" && payload !== null
    ? (payload as Record<string, unknown>)
    : null;
}

function readPreviewString(record: Record<string, unknown>, ...keys: string[]): string | undefined {
  for (const key of keys) {
    const value = record[key];
    if (typeof value === "string" && value.length > 0) {
      return value;
    }
  }
  return undefined;
}

function readPreviewNumber(record: Record<string, unknown>, ...keys: string[]): number | undefined {
  for (const key of keys) {
    const value = record[key];
    if (typeof value === "number" && Number.isFinite(value)) {
      return value;
    }
  }
  return undefined;
}

function readPreviewBoolean(
  record: Record<string, unknown>,
  ...keys: string[]
): boolean | undefined {
  for (const key of keys) {
    const value = record[key];
    if (typeof value === "boolean") {
      return value;
    }
  }
  return undefined;
}

export function mapPlanChangePreview(payload: unknown): PlanChangePreview {
  const record = asPreviewRecord(payload);
  if (!record) {
    throw new Error("Invalid plan change preview.");
  }
  const currentPlanId = readPreviewString(record, "currentPlanId", "CurrentPlanId");
  const currentPlanDisplayName = readPreviewString(
    record,
    "currentPlanDisplayName",
    "CurrentPlanDisplayName",
  );
  const targetPlanId = readPreviewString(record, "targetPlanId", "TargetPlanId");
  const targetPlanDisplayName = readPreviewString(
    record,
    "targetPlanDisplayName",
    "TargetPlanDisplayName",
  );
  if (!currentPlanId || !currentPlanDisplayName || !targetPlanId || !targetPlanDisplayName) {
    throw new Error("Invalid plan change preview.");
  }
  const conflictsPayload = record.usageConflicts ?? record.UsageConflicts;
  const lostPayload = record.lostFeatures ?? record.LostFeatures;
  return {
    currentPlanId,
    currentPlanDisplayName,
    targetPlanId,
    targetPlanDisplayName,
    activeStaffCount: readPreviewNumber(record, "activeStaffCount", "ActiveStaffCount"),
    activeBranchCount: readPreviewNumber(record, "activeBranchCount", "ActiveBranchCount"),
    branchCountAvailable: readPreviewBoolean(
      record,
      "branchCountAvailable",
      "BranchCountAvailable",
    ),
    usageConflicts: Array.isArray(conflictsPayload)
      ? conflictsPayload.flatMap((item) => {
          const conflict = asPreviewRecord(item);
          if (!conflict) {
            return [];
          }
          const kind = readPreviewString(conflict, "kind", "Kind", "dimension", "Dimension");
          const message = readPreviewString(conflict, "message", "Message");
          const currentUsage = readPreviewNumber(conflict, "currentUsage", "CurrentUsage");
          const targetLimit = readPreviewNumber(conflict, "targetLimit", "TargetLimit");
          if (!kind || !message || currentUsage === undefined || targetLimit === undefined) {
            return [];
          }
          return [{ kind, currentUsage, targetLimit, message }];
        })
      : [],
    lostFeatures: Array.isArray(lostPayload)
      ? lostPayload.filter((item): item is string => typeof item === "string")
      : [],
    hasBlockingUsageConflicts:
      readPreviewBoolean(record, "hasBlockingUsageConflicts", "HasBlockingUsageConflicts") ===
      true,
  };
}

export function getSubscriptionPlanChangePreview(
  baseUrl: string,
  organizationId: string,
  subscriptionId: string,
  options: { planId?: string; planKey?: string; activeBranchCount?: number },
  signal?: AbortSignal,
): Promise<PlanChangePreview> {
  const params = new URLSearchParams();
  if (options.planId) {
    params.set("planId", options.planId);
  }
  if (options.planKey) {
    params.set("planKey", options.planKey);
  }
  if (options.activeBranchCount != null) {
    params.set("activeBranchCount", String(options.activeBranchCount));
  }
  const encoded = params.toString();
  const suffix = encoded.length > 0 ? `?${encoded}` : "";
  return platformRequest<unknown>(baseUrl, {
    path: orgSubscriptionPath(organizationId, `/${subscriptionId}/plan-change-preview${suffix}`),
    signal,
  }).then(mapPlanChangePreview);
}

export function suspendSubscription(
  baseUrl: string,
  subscriptionId: string,
  body?: SubscriptionLifecycleBody,
  signal?: AbortSignal,
): Promise<OrganizationSubscription> {
  return commercialMutationRequest<unknown>(baseUrl, {
    method: "POST",
    path: topLevelSubscriptionPath(subscriptionId, "suspend"),
    body: body ?? {},
    signal,
  }).then(requireSubscription);
}

export function reactivateSubscription(
  baseUrl: string,
  subscriptionId: string,
  body?: ReactivateSubscriptionBody,
  signal?: AbortSignal,
): Promise<OrganizationSubscription> {
  return commercialMutationRequest<unknown>(baseUrl, {
    method: "POST",
    path: topLevelSubscriptionPath(subscriptionId, "reactivate"),
    body: body ?? {},
    signal,
  }).then(requireSubscription);
}

export function cancelSubscription(
  baseUrl: string,
  subscriptionId: string,
  body?: SubscriptionLifecycleBody,
  signal?: AbortSignal,
): Promise<OrganizationSubscription> {
  return commercialMutationRequest<unknown>(baseUrl, {
    method: "POST",
    path: topLevelSubscriptionPath(subscriptionId, "cancel"),
    body: body ?? {},
    signal,
  }).then(requireSubscription);
}

export function enterSubscriptionGracePeriod(
  baseUrl: string,
  subscriptionId: string,
  body: GracePeriodBody,
  signal?: AbortSignal,
): Promise<OrganizationSubscription> {
  return commercialMutationRequest<unknown>(baseUrl, {
    method: "POST",
    path: topLevelSubscriptionPath(subscriptionId, "grace-period"),
    body,
    signal,
  }).then(requireSubscription);
}

export function markSubscriptionPastDue(
  baseUrl: string,
  subscriptionId: string,
  body?: SubscriptionLifecycleBody,
  signal?: AbortSignal,
): Promise<OrganizationSubscription> {
  return commercialMutationRequest<unknown>(baseUrl, {
    method: "POST",
    path: topLevelSubscriptionPath(subscriptionId, "past-due"),
    body: body ?? {},
    signal,
  }).then(requireSubscription);
}

export function expireSubscription(
  baseUrl: string,
  subscriptionId: string,
  body?: SubscriptionLifecycleBody,
  signal?: AbortSignal,
): Promise<OrganizationSubscription> {
  return commercialMutationRequest<unknown>(baseUrl, {
    method: "POST",
    path: topLevelSubscriptionPath(subscriptionId, "expire"),
    body: body ?? {},
    signal,
  }).then(requireSubscription);
}

/** Exists on the API; server always returns payment-required. Typed only for later packages. */
export function activateSubscription(
  baseUrl: string,
  subscriptionId: string,
  body: ActivateSubscriptionBody,
  signal?: AbortSignal,
): Promise<OrganizationSubscription> {
  return commercialMutationRequest<unknown>(baseUrl, {
    method: "POST",
    path: topLevelSubscriptionPath(subscriptionId, "activate"),
    body,
    signal,
  }).then(requireSubscription);
}

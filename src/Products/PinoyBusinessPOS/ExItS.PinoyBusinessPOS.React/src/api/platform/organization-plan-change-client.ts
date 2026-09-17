import {
  platformRequest,
  PlatformApiError,
  type PlatformProblemDetails,
} from "@/api/platform/platform-http";

export type PlanUsageConflictDto = {
  resource: string;
  currentUsage: number;
  targetLimit: number;
  message: string;
};

export type PlanChangePreviewDto = {
  currentPlanId: string | null;
  currentPlanDisplayName: string | null;
  targetPlanId: string | null;
  targetPlanDisplayName: string | null;
  activeStaffCount: number | null;
  activeBranchCount: number | null;
  branchCountAvailable: boolean;
  branchCountUnavailableReason: string | null;
  usageConflicts: PlanUsageConflictDto[];
  lostFeatures: string[];
  hasBlockingUsageConflicts: boolean;
};

type PlanChangeClientResult<T> =
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
  if (typeof value !== "string") return null;
  const trimmed = value.trim();
  return trimmed.length > 0 ? trimmed : null;
}

function pickNumber(raw: Record<string, unknown>, camel: string, pascal: string): number | null {
  const value = pick(raw, camel, pascal);
  if (value == null || value === "") return null;
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : null;
}

function normalizeConflict(raw: unknown): PlanUsageConflictDto | null {
  const r = asRecord(raw);
  if (!r) return null;
  const resource =
    pickString(r, "resource", "Resource") ?? pickString(r, "kind", "Kind") ?? null;
  const message = pickString(r, "message", "Message");
  const currentUsage = pickNumber(r, "currentUsage", "CurrentUsage");
  const targetLimit = pickNumber(r, "targetLimit", "TargetLimit");
  if (!resource || !message) return null;
  return {
    resource,
    currentUsage: currentUsage ?? 0,
    targetLimit: targetLimit ?? 0,
    message,
  };
}

export function normalizePlanChangePreview(raw: Record<string, unknown>): PlanChangePreviewDto {
  const conflicts = pick(raw, "usageConflicts", "UsageConflicts");
  const lost = pick(raw, "lostFeatures", "LostFeatures");
  return {
    currentPlanId: pickString(raw, "currentPlanId", "CurrentPlanId"),
    currentPlanDisplayName: pickString(raw, "currentPlanDisplayName", "CurrentPlanDisplayName"),
    targetPlanId: pickString(raw, "targetPlanId", "TargetPlanId"),
    targetPlanDisplayName: pickString(raw, "targetPlanDisplayName", "TargetPlanDisplayName"),
    activeStaffCount: pickNumber(raw, "activeStaffCount", "ActiveStaffCount"),
    activeBranchCount: pickNumber(raw, "activeBranchCount", "ActiveBranchCount"),
    branchCountAvailable: pick(raw, "branchCountAvailable", "BranchCountAvailable") !== false,
    branchCountUnavailableReason: pickString(
      raw,
      "branchCountUnavailableReason",
      "BranchCountUnavailableReason",
    ),
    usageConflicts: Array.isArray(conflicts)
      ? conflicts
          .map(normalizeConflict)
          .filter((conflict): conflict is PlanUsageConflictDto => conflict != null)
      : [],
    lostFeatures: Array.isArray(lost)
      ? lost.filter((item): item is string => typeof item === "string")
      : [],
    hasBlockingUsageConflicts:
      pick(raw, "hasBlockingUsageConflicts", "HasBlockingUsageConflicts") === true,
  };
}

/**
 * Read-only downgrade/upgrade impact check. Never mutates the subscription and never
 * deletes organization data — used to block a plan change that would exceed limits.
 */
export async function getOrganizationPlanChangePreview(
  input: { organizationId: string; subscriptionId: string; planId: string },
  signal?: AbortSignal,
): Promise<PlanChangeClientResult<PlanChangePreviewDto>> {
  try {
    const query = new URLSearchParams({ planId: input.planId });
    const payload = await platformRequest<Record<string, unknown>>({
      method: "GET",
      path: `/api/v1/platform/organizations/${input.organizationId}/subscriptions/${input.subscriptionId}/plan-change-preview?${query.toString()}`,
      signal,
    });
    return { ok: true, value: normalizePlanChangePreview(payload) };
  } catch (error) {
    if (error instanceof PlatformApiError) {
      return { ok: false, status: error.status, body: error.problem };
    }
    throw error;
  }
}

import { platformRequest, PlatformApiError } from "@/api/platform/platform-http";
import type { PlatformProblemDetails } from "@/api/platform/platform-problem";
import { POS_PRODUCT_CODE } from "@/api/platform/browser-session";

export type OrganizationCurrentPlanDto = {
  organizationId: string;
  productCode: string;
  planDisplayName: string | null;
  planKey: string | null;
  subscriptionStatus: string | null;
};

type OrganizationCurrentPlanClientResult<T> =
  | { ok: true; value: T }
  | { ok: false; status: number; body: PlatformProblemDetails | null };

function pickString(raw: Record<string, unknown>, camel: string, pascal: string): string | null {
  const value = raw[camel] ?? raw[pascal];
  if (typeof value !== "string") {
    return null;
  }
  const trimmed = value.trim();
  return trimmed.length > 0 ? trimmed : null;
}

function normalizeCurrentPlan(raw: Record<string, unknown>): OrganizationCurrentPlanDto {
  const currentPlan =
    (raw.currentPlan as Record<string, unknown> | null | undefined) ??
    (raw.CurrentPlan as Record<string, unknown> | null | undefined) ??
    null;
  const currentSubscription =
    (raw.currentSubscription as Record<string, unknown> | null | undefined) ??
    (raw.CurrentSubscription as Record<string, unknown> | null | undefined) ??
    null;

  return {
    organizationId: String(raw.organizationId ?? raw.OrganizationId ?? ""),
    productCode: String(raw.productCode ?? raw.ProductCode ?? POS_PRODUCT_CODE),
    planDisplayName: currentPlan
      ? pickString(currentPlan, "displayName", "DisplayName")
      : null,
    planKey: currentPlan ? pickString(currentPlan, "planKey", "PlanKey") : null,
    subscriptionStatus: currentSubscription
      ? pickString(currentSubscription, "status", "Status")
      : null,
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
    return { ok: true, value: normalizeCurrentPlan(payload) };
  } catch (error) {
    if (error instanceof PlatformApiError) {
      return { ok: false, status: error.status, body: error.problem };
    }
    throw error;
  }
}

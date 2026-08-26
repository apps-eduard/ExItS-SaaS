import { PosApiError } from "@/api/pos/pos-http";
import { PlatformApiError } from "@/api/platform/platform-http";
import type { MessageKey } from "@/i18n/messages";
import type { PosSessionGrantFacts } from "@/access/pos-capabilities";

export type CommercialAccessState =
  | "allowed"
  | "suspended"
  | "product_unavailable"
  | "entitlement_unavailable"
  | "feature_denied";

const SUSPENDED_REASON_MARKERS = [
  "suspended",
  "subscription_ineligible",
  "entitlement_denied",
] as const;

/** Classify session-grant commercial posture without a second store. */
export function resolveCommercialAccessState(
  grant: PosSessionGrantFacts | null | undefined,
): CommercialAccessState {
  if (!grant) {
    return "entitlement_unavailable";
  }

  const reason = (grant.productAccessReasonCode ?? "").toLowerCase();
  if (SUSPENDED_REASON_MARKERS.some((marker) => reason.includes(marker))) {
    return "suspended";
  }

  if (!grant.productAccessAllowed) {
    return "product_unavailable";
  }

  return "allowed";
}

export function commercialAccessStateMessageKey(
  state: CommercialAccessState,
): MessageKey | null {
  switch (state) {
    case "allowed":
      return null;
    case "suspended":
      return "commercial.subscriptionSuspended";
    case "product_unavailable":
      return "commercial.productUnavailable";
    case "entitlement_unavailable":
      return "commercial.accessUnavailable";
    case "feature_denied":
      return "commercial.notIncludedInPlan";
  }
}

/** Map POS commercial / subscription denial codes to user-facing i18n keys. */
export function mapCommercialAccessErrorKey(error: unknown): MessageKey | null {
  const codeDetail = extractCommercialErrorSignals(error);
  if (!codeDetail) {
    return null;
  }

  const { code, detail } = codeDetail;

  if (
    code.includes("commercial.access_unknown") ||
    code === "pos.commercial.access_unknown" ||
    code.includes("development_headers.unavailable")
  ) {
    return "commercial.accessUnavailable";
  }

  if (
    detail.includes("suspended") ||
    code.includes("subscription_ineligible") ||
    (code.includes("capability_denied") && detail.includes("suspended"))
  ) {
    return "commercial.subscriptionSuspended";
  }

  if (
    code.includes("commercial.capability_denied") ||
    code === "pos.commercial.capability_denied"
  ) {
    return "commercial.notIncludedInPlan";
  }

  if (
    code.includes("product_access_denied") ||
    code.includes("product_access_inactive") ||
    code.includes("product_access_missing") ||
    code === "application.auth.product_access_denied"
  ) {
    return "commercial.productUnavailable";
  }

  if (code.includes("pos_device.capacity_exceeded") || code.includes("capacity_exceeded")) {
    return "devices.capacity.limitReached";
  }

  return null;
}

export function describeCommercialAccessError(
  error: unknown,
  t: (key: MessageKey) => string,
): string | null {
  const key = mapCommercialAccessErrorKey(error);
  return key ? t(key) : null;
}

/** Shared POS API error copy — commercial mapper first, then optional fallback key. */
export function describePosApiError(
  error: unknown,
  t: (key: MessageKey) => string,
  fallbackKey: MessageKey = "error.detail",
): string {
  const commercial = describeCommercialAccessError(error, t);
  if (commercial) {
    return commercial;
  }

  if (error instanceof PosApiError) {
    return error.problem.detail ?? error.message ?? t(fallbackKey);
  }

  if (error instanceof PlatformApiError) {
    const commercial = describeCommercialAccessError(error, t);
    if (commercial) {
      return commercial;
    }

    return error.problem.detail ?? error.message ?? t(fallbackKey);
  }

  if (error instanceof Error && error.message.trim().length > 0) {
    return error.message;
  }

  return t(fallbackKey);
}

function extractCommercialErrorSignals(
  error: unknown,
): { code: string; detail: string } | null {
  if (error instanceof PosApiError) {
    return {
      code: (error.errorCode ?? "").toLowerCase(),
      detail: (error.problem.detail ?? error.message).toLowerCase(),
    };
  }

  if (error instanceof PlatformApiError) {
    return {
      code: (error.errorCode ?? "").toLowerCase(),
      detail: (error.problem.detail ?? error.message).toLowerCase(),
    };
  }

  return null;
}

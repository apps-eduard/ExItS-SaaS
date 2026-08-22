import { PosApiError } from "@/api/pos/pos-http";
import type { MessageKey } from "@/i18n/messages";

/** Map POS commercial / subscription denial codes to user-facing i18n keys. */
export function mapCommercialAccessErrorKey(error: unknown): MessageKey | null {
  if (!(error instanceof PosApiError)) {
    return null;
  }

  const code = (error.errorCode ?? "").toLowerCase();
  const detail = (error.problem.detail ?? error.message).toLowerCase();

  if (
    code.includes("commercial.access_unknown") ||
    code === "pos.commercial.access_unknown" ||
    code.includes("development_headers.unavailable")
  ) {
    return "commercial.accessUnavailable";
  }

  if (
    code.includes("commercial.capability_denied") ||
    code === "pos.commercial.capability_denied"
  ) {
    return "commercial.notIncludedInPlan";
  }

  if (
    code.includes("product_access_denied") ||
    code === "application.auth.product_access_denied"
  ) {
    return "accessDenied.title";
  }

  if (code.includes("pos_device.capacity_exceeded") || code.includes("capacity_exceeded")) {
    return "devices.capacity.limitReached";
  }

  if (
    detail.includes("suspended") ||
    (code.includes("capability_denied") && detail.includes("suspended"))
  ) {
    return "commercial.subscriptionSuspended";
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

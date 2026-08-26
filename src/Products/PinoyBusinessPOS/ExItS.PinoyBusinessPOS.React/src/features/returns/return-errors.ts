import { PosApiError } from "@/api/pos/pos-http";
import { isCashShiftRequiredError, isStaleReturnConflict } from "@/api/pos/pos-sale-returns-client";
import type { MessageKey } from "@/i18n/messages";

export function mapReturnErrorKey(error: unknown): MessageKey {
  if (isCashShiftRequiredError(error)) {
    return "returns.errorNoShift";
  }
  if (isStaleReturnConflict(error)) {
    return "returns.errorStale";
  }
  if (!(error instanceof PosApiError)) {
    return "returns.errorGeneric";
  }

  const code = (error.errorCode ?? "").toLowerCase();
  const detail = (error.problem.detail ?? error.message).toLowerCase();

  if (
    error.status === 403 ||
    code.includes("capability.denied") ||
    detail.includes("processreturn")
  ) {
    return "returns.errorDenied";
  }

  if (error.status === 404 || code.includes("not_found")) {
    return "returns.errorNotFound";
  }

  if (code.includes("offline") || detail.includes("internet")) {
    return "returns.errorOffline";
  }

  return "returns.errorGeneric";
}

export function describeReturnError(error: unknown, t: (key: MessageKey) => string): string {
  return t(mapReturnErrorKey(error));
}

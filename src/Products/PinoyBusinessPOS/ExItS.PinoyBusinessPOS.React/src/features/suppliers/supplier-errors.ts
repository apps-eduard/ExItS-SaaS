import { PosApiError } from "@/api/pos/pos-http";
import type { MessageKey } from "@/i18n/messages";

export function mapSupplierErrorKey(error: unknown): MessageKey {
  if (!(error instanceof PosApiError)) {
    return "suppliers.errorGeneric";
  }

  const code = (error.errorCode ?? "").toLowerCase();

  if (code.includes("name.conflict")) {
    return "suppliers.errorNameConflict";
  }
  if (code.includes("email.conflict")) {
    return "suppliers.errorEmailConflict";
  }
  if (code.includes("mobile.conflict")) {
    return "suppliers.errorMobileConflict";
  }
  if (code.includes("tax_number.conflict")) {
    return "suppliers.errorTaxConflict";
  }
  if (code.includes("concurrency_conflict")) {
    return "suppliers.errorConcurrency";
  }
  if (
    error.status === 403 ||
    code.includes("capability.denied") ||
    code.includes("not_authorized")
  ) {
    return "suppliers.errorDenied";
  }
  if (error.status === 404 || code.includes("not_found")) {
    return "suppliers.errorNotFound";
  }

  return "suppliers.errorGeneric";
}

export function describeSupplierError(error: unknown, t: (key: MessageKey) => string): string {
  return t(mapSupplierErrorKey(error));
}

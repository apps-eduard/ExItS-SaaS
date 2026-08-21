import { PosApiError } from "@/api/pos/pos-http";
import type { MessageKey } from "@/i18n/messages";

/**
 * Map POS sale / device / session error codes to friendly i18n keys.
 * Fail closed: unknown codes use a generic checkout failure message.
 */
export function mapCheckoutSaleErrorKey(error: unknown): MessageKey {
  if (!(error instanceof PosApiError)) {
    return "checkout.errorGeneric";
  }

  const code = (error.errorCode ?? "").toLowerCase();
  const detail = (error.problem.detail ?? error.message).toLowerCase();

  if (
    error.status === 401 ||
    code.includes("session") ||
    code.includes("unauthorized") ||
    code === "application.auth.session_invalid" ||
    code === "application.auth.session_expired"
  ) {
    return "checkout.errorSession";
  }

  if (
    code.includes("apply_commercial_discount") ||
    (code.includes("discount") && (error.status === 403 || detail.includes("denied"))) ||
    (code.includes("capability.denied") && detail.includes("commercial discount")) ||
    (code.includes("capability.denied") && detail.includes("applycommercialdiscount"))
  ) {
    return "checkout.errorDiscountDenied";
  }

  if (
    code.includes("product_access") ||
    code === "application.auth.product_access_denied" ||
    code.includes("capability.denied") ||
    code.includes("role_denied")
  ) {
    return "checkout.errorProductAccess";
  }

  if (
    code.includes("no_open_shift") ||
    code.includes("cashier_shift") ||
    detail.includes("open shift")
  ) {
    return "checkout.errorNoShift";
  }

  if (code.includes("pos_device.revoked") || detail.includes("revoked")) {
    return "checkout.errorDeviceRevoked";
  }

  if (
    code.includes("branch_conflict") ||
    code.includes("wrong_branch") ||
    (code.includes("pos_device") && detail.includes("branch"))
  ) {
    return "checkout.errorDeviceWrongBranch";
  }

  if (
    code.includes("pos_device.not_authorized") ||
    code.includes("not_authorized") ||
    detail.includes("installation device") ||
    detail.includes("not registered")
  ) {
    return "checkout.errorDeviceUnregistered";
  }

  if (code.includes("insufficient_stock") || detail.includes("insufficient stock")) {
    return "checkout.errorInsufficientStock";
  }

  if (code.includes("amount_tendered.below_total") || detail.includes("at least the sale total")) {
    return "checkout.errorInsufficientTender";
  }

  if (code.includes("amount_tendered") || detail.includes("amount tendered")) {
    return "checkout.errorInvalidTender";
  }

  if (code.includes("product.not_found") || code.includes("product.not_active")) {
    return "checkout.errorProductUnavailable";
  }

  return "checkout.errorGeneric";
}

export function describeCheckoutSaleError(error: unknown, t: (key: MessageKey) => string): string {
  const key = mapCheckoutSaleErrorKey(error);
  if (key === "checkout.errorGeneric" && error instanceof PosApiError) {
    return error.problem.detail ?? error.message ?? t(key);
  }
  return t(key);
}

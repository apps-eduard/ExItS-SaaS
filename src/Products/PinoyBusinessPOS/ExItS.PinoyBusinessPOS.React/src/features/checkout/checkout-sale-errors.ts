import { PosApiError } from "@/api/pos/pos-http";
import {
  describeCommercialAccessError,
  mapCommercialAccessErrorKey,
} from "@/access/pos-commercial-errors";
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
    (code.includes("discount") &&
      !code.includes("price_override") &&
      (error.status === 403 || detail.includes("denied"))) ||
    (code.includes("capability.denied") && detail.includes("commercial discount")) ||
    (code.includes("capability.denied") && detail.includes("applycommercialdiscount"))
  ) {
    return "checkout.errorDiscountDenied";
  }

  if (
    code.includes("price_override.exceeds_manager_limit") ||
    detail.includes("above your allowed limit") ||
    detail.includes("exceeds the manager")
  ) {
    return "checkout.errorOverrideAboveLimit";
  }

  if (
    code.includes("price_override.invalid_amount") ||
    (code.includes("price_override") && detail.includes("positive"))
  ) {
    return "checkout.errorOverrideInvalid";
  }

  if (code.includes("price_override.reason_required")) {
    return "checkout.errorOverrideReasonRequired";
  }

  if (code.includes("price_override.stale_baseline")) {
    return "checkout.errorOverrideStaleBaseline";
  }

  if (
    code.includes("price_override.offline_not_supported") ||
    code.includes("overridesaleprice") ||
    (code.includes("capability.denied") &&
      (detail.includes("overridesaleprice") || detail.includes("override sale price"))) ||
    (code.includes("price_override") && (error.status === 403 || detail.includes("denied")))
  ) {
    return "checkout.errorOverrideDenied";
  }

  if (
    code.includes("voidsale") ||
    (code.includes("capability.denied") &&
      (detail.includes("voidsale") || detail.includes("void sale"))) ||
    (code.includes("void") && detail.includes("denied"))
  ) {
    return "checkout.errorVoidDenied";
  }

  if (
    code.includes("createcredit") ||
    (code.includes("capability.denied") &&
      (detail.includes("credit") || detail.includes("utang")) &&
      !detail.includes("suspended"))
  ) {
    return "checkout.errorCreditDenied";
  }

  const commercial = mapCommercialAccessErrorKey(error);
  if (commercial) {
    return commercial;
  }

  if (code.includes("capability.denied") || code.includes("role_denied")) {
    return "checkout.errorGeneric";
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

  if (code.includes("gcash_reference") || detail.includes("gcash reference")) {
    return "checkout.errorGCashReference";
  }

  if (
    code.includes("utang.total_must_be_positive") ||
    detail.includes("utang total must be greater")
  ) {
    return "checkout.errorUtangZero";
  }

  if (code.includes("utang.customer_required") || detail.includes("utang requires a customer")) {
    return "checkout.errorUtangCustomer";
  }

  if (code.includes("void_reason") || detail.includes("void reason")) {
    return "checkout.errorVoidReason";
  }

  if (code.includes("already_voided") || detail.includes("already voided")) {
    return "checkout.errorAlreadyVoided";
  }

  if (code.includes("product.not_found") || code.includes("product.not_active")) {
    return "checkout.errorProductUnavailable";
  }

  return "checkout.errorGeneric";
}

export function describeCheckoutSaleError(error: unknown, t: (key: MessageKey) => string): string {
  if (import.meta.env.DEV && !(error instanceof PosApiError)) {
    const name = error instanceof Error ? error.name : typeof error;
    const message = error instanceof Error ? error.message : String(error);
    console.warn("[checkout] non-PosApiError failure", { name, message });
  }

  const commercial = describeCommercialAccessError(error, t);
  if (commercial) {
    return commercial;
  }

  const key = mapCheckoutSaleErrorKey(error);
  if (key === "checkout.errorGeneric" && error instanceof PosApiError) {
    return error.problem.detail ?? error.message ?? t(key);
  }
  return t(key);
}
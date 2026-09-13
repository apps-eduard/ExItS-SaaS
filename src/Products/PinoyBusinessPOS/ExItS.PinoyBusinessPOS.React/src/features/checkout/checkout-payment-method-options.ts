import type { CheckoutPaymentMethod } from "@/api/pos/pos-sales-client";
import type { CheckoutUiPaymentChoice } from "@/features/checkout/CheckoutPaymentMethodCards";

/** Built-in defaults when checkout method list cannot be loaded. */
export const DEFAULT_CHECKOUT_METHOD_CODES: readonly CheckoutPaymentMethod[] = [
  "Cash",
  "ManualGCash",
  "Utang",
] as const;

const API_TO_UI: Record<CheckoutPaymentMethod, CheckoutUiPaymentChoice> = {
  Cash: "Cash",
  ManualGCash: "GCash",
  Utang: "Utang",
  BankTransfer: "BankTransfer",
  Check: "Check",
  ManualMaya: "ManualMaya",
};

export function toUiPaymentChoice(methodCode: string): CheckoutUiPaymentChoice | null {
  if (methodCode in API_TO_UI) {
    return API_TO_UI[methodCode as CheckoutPaymentMethod];
  }
  return null;
}

export function toApiPaymentMethod(choice: CheckoutUiPaymentChoice): CheckoutPaymentMethod {
  if (choice === "GCash") {
    return "ManualGCash";
  }
  return choice;
}

/** Manual confirmation methods that reuse the optional/required reference field. */
export function isManualReferencePaymentChoice(choice: CheckoutUiPaymentChoice): boolean {
  return (
    choice === "GCash" ||
    choice === "BankTransfer" ||
    choice === "Check" ||
    choice === "ManualMaya"
  );
}

export function filterCheckoutUiChoices(
  methodCodes: readonly string[],
): CheckoutUiPaymentChoice[] {
  const seen = new Set<CheckoutUiPaymentChoice>();
  const result: CheckoutUiPaymentChoice[] = [];
  for (const code of methodCodes) {
    const ui = toUiPaymentChoice(code);
    if (ui && !seen.has(ui)) {
      seen.add(ui);
      result.push(ui);
    }
  }
  return result.length > 0 ? result : ["Cash", "GCash", "Utang"];
}

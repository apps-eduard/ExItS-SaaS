import type { OrganizationOnlineSupplierPaymentsCapability } from "@/api/platform/organization-online-supplier-payments-client";
import type { PaymentMethodSettingDto } from "@/api/pos/pos-payment-methods-client";

/**
 * Buyer supplier-payable payment CTA modes.
 * Manual [Record Payment] is never offered — free self-settlement is forbidden.
 */
export type BuyerSupplierPaymentCta = "hidden" | "unavailable" | "pay_now";

export type BuyerSupplierPaymentGateInput = {
  platformCapability: OrganizationOnlineSupplierPaymentsCapability | null | undefined;
  paymentMethods: PaymentMethodSettingDto[] | null | undefined;
  /** Payable is Open/PartiallyPaid with balance &gt; 0. */
  payableEligible: boolean;
  allowManage: boolean;
  online: boolean;
};

/**
 * True when at least one OnlinePayments catalog method is ready (not ComingSoon).
 * Today all Online* / Card / QrPh entries are ComingSoon — gate yields unavailable when Available.
 */
export function hasReadyOnlineSupplierPaymentMethod(
  methods: PaymentMethodSettingDto[] | null | undefined,
): boolean {
  if (!methods?.length) {
    return false;
  }
  return methods.some(
    (m) =>
      m.requiredCapability === "OnlinePayments" &&
      !m.comingSoon &&
      m.availability !== "ComingSoon" &&
      m.entitled,
  );
}

/**
 * Buyer gate matrix:
 * - Disabled / Suspended / unknown → hidden (no payment CTA)
 * - Available + no ready online method → unavailable (truthful; no fake settle)
 * - Available + ready online method → pay_now
 *
 * Platform-disabled Online Payments must never be treated as org setup Needs Attention.
 */
export function resolveBuyerSupplierPaymentCta(
  input: BuyerSupplierPaymentGateInput,
): BuyerSupplierPaymentCta {
  if (!input.allowManage || !input.online || !input.payableEligible) {
    return "hidden";
  }

  const status = input.platformCapability?.status ?? "Disabled";
  if (status === "Disabled" || status === "Suspended") {
    return "hidden";
  }

  if (status !== "Available") {
    return "hidden";
  }

  if (!hasReadyOnlineSupplierPaymentMethod(input.paymentMethods)) {
    return "unavailable";
  }

  return "pay_now";
}

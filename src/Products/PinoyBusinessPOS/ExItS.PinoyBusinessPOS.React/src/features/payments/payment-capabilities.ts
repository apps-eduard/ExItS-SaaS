/** Centralized payment capability feature codes — never gate with planKey compares. */
export const FEATURE_STORE_BASIC_PAYMENTS = "store-basic-payments";
export const FEATURE_STORE_PAYMENT_MANAGEMENT = "store-payment-management";
export const FEATURE_STORE_ONLINE_PAYMENTS = "store-online-payments";

export type PaymentCapabilityCode = "BasicPayments" | "PaymentManagement" | "OnlinePayments";

export function paymentCapabilityFeatureCode(capability: PaymentCapabilityCode): string {
  switch (capability) {
    case "BasicPayments":
      return FEATURE_STORE_BASIC_PAYMENTS;
    case "PaymentManagement":
      return FEATURE_STORE_PAYMENT_MANAGEMENT;
    case "OnlinePayments":
      return FEATURE_STORE_ONLINE_PAYMENTS;
  }
}

export type PlanPaymentEntitlementDisplay = {
  basicPayments: boolean;
  paymentManagement: boolean;
  onlinePayments: boolean;
};

/** Display-only plan payment matrix (mirrors Platform grants; runtime uses feature codes). */
export const PLAN_PAYMENT_ENTITLEMENTS: Readonly<Record<string, PlanPaymentEntitlementDisplay>> = {
  starter: { basicPayments: true, paymentManagement: false, onlinePayments: false },
  growth: { basicPayments: true, paymentManagement: false, onlinePayments: false },
  pro: { basicPayments: true, paymentManagement: true, onlinePayments: false },
  "pro-plus": { basicPayments: true, paymentManagement: true, onlinePayments: true },
};

export function getPlanPaymentEntitlements(planKey: string): PlanPaymentEntitlementDisplay {
  return (
    PLAN_PAYMENT_ENTITLEMENTS[planKey.trim().toLowerCase()] ?? {
      basicPayments: true,
      paymentManagement: false,
      onlinePayments: false,
    }
  );
}

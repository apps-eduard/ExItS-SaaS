/** Connected PO intended payment methods (not proof of payment). */
export type ConnectedPoPaymentMethodCode =
  | "Cash"
  | "BankTransfer"
  | "BankDeposit"
  | "Check"
  | "ManualGCash"
  | "Utang";

export type ConnectedPoPaymentOption = {
  code: ConnectedPoPaymentMethodCode;
  labelKey: string;
  /** Fallback help when timing is unknown — prefer resolveConnectedPoPaymentHelpKey. */
  helpKey: string;
  /** When true, option requires Utang eligibility. */
  requiresUtangEligibility?: boolean;
};

export const CONNECTED_PO_PAYMENT_OPTIONS: readonly ConnectedPoPaymentOption[] = [
  {
    code: "Cash",
    labelKey: "purchasing.paymentMethod.cod",
    helpKey: "purchasing.paymentHelp.cod.payOnDelivery",
  },
  {
    code: "BankTransfer",
    labelKey: "purchasing.paymentMethod.bankTransfer",
    helpKey: "purchasing.paymentHelp.bankTransfer.payOnDelivery",
  },
  {
    code: "BankDeposit",
    labelKey: "purchasing.paymentMethod.bankDeposit",
    helpKey: "purchasing.paymentHelp.bankDeposit.payOnDelivery",
  },
  {
    code: "Check",
    labelKey: "purchasing.paymentMethod.check",
    helpKey: "purchasing.paymentHelp.check.payOnDelivery",
  },
  {
    code: "ManualGCash",
    labelKey: "purchasing.paymentMethod.gcash",
    helpKey: "purchasing.paymentHelp.gcash.payOnDelivery",
  },
  {
    code: "Utang",
    labelKey: "purchasing.paymentMethod.utang",
    helpKey: "purchasing.paymentHelp.utang",
    requiresUtangEligibility: true,
  },
];

type PaymentHelpTimingSuffix = "payBefore" | "payOnDelivery" | "supplierCredit";

function paymentHelpTimingSuffix(timing: string | null | undefined): PaymentHelpTimingSuffix {
  if (timing === "PayBeforeFulfillment") {
    return "payBefore";
  }
  if (timing === "SupplierCredit") {
    return "supplierCredit";
  }
  return "payOnDelivery";
}

/**
 * Payment-method info copy keyed to the selected payment timing
 * (pay before / on delivery / supplier credit).
 */
export function resolveConnectedPoPaymentHelpKey(
  method: ConnectedPoPaymentMethodCode | "" | null | undefined,
  timing: string | null | undefined,
): string | null {
  if (!method) {
    return null;
  }
  if (method === "Utang") {
    return "purchasing.paymentHelp.utang";
  }

  const methodKey =
    method === "Cash"
      ? "cod"
      : method === "BankTransfer"
        ? "bankTransfer"
        : method === "BankDeposit"
          ? "bankDeposit"
          : method === "Check"
            ? "check"
            : method === "ManualGCash"
              ? "gcash"
              : null;
  if (!methodKey) {
    return null;
  }

  return `purchasing.paymentHelp.${methodKey}.${paymentHelpTimingSuffix(timing)}`;
}

export type PoUtangEligibility =
  | { eligible: true }
  | { eligible: false; reasonKey: string };

/** Reuse B2B credit policy projection for Utang on Create PO. */
export function resolvePoUtangEligibility(args: {
  connected: boolean;
  creditStatus?: string | null;
  availableCredit?: number | null;
  orderTotal: number;
  canManagePurchasing: boolean;
}): PoUtangEligibility {
  if (!args.connected) {
    return { eligible: false, reasonKey: "purchasing.utang.notConnected" };
  }
  if (!args.canManagePurchasing) {
    return { eligible: false, reasonKey: "purchasing.utang.permissionDenied" };
  }
  const status = (args.creditStatus ?? "").trim();
  if (status.toLowerCase() !== "approved") {
    return { eligible: false, reasonKey: "purchasing.utang.creditNotApproved" };
  }
  const available = args.availableCredit ?? 0;
  if (!(available > 0)) {
    return { eligible: false, reasonKey: "purchasing.utang.noAvailableCredit" };
  }
  if (args.orderTotal > available) {
    return { eligible: false, reasonKey: "purchasing.utang.insufficientCredit" };
  }
  return { eligible: true };
}

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
  helpKey: string;
  /** When true, option requires Utang eligibility. */
  requiresUtangEligibility?: boolean;
};

export const CONNECTED_PO_PAYMENT_OPTIONS: readonly ConnectedPoPaymentOption[] = [
  {
    code: "Cash",
    labelKey: "purchasing.paymentMethod.cod",
    helpKey: "purchasing.paymentHelp.cod",
  },
  {
    code: "BankTransfer",
    labelKey: "purchasing.paymentMethod.bankTransfer",
    helpKey: "purchasing.paymentHelp.bankTransfer",
  },
  {
    code: "BankDeposit",
    labelKey: "purchasing.paymentMethod.bankDeposit",
    helpKey: "purchasing.paymentHelp.bankDeposit",
  },
  {
    code: "Check",
    labelKey: "purchasing.paymentMethod.check",
    helpKey: "purchasing.paymentHelp.check",
  },
  {
    code: "ManualGCash",
    labelKey: "purchasing.paymentMethod.gcash",
    helpKey: "purchasing.paymentHelp.gcash",
  },
  {
    code: "Utang",
    labelKey: "purchasing.paymentMethod.utang",
    helpKey: "purchasing.paymentHelp.utang",
    requiresUtangEligibility: true,
  },
];

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

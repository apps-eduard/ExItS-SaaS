import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { Card } from "@/components/ui/card";
import {
  RECEIVE_PAYMENT_METHODS,
  remainingCredit,
  type ReceivePaymentMethodCode,
  type ReceivePaymentMode,
  type ReceiveSettlementFields,
} from "@/features/purchasing/receive-payment";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";

function methodLabelKey(method: string): MessageKey {
  switch (method) {
    case "BankTransfer":
      return "supplierPayables.method.bankTransfer";
    case "BankDeposit":
      return "supplierPayables.method.bankDeposit";
    case "GCash":
      return "supplierPayables.method.gcash";
    case "Check":
      return "supplierPayables.method.check";
    case "Other":
      return "supplierPayables.method.other";
    default:
      return "supplierPayables.method.cash";
  }
}

type ReceivePaymentSectionProps = {
  estimatedTotal: number;
  mode: ReceivePaymentMode;
  onModeChange: (mode: ReceivePaymentMode) => void;
  paidNowText: string;
  onPaidNowChange: (value: string) => void;
  dueDate: string;
  onDueDateChange: (value: string) => void;
  paymentMethod: ReceivePaymentMethodCode;
  onPaymentMethodChange: (value: ReceivePaymentMethodCode) => void;
  paidNowValue: number | null;
  disabled?: boolean;
  /** When false, hide supplier-credit mode (e.g. direct purchase with no supplier). */
  allowSupplierCredit?: boolean;
  testIdPrefix?: string;
  /** PO receive: payment method locked from purchase order term. */
  lockedFromPo?: boolean;
  lockedPaymentMethodLabel?: string;
  settlementFields?: ReceiveSettlementFields;
  onSettlementFieldsChange?: (fields: ReceiveSettlementFields) => void;
};

/**
 * Payment-at-receipt fields shared by PO receive and direct receive stock.
 * PaidNow is a receipt settlement snapshot — not a SupplierPayablePayment row.
 */
export function ReceivePaymentSection({
  estimatedTotal,
  mode,
  onModeChange,
  paidNowText,
  onPaidNowChange,
  dueDate,
  onDueDateChange,
  paymentMethod,
  onPaymentMethodChange,
  paidNowValue,
  disabled = false,
  allowSupplierCredit = true,
  testIdPrefix = "receive-payment",
  lockedFromPo = false,
  lockedPaymentMethodLabel,
  settlementFields,
  onSettlementFieldsChange,
}: ReceivePaymentSectionProps) {
  const { t } = useI18n();
  const paid = paidNowValue ?? 0;
  const remaining = remainingCredit(estimatedTotal, paid);
  const creditMode = mode === "supplierCredit";
  const showDueDate = creditMode && remaining > 0 && !lockedFromPo;
  const showMethodDropdown = !lockedFromPo && paid > 0;
  const showLockedMethod = lockedFromPo && Boolean(lockedPaymentMethodLabel ?? paymentMethod);
  const settlement = settlementFields ?? {
    gCashReference: "",
    bankName: "",
    transferOrDepositReference: "",
    settlementDate: "",
    checkNumber: "",
    checkDate: "",
    settlementNotes: "",
  };

  function patchSettlement(patch: Partial<ReceiveSettlementFields>) {
    onSettlementFieldsChange?.({ ...settlement, ...patch });
  }

  const showGCashFields = lockedFromPo && paymentMethod === "GCash";
  const showBankFields =
    lockedFromPo && (paymentMethod === "BankTransfer" || paymentMethod === "BankDeposit");
  const showCheckFields = lockedFromPo && paymentMethod === "Check";

  return (
    <Card data-testid={`${testIdPrefix}-section`}>
      <h2 className="m-0 mb-3 text-[length:var(--exits-text-base)] font-semibold">
        {t("purchasing.paymentAtReceipt")}
      </h2>
      <div className="grid gap-3">
        {!lockedFromPo ? (
          <div
            className="flex flex-col gap-2 sm:flex-row sm:flex-wrap"
            role="group"
            aria-label={t("purchasing.paymentStatus")}
            data-testid={`${testIdPrefix}-mode`}
          >
            <button
              type="button"
              className={` rounded-md border px-3 text-[length:var(--exits-text-sm)] ${
                mode === "paidInFull"
                  ? "border-[var(--exits-primary)] bg-[color-mix(in_srgb,var(--exits-primary)_12%,transparent)] font-medium"
                  : "border-border bg-background"
              }`}
              disabled={disabled}
              aria-pressed={mode === "paidInFull"}
              onClick={() => onModeChange("paidInFull")}
              data-testid={`${testIdPrefix}-mode-full`}
            >
              {t("purchasing.paidInFull")}
            </button>
            {allowSupplierCredit ? (
              <button
                type="button"
                className={` rounded-md border px-3 text-[length:var(--exits-text-sm)] ${
                  creditMode
                    ? "border-[var(--exits-primary)] bg-[color-mix(in_srgb,var(--exits-primary)_12%,transparent)] font-medium"
                    : "border-border bg-background"
                }`}
                disabled={disabled}
                aria-pressed={creditMode}
                onClick={() => onModeChange("supplierCredit")}
                data-testid={`${testIdPrefix}-mode-credit`}
              >
                {t("purchasing.supplierCredit")}
              </button>
            ) : null}
          </div>
        ) : null}

        {showLockedMethod ? (
          <div
            className="text-[length:var(--exits-text-sm)]"
            data-testid={`${testIdPrefix}-locked-method`}
          >
            <p className="m-0">
              <span className="text-muted">{t("purchasing.paymentMethod")}: </span>
              <span className="font-medium">
                {lockedPaymentMethodLabel ??
                  t(methodLabelKey(paymentMethod))}
              </span>
            </p>
            <p className="m-0 mt-1 text-muted" data-testid={`${testIdPrefix}-locked-hint`}>
              {t("purchasing.paymentLockedFromPo")}
            </p>
          </div>
        ) : null}

        <dl
          className="m-0 grid gap-2 text-[length:var(--exits-text-sm)] sm:grid-cols-3"
          data-testid={`${testIdPrefix}-preview`}
        >
          <div>
            <dt className="text-muted">{t("purchasing.receiptValue")}</dt>
            <dd className="m-0">
              <MoneyDisplay amount={estimatedTotal} testId={`${testIdPrefix}-total`} />
            </dd>
          </div>
          <div>
            <dt className="text-muted">{t("purchasing.paidNow")}</dt>
            <dd className="m-0">
              <MoneyDisplay amount={paid} testId={`${testIdPrefix}-paid-preview`} />
            </dd>
          </div>
          <div>
            <dt className="text-muted">{t("purchasing.balanceDue")}</dt>
            <dd className="m-0" data-testid={`${testIdPrefix}-remaining`}>
              <MoneyDisplay amount={remaining} />
            </dd>
          </div>
        </dl>

        {creditMode && !lockedFromPo ? (
          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            {t("purchasing.paidNow")}
            <input
              type="text"
              inputMode="decimal"
              className="rounded-md border border-border bg-background px-3"
              value={paidNowText}
              disabled={disabled}
              onChange={(e) => onPaidNowChange(e.target.value)}
              data-testid={`${testIdPrefix}-paid-now`}
            />
          </label>
        ) : null}

        {showMethodDropdown ? (
          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            {t("purchasing.paymentMethodAtReceipt")}
            <select
              className="exits-select"
              value={paymentMethod}
              disabled={disabled}
              onChange={(e) =>
                onPaymentMethodChange(e.target.value as ReceivePaymentMethodCode)
              }
              data-testid={`${testIdPrefix}-method`}
            >
              {RECEIVE_PAYMENT_METHODS.map((method) => (
                <option key={method} value={method}>
                  {t(methodLabelKey(method))}
                </option>
              ))}
            </select>
          </label>
        ) : null}

        {showGCashFields ? (
          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            {t("purchasing.gcashReference")}
            <input
              type="text"
              className="rounded-md border border-border bg-background px-3"
              value={settlement.gCashReference}
              disabled={disabled}
              onChange={(e) => patchSettlement({ gCashReference: e.target.value })}
              data-testid={`${testIdPrefix}-gcash-ref`}
            />
          </label>
        ) : null}

        {showBankFields ? (
          <>
            <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
              {t("customers.bankName")}
              <input
                type="text"
                className="rounded-md border border-border bg-background px-3"
                value={settlement.bankName}
                disabled={disabled}
                onChange={(e) => patchSettlement({ bankName: e.target.value })}
                data-testid={`${testIdPrefix}-bank-name`}
              />
            </label>
            <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
              {t("purchasing.transferOrDepositReference")}
              <input
                type="text"
                className="rounded-md border border-border bg-background px-3"
                value={settlement.transferOrDepositReference}
                disabled={disabled}
                onChange={(e) =>
                  patchSettlement({ transferOrDepositReference: e.target.value })
                }
                data-testid={`${testIdPrefix}-bank-ref`}
              />
            </label>
            <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
              {t("purchasing.settlementDate")}
              <input
                type="date"
                className="rounded-md border border-border bg-background px-3"
                value={settlement.settlementDate}
                disabled={disabled}
                onChange={(e) => patchSettlement({ settlementDate: e.target.value })}
                data-testid={`${testIdPrefix}-settlement-date`}
              />
            </label>
          </>
        ) : null}

        {showCheckFields ? (
          <>
            <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
              {t("purchasing.checkNumber")}
              <input
                type="text"
                className="rounded-md border border-border bg-background px-3"
                value={settlement.checkNumber}
                disabled={disabled}
                onChange={(e) => patchSettlement({ checkNumber: e.target.value })}
                data-testid={`${testIdPrefix}-check-number`}
              />
            </label>
            <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
              {t("customers.bankName")}
              <input
                type="text"
                className="rounded-md border border-border bg-background px-3"
                value={settlement.bankName}
                disabled={disabled}
                onChange={(e) => patchSettlement({ bankName: e.target.value })}
                data-testid={`${testIdPrefix}-check-bank`}
              />
            </label>
            <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
              {t("purchasing.checkDate")}
              <input
                type="date"
                className="rounded-md border border-border bg-background px-3"
                value={settlement.checkDate}
                disabled={disabled}
                onChange={(e) => patchSettlement({ checkDate: e.target.value })}
                data-testid={`${testIdPrefix}-check-date`}
              />
            </label>
            <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
              {t("purchasing.checkPendingClearingHint")}
            </p>
          </>
        ) : null}

        {(showGCashFields || showBankFields || showCheckFields) && onSettlementFieldsChange ? (
          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            {t("purchasing.settlementNotesOptional")}
            <input
              type="text"
              className="rounded-md border border-border bg-background px-3"
              value={settlement.settlementNotes}
              disabled={disabled}
              onChange={(e) => patchSettlement({ settlementNotes: e.target.value })}
              data-testid={`${testIdPrefix}-settlement-notes`}
            />
          </label>
        ) : null}

        {showDueDate ? (
          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            {t("purchasing.dueDateOptional")}
            <input
              type="date"
              className="rounded-md border border-border bg-background px-3"
              value={dueDate}
              disabled={disabled}
              onChange={(e) => onDueDateChange(e.target.value)}
              data-testid={`${testIdPrefix}-due-date`}
            />
          </label>
        ) : null}
      </div>
    </Card>
  );
}

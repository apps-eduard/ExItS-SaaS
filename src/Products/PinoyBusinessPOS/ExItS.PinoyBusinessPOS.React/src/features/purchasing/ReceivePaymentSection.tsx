import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { Card } from "@/components/ui/card";
import {
  RECEIVE_PAYMENT_METHODS,
  remainingCredit,
  type ReceivePaymentMethodCode,
  type ReceivePaymentMode,
} from "@/features/purchasing/receive-payment";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";

function methodLabelKey(method: string): MessageKey {
  switch (method) {
    case "BankTransfer":
      return "supplierPayables.method.bankTransfer";
    case "GCash":
      return "supplierPayables.method.gcash";
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
}: ReceivePaymentSectionProps) {
  const { t } = useI18n();
  const paid = paidNowValue ?? 0;
  const remaining = remainingCredit(estimatedTotal, paid);
  const creditMode = mode === "supplierCredit";
  const showDueDate = creditMode && remaining > 0;
  const showMethod = paid > 0;

  return (
    <Card data-testid={`${testIdPrefix}-section`}>
      <h2 className="m-0 mb-3 text-[length:var(--exits-text-base)] font-semibold">
        {t("purchasing.paymentAtReceipt")}
      </h2>
      <div className="grid gap-3">
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

        <dl
          className="m-0 grid gap-2 text-[length:var(--exits-text-sm)] sm:grid-cols-3"
          data-testid={`${testIdPrefix}-preview`}
        >
          <div>
            <dt className="text-muted">{t("purchasing.purchaseTotal")}</dt>
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

        {creditMode ? (
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

        {showMethod ? (
          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            {t("purchasing.paymentMethodAtReceipt")}
            <select
              className="rounded-md border border-border bg-background px-3"
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

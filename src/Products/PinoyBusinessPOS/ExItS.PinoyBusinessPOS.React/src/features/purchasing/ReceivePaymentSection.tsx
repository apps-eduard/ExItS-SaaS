import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { Card } from "@/components/ui/card";
import { remainingCredit } from "@/features/purchasing/receive-payment";
import { useI18n } from "@/i18n/I18nProvider";

type ReceivePaymentSectionProps = {
  estimatedTotal: number;
  paidNowText: string;
  onPaidNowChange: (value: string) => void;
  dueDate: string;
  onDueDateChange: (value: string) => void;
  paidNowValue: number | null;
  disabled?: boolean;
  testIdPrefix?: string;
};

/**
 * Optional payment-at-receipt fields shared by PO receive and direct receive stock.
 */
export function ReceivePaymentSection({
  estimatedTotal,
  paidNowText,
  onPaidNowChange,
  dueDate,
  onDueDateChange,
  paidNowValue,
  disabled = false,
  testIdPrefix = "receive-payment",
}: ReceivePaymentSectionProps) {
  const { t } = useI18n();
  const paid = paidNowValue ?? 0;
  const remaining = remainingCredit(estimatedTotal, paid);
  const showDueDate = remaining > 0;

  return (
    <Card data-testid={`${testIdPrefix}-section`}>
      <h2 className="m-0 mb-3 text-[length:var(--exits-text-base)] font-semibold">
        {t("purchasing.paymentAtReceipt")}
      </h2>
      <div className="grid gap-3">
        <div className="flex flex-wrap items-baseline gap-2 text-[length:var(--exits-text-sm)]">
          <span className="text-muted">{t("purchasing.totalCost")}:</span>
          <MoneyDisplay amount={estimatedTotal} testId={`${testIdPrefix}-total`} />
        </div>
        <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
          {t("purchasing.paidNow")}
          <input
            type="text"
            inputMode="decimal"
            className="min-h-11 rounded-md border border-border bg-background px-3"
            value={paidNowText}
            disabled={disabled}
            onChange={(e) => onPaidNowChange(e.target.value)}
            data-testid={`${testIdPrefix}-paid-now`}
          />
        </label>
        <div
          className="flex flex-wrap items-baseline gap-2 text-[length:var(--exits-text-sm)]"
          data-testid={`${testIdPrefix}-remaining`}
        >
          <span className="text-muted">{t("purchasing.remainingCredit")}:</span>
          <MoneyDisplay amount={remaining} />
        </div>
        {showDueDate ? (
          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            {t("purchasing.dueDateOptional")}
            <input
              type="date"
              className="min-h-11 rounded-md border border-border bg-background px-3"
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

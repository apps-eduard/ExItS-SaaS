import { getPlanPaymentEntitlements } from "@/features/payments/payment-capabilities";
import type { MessageKey } from "@/i18n/messages";

/** Compact one-line payment capabilities for explore cards (not the nested checklist). */
export function buildPlanPaymentSummaryText(
  planKey: string,
  t: (key: MessageKey) => string,
): string {
  const ent = getPlanPaymentEntitlements(planKey);

  if (ent.onlinePayments) {
    return [
      t("personal.explore.payments.online"),
      t("personal.explore.payments.onlineGCash"),
      t("personal.explore.payments.onlineMaya"),
      `${t("personal.explore.payments.qrPh")} / ${t("personal.explore.payments.cards")}`,
    ].join(" · ");
  }

  if (ent.paymentManagement) {
    return [
      t("personal.explore.payments.bankTransfer"),
      t("personal.explore.payments.check"),
      t("personal.explore.payments.manualMethods"),
    ].join(" · ");
  }

  return [
    t("personal.explore.payments.cash"),
    t("personal.explore.payments.gcash"),
    t("personal.explore.payments.utang"),
  ].join(" · ");
}

export function PlanPaymentSummary({
  planKey,
  t,
}: {
  planKey: string;
  t: (key: MessageKey) => string;
}) {
  return (
    <p
      className="plan-payment-summary m-0 text-[length:var(--exits-text-xs)] text-muted"
      data-testid={`explore-payments-${planKey}`}
    >
      <span className="plan-payment-summary__label">{t("personal.explore.payments.title")}: </span>
      {buildPlanPaymentSummaryText(planKey, t)}
    </p>
  );
}

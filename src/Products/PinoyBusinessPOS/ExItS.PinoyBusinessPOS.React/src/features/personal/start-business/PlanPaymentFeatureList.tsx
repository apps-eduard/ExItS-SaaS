import { Check, Minus } from "lucide-react";
import { getPlanPaymentEntitlements } from "@/features/payments/payment-capabilities";
import type { MessageKey } from "@/i18n/messages";

type PaymentRow = {
  id: string;
  labelKey: MessageKey;
  included: boolean;
  nested?: boolean;
};

export function buildPlanPaymentRows(planKey: string): PaymentRow[] {
  const ent = getPlanPaymentEntitlements(planKey);
  const rows: PaymentRow[] = [
    { id: "cash", labelKey: "personal.explore.payments.cash", included: ent.basicPayments },
    { id: "gcash", labelKey: "personal.explore.payments.gcash", included: ent.basicPayments },
    { id: "utang", labelKey: "personal.explore.payments.utang", included: ent.basicPayments },
    {
      id: "management",
      labelKey: "personal.explore.payments.management",
      included: ent.paymentManagement,
    },
  ];

  if (ent.paymentManagement) {
    rows.push(
      { id: "bank", labelKey: "personal.explore.payments.bankTransfer", included: true, nested: true },
      { id: "check", labelKey: "personal.explore.payments.check", included: true, nested: true },
      {
        id: "manual",
        labelKey: "personal.explore.payments.manualMethods",
        included: true,
        nested: true,
      },
    );
  }

  rows.push({
    id: "online",
    labelKey: "personal.explore.payments.online",
    included: ent.onlinePayments,
  });

  if (ent.onlinePayments) {
    rows.push(
      {
        id: "online-integrations",
        labelKey: "personal.explore.payments.onlineIntegrations",
        included: true,
        nested: true,
      },
      {
        id: "online-gcash",
        labelKey: "personal.explore.payments.onlineGCash",
        included: true,
        nested: true,
      },
      {
        id: "online-maya",
        labelKey: "personal.explore.payments.onlineMaya",
        included: true,
        nested: true,
      },
      { id: "qr", labelKey: "personal.explore.payments.qrPh", included: true, nested: true },
      { id: "cards", labelKey: "personal.explore.payments.cards", included: true, nested: true },
      {
        id: "online-banking",
        labelKey: "personal.explore.payments.onlineBanking",
        included: true,
        nested: true,
      },
    );
  }

  return rows;
}

export function PlanPaymentFeatureList({
  planKey,
  t,
}: {
  planKey: string;
  t: (key: MessageKey) => string;
}) {
  const rows = buildPlanPaymentRows(planKey);
  return (
    <div className="plan-payment-section" data-testid={`explore-payments-${planKey}`}>
      <p className="plan-payment-section__title m-0">{t("personal.explore.payments.title")}</p>
      <ul className="m-0 mt-1.5 list-none space-y-1 p-0 text-[length:var(--exits-text-sm)]">
        {rows.map((row) => (
          <li
            key={row.id}
            className={`flex items-start gap-1.5 ${row.nested ? "ps-4 text-muted" : ""} ${
              row.included ? "text-foreground" : "text-muted"
            }`}
          >
            {row.included ? (
              <Check className="mt-0.5 size-3.5 shrink-0 text-primary" aria-hidden strokeWidth={2.25} />
            ) : (
              <Minus className="mt-0.5 size-3.5 shrink-0 opacity-70" aria-hidden strokeWidth={2.25} />
            )}
            <span>
              {t(row.labelKey)}
              {row.id === "online-integrations" ? (
                <span className="mt-0.5 block text-[length:var(--exits-text-xs)] text-muted">
                  {t("personal.explore.payments.onlineNote")}
                </span>
              ) : null}
            </span>
          </li>
        ))}
      </ul>
    </div>
  );
}

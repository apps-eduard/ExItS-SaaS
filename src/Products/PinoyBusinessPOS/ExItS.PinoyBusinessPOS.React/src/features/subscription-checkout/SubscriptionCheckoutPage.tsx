import { useMemo } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { getSubscriptionPayment } from "@/api/platform/subscription-payment-client";
import { Button } from "@/components/ui/button";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { Notice } from "@/components/exits/Notice";
import { PageHeader } from "@/components/exits/PageHeader";
import {
  formatPaymentMoney,
  isPaidStatus,
  paymentChannelPath,
  paymentResultPath,
} from "@/features/subscription-checkout/checkout-helpers";
import { useI18n } from "@/i18n/I18nProvider";
import { useSession } from "@/session/SessionProvider";

export function SubscriptionCheckoutPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const { paymentId = "" } = useParams();
  const { session } = useSession();
  const organizationId = session?.selectedOrganizationId ?? "";

  const paymentQuery = useQuery({
    queryKey: ["subscription-payment", organizationId, paymentId],
    queryFn: ({ signal }) => getSubscriptionPayment(organizationId, paymentId, signal),
    enabled: Boolean(organizationId && paymentId),
  });

  const payment = paymentQuery.data;

  const alreadyPaid = useMemo(
    () => (payment ? isPaidStatus(payment.status) : false),
    [payment],
  );

  if (!organizationId) {
    return (
      <div className="exits-page flex flex-col gap-3" data-testid="subscription-checkout-missing-org">
        <PageHeader title={t("subscriptionCheckout.title")} />
        <ErrorState
          title={t("subscriptionCheckout.errorTitle")}
          detail={t("subscriptionCheckout.missingOrganization")}
        />
      </div>
    );
  }

  if (paymentQuery.isPending) {
    return <LoadingSkeleton label={t("subscriptionCheckout.loading")} />;
  }

  if (paymentQuery.isError || !payment) {
    return (
      <div className="exits-page flex flex-col gap-3" data-testid="subscription-checkout-error">
        <PageHeader title={t("subscriptionCheckout.title")} />
        <ErrorState
          title={t("subscriptionCheckout.errorTitle")}
          detail={t("subscriptionCheckout.errorDetail")}
        />
        <Button type="button" className="w-fit" onClick={() => void paymentQuery.refetch()}>
          {t("subscriptionCheckout.retry")}
        </Button>
      </div>
    );
  }

  if (alreadyPaid) {
    return (
      <div className="exits-page flex flex-col gap-3" data-testid="subscription-checkout-already-paid">
        <PageHeader title={t("subscriptionCheckout.title")} />
        <Notice tone="success">{t("subscriptionCheckout.alreadyPaid")}</Notice>
        <Button type="button" onClick={() => navigate(paymentResultPath(paymentId), { replace: true })}>
          {t("subscriptionCheckout.viewReceipt")}
        </Button>
      </div>
    );
  }

  const showDiscount = payment.discountPercent > 0 || payment.discountAmount > 0;

  return (
    <div className="exits-page mx-auto flex w-full max-w-lg flex-col gap-4" data-testid="subscription-checkout-page">
      <PageHeader
        title={t("subscriptionCheckout.title")}
        description={t("subscriptionCheckout.lede")}
      />

      <Notice tone="warning" testId="subscription-checkout-test-banner" title={t("subscriptionCheckout.testBannerTitle")}>
        {t("subscriptionCheckout.testBannerBody")}
      </Notice>

      <section
        className="exits-list__card flex flex-col gap-2 p-4"
        data-testid="subscription-checkout-summary"
      >
        <h2 className="m-0 text-[length:var(--exits-text-base)] font-semibold">
          {t("subscriptionCheckout.summaryTitle")}
        </h2>
        <dl className="m-0 grid grid-cols-[auto_1fr] gap-x-3 gap-y-1.5 text-[length:var(--exits-text-sm)]">
          <dt className="text-muted">{t("subscriptionCheckout.plan")}</dt>
          <dd className="m-0 font-medium capitalize">{payment.planKey}</dd>
          <dt className="text-muted">{t("subscriptionCheckout.billing")}</dt>
          <dd className="m-0">{payment.billingCycle}</dd>
          <dt className="text-muted">{t("subscriptionCheckout.reference")}</dt>
          <dd className="m-0 font-mono text-[length:var(--exits-text-xs)]">{payment.referenceNumber}</dd>
          <dt className="text-muted">{t("subscriptionCheckout.base")}</dt>
          <dd className="m-0">{formatPaymentMoney(payment.baseAmount, payment.currencyCode)}</dd>
          {showDiscount ? (
            <>
              <dt className="text-muted">{t("subscriptionCheckout.discount")}</dt>
              <dd className="m-0">
                {formatPaymentMoney(payment.discountAmount, payment.currencyCode)}
                {payment.discountPercent > 0
                  ? ` (${Math.round(payment.discountPercent)}%)`
                  : ""}
              </dd>
            </>
          ) : null}
          <dt className="text-muted font-semibold">{t("subscriptionCheckout.total")}</dt>
          <dd className="m-0 text-[length:var(--exits-text-lg)] font-semibold">
            {formatPaymentMoney(payment.finalAmount, payment.currencyCode)}
          </dd>
        </dl>
      </section>

      <section className="flex flex-col gap-2">
        <h2 className="m-0 text-[length:var(--exits-text-base)] font-semibold">
          {t("subscriptionCheckout.methodTitle")}
        </h2>
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
          {t("subscriptionCheckout.methodHint")}
        </p>
        <div className="flex flex-col gap-2">
          <Button
            type="button"
            data-testid="checkout-method-gcash"
            onClick={() => navigate(paymentChannelPath(paymentId, "gcash"))}
          >
            {t("subscriptionCheckout.method.gcash")}
          </Button>
          <Button
            type="button"
            variant="secondary"
            data-testid="checkout-method-maya"
            onClick={() => navigate(paymentChannelPath(paymentId, "maya"))}
          >
            {t("subscriptionCheckout.method.maya")}
          </Button>
          <Button
            type="button"
            variant="secondary"
            data-testid="checkout-method-card"
            onClick={() => navigate(paymentChannelPath(paymentId, "card"))}
          >
            {t("subscriptionCheckout.method.card")}
          </Button>
        </div>
      </section>
    </div>
  );
}

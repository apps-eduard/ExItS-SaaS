import { useNavigate, useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { getSubscriptionPayment } from "@/api/platform/subscription-payment-client";
import { Button } from "@/components/ui/button";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { Notice } from "@/components/exits/Notice";
import { PageHeader } from "@/components/exits/PageHeader";
import {
  buildTimelineEntries,
  formatPaymentMoney,
  isFailedStatus,
  isPaidStatus,
  isProcessingStatus,
  isTestEnvironment,
} from "@/features/subscription-checkout/checkout-helpers";
import { useI18n } from "@/i18n/I18nProvider";
import { useSession } from "@/session/SessionProvider";

function formatWhen(iso: string): string {
  try {
    return new Date(iso).toLocaleString();
  } catch {
    return iso;
  }
}

export function PaymentResultPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const { paymentId = "" } = useParams();
  const { session } = useSession();
  const organizationId = session?.selectedOrganizationId ?? "";

  const paymentQuery = useQuery({
    queryKey: ["subscription-payment", organizationId, paymentId],
    queryFn: ({ signal }) => getSubscriptionPayment(organizationId, paymentId, signal),
    enabled: Boolean(organizationId && paymentId),
    refetchInterval: (query) => {
      const status = query.state.data?.status;
      return status && isProcessingStatus(status) ? 4000 : false;
    },
  });

  if (!organizationId || paymentQuery.isPending) {
    return <LoadingSkeleton label={t("subscriptionCheckout.loading")} />;
  }

  if (paymentQuery.isError || !paymentQuery.data) {
    return (
      <div className="exits-page flex flex-col gap-3">
        <PageHeader title={t("subscriptionCheckout.resultTitle")} />
        <ErrorState
          title={t("subscriptionCheckout.errorTitle")}
          detail={t("subscriptionCheckout.errorDetail")}
        />
      </div>
    );
  }

  const payment = paymentQuery.data;
  const paid = isPaidStatus(payment.status);
  const failed = isFailedStatus(payment.status);
  const processing = isProcessingStatus(payment.status);
  const timeline = buildTimelineEntries(payment);

  return (
    <div
      className="exits-page mx-auto flex w-full max-w-lg flex-col gap-4"
      data-testid="subscription-payment-result"
    >
      <PageHeader title={t("subscriptionCheckout.resultTitle")} />

      {isTestEnvironment(payment.environment) ? (
        <Notice tone="warning" testId="subscription-result-test-banner" title={t("subscriptionCheckout.testBannerTitle")}>
          {t("subscriptionCheckout.testBannerBody")}
        </Notice>
      ) : null}

      {paid ? (
        <Notice tone="success" title={t("subscriptionCheckout.result.paidTitle")}>
          {t("subscriptionCheckout.result.paidBody")}
        </Notice>
      ) : null}
      {failed ? (
        <Notice tone="danger" title={t("subscriptionCheckout.result.failedTitle")}>
          {payment.failureReason ?? t("subscriptionCheckout.result.failedBody")}
        </Notice>
      ) : null}
      {processing ? (
        <Notice tone="info" title={t("subscriptionCheckout.result.processingTitle")}>
          {t("subscriptionCheckout.result.processingBody")}
        </Notice>
      ) : null}

      <section className="exits-list__card flex flex-col gap-2 p-4" data-testid="subscription-receipt">
        <h2 className="m-0 text-[length:var(--exits-text-base)] font-semibold">
          {t("subscriptionCheckout.receiptTitle")}
        </h2>
        <dl className="m-0 grid grid-cols-[auto_1fr] gap-x-3 gap-y-1.5 text-[length:var(--exits-text-sm)]">
          <dt className="text-muted">{t("subscriptionCheckout.reference")}</dt>
          <dd className="m-0 font-mono text-[length:var(--exits-text-xs)]">{payment.referenceNumber}</dd>
          <dt className="text-muted">{t("subscriptionCheckout.status")}</dt>
          <dd className="m-0 font-medium">{payment.status}</dd>
          <dt className="text-muted">{t("subscriptionCheckout.channel")}</dt>
          <dd className="m-0">{payment.channel ?? "—"}</dd>
          <dt className="text-muted">{t("subscriptionCheckout.total")}</dt>
          <dd className="m-0 font-semibold">
            {formatPaymentMoney(payment.finalAmount, payment.currencyCode)}
          </dd>
          {payment.providerReference ? (
            <>
              <dt className="text-muted">{t("subscriptionCheckout.providerRef")}</dt>
              <dd className="m-0 font-mono text-[length:var(--exits-text-xs)]">
                {payment.providerReference}
              </dd>
            </>
          ) : null}
          {payment.cardLast4 ? (
            <>
              <dt className="text-muted">{t("subscriptionCheckout.cardMasked")}</dt>
              <dd className="m-0">
                {(payment.cardBrand ?? "Card")} ···· {payment.cardLast4}
              </dd>
            </>
          ) : null}
          {payment.discountPercent > 0 || payment.discountAmount > 0 ? (
            <>
              <dt className="text-muted">{t("subscriptionCheckout.base")}</dt>
              <dd className="m-0">{formatPaymentMoney(payment.baseAmount, payment.currencyCode)}</dd>
              <dt className="text-muted">{t("subscriptionCheckout.discount")}</dt>
              <dd className="m-0">
                {formatPaymentMoney(payment.discountAmount, payment.currencyCode)}
                {payment.discountPercent > 0
                  ? ` (${Math.round(payment.discountPercent)}%)`
                  : ""}
              </dd>
            </>
          ) : null}
        </dl>
      </section>

      <section className="exits-list__card flex flex-col gap-2 p-4" data-testid="subscription-activity-timeline">
        <h2 className="m-0 text-[length:var(--exits-text-base)] font-semibold">
          {t("subscriptionCheckout.activityTitle")}
        </h2>
        <ol className="m-0 list-none space-y-2 p-0">
          {timeline.map((entry) => (
            <li key={entry.id} className="flex flex-col gap-0.5 border-s-2 border-border ps-3">
              <span className="text-[length:var(--exits-text-sm)] font-medium">{entry.label}</span>
              <span className="text-[length:var(--exits-text-xs)] text-muted">
                {formatWhen(entry.at)}
              </span>
            </li>
          ))}
        </ol>
      </section>

      <div className="flex flex-wrap gap-2">
        {paid ? (
          <Button
            type="button"
            data-testid="subscription-continue-onboarding"
            onClick={() => navigate("/onboarding", { replace: true })}
          >
            {t("subscriptionCheckout.continueOnboarding")}
          </Button>
        ) : null}
        {failed ? (
          <Button
            type="button"
            data-testid="subscription-try-again"
            onClick={() => navigate(`/subscription-checkout/${paymentId}`, { replace: true })}
          >
            {t("subscriptionCheckout.tryAgain")}
          </Button>
        ) : null}
        {processing ? (
          <Button
            type="button"
            variant="secondary"
            data-testid="subscription-refresh-status"
            onClick={() => void paymentQuery.refetch()}
          >
            {t("subscriptionCheckout.refreshStatus")}
          </Button>
        ) : null}
      </div>
    </div>
  );
}

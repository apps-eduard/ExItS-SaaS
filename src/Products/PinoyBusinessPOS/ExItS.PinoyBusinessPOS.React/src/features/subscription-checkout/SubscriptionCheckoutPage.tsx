import { useEffect, useMemo, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { useMutation, useQuery } from "@tanstack/react-query";
import {
  getPersonalSubscriptionPayment,
  retryPersonalSubscriptionPayment,
  selectPersonalSubscriptionPaymentChannel,
  type SubscriptionPaymentChannel,
} from "@/api/platform/subscription-payment-client";
import { Button } from "@/components/ui/button";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { Notice } from "@/components/exits/Notice";
import { PageHeader } from "@/components/exits/PageHeader";
import {
  buildTimelineEntries,
  checkoutTitleKey,
  formatPaymentMoney,
  isCancelledStatus,
  isExpiredStatus,
  isFailedStatus,
  isPaidStatus,
  isPendingStatus,
  isProcessingStatus,
  isTestEnvironment,
  paymentChannelPath,
  startBusinessAfterPaidPath,
  summaryCardTitleKey,
} from "@/features/subscription-checkout/checkout-helpers";
import {
  clearPendingSubscriptionCheckout,
  writePendingSubscriptionCheckout,
} from "@/features/subscription-checkout/pending-subscription-checkout";
import { useI18n } from "@/i18n/I18nProvider";

function formatWhen(iso: string): string {
  try {
    return new Date(iso).toLocaleString();
  } catch {
    return iso;
  }
}

function channelSlug(channel: string | null | undefined): "gcash" | "maya" | "card" | null {
  switch ((channel ?? "").toLowerCase()) {
    case "gcash":
      return "gcash";
    case "maya":
      return "maya";
    case "card":
      return "card";
    default:
      return null;
  }
}

export function SubscriptionCheckoutPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const { paymentId = "" } = useParams();
  const [actionError, setActionError] = useState<string | null>(null);
  const [forceMethodPicker, setForceMethodPicker] = useState(false);

  const paymentQuery = useQuery({
    queryKey: ["subscription-payment", "personal", paymentId],
    queryFn: ({ signal }) => getPersonalSubscriptionPayment(paymentId, signal),
    enabled: Boolean(paymentId),
    refetchInterval: (query) => {
      const status = query.state.data?.status;
      return status && isProcessingStatus(status) ? 4000 : false;
    },
  });

  const payment = paymentQuery.data;

  useEffect(() => {
    if (!payment) {
      return;
    }
    // Keep marker for unpaid / in-flight checkout only. Terminal Paid is cleared on Continue;
    // Failed/Cancelled/Expired keep the marker until retry or explicit leave (Back to plans).
    writePendingSubscriptionCheckout({
      paymentId: payment.id,
      organizationId: payment.organizationId,
      planKey: payment.planKey,
      billingCycle: payment.billingCycle,
    });
  }, [payment?.id, payment?.organizationId, payment?.planKey, payment?.billingCycle, payment?.status]);

  const retryMutation = useMutation({
    mutationFn: () => retryPersonalSubscriptionPayment(paymentId),
    onSuccess: (created) => {
      writePendingSubscriptionCheckout({
        paymentId: created.id,
        organizationId: created.organizationId,
        planKey: created.planKey,
        billingCycle: created.billingCycle,
      });
      navigate(`/subscription-checkout/${created.id}`, { replace: true });
    },
    onError: (error) => {
      setActionError(error instanceof Error ? error.message : t("subscriptionCheckout.retryFailed"));
    },
  });

  const selectChannelMutation = useMutation({
    mutationFn: (channel: SubscriptionPaymentChannel) =>
      selectPersonalSubscriptionPaymentChannel(paymentId, channel),
    onSuccess: async (updated, channel) => {
      setForceMethodPicker(false);
      await paymentQuery.refetch();
      const slug = channelSlug(updated.channel ?? channel);
      if (slug) {
        navigate(paymentChannelPath(paymentId, slug));
      }
    },
    onError: (error) => {
      setActionError(error instanceof Error ? error.message : t("subscriptionCheckout.errorDetail"));
    },
  });

  const alreadyPaid = useMemo(
    () => (payment ? isPaidStatus(payment.status) : false),
    [payment],
  );

  if (!paymentId) {
    return (
      <div className="flex flex-col gap-3" data-testid="subscription-checkout-missing-id">
        <PageHeader title={t("subscriptionCheckout.title")} />
        <ErrorState
          title={t("subscriptionCheckout.errorTitle")}
          detail={t("subscriptionCheckout.errorDetail")}
        />
      </div>
    );
  }

  if (paymentQuery.isPending) {
    return (
      <div data-testid="subscription-checkout-loading">
        <LoadingSkeleton label={t("subscriptionCheckout.loading")} />
      </div>
    );
  }

  if (paymentQuery.isError || !payment) {
    return (
      <div className="flex flex-col gap-3" data-testid="subscription-checkout-error">
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

  const pending = isPendingStatus(payment.status);
  const processing = isProcessingStatus(payment.status);
  const failed = isFailedStatus(payment.status);
  const cancelled = isCancelledStatus(payment.status);
  const expired = isExpiredStatus(payment.status);
  const paid = alreadyPaid;
  const hasChannel = Boolean(payment.channel) && !forceMethodPicker;
  const showDiscount = payment.discountPercent > 0 || payment.discountAmount > 0;
  const timeline = buildTimelineEntries(payment);
  const title = t(checkoutTitleKey(payment));
  const summaryTitle = t(summaryCardTitleKey(payment));
  const channelContinueSlug = channelSlug(payment.channel);

  return (
    <div className="flex w-full flex-col gap-4" data-testid="subscription-checkout-page">
      <PageHeader title={title} description={t("subscriptionCheckout.lede")} />

      <div className="flex flex-wrap gap-2">
        <Button type="button" variant="ghost" asChild data-testid="subscription-back-to-plans">
          <Link
            to="/personal/explore-pos"
            onClick={() => clearPendingSubscriptionCheckout()}
          >
            {t("subscriptionCheckout.backToPlans")}
          </Link>
        </Button>
      </div>

      {isTestEnvironment(payment.environment) ? (
        <Notice
          tone="warning"
          testId="subscription-checkout-test-banner"
          title={t("subscriptionCheckout.testBannerTitle")}
        >
          {t("subscriptionCheckout.testBannerBody")}
        </Notice>
      ) : null}

      {paid ? (
        <Notice tone="success" title={t("subscriptionCheckout.state.paidTitle")}>
          {t("subscriptionCheckout.result.paidBody")}
        </Notice>
      ) : null}
      {failed ? (
        <Notice tone="danger" title={t("subscriptionCheckout.state.failedTitle")}>
          {payment.failureReason ?? t("subscriptionCheckout.result.failedBody")}
        </Notice>
      ) : null}
      {processing ? (
        <Notice tone="info" title={t("subscriptionCheckout.state.processingTitle")}>
          {t("subscriptionCheckout.result.processingBody")}
        </Notice>
      ) : null}
      {cancelled ? (
        <Notice tone="warning" title={t("subscriptionCheckout.state.cancelledTitle")}>
          {t("subscriptionCheckout.state.cancelledBody")}
        </Notice>
      ) : null}
      {expired ? (
        <Notice tone="warning" title={t("subscriptionCheckout.state.expiredTitle")}>
          {t("subscriptionCheckout.state.expiredBody")}
        </Notice>
      ) : null}

      {actionError ? (
        <Notice tone="danger" title={t("subscriptionCheckout.errorTitle")}>
          {actionError}
        </Notice>
      ) : null}

      <section
        className="exits-list__card flex flex-col gap-2 p-4"
        data-testid={paid ? "subscription-receipt" : "subscription-payment-details"}
      >
        <h2 className="m-0 text-[length:var(--exits-text-base)] font-semibold">{summaryTitle}</h2>
        <dl className="m-0 grid grid-cols-[auto_1fr] gap-x-3 gap-y-1.5 text-[length:var(--exits-text-sm)]">
          <dt className="text-muted">{t("subscriptionCheckout.plan")}</dt>
          <dd className="m-0 font-medium capitalize">{payment.planKey}</dd>
          <dt className="text-muted">{t("subscriptionCheckout.billing")}</dt>
          <dd className="m-0">{payment.billingCycle}</dd>
          <dt className="text-muted">{t("subscriptionCheckout.reference")}</dt>
          <dd className="m-0 font-mono text-[length:var(--exits-text-xs)]">{payment.referenceNumber}</dd>
          <dt className="text-muted">{t("subscriptionCheckout.status")}</dt>
          <dd className="m-0 font-medium">{payment.status}</dd>
          {payment.channel ? (
            <>
              <dt className="text-muted">{t("subscriptionCheckout.channel")}</dt>
              <dd className="m-0">{payment.channel}</dd>
            </>
          ) : null}
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
          {payment.providerReference ? (
            <>
              <dt className="text-muted">{t("subscriptionCheckout.providerRef")}</dt>
              <dd className="m-0 font-mono text-[length:var(--exits-text-xs)]">
                {payment.providerReference}
              </dd>
            </>
          ) : null}
        </dl>
      </section>

      {pending && !hasChannel ? (
        <section className="flex flex-col gap-2" data-testid="subscription-method-picker">
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
              disabled={selectChannelMutation.isPending}
              onClick={() => selectChannelMutation.mutate("GCash")}
            >
              {t("subscriptionCheckout.method.gcash")}
            </Button>
            <Button
              type="button"
              variant="secondary"
              data-testid="checkout-method-maya"
              disabled={selectChannelMutation.isPending}
              onClick={() => selectChannelMutation.mutate("Maya")}
            >
              {t("subscriptionCheckout.method.maya")}
            </Button>
            <Button
              type="button"
              variant="secondary"
              data-testid="checkout-method-card"
              disabled={selectChannelMutation.isPending}
              onClick={() => selectChannelMutation.mutate("Card")}
            >
              {t("subscriptionCheckout.method.card")}
            </Button>
          </div>
        </section>
      ) : null}

      {pending && hasChannel && channelContinueSlug ? (
        <section className="flex flex-col gap-2" data-testid="subscription-channel-continue">
          <p className="m-0 text-[length:var(--exits-text-sm)]">
            {payment.channel} · {formatPaymentMoney(payment.finalAmount, payment.currencyCode)}
          </p>
          <Button
            type="button"
            data-testid="subscription-continue-channel"
            onClick={() => navigate(paymentChannelPath(paymentId, channelContinueSlug))}
          >
            {t("subscriptionCheckout.continueWithChannel").replace("{channel}", payment.channel ?? "")}
          </Button>
          <Button
            type="button"
            variant="secondary"
            data-testid="subscription-change-method"
            onClick={() => {
              setForceMethodPicker(true);
              setActionError(null);
            }}
          >
            {t("subscriptionCheckout.chooseAnotherMethod")}
          </Button>
        </section>
      ) : null}

      {processing ? (
        <div className="flex flex-wrap gap-2">
          <Button
            type="button"
            variant="secondary"
            data-testid="subscription-refresh-status"
            onClick={() => void paymentQuery.refetch()}
          >
            {t("subscriptionCheckout.refreshStatus")}
          </Button>
        </div>
      ) : null}

      {failed || cancelled || expired ? (
        <div className="flex flex-wrap gap-2">
          <Button
            type="button"
            data-testid="subscription-try-again"
            disabled={retryMutation.isPending}
            onClick={() => retryMutation.mutate()}
          >
            {expired
              ? t("subscriptionCheckout.startNewPayment")
              : t("subscriptionCheckout.tryAgain")}
          </Button>
          <Button type="button" variant="secondary" asChild data-testid="subscription-change-method-failed">
            <Link
              to="/personal/explore-pos"
              onClick={() => clearPendingSubscriptionCheckout()}
            >
              {t("subscriptionCheckout.chooseAnotherMethod")}
            </Link>
          </Button>
          <Button type="button" variant="ghost" asChild>
            <Link
              to="/personal/explore-pos"
              onClick={() => clearPendingSubscriptionCheckout()}
            >
              {t("subscriptionCheckout.backToPlans")}
            </Link>
          </Button>
        </div>
      ) : null}

      {paid ? (
        <div className="flex flex-wrap gap-2">
          <Button
            type="button"
            data-testid="subscription-continue-onboarding"
            onClick={() => {
              clearPendingSubscriptionCheckout();
              navigate(startBusinessAfterPaidPath(payment), { replace: true });
            }}
          >
            {t("subscriptionCheckout.continueOnboarding")}
          </Button>
        </div>
      ) : null}

      <section className="exits-list__card flex flex-col gap-2 p-4" data-testid="subscription-activity-timeline">
        <h2 className="m-0 text-[length:var(--exits-text-base)] font-semibold">
          {t("subscriptionCheckout.activityTitle")}
        </h2>
        <ol className="m-0 list-none space-y-2 p-0">
          {timeline.map((entry) => (
            <li key={entry.id} className="flex flex-col gap-0.5 border-s-2 border-border ps-3">
              <span className="text-[length:var(--exits-text-sm)] font-medium">{entry.label}</span>
              <span className="text-[length:var(--exits-text-xs)] text-muted">{formatWhen(entry.at)}</span>
            </li>
          ))}
        </ol>
      </section>
    </div>
  );
}

import { useEffect, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { Button } from "@/components/ui/button";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { Notice } from "@/components/exits/Notice";
import { PageHeader } from "@/components/exits/PageHeader";
import {
  cancelOrganizationSubscriptionHostedCheckout,
  cancelPersonalSubscriptionHostedCheckout,
  syncOrganizationSubscriptionHostedCheckout,
  syncPersonalSubscriptionHostedCheckout,
  type SubscriptionPaymentTransactionDto,
} from "@/api/platform/subscription-payment-client";
import {
  isCancelledStatus,
  isFailedStatus,
  isPaidStatus,
} from "@/features/subscription-checkout/checkout-helpers";
import { useI18n } from "@/i18n/I18nProvider";

const POLL_MS = 2000;

export function BillingPaymentReturnPage({
  outcome,
  maxAttempts = 8,
}: {
  outcome: "success" | "cancelled";
  maxAttempts?: number;
}) {
  const { t } = useI18n();
  const [searchParams] = useSearchParams();
  const paymentId = searchParams.get("paymentId") ?? "";
  const organizationId = searchParams.get("organizationId") ?? "";
  const [attempts, setAttempts] = useState(0);

  const paymentQuery = useQuery({
    queryKey: ["subscription-payment", "hosted-return", outcome, paymentId, organizationId],
    enabled: Boolean(paymentId),
    queryFn: ({ signal }) => loadReturnPayment(outcome, paymentId, organizationId, signal),
    refetchInterval: (query) => {
      if (outcome === "cancelled") return false;
      const status = query.state.data?.status;
      if (!status || isPaidStatus(status) || isFailedStatus(status) || isCancelledStatus(status)) {
        return false;
      }
      return attempts >= maxAttempts ? false : POLL_MS;
    },
  });

  useEffect(() => {
    if (outcome === "cancelled" || !paymentQuery.isSuccess) return;
    const status = paymentQuery.data?.status;
    if (status && (isPaidStatus(status) || isFailedStatus(status) || isCancelledStatus(status))) {
      return;
    }
    setAttempts((current) => current + 1);
  }, [outcome, paymentQuery.dataUpdatedAt, paymentQuery.isSuccess, paymentQuery.data?.status]);

  if (!paymentId) {
    return (
      <div className="mx-auto flex w-full max-w-xl flex-col gap-3" data-testid="billing-payment-missing">
        <PageHeader title={t("billingPayment.failedTitle")} />
        <ErrorState title={t("billingPayment.failedTitle")} detail={t("subscriptionCheckout.errorDetail")} />
      </div>
    );
  }

  const payment = paymentQuery.data;
  const paid = payment ? isPaidStatus(payment.status) : false;
  const failed = payment ? isFailedStatus(payment.status) : false;
  const cancelled = outcome === "cancelled" || (payment ? isCancelledStatus(payment.status) : false);
  const delayed = outcome === "success" && !paid && !failed && !cancelled && attempts >= maxAttempts;
  const verifying = outcome === "success" && !paid && !failed && !cancelled && !delayed;

  return (
    <div className="mx-auto flex w-full max-w-xl flex-col gap-4 px-1" data-testid="billing-payment-return">
      <PageHeader
        title={
          paid
            ? t("billingPayment.paidTitle")
            : cancelled
              ? t("billingPayment.cancelledTitle")
              : failed
                ? t("billingPayment.failedTitle")
                : delayed
                  ? t("billingPayment.delayedTitle")
                  : t("billingPayment.verifyingTitle")
        }
      />

      {paymentQuery.isPending && outcome === "success" ? (
        <div data-testid="billing-payment-verifying">
          <LoadingSkeleton label={t("billingPayment.verifyingBody")} />
        </div>
      ) : null}

      {paid ? (
        <Notice tone="success" title={t("billingPayment.paidTitle")} testId="billing-payment-paid">
          {t("billingPayment.paidBody")}
        </Notice>
      ) : null}

      {verifying && !paymentQuery.isPending ? (
        <Notice tone="info" title={t("billingPayment.verifyingTitle")} testId="billing-payment-verifying">
          {t("billingPayment.verifyingBody")}
        </Notice>
      ) : null}

      {delayed ? (
        <Notice tone="info" title={t("billingPayment.delayedTitle")} testId="billing-payment-delayed">
          {t("billingPayment.delayedBody")}
        </Notice>
      ) : null}

      {failed ? (
        <Notice tone="danger" title={t("billingPayment.failedTitle")} testId="billing-payment-failed">
          {t("billingPayment.failedBody")}
        </Notice>
      ) : null}

      {cancelled && !paid ? (
        <Notice tone="warning" title={t("billingPayment.cancelledTitle")} testId="billing-payment-cancelled">
          {t("billingPayment.cancelledBody")}
        </Notice>
      ) : null}

      <div className="flex flex-col gap-2 sm:flex-row">
        <Button type="button" className="w-full sm:w-auto" asChild data-testid="billing-payment-try-again">
          <Link to={payment ? `/subscription-checkout/${payment.id}` : "/personal/explore-pos"}>
            {t("billingPayment.tryAgain")}
          </Link>
        </Button>
        <Button type="button" variant="secondary" className="w-full sm:w-auto" asChild data-testid="billing-payment-back">
          <Link to={organizationId ? "/org/subscription" : "/personal/explore-pos"}>
            {t("billingPayment.backToBilling")}
          </Link>
        </Button>
      </div>
    </div>
  );
}

function loadReturnPayment(
  outcome: "success" | "cancelled",
  paymentId: string,
  organizationId: string,
  signal: AbortSignal,
): Promise<SubscriptionPaymentTransactionDto> {
  if (outcome === "cancelled") {
    return organizationId
      ? cancelOrganizationSubscriptionHostedCheckout(organizationId, paymentId, signal)
      : cancelPersonalSubscriptionHostedCheckout(paymentId, signal);
  }
  return organizationId
    ? syncOrganizationSubscriptionHostedCheckout(organizationId, paymentId, signal)
    : syncPersonalSubscriptionHostedCheckout(paymentId, signal);
}

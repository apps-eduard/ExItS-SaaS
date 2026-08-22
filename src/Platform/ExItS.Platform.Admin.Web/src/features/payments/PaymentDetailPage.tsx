import { useState } from "react";
import { Link, useParams } from "react-router-dom";
import { PLATFORM_PERMISSIONS } from "@/api/authorization/authorization-types";
import { PlatformApiError } from "@/api/platform-http";
import {
  parsePaymentId,
  paymentsListHref,
} from "@/api/payments/payment-client";
import { subscriptionDetailHref } from "@/api/subscriptions/subscription-portfolio-query";
import { Alert } from "@/components/ui/alert";
import { ConfirmActionDialog } from "@/components/exits/ConfirmActionDialog";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import { ErrorState } from "@/components/exits/ErrorState";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusIndicator } from "@/components/exits/StatusIndicator";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import {
  useConfirmManualPaymentMutation,
  useRejectManualPaymentMutation,
  useVoidManualPaymentMutation,
} from "@/features/commercial/use-commercial-mutations";
import { paymentActionCapabilities } from "@/features/organizations/billing-lifecycle";
import { commercialMutationFailureCopy } from "@/features/organizations/commercial-mutation-feedback";
import { useOrganizationSubscriptionsQuery } from "@/features/organizations/use-organization-workspace-queries";
import { ShellNotFoundPage } from "@/features/overview/ShellNotFoundPage";
import { PaymentNotFoundPage } from "@/features/payments/PaymentNotFoundPage";
import { usePaymentDetailQuery } from "@/features/payments/use-payment-portfolio-queries";
import { useAuthorization } from "@/hooks/use-authorization";
import { usePreferences } from "@/hooks/use-preferences";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";
import type { MessageKey } from "@/lib/i18n/messages";

const STATUS_LABELS: Record<string, MessageKey> = {
  PendingConfirmation: "organization.billing.status.PendingConfirmation",
  Confirmed: "organization.billing.status.Confirmed",
  Rejected: "organization.billing.status.Rejected",
  Voided: "organization.billing.status.Voided",
};

type ConfirmKind = "confirm" | "reject" | "void";

function isForbidden(error: unknown): boolean {
  return error instanceof PlatformApiError && (error.status === 401 || error.status === 403);
}

function isNotFound(error: unknown): boolean {
  return error instanceof PlatformApiError && error.status === 404;
}

function statusTone(status: string): "success" | "warning" | "danger" | "neutral" {
  if (status === "Confirmed") return "success";
  if (status === "PendingConfirmation") return "warning";
  if (status === "Rejected" || status === "Voided") return "danger";
  return "neutral";
}

function formatInstant(value: string | undefined, language: string): string {
  if (!value) return "—";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return value;
  return new Intl.DateTimeFormat(language === "fil-PH" ? "fil-PH" : "en-GB", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(date);
}

function actorReference(actorIdentifier: string | null, fallback: string): string {
  return actorIdentifier?.trim() || fallback;
}

const confirmCopy: Record<
  ConfirmKind,
  { title: MessageKey; description: MessageKey; confirm: MessageKey; destructive?: boolean }
> = {
  confirm: {
    title: "organization.billing.confirm.title",
    description: "organization.billing.confirm.description",
    confirm: "organization.billing.confirm.action",
  },
  reject: {
    title: "organization.billing.reject.title",
    description: "organization.billing.reject.description",
    confirm: "organization.billing.reject.action",
    destructive: true,
  },
  void: {
    title: "organization.billing.void.title",
    description: "organization.billing.void.description",
    confirm: "organization.billing.void.action",
    destructive: true,
  },
};

export function PaymentDetailPage() {
  const { t, language } = usePreferences();
  const authorization = useAuthorization();
  const params = useParams();
  const paymentId = parsePaymentId(params.paymentId);
  const canView =
    authorization.status === "loaded" &&
    authorization.hasPermission(PLATFORM_PERMISSIONS.manageManualPayments);
  const canManagePayments = authorization.hasPermission(PLATFORM_PERMISSIONS.manageManualPayments);
  const canManageSubscriptions = authorization.hasPermission(
    PLATFORM_PERMISSIONS.manageSubscriptions,
  );

  const query = usePaymentDetailQuery(canView ? paymentId : null, canView);
  const payment = query.data;
  const subscriptionsQuery = useOrganizationSubscriptionsQuery(payment?.organizationId ?? null, {
    page: 1,
    search: "",
    status: "",
    isTrial: "",
    productCode: "",
    sortBy: "UpdatedAtUtc",
    sortDesc: true,
  });

  const confirmPayment = useConfirmManualPaymentMutation();
  const rejectPayment = useRejectManualPaymentMutation();
  const voidPayment = useVoidManualPaymentMutation();
  const pending = confirmPayment.isPending || rejectPayment.isPending || voidPayment.isPending;

  const [feedback, setFeedback] = useState<{
    tone: "info" | "danger";
    title: string;
    detail: string;
  } | null>(null);
  const [confirm, setConfirm] = useState<ConfirmKind | null>(null);
  const [rejectReason, setRejectReason] = useState("");
  const [voidReason, setVoidReason] = useState("");

  if (authorization.status === "loading") {
    return (
      <section aria-busy="true">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="mt-3 h-16 w-full max-w-xl" />
      </section>
    );
  }

  if (!canView) return <ShellNotFoundPage />;
  if (paymentId == null) return <PaymentNotFoundPage />;

  if (query.isPending) {
    return (
      <section
        className="grid max-w-3xl gap-3"
        role="status"
        aria-busy="true"
        aria-label={t("payments.detail.loading")}
      >
        <DashboardWidgetSkeleton rows={8} />
      </section>
    );
  }

  if (query.isError && isForbidden(query.error)) return <ShellNotFoundPage />;
  if (query.isError && isNotFound(query.error)) return <PaymentNotFoundPage />;

  if (query.isError) {
    return (
      <ErrorState
        diagnostic={normalizeDiagnosticError({
          error: query.error,
          operation: "Load payment",
        })}
        title={t("payments.detail.error")}
        headingLevel="h1"
        onRetry={() => void query.refetch()}
      />
    );
  }

  if (!payment) return <PaymentNotFoundPage />;

  const currentPayment = payment;
  const subscriptions = subscriptionsQuery.data?.items ?? [];
  const caps = paymentActionCapabilities(currentPayment, {
    canManagePayments,
    canManageSubscriptions,
    subscriptions,
  });

  function showError(error: unknown) {
    const copy = commercialMutationFailureCopy(error, t);
    setFeedback({ tone: "danger", title: copy.title, detail: copy.detail });
  }

  async function runConfirmAction() {
    if (!confirm || pending) return;
    const actor = actorReference(authorization.actorIdentifier, "platform-admin");
    try {
      if (confirm === "confirm") {
        await confirmPayment.mutateAsync({
          paymentId: currentPayment.id,
          body: { confirmedBy: actor },
        });
        setFeedback({
          tone: "info",
          title: t("organization.billing.confirm.success"),
          detail: "",
        });
      } else if (confirm === "reject") {
        await rejectPayment.mutateAsync({
          paymentId: currentPayment.id,
          body: { rejectedBy: actor, reason: rejectReason.trim() },
        });
        setFeedback({
          tone: "info",
          title: t("organization.billing.reject.success"),
          detail: "",
        });
      } else {
        await voidPayment.mutateAsync({
          paymentId: currentPayment.id,
          body: { voidedBy: actor, reason: voidReason.trim() },
        });
        setFeedback({
          tone: "info",
          title: t("organization.billing.void.success"),
          detail: "",
        });
      }
      setConfirm(null);
      setRejectReason("");
      setVoidReason("");
    } catch (error) {
      showError(error);
    }
  }

  return (
    <section className="grid max-w-3xl gap-4">
      <PageHeader
        title={t("payments.detail.title")}
        description={t("payments.detail.description")}
        actions={
          <Button type="button" variant="outline" size="sm" asChild>
            <Link to={paymentsListHref()}>{t("payments.detail.back")}</Link>
          </Button>
        }
      />

      {feedback ? (
        <Alert title={feedback.title} tone={feedback.tone === "danger" ? "danger" : "info"}>
          {feedback.detail}
        </Alert>
      ) : null}

      <div className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3">
        <div className="flex flex-wrap items-start justify-between gap-2">
          <p className="text-lg font-semibold tabular-nums">
            {payment.amount} {payment.currencyCode}
          </p>
          <StatusIndicator
            tone={statusTone(payment.status)}
            label={
              STATUS_LABELS[payment.status] ? t(STATUS_LABELS[payment.status]!) : payment.status
            }
          />
        </div>
        <dl className="mt-3 grid gap-2 text-[length:var(--exits-text-sm)] sm:grid-cols-2">
          <div>
            <dt className="text-muted">{t("payments.detail.field.organization")}</dt>
            <dd>
              <Link
                className="font-medium text-primary hover:underline"
                to={`/admin/organizations/${payment.organizationId}`}
              >
                {payment.organizationId}
              </Link>
            </dd>
          </div>
          <div>
            <dt className="text-muted">{t("payments.detail.field.product")}</dt>
            <dd>{payment.productCode}</dd>
          </div>
          <div>
            <dt className="text-muted">{t("payments.detail.field.method")}</dt>
            <dd>{payment.method}</dd>
          </div>
          <div>
            <dt className="text-muted">{t("payments.detail.field.reference")}</dt>
            <dd>{payment.externalReference || "—"}</dd>
          </div>
          <div>
            <dt className="text-muted">{t("payments.detail.field.paidAt")}</dt>
            <dd>{formatInstant(payment.paidAtUtc, language)}</dd>
          </div>
          <div>
            <dt className="text-muted">{t("payments.detail.field.subscription")}</dt>
            <dd>
              {payment.subscriptionId ? (
                <Link
                  className="font-medium text-primary hover:underline"
                  to={subscriptionDetailHref(payment.subscriptionId)}
                >
                  {payment.subscriptionId}
                </Link>
              ) : (
                "—"
              )}
            </dd>
          </div>
        </dl>
        <div className="mt-3 flex flex-wrap gap-2">
          <Button type="button" size="sm" variant="outline" asChild>
            <Link to={`/admin/organizations/${payment.organizationId}/billing`}>
              {t("payments.detail.link.billing")}
            </Link>
          </Button>
          {payment.subscriptionId ? (
            <Button type="button" size="sm" variant="outline" asChild>
              <Link to={subscriptionDetailHref(payment.subscriptionId)}>
                {t("payments.detail.link.subscription")}
              </Link>
            </Button>
          ) : null}
        </div>
        {canManagePayments ? (
          <div className="mt-4 flex flex-wrap gap-2 border-t border-border pt-3">
            {caps.confirm ? (
              <Button type="button" size="sm" disabled={pending} onClick={() => setConfirm("confirm")}>
                {t("organization.billing.confirm.action")}
              </Button>
            ) : null}
            {caps.reject ? (
              <Button
                type="button"
                size="sm"
                variant="outline"
                disabled={pending}
                onClick={() => setConfirm("reject")}
              >
                {t("organization.billing.reject.action")}
              </Button>
            ) : null}
            {caps.void ? (
              <Button
                type="button"
                size="sm"
                variant="destructive"
                disabled={pending}
                onClick={() => setConfirm("void")}
              >
                {t("organization.billing.void.action")}
              </Button>
            ) : null}
          </div>
        ) : null}
        <dl className="mt-4 grid gap-1 border-t border-border pt-3 text-[length:var(--exits-text-xs)]">
          <div>
            <dt className="text-muted">{t("payments.detail.field.paymentId")}</dt>
            <dd className="break-all font-mono">{payment.id}</dd>
          </div>
        </dl>
      </div>

      {confirm ? (
        <ConfirmActionDialog
          open
          title={t(confirmCopy[confirm].title)}
          description={t(confirmCopy[confirm].description)}
          confirmLabel={t(confirmCopy[confirm].confirm)}
          cancelLabel={t("organization.subscriptions.dialog.dismiss")}
          pendingLabel={t("organization.subscriptions.submitting")}
          destructive={confirmCopy[confirm].destructive}
          pending={pending}
          onCancel={() => setConfirm(null)}
          onConfirm={() => void runConfirmAction()}
        >
          {confirm === "reject" ? (
            <label className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium" htmlFor="payment-reject-reason">
              {t("organization.billing.reject.reason")}
              <Input
                id="payment-reject-reason"
                value={rejectReason}
                onChange={(event) => setRejectReason(event.target.value)}
              />
            </label>
          ) : null}
          {confirm === "void" ? (
            <label className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium" htmlFor="payment-void-reason">
              {t("organization.billing.void.reason")}
              <Input
                id="payment-void-reason"
                value={voidReason}
                onChange={(event) => setVoidReason(event.target.value)}
              />
            </label>
          ) : null}
        </ConfirmActionDialog>
      ) : null}
    </section>
  );
}

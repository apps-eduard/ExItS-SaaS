import { useMemo, useState } from "react";
import { CreditCard, Plus } from "lucide-react";
import { useSearchParams } from "react-router-dom";
import { PLATFORM_PERMISSIONS } from "@/api/authorization/authorization-types";
import { createCorrelationId } from "@/api/platform-http";
import type { CatalogPlan } from "@/api/catalog/plan-catalog-types";
import type { OrganizationPayment } from "@/api/organizations/billing-list-query";
import {
  ORGANIZATION_BILLING_PAGE_SIZE,
  ORGANIZATION_PAYMENT_STATUSES,
  organizationBillingSearchParams,
  parseOrganizationBillingSearchParams,
  type OrganizationBillingUrlState,
  type OrganizationPaymentStatus,
} from "@/api/organizations/billing-list-query";
import type { OrganizationSubscription } from "@/api/organizations/subscription-list-query";
import { Alert } from "@/components/ui/alert";
import { AdminTable } from "@/components/exits/AdminTable";
import { ConfirmActionDialog } from "@/components/exits/ConfirmActionDialog";
import { ErrorState } from "@/components/exits/ErrorState";
import { StatusIndicator } from "@/components/exits/StatusIndicator";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import {
  useCatalogPlanVersionsQuery,
  useCatalogProductPlansQuery,
} from "@/features/catalog/use-catalog-detail-queries";
import {
  useActivateSubscriptionFromPaymentMutation,
  useConfirmManualPaymentMutation,
  useCreateManualPaymentMutation,
  useCreatePaidSubscriptionMutation,
  useRejectManualPaymentMutation,
  useSimulateLocalValidationPaymentMutation,
  useUpgradeSubscriptionFromPaymentMutation,
  useVoidManualPaymentMutation,
} from "@/features/commercial/use-commercial-mutations";
import {
  computePaidPeriod,
  defaultBillingCycle,
  defaultPaymentAmountForPlan,
  findSubscriptionForPayment,
  MANUAL_PAYMENT_METHODS,
  parseBillingUpgradeContext,
  paymentActionCapabilities,
  planPriceLabel,
  primaryBillingProductCode,
  supportedBillingCycles,
  type BillingCycleChoice,
  type BillingUpgradeContext,
  type ManualPaymentMethod,
} from "@/features/organizations/billing-lifecycle";
import { commercialMutationFailureCopy } from "@/features/organizations/commercial-mutation-feedback";
import {
  organizationHasPinoyBusinessPosSubscription,
  PINOY_BUSINESS_POS_PRODUCT_CODE,
  publishedPlanVersionId,
} from "@/features/organizations/subscription-lifecycle";
import {
  organizationSubscriptionStatusLabel,
  organizationSubscriptionStatusTone,
} from "@/features/organizations/organization-subscription-status";
import {
  useOrganizationPaymentsQuery,
  useOrganizationSubscriptionsQuery,
} from "@/features/organizations/use-organization-workspace-queries";
import { useAuthorization } from "@/hooks/use-authorization";
import { useMediaQuery } from "@/hooks/use-media-query";
import { usePreferences } from "@/hooks/use-preferences";
import { env } from "@/lib/env";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";
import type { MessageKey } from "@/lib/i18n/messages";

const STATUS_LABELS: Record<string, MessageKey> = {
  PendingConfirmation: "organization.billing.status.PendingConfirmation",
  Confirmed: "organization.billing.status.Confirmed",
  Rejected: "organization.billing.status.Rejected",
  Voided: "organization.billing.status.Voided",
};

const controlClass =
  "h-[var(--exits-control-height)] min-h-[var(--exits-touch-target-min)] rounded-[var(--exits-density-radius)] border border-input bg-surface px-3 text-[length:var(--exits-text-sm)] text-foreground";

type ConfirmKind = "confirm" | "reject" | "void" | "activate" | "upgrade";

function statusTone(status: string): "success" | "warning" | "danger" | "neutral" {
  if (status === "Confirmed") {
    return "success";
  }
  if (status === "PendingConfirmation") {
    return "warning";
  }
  if (status === "Rejected" || status === "Voided") {
    return "danger";
  }
  return "neutral";
}

function formatInstant(value: string | undefined, language: string): string | null {
  if (!value) {
    return null;
  }
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return value;
  }
  return new Intl.DateTimeFormat(language === "fil-PH" ? "fil-PH" : "en-GB", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(date);
}

function formatAmount(item: OrganizationPayment): string {
  return `${item.amount} ${item.currencyCode}`;
}

function actorReference(actorIdentifier: string | null, fallback: string): string {
  return actorIdentifier?.trim() || fallback;
}

function defaultPaidAtLocal(): string {
  const now = new Date();
  const pad = (value: number) => String(value).padStart(2, "0");
  return `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}T${pad(now.getHours())}:${pad(now.getMinutes())}`;
}

function localDateTimeToIso(value: string): string {
  const parsed = new Date(value);
  return Number.isNaN(parsed.getTime()) ? new Date().toISOString() : parsed.toISOString();
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
  activate: {
    title: "organization.billing.activate.title",
    description: "organization.billing.activate.description",
    confirm: "organization.billing.activate.action",
  },
  upgrade: {
    title: "organization.billing.upgrade.complete.title",
    description: "organization.billing.upgrade.complete.description",
    confirm: "organization.billing.upgrade.complete.action",
  },
};

export function OrganizationBillingLifecycle({ organizationId }: { organizationId: string }) {
  const { t, language } = usePreferences();
  const authorization = useAuthorization();
  const canManagePayments = authorization.hasPermission(PLATFORM_PERMISSIONS.manageManualPayments);
  const canManageSubscriptions = authorization.hasPermission(PLATFORM_PERMISSIONS.manageSubscriptions);
  const showTable = useMediaQuery("(min-width: 768px)");
  const [searchParams, setSearchParams] = useSearchParams();
  const state = useMemo(() => parseOrganizationBillingSearchParams(searchParams), [searchParams]);
  const upgradeContext = useMemo(() => parseBillingUpgradeContext(searchParams), [searchParams]);
  const paymentsQuery = useOrganizationPaymentsQuery(organizationId, state);
  const subscriptionsQuery = useOrganizationSubscriptionsQuery(organizationId, {
    page: 1,
    search: "",
    status: "",
    isTrial: "",
    productCode: "",
    sortBy: "UpdatedAtUtc",
    sortDesc: true,
  });
  const posPlansQuery = useCatalogProductPlansQuery(PINOY_BUSINESS_POS_PRODUCT_CODE);

  const [feedback, setFeedback] = useState<{
    tone: "info" | "danger";
    title: string;
    detail: string;
  } | null>(null);
  const [recordOpen, setRecordOpen] = useState(false);
  const [subscribeOpen, setSubscribeOpen] = useState(false);
  const [detailPayment, setDetailPayment] = useState<OrganizationPayment | null>(null);
  const [confirm, setConfirm] = useState<{ kind: ConfirmKind; payment: OrganizationPayment } | null>(
    null,
  );
  const [rejectReason, setRejectReason] = useState("");
  const [voidReason, setVoidReason] = useState("");
  const [upgradeBillingCycle, setUpgradeBillingCycle] = useState<BillingCycleChoice>(
    upgradeContext?.billingCycle ?? "Monthly",
  );

  const createPayment = useCreateManualPaymentMutation();
  const confirmPayment = useConfirmManualPaymentMutation();
  const rejectPayment = useRejectManualPaymentMutation();
  const voidPayment = useVoidManualPaymentMutation();
  const activateFromPayment = useActivateSubscriptionFromPaymentMutation();
  const upgradeFromPayment = useUpgradeSubscriptionFromPaymentMutation();
  const simulatePayment = useSimulateLocalValidationPaymentMutation();

  const subscriptions = subscriptionsQuery.isSuccess ? (subscriptionsQuery.data?.items ?? []) : [];
  const payments = paymentsQuery.data?.items ?? [];
  const posPlans = posPlansQuery.isSuccess ? (posPlansQuery.data ?? []) : [];
  const posPlansDiagnostic = posPlansQuery.error
    ? normalizeDiagnosticError({
        error: posPlansQuery.error,
        operation: "Load plan catalog",
      })
    : null;
  const subscriptionsDiagnostic = subscriptionsQuery.error
    ? normalizeDiagnosticError({
        error: subscriptionsQuery.error,
        operation: "Load organization subscriptions",
      })
    : null;
  const upgradeTargetPlan = upgradeContext
    ? posPlans.find((plan) => plan.id === upgradeContext.targetPlanId)
    : undefined;
  const upgradeCurrentSubscription = upgradeContext
    ? subscriptions.find((item) => item.id === upgradeContext.upgradeSubscriptionId)
    : undefined;
  const upgradeCurrentPlan = upgradeCurrentSubscription
    ? posPlans.find((plan) => plan.id === upgradeCurrentSubscription.planId)
    : undefined;
  const posSubscription = subscriptions.find(
    (item) => item.productCode === PINOY_BUSINESS_POS_PRODUCT_CODE,
  );
  const hasPosSubscription = organizationHasPinoyBusinessPosSubscription(subscriptions);
  const pendingPayments = payments.filter((item) => item.status === "PendingConfirmation");
  const latestPayment = payments[0];

  const pending =
    createPayment.isPending ||
    confirmPayment.isPending ||
    rejectPayment.isPending ||
    voidPayment.isPending ||
    activateFromPayment.isPending ||
    upgradeFromPayment.isPending ||
    simulatePayment.isPending;

  function replaceState(patch: Partial<OrganizationBillingUrlState>) {
    const current = parseOrganizationBillingSearchParams(new URLSearchParams(window.location.search));
    setSearchParams(organizationBillingSearchParams({ ...current, ...patch }), { replace: true });
  }

  function showError(error: unknown) {
    const copy = commercialMutationFailureCopy(error, t);
    setFeedback({ tone: "danger", title: copy.title, detail: copy.detail });
  }

  function showSuccess(titleKey: MessageKey, detailKey?: MessageKey) {
    setFeedback({
      tone: "info",
      title: t(titleKey),
      detail: detailKey ? t(detailKey) : "",
    });
  }

  async function runConfirmAction() {
    if (!confirm || pending) {
      return;
    }
    const actor = actorReference(authorization.actorIdentifier, "platform-admin");
    const { kind, payment } = confirm;
    try {
      if (kind === "confirm") {
        await confirmPayment.mutateAsync({
          paymentId: payment.id,
          body: { confirmedBy: actor },
        });
        showSuccess("organization.billing.confirm.success");
      } else if (kind === "reject") {
        await rejectPayment.mutateAsync({
          paymentId: payment.id,
          body: { rejectedBy: actor, reason: rejectReason.trim() },
        });
        showSuccess("organization.billing.reject.success");
      } else if (kind === "void") {
        await voidPayment.mutateAsync({
          paymentId: payment.id,
          body: { voidedBy: actor, reason: voidReason.trim() },
        });
        showSuccess("organization.billing.void.success");
      } else if (kind === "activate") {
        const subscription = findSubscriptionForPayment(payment, subscriptions);
        if (!subscription) {
          setFeedback({
            tone: "danger",
            title: t("organization.billing.activate.noEligibleSubscription"),
            detail: "",
          });
          return;
        }
        const subscriptionPlan = posPlans.find((plan) => plan.id === subscription.planId);
        const billingCycle: BillingCycleChoice = subscriptionPlan
          ? defaultBillingCycle(subscriptionPlan)
          : "Monthly";
        const period = computePaidPeriod(billingCycle);
        await activateFromPayment.mutateAsync({
          paymentId: payment.id,
          body: {
            confirmedBy: actor,
            subscriptionId: subscription.id,
            periodStartUtc: period.periodStartUtc,
            periodEndUtc: period.periodEndUtc,
            billingCycle,
          },
        });
        showSuccess("organization.billing.activate.success");
      } else if (kind === "upgrade") {
        if (!upgradeContext || !upgradeTargetPlan) {
          setFeedback({
            tone: "danger",
            title: t("organization.billing.upgrade.contextMissing"),
            detail: "",
          });
          return;
        }
        await upgradeFromPayment.mutateAsync({
          paymentId: payment.id,
          body: {
            subscriptionId: upgradeContext.upgradeSubscriptionId,
            targetPlanId: upgradeContext.targetPlanId,
            billingCycle: upgradeBillingCycle,
          },
        });
        showSuccess("organization.billing.upgrade.complete.success");
        setSearchParams(organizationBillingSearchParams(state), { replace: true });
      }
      setConfirm(null);
      setRejectReason("");
      setVoidReason("");
      setDetailPayment(null);
    } catch (error) {
      showError(error);
    }
  }

  const totalPages = paymentsQuery.data
    ? Math.max(1, Math.ceil(paymentsQuery.data.totalCount / ORGANIZATION_BILLING_PAGE_SIZE))
    : 1;
  const diagnostic = paymentsQuery.error
    ? normalizeDiagnosticError({
        error: paymentsQuery.error,
        operation: "Load organization billing",
      })
    : null;

  return (
    <div className="grid gap-4">
      {feedback ? (
        <Alert tone={feedback.tone} title={feedback.title}>
          {feedback.detail}
        </Alert>
      ) : null}

      {posPlansQuery.isError && !paymentsQuery.isError && posPlansDiagnostic ? (
        <ErrorState
          diagnostic={posPlansDiagnostic}
          title={t("organization.billing.plansCatalog.error")}
          headingLevel="h2"
          onRetry={() => void posPlansQuery.refetch()}
        />
      ) : null}

      {subscriptionsQuery.isError && !paymentsQuery.isError && subscriptionsDiagnostic ? (
        <Alert title={t("organization.billing.subscriptions.error")} tone="danger">
          <div className="mt-2">
            <Button type="button" size="sm" variant="outline" onClick={() => void subscriptionsQuery.refetch()}>
              {t("diagnostics.retry")}
            </Button>
          </div>
        </Alert>
      ) : null}

      <BillingSummary
        subscription={posSubscription}
        latestPayment={latestPayment}
        pendingCount={pendingPayments.length}
        plans={posPlans}
        language={language}
        t={t}
      />

      {upgradeContext && upgradeTargetPlan && upgradeCurrentPlan ? (
        <Card className="grid gap-2 px-3 py-3">
          <p className="text-[length:var(--exits-text-sm)] font-medium">
            {t("organization.billing.upgrade.panelTitle")}
          </p>
          <p className="text-[length:var(--exits-text-xs)] text-muted">
            {t("organization.billing.upgrade.currentPlan")}: {upgradeCurrentPlan.displayName}
          </p>
          <p className="text-[length:var(--exits-text-xs)] text-muted">
            {t("organization.billing.upgrade.targetPlan")}: {upgradeTargetPlan.displayName}
          </p>
          {supportedBillingCycles(upgradeTargetPlan).length > 1 ? (
            <label className="grid max-w-xs gap-1 text-[length:var(--exits-text-xs)] font-medium">
              {t("organization.billing.upgrade.billingCycle")}
              <select
                className={controlClass}
                value={upgradeBillingCycle}
                onChange={(event) =>
                  setUpgradeBillingCycle(event.target.value as BillingCycleChoice)
                }
              >
                {supportedBillingCycles(upgradeTargetPlan).map((cycle) => (
                  <option key={cycle} value={cycle}>
                    {cycle}
                  </option>
                ))}
              </select>
            </label>
          ) : (
            <p className="text-[length:var(--exits-text-xs)] text-muted">
              {t("organization.billing.upgrade.billingCycle")}: {upgradeBillingCycle}
            </p>
          )}
          <p className="text-[length:var(--exits-text-xs)] text-muted">
            {t("organization.billing.upgrade.requiredPayment")}:{" "}
            {planPriceLabel(upgradeTargetPlan, upgradeBillingCycle)}
          </p>
          <p className="text-[length:var(--exits-text-xs)] text-muted">
            {t("organization.billing.upgrade.instructions")}
          </p>
        </Card>
      ) : null}

      {canManagePayments ? (
        <div className="flex flex-wrap gap-2">
          <Button type="button" size="sm" disabled={pending} onClick={() => setRecordOpen(true)}>
            <Plus aria-hidden className="mr-2 size-4" />
            {t("organization.billing.record")}
          </Button>
          {canManageSubscriptions && !hasPosSubscription ? (
            <Button
              type="button"
              size="sm"
              variant="outline"
              disabled={pending || posPlansQuery.isError || posPlans.length === 0}
              onClick={() => setSubscribeOpen(true)}
            >
              <CreditCard aria-hidden className="mr-2 size-4" />
              {t("organization.billing.subscribeWithPayment")}
            </Button>
          ) : null}
          {env.localValidationToolsEnabled && canManageSubscriptions ? (
            <LocalValidationSimulateButton
              organizationId={organizationId}
              subscription={posSubscription}
              plans={posPlansQuery.isSuccess ? posPlans : []}
              pending={pending}
              onError={showError}
              onSuccess={() => showSuccess("organization.billing.simulate.success")}
            />
          ) : null}
        </div>
      ) : null}

      <p className="text-[length:var(--exits-text-xs)] text-muted">
        {t("organization.billing.saasNotice")}
      </p>

      <label
        className="grid max-w-sm gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted"
        htmlFor="org-billing-status"
      >
        {t("organization.billing.status")}
        <select
          id="org-billing-status"
          className={controlClass}
          value={state.status}
          onChange={(event) =>
            replaceState({
              status: event.target.value as OrganizationPaymentStatus | "",
              page: 1,
            })
          }
        >
          <option value="">{t("organization.billing.status.all")}</option>
          {ORGANIZATION_PAYMENT_STATUSES.map((status) => (
            <option key={status} value={status}>
              {t(STATUS_LABELS[status]!)}
            </option>
          ))}
        </select>
      </label>

      {paymentsQuery.isPending ? (
        <div role="status" aria-busy="true" aria-label={t("organization.billing.loading")}>
          <DashboardWidgetSkeleton rows={5} />
        </div>
      ) : null}

      {paymentsQuery.isError && diagnostic ? (
        <ErrorState
          diagnostic={diagnostic}
          title={t("organization.billing.error")}
          headingLevel="h2"
          onRetry={() => void paymentsQuery.refetch()}
        />
      ) : null}

      {paymentsQuery.data ? (
        <>
          {showTable ? (
            <div className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3">
              <AdminTable
                caption={t("organization.billing.caption")}
                empty={
                  state.status
                    ? t("organization.billing.zeroResult")
                    : t("organization.billing.empty")
                }
                columns={[
                  {
                    id: "product",
                    header: t("organization.billing.column.product"),
                    cell: (item) => <span className="font-medium">{item.productCode}</span>,
                  },
                  {
                    id: "amount",
                    header: t("organization.billing.column.amount"),
                    cell: (item) => formatAmount(item),
                  },
                  {
                    id: "reference",
                    header: t("organization.billing.column.reference"),
                    cell: (item) => item.externalReference || "—",
                  },
                  {
                    id: "status",
                    header: t("organization.billing.column.status"),
                    cell: (item) => (
                      <StatusIndicator
                        tone={statusTone(item.status)}
                        label={
                          STATUS_LABELS[item.status] ? t(STATUS_LABELS[item.status]!) : item.status
                        }
                      />
                    ),
                  },
                  {
                    id: "paid",
                    header: t("organization.billing.column.paid"),
                    cell: (item) => formatInstant(item.paidAtUtc, language) || "—",
                  },
                  {
                    id: "actions",
                    header: t("organization.billing.column.actions"),
                    cell: (item) => (
                      <PaymentActions
                        payment={item}
                        subscriptions={subscriptions}
                        canManagePayments={canManagePayments}
                        canManageSubscriptions={canManageSubscriptions}
                        pending={pending}
                        upgradeContext={upgradeContext}
                        targetPlan={upgradeTargetPlan}
                        billingCycle={upgradeBillingCycle}
                        t={t}
                        onDetail={() => setDetailPayment(item)}
                        onConfirm={(kind) => setConfirm({ kind, payment: item })}
                      />
                    ),
                  },
                ]}
                rows={payments}
              />
            </div>
          ) : (
            <ul className="grid gap-2">
              {payments.length === 0 ? (
                <li className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-3 py-3 text-[length:var(--exits-text-sm)] text-muted">
                  {state.status
                    ? t("organization.billing.zeroResult")
                    : t("organization.billing.empty")}
                </li>
              ) : (
                payments.map((item) => (
                  <li
                    key={item.id}
                    className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-3 py-2.5"
                  >
                    <p className="font-medium">{item.productCode}</p>
                    <p className="mt-0.5 text-[length:var(--exits-text-xs)] text-muted">
                      {formatAmount(item)}
                    </p>
                    {item.externalReference ? (
                      <p className="mt-0.5 text-[length:var(--exits-text-xs)] text-muted">
                        {item.externalReference}
                      </p>
                    ) : null}
                    <div className="mt-1.5">
                      <StatusIndicator
                        tone={statusTone(item.status)}
                        label={
                          STATUS_LABELS[item.status] ? t(STATUS_LABELS[item.status]!) : item.status
                        }
                      />
                    </div>
                    <div className="mt-2">
                      <PaymentActions
                        payment={item}
                        subscriptions={subscriptions}
                        canManagePayments={canManagePayments}
                        canManageSubscriptions={canManageSubscriptions}
                        pending={pending}
                        upgradeContext={upgradeContext}
                        targetPlan={upgradeTargetPlan}
                        billingCycle={upgradeBillingCycle}
                        t={t}
                        onDetail={() => setDetailPayment(item)}
                        onConfirm={(kind) => setConfirm({ kind, payment: item })}
                      />
                    </div>
                  </li>
                ))
              )}
            </ul>
          )}
          {totalPages > 1 ? (
            <div className="flex flex-wrap items-center gap-2">
              <Button
                type="button"
                size="sm"
                variant="outline"
                disabled={state.page <= 1}
                onClick={() => replaceState({ page: state.page - 1 })}
              >
                {t("organizations.previous")}
              </Button>
              <p className="text-[length:var(--exits-text-xs)] text-muted">
                {t("organizations.page")} {state.page} / {totalPages}
              </p>
              <Button
                type="button"
                size="sm"
                variant="outline"
                disabled={state.page >= totalPages}
                onClick={() => replaceState({ page: state.page + 1 })}
              >
                {t("organizations.next")}
              </Button>
            </div>
          ) : null}
        </>
      ) : null}

      {recordOpen ? (
        <RecordPaymentDialog
          organizationId={organizationId}
          productCode={primaryBillingProductCode(subscriptions)}
          plans={posPlans}
          targetPlan={upgradeTargetPlan}
          billingCycle={upgradeBillingCycle}
          pending={createPayment.isPending}
          onCancel={() => {
            createPayment.reset();
            setRecordOpen(false);
          }}
          onSubmit={async (body) => {
            try {
              await createPayment.mutateAsync(body);
              showSuccess("organization.billing.record.success");
              setRecordOpen(false);
            } catch (error) {
              showError(error);
            }
          }}
        />
      ) : null}

      {subscribeOpen ? (
        <SubscribeWithPaymentDialog
          organizationId={organizationId}
          plans={posPlans}
          actorIdentifier={authorization.actorIdentifier}
          onCancel={() => setSubscribeOpen(false)}
          onComplete={() => {
            showSuccess("organization.billing.subscribe.success");
            setSubscribeOpen(false);
          }}
          onError={showError}
        />
      ) : null}

      {detailPayment ? (
        <PaymentDetailDialog
          payment={detailPayment}
          subscriptions={subscriptions}
          language={language}
          canManagePayments={canManagePayments}
          canManageSubscriptions={canManageSubscriptions}
          pending={pending}
          upgradeContext={upgradeContext}
          targetPlan={upgradeTargetPlan}
          billingCycle={upgradeBillingCycle}
          onClose={() => setDetailPayment(null)}
          onAction={(kind) => {
            setDetailPayment(null);
            setConfirm({ kind, payment: detailPayment });
          }}
        />
      ) : null}

      {confirm ? (
        <ConfirmActionDialog
          open
          title={t(confirmCopy[confirm.kind].title)}
          description={t(confirmCopy[confirm.kind].description)}
          confirmLabel={t(confirmCopy[confirm.kind].confirm)}
          cancelLabel={t("organization.billing.dialog.dismiss")}
          pendingLabel={t("organization.billing.submitting")}
          destructive={confirmCopy[confirm.kind].destructive}
          pending={pending}
          confirmDisabled={
            (confirm.kind === "reject" && rejectReason.trim().length === 0) ||
            (confirm.kind === "void" && voidReason.trim().length === 0)
          }
          onCancel={() => {
            setConfirm(null);
            setRejectReason("");
            setVoidReason("");
          }}
          onConfirm={() => void runConfirmAction()}
        >
          {confirm.kind === "reject" ? (
            <label className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium" htmlFor="reject-reason">
              {t("organization.billing.reject.reason")}
              <Input
                id="reject-reason"
                value={rejectReason}
                onChange={(event) => setRejectReason(event.target.value)}
              />
            </label>
          ) : null}
          {confirm.kind === "void" ? (
            <label className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium" htmlFor="void-reason">
              {t("organization.billing.void.reason")}
              <Input
                id="void-reason"
                value={voidReason}
                onChange={(event) => setVoidReason(event.target.value)}
              />
            </label>
          ) : null}
        </ConfirmActionDialog>
      ) : null}
    </div>
  );
}

function BillingSummary({
  subscription,
  latestPayment,
  pendingCount,
  plans,
  language,
  t,
}: {
  subscription?: OrganizationSubscription;
  latestPayment?: OrganizationPayment;
  pendingCount: number;
  plans: CatalogPlan[];
  language: string;
  t: (key: MessageKey) => string;
}) {
  const plan = subscription ? plans.find((item) => item.id === subscription.planId) : undefined;
  return (
    <div className="grid gap-2 sm:grid-cols-2">
      <Card className="px-3 py-2.5">
        <p className="text-[length:var(--exits-text-xs)] font-medium text-muted">
          {t("organization.billing.summary.subscription")}
        </p>
        {subscription ? (
          <>
            <p className="mt-1 font-medium">
              {subscription.productDisplayName || subscription.productCode}
            </p>
            <p className="text-[length:var(--exits-text-sm)]">
              {plan?.displayName || subscription.planDisplayName || subscription.planKey || "—"}
            </p>
            <div className="mt-1">
              <StatusIndicator
                tone={organizationSubscriptionStatusTone(subscription.status)}
                label={organizationSubscriptionStatusLabel(subscription.status, t)}
              />
            </div>
            {plan ? (
              <p className="mt-1 text-[length:var(--exits-text-xs)] text-muted">
                {t("organization.billing.summary.catalogPrice")}: {planPriceLabel(plan)}
              </p>
            ) : null}
          </>
        ) : (
          <p className="mt-1 text-[length:var(--exits-text-sm)] text-muted">
            {t("organization.billing.summary.noSubscription")}
          </p>
        )}
      </Card>
      <Card className="px-3 py-2.5">
        <p className="text-[length:var(--exits-text-xs)] font-medium text-muted">
          {t("organization.billing.summary.payments")}
        </p>
        {latestPayment ? (
          <>
            <p className="mt-1 font-medium">
              {latestPayment.amount} {latestPayment.currencyCode}
            </p>
            <div className="mt-1">
              <StatusIndicator
                tone={statusTone(latestPayment.status)}
                label={
                  STATUS_LABELS[latestPayment.status]
                    ? t(STATUS_LABELS[latestPayment.status]!)
                    : latestPayment.status
                }
              />
            </div>
            <p className="mt-1 text-[length:var(--exits-text-xs)] text-muted">
              {formatInstant(latestPayment.paidAtUtc, language) || "—"}
            </p>
          </>
        ) : (
          <p className="mt-1 text-[length:var(--exits-text-sm)] text-muted">
            {t("organization.billing.empty")}
          </p>
        )}
        {pendingCount > 0 ? (
          <p className="mt-2 text-[length:var(--exits-text-xs)] text-warning">
            {t("organization.billing.summary.pendingCount").replace("{count}", String(pendingCount))}
          </p>
        ) : null}
      </Card>
    </div>
  );
}

function PaymentActions({
  payment,
  subscriptions,
  canManagePayments,
  canManageSubscriptions,
  pending,
  upgradeContext,
  targetPlan,
  billingCycle,
  t,
  onDetail,
  onConfirm,
}: {
  payment: OrganizationPayment;
  subscriptions: OrganizationSubscription[];
  canManagePayments: boolean;
  canManageSubscriptions: boolean;
  pending: boolean;
  upgradeContext?: BillingUpgradeContext | null;
  targetPlan?: CatalogPlan | null;
  billingCycle?: BillingCycleChoice;
  t: (key: MessageKey) => string;
  onDetail: () => void;
  onConfirm: (kind: ConfirmKind) => void;
}) {
  const caps = paymentActionCapabilities(payment, {
    canManagePayments,
    canManageSubscriptions,
    subscriptions,
    upgradeContext,
    targetPlan,
    billingCycle,
  });
  return (
    <div className="flex flex-wrap gap-1">
      <Button type="button" size="sm" variant="ghost" onClick={onDetail}>
        {t("organization.billing.detail")}
      </Button>
      {caps.confirm ? (
        <Button type="button" size="sm" disabled={pending} onClick={() => onConfirm("confirm")}>
          {t("organization.billing.confirm.action")}
        </Button>
      ) : null}
      {caps.reject ? (
        <Button
          type="button"
          size="sm"
          variant="outline"
          disabled={pending}
          onClick={() => onConfirm("reject")}
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
          onClick={() => onConfirm("void")}
        >
          {t("organization.billing.void.action")}
        </Button>
      ) : null}
      {caps.activateFromPayment ? (
        <Button type="button" size="sm" disabled={pending} onClick={() => onConfirm("activate")}>
          {t("organization.billing.activate.action")}
        </Button>
      ) : null}
      {caps.completeUpgradeFromPayment ? (
        <Button type="button" size="sm" disabled={pending} onClick={() => onConfirm("upgrade")}>
          {t("organization.billing.upgrade.complete.action")}
        </Button>
      ) : null}
    </div>
  );
}

function PaymentDetailDialog({
  payment,
  subscriptions,
  language,
  canManagePayments,
  canManageSubscriptions,
  pending,
  upgradeContext,
  targetPlan,
  billingCycle,
  onClose,
  onAction,
}: {
  payment: OrganizationPayment;
  subscriptions: OrganizationSubscription[];
  language: string;
  canManagePayments: boolean;
  canManageSubscriptions: boolean;
  pending: boolean;
  upgradeContext?: BillingUpgradeContext | null;
  targetPlan?: CatalogPlan | null;
  billingCycle?: BillingCycleChoice;
  onClose: () => void;
  onAction: (kind: ConfirmKind) => void;
}) {
  const { t } = usePreferences();
  const subscription = subscriptions.find((item) => item.id === payment.subscriptionId);
  const caps = paymentActionCapabilities(payment, {
    canManagePayments,
    canManageSubscriptions,
    subscriptions,
    upgradeContext,
    targetPlan,
    billingCycle,
  });
  return (
    <ConfirmActionDialog
      open
      title={t("organization.billing.detailTitle")}
      description={t("organization.billing.detailDescription")}
      confirmLabel={t("organization.billing.dialog.dismiss")}
      cancelLabel={t("organization.billing.dialog.dismiss")}
      pendingLabel={t("organization.billing.submitting")}
      pending={false}
      onCancel={onClose}
      onConfirm={onClose}
    >
      <dl className="grid gap-2 text-[length:var(--exits-text-sm)]">
        <div>
          <dt className="text-muted">{t("organization.billing.column.amount")}</dt>
          <dd>{formatAmount(payment)}</dd>
        </div>
        <div>
          <dt className="text-muted">{t("organization.billing.column.reference")}</dt>
          <dd>{payment.externalReference || "—"}</dd>
        </div>
        <div>
          <dt className="text-muted">{t("organization.billing.column.status")}</dt>
          <dd>
            <StatusIndicator
              tone={statusTone(payment.status)}
              label={
                STATUS_LABELS[payment.status] ? t(STATUS_LABELS[payment.status]!) : payment.status
              }
            />
          </dd>
        </div>
        <div>
          <dt className="text-muted">{t("organization.billing.column.paid")}</dt>
          <dd>{formatInstant(payment.paidAtUtc, language) || "—"}</dd>
        </div>
        {subscription ? (
          <div>
            <dt className="text-muted">{t("organization.billing.linkedSubscription")}</dt>
            <dd>{subscription.planDisplayName || subscription.planKey || subscription.planId}</dd>
          </div>
        ) : null}
        {payment.confirmedAtUtc ? (
          <div>
            <dt className="text-muted">{t("organization.billing.confirmedAt")}</dt>
            <dd>{formatInstant(payment.confirmedAtUtc, language)}</dd>
          </div>
        ) : null}
        {payment.rejectionReason ? (
          <div>
            <dt className="text-muted">{t("organization.billing.reject.reason")}</dt>
            <dd>{payment.rejectionReason}</dd>
          </div>
        ) : null}
        {payment.voidReason ? (
          <div>
            <dt className="text-muted">{t("organization.billing.void.reason")}</dt>
            <dd>{payment.voidReason}</dd>
          </div>
        ) : null}
      </dl>
      <div className="flex flex-wrap gap-1">
        {caps.confirm ? (
          <Button type="button" size="sm" disabled={pending} onClick={() => onAction("confirm")}>
            {t("organization.billing.confirm.action")}
          </Button>
        ) : null}
        {caps.reject ? (
          <Button type="button" size="sm" variant="outline" disabled={pending} onClick={() => onAction("reject")}>
            {t("organization.billing.reject.action")}
          </Button>
        ) : null}
        {caps.void ? (
          <Button type="button" size="sm" variant="destructive" disabled={pending} onClick={() => onAction("void")}>
            {t("organization.billing.void.action")}
          </Button>
        ) : null}
        {caps.activateFromPayment ? (
          <Button type="button" size="sm" disabled={pending} onClick={() => onAction("activate")}>
            {t("organization.billing.activate.action")}
          </Button>
        ) : null}
        {caps.completeUpgradeFromPayment ? (
          <Button type="button" size="sm" disabled={pending} onClick={() => onAction("upgrade")}>
            {t("organization.billing.upgrade.complete.action")}
          </Button>
        ) : null}
      </div>
    </ConfirmActionDialog>
  );
}

function RecordPaymentDialog({
  organizationId,
  productCode,
  plans,
  targetPlan,
  billingCycle = "Monthly",
  pending,
  onCancel,
  onSubmit,
}: {
  organizationId: string;
  productCode: string;
  plans: CatalogPlan[];
  targetPlan?: CatalogPlan | null;
  billingCycle?: BillingCycleChoice;
  pending: boolean;
  onCancel: () => void;
  onSubmit: (body: {
    organizationId: string;
    productCode: string;
    amount: number;
    currencyCode: string;
    method: string;
    externalReference: string;
    paidAtUtc: string;
  }) => Promise<void>;
}) {
  const { t } = usePreferences();
  const activePlans = plans.filter((plan) => plan.status === "Active");
  const initialPlan = targetPlan ?? activePlans[0];
  const [planId, setPlanId] = useState(initialPlan?.id ?? "");
  const selectedPlan = activePlans.find((plan) => plan.id === planId);
  const [cycle, setCycle] = useState<BillingCycleChoice>(
    billingCycle ?? (selectedPlan ? defaultBillingCycle(selectedPlan) : "Monthly"),
  );
  const [method, setMethod] = useState<ManualPaymentMethod>("GCash");
  const [amount, setAmount] = useState(
    selectedPlan ? String(defaultPaymentAmountForPlan(selectedPlan, cycle) ?? "") : "",
  );
  const [reference, setReference] = useState("");
  const [paidAtLocal, setPaidAtLocal] = useState(defaultPaidAtLocal());
  const [submitting, setSubmitting] = useState(false);
  const amountValue = Number(amount);
  const validAmount = Number.isFinite(amountValue) && amountValue > 0;
  const validReference = reference.trim().length > 0;
  const currencyCode = selectedPlan?.currencyCode ?? "PHP";

  return (
    <ConfirmActionDialog
      open
      title={t("organization.billing.record.title")}
      description={t("organization.billing.record.description")}
      confirmLabel={t("organization.billing.record.action")}
      cancelLabel={t("organization.billing.dialog.dismiss")}
      pendingLabel={t("organization.billing.submitting")}
      pending={pending || submitting}
      confirmDisabled={!validAmount || !validReference || !selectedPlan}
      onCancel={onCancel}
      onConfirm={() => {
        if (submitting || pending || !validAmount || !validReference || !selectedPlan) {
          return;
        }
        setSubmitting(true);
        void onSubmit({
          organizationId,
          productCode,
          amount: amountValue,
          currencyCode,
          method,
          externalReference: reference.trim(),
          paidAtUtc: localDateTimeToIso(paidAtLocal),
        }).finally(() => setSubmitting(false));
      }}
    >
      {activePlans.length > 0 ? (
        <label className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium" htmlFor="record-plan">
          {t("organization.billing.record.plan")}
          <select
            id="record-plan"
            className={controlClass}
            value={planId}
            onChange={(event) => {
              const nextPlan = activePlans.find((plan) => plan.id === event.target.value);
              setPlanId(event.target.value);
              if (nextPlan) {
                const nextCycle = defaultBillingCycle(nextPlan);
                setCycle(nextCycle);
                setAmount(String(defaultPaymentAmountForPlan(nextPlan, nextCycle) ?? ""));
              }
            }}
          >
            {activePlans.map((plan) => (
              <option key={plan.id} value={plan.id}>
                {plan.displayName} ({planPriceLabel(plan, cycle)})
              </option>
            ))}
          </select>
        </label>
      ) : null}
      {selectedPlan && supportedBillingCycles(selectedPlan).length > 1 ? (
        <label className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium" htmlFor="record-cycle">
          {t("organization.billing.upgrade.billingCycle")}
          <select
            id="record-cycle"
            className={controlClass}
            value={cycle}
            onChange={(event) => {
              const nextCycle = event.target.value as BillingCycleChoice;
              setCycle(nextCycle);
              if (selectedPlan) {
                setAmount(String(defaultPaymentAmountForPlan(selectedPlan, nextCycle) ?? ""));
              }
            }}
          >
            {supportedBillingCycles(selectedPlan).map((item) => (
              <option key={item} value={item}>
                {item}
              </option>
            ))}
          </select>
        </label>
      ) : null}
      <label className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium" htmlFor="record-method">
        {t("organization.billing.column.method")}
        <select
          id="record-method"
          className={controlClass}
          value={method}
          onChange={(event) => setMethod(event.target.value as ManualPaymentMethod)}
        >
          {MANUAL_PAYMENT_METHODS.map((item) => (
            <option key={item} value={item}>
              {item}
            </option>
          ))}
        </select>
      </label>
      <label className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium" htmlFor="record-amount">
        {t("organization.billing.column.amount")}
        <Input
          id="record-amount"
          inputMode="decimal"
          value={amount}
          onChange={(event) => setAmount(event.target.value)}
          aria-invalid={!validAmount && amount.length > 0}
        />
      </label>
      <label className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium" htmlFor="record-reference">
        {t("organization.billing.column.reference")}
        <Input id="record-reference" value={reference} onChange={(event) => setReference(event.target.value)} />
      </label>
      <label className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium" htmlFor="record-paid-at">
        {t("organization.billing.column.paid")}
        <Input
          id="record-paid-at"
          type="datetime-local"
          value={paidAtLocal}
          onChange={(event) => setPaidAtLocal(event.target.value)}
        />
      </label>
    </ConfirmActionDialog>
  );
}

function SubscribeWithPaymentDialog({
  organizationId,
  plans,
  actorIdentifier,
  onCancel,
  onComplete,
  onError,
}: {
  organizationId: string;
  plans: CatalogPlan[];
  actorIdentifier: string | null;
  onCancel: () => void;
  onComplete: () => void;
  onError: (error: unknown) => void;
}) {
  const { t } = usePreferences();
  const activePlans = plans.filter((plan) => plan.status === "Active");
  const [planId, setPlanId] = useState(activePlans[0]?.id ?? "");
  const selectedPlan = activePlans.find((plan) => plan.id === planId);
  const [billingCycle, setBillingCycle] = useState<BillingCycleChoice>(
    selectedPlan ? defaultBillingCycle(selectedPlan) : "Monthly",
  );
  const versionsQuery = useCatalogPlanVersionsQuery(selectedPlan?.productCode ?? null, planId || null);
  const versionId = publishedPlanVersionId(versionsQuery.data ?? []);
  const createPayment = useCreateManualPaymentMutation();
  const confirmPayment = useConfirmManualPaymentMutation();
  const createPaidSubscription = useCreatePaidSubscriptionMutation();
  const [reference, setReference] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const actor = actorReference(actorIdentifier, "platform-admin");
  const pending =
    createPayment.isPending || confirmPayment.isPending || createPaidSubscription.isPending || submitting;

  return (
    <ConfirmActionDialog
      open
      title={t("organization.billing.subscribe.title")}
      description={t("organization.billing.subscribe.description")}
      confirmLabel={t("organization.billing.subscribe.action")}
      cancelLabel={t("organization.billing.dialog.dismiss")}
      pendingLabel={t("organization.billing.submitting")}
      pending={pending}
      confirmDisabled={
        !selectedPlan ||
        !versionId ||
        reference.trim().length === 0 ||
        defaultPaymentAmountForPlan(selectedPlan, billingCycle) == null
      }
      onCancel={onCancel}
      onConfirm={() => {
        if (!selectedPlan || !versionId || submitting) {
          return;
        }
        const amount = defaultPaymentAmountForPlan(selectedPlan, billingCycle);
        if (amount == null) {
          return;
        }
        setSubmitting(true);
        void (async () => {
          try {
            const period = computePaidPeriod(billingCycle);
            const payment = await createPayment.mutateAsync({
              organizationId,
              productCode: selectedPlan.productCode,
              amount,
              currencyCode: selectedPlan.currencyCode ?? "PHP",
              method: "GCash",
              externalReference: reference.trim(),
              paidAtUtc: new Date().toISOString(),
            });
            const confirmed = await confirmPayment.mutateAsync({
              paymentId: payment.id,
              body: { confirmedBy: actor },
            });
            await createPaidSubscription.mutateAsync({
              organizationId,
              body: {
                planId: selectedPlan.id,
                planVersionId: versionId,
                periodStartUtc: period.periodStartUtc,
                periodEndUtc: period.periodEndUtc,
                paymentId: confirmed.id,
                billingCycle,
              },
            });
            onComplete();
          } catch (error) {
            onError(error);
          } finally {
            setSubmitting(false);
          }
        })();
      }}
    >
      <label className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium" htmlFor="subscribe-plan">
        {t("organization.billing.record.plan")}
        <select
          id="subscribe-plan"
          className={controlClass}
          value={planId}
          onChange={(event) => {
            const nextPlan = activePlans.find((plan) => plan.id === event.target.value);
            setPlanId(event.target.value);
            if (nextPlan) {
              setBillingCycle(defaultBillingCycle(nextPlan));
            }
          }}
        >
          {activePlans.map((plan) => (
            <option key={plan.id} value={plan.id}>
              {plan.displayName} ({planPriceLabel(plan, billingCycle)})
            </option>
          ))}
        </select>
      </label>
      {selectedPlan && supportedBillingCycles(selectedPlan).length > 1 ? (
        <label className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium" htmlFor="subscribe-cycle">
          {t("organization.billing.upgrade.billingCycle")}
          <select
            id="subscribe-cycle"
            className={controlClass}
            value={billingCycle}
            onChange={(event) => setBillingCycle(event.target.value as BillingCycleChoice)}
          >
            {supportedBillingCycles(selectedPlan).map((item) => (
              <option key={item} value={item}>
                {item}
              </option>
            ))}
          </select>
        </label>
      ) : null}
      <label className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium" htmlFor="subscribe-reference">
        {t("organization.billing.column.reference")}
        <Input id="subscribe-reference" value={reference} onChange={(event) => setReference(event.target.value)} />
      </label>
    </ConfirmActionDialog>
  );
}

function LocalValidationSimulateButton({
  organizationId,
  subscription,
  plans,
  pending,
  onError,
  onSuccess,
}: {
  organizationId: string;
  subscription?: OrganizationSubscription;
  plans: CatalogPlan[];
  pending: boolean;
  onError: (error: unknown) => void;
  onSuccess: () => void;
}) {
  const { t } = usePreferences();
  const simulate = useSimulateLocalValidationPaymentMutation();
  const plan = plans.find((item) => item.id === subscription?.planId) ?? plans[0];
  const amount = plan ? defaultPaymentAmountForPlan(plan) : undefined;

  if (!subscription || amount == null) {
    return null;
  }

  return (
    <Button
      type="button"
      size="sm"
      variant="outline"
      disabled={pending || simulate.isPending}
      onClick={() => {
        void simulate
          .mutateAsync({
            body: {
              simulation: "succeed",
              organizationId,
              subscriptionId: subscription.id,
              amount,
              currencyCode: plan?.currencyCode ?? "PHP",
              idempotencyKey: createCorrelationId(),
              purpose: subscription.status === "Trialing" ? "convert-trial" : "initial",
              billingCycle: "Monthly",
            },
            localValidationToolsEnabled: env.localValidationToolsEnabled,
          })
          .then(onSuccess)
          .catch(onError);
      }}
    >
      {t("organization.billing.simulate.action")}
    </Button>
  );
}

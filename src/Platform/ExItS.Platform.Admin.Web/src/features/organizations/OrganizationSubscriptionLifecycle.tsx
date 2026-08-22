import { useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { PLATFORM_PERMISSIONS } from "@/api/authorization/authorization-types";
import type { CatalogPlan } from "@/api/catalog/plan-catalog-types";
import { classifyCommercialMutationFailure } from "@/api/commercial/commercial-errors";
import type { OrganizationSubscription } from "@/api/organizations/subscription-list-query";
import { getSubscriptionPlanChangePreview } from "@/api/subscriptions/subscription-mutations-client";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { ConfirmActionDialog } from "@/components/exits/ConfirmActionDialog";
import { StatusIndicator } from "@/components/exits/StatusIndicator";
import {
  DropdownMenu,
  DropdownMenuItem,
} from "@/components/ui/dropdown-menu";
import { Input } from "@/components/ui/input";
import {
  useCatalogPlanVersionsQuery,
  useCatalogProductPlansQuery,
  useCatalogTrialsQuery,
} from "@/features/catalog/use-catalog-detail-queries";
import { commercialMutationFailureCopy } from "@/features/organizations/commercial-mutation-feedback";
import {
  buildBillingUpgradeSearchParams,
  defaultBillingCycle,
} from "@/features/organizations/billing-lifecycle";
import {
  organizationSubscriptionStatusLabel,
  organizationSubscriptionStatusTone,
} from "@/features/organizations/organization-subscription-status";
import {
  findCatalogPlan,
  isPinoyBusinessPosSubscription,
  organizationHasPinoyBusinessPosSubscription,
  PINOY_BUSINESS_POS_PRODUCT_CODE,
  planChangeDirection,
  publishedPlanVersionId,
  subscriptionLifecycleCapabilities,
  subscriptionPeriodEnd,
  trialEligiblePlans,
} from "@/features/organizations/subscription-lifecycle";
import {
  useApplyPendingPlanMutation,
  useCancelSubscriptionMutation,
  useDowngradeSubscriptionMutation,
  useEnterGracePeriodMutation,
  useExpireSubscriptionMutation,
  useMarkPastDueMutation,
  useReactivateSubscriptionMutation,
  useStartTrialMutation,
  useSuspendSubscriptionMutation,
  useUpgradeSubscriptionMutation,
} from "@/features/commercial/use-commercial-mutations";
import { selectActiveTrialDefinition } from "@/api/catalog/trial-catalog-client";
import { useAuthorization } from "@/hooks/use-authorization";
import { usePreferences } from "@/hooks/use-preferences";
import { env } from "@/lib/env";
import type { MessageKey } from "@/lib/i18n/messages";

type ConfirmKind =
  | "suspend"
  | "reactivate"
  | "cancel"
  | "applyPending"
  | "pastDue"
  | "expire"
  | "gracePeriod";

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
  }).format(date);
}

function productLabel(item: OrganizationSubscription): string {
  return item.productDisplayName || item.productCode;
}

function planLabel(item: OrganizationSubscription, plan?: CatalogPlan): string {
  return plan?.displayName || item.planDisplayName || item.planKey || item.planId;
}

function defaultGraceEndLocal(): string {
  const date = new Date();
  date.setDate(date.getDate() + 7);
  const pad = (value: number) => String(value).padStart(2, "0");
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
}

function localDateTimeToIso(value: string): string {
  const parsed = new Date(value);
  return Number.isNaN(parsed.getTime()) ? new Date().toISOString() : parsed.toISOString();
}

export function OrganizationSubscriptionLifecycle({
  organizationId,
  subscriptions,
}: {
  organizationId: string;
  subscriptions: OrganizationSubscription[];
}) {
  const { t, language } = usePreferences();
  const navigate = useNavigate();
  const authorization = useAuthorization();
  const canManage = authorization.hasPermission(PLATFORM_PERMISSIONS.manageSubscriptions);
  const posPlansQuery = useCatalogProductPlansQuery(PINOY_BUSINESS_POS_PRODUCT_CODE);
  const extraProductCodes = useMemo(() => {
    const codes = new Set(
      subscriptions
        .map((item) => item.productCode)
        .filter((code) => code && code !== PINOY_BUSINESS_POS_PRODUCT_CODE),
    );
    return [...codes];
  }, [subscriptions]);
  const extraPlansQuery = useCatalogProductPlansQuery(extraProductCodes[0] ?? null);

  const plansByProduct = useMemo(() => {
    const map = new Map<string, CatalogPlan[]>();
    map.set(PINOY_BUSINESS_POS_PRODUCT_CODE, posPlansQuery.data ?? []);
    if (extraProductCodes[0] && extraPlansQuery.data) {
      map.set(extraProductCodes[0], extraPlansQuery.data);
    }
    return map;
  }, [posPlansQuery.data, extraPlansQuery.data, extraProductCodes]);

  const [feedback, setFeedback] = useState<{ tone: "info" | "danger"; title: string; detail: string } | null>(
    null,
  );
  const [startTrialOpen, setStartTrialOpen] = useState(false);
  const [changePlanItem, setChangePlanItem] = useState<OrganizationSubscription | null>(null);
  const [confirm, setConfirm] = useState<{ kind: ConfirmKind; item: OrganizationSubscription } | null>(
    null,
  );
  const [graceEndLocal, setGraceEndLocal] = useState(defaultGraceEndLocal);

  const startTrial = useStartTrialMutation();
  const upgrade = useUpgradeSubscriptionMutation();
  const downgrade = useDowngradeSubscriptionMutation();
  const applyPending = useApplyPendingPlanMutation();
  const suspend = useSuspendSubscriptionMutation();
  const reactivate = useReactivateSubscriptionMutation();
  const cancel = useCancelSubscriptionMutation();
  const grace = useEnterGracePeriodMutation();
  const pastDue = useMarkPastDueMutation();
  const expire = useExpireSubscriptionMutation();

  const pending =
    startTrial.isPending ||
    upgrade.isPending ||
    downgrade.isPending ||
    applyPending.isPending ||
    suspend.isPending ||
    reactivate.isPending ||
    cancel.isPending ||
    grace.isPending ||
    pastDue.isPending ||
    expire.isPending;

  const hasPosSubscription = organizationHasPinoyBusinessPosSubscription(subscriptions);
  const eligibleTrialPlans = trialEligiblePlans(posPlansQuery.data ?? []);

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

  async function runConfirm() {
    if (!confirm || pending) {
      return;
    }
    const { kind, item } = confirm;
    const versionBody = item.version != null ? { expectedVersion: item.version } : undefined;
    try {
      if (kind === "suspend") {
        await suspend.mutateAsync({ subscriptionId: item.id, body: versionBody });
        showSuccess("organization.subscriptions.suspend.success");
      } else if (kind === "reactivate") {
        await reactivate.mutateAsync({ subscriptionId: item.id, body: versionBody });
        showSuccess("organization.subscriptions.reactivate.success");
      } else if (kind === "cancel") {
        await cancel.mutateAsync({ subscriptionId: item.id, body: versionBody });
        showSuccess("organization.subscriptions.cancel.success");
      } else if (kind === "applyPending") {
        await applyPending.mutateAsync({
          organizationId,
          subscriptionId: item.id,
        });
        showSuccess("organization.subscriptions.applyPending.success");
      } else if (kind === "pastDue") {
        await pastDue.mutateAsync({ subscriptionId: item.id, body: versionBody });
        showSuccess("organization.subscriptions.pastDue.success");
      } else if (kind === "expire") {
        await expire.mutateAsync({ subscriptionId: item.id, body: versionBody });
        showSuccess("organization.subscriptions.expire.success");
      } else {
        await grace.mutateAsync({
          subscriptionId: item.id,
          body: {
            gracePeriodEndUtc: localDateTimeToIso(graceEndLocal),
            expectedVersion: item.version,
          },
        });
        showSuccess("organization.subscriptions.grace.success");
      }
      setConfirm(null);
    } catch (error) {
      showError(error);
    }
  }

  const confirmCopy: Record<
    ConfirmKind,
    { title: MessageKey; description: MessageKey; confirm: MessageKey; destructive: boolean }
  > = {
    suspend: {
      title: "organization.subscriptions.suspend.title",
      description: "organization.subscriptions.suspend.description",
      confirm: "organization.subscriptions.suspend.confirm",
      destructive: true,
    },
    reactivate: {
      title: "organization.subscriptions.reactivate.title",
      description: "organization.subscriptions.reactivate.description",
      confirm: "organization.subscriptions.reactivate.confirm",
      destructive: false,
    },
    cancel: {
      title: "organization.subscriptions.cancel.title",
      description: "organization.subscriptions.cancel.description",
      confirm: "organization.subscriptions.cancel.confirm",
      destructive: true,
    },
    applyPending: {
      title: "organization.subscriptions.applyPending.title",
      description: "organization.subscriptions.applyPending.description",
      confirm: "organization.subscriptions.applyPending.confirm",
      destructive: false,
    },
    pastDue: {
      title: "organization.subscriptions.pastDue.title",
      description: "organization.subscriptions.pastDue.description",
      confirm: "organization.subscriptions.pastDue.confirm",
      destructive: true,
    },
    expire: {
      title: "organization.subscriptions.expire.title",
      description: "organization.subscriptions.expire.description",
      confirm: "organization.subscriptions.expire.confirm",
      destructive: true,
    },
    gracePeriod: {
      title: "organization.subscriptions.grace.title",
      description: "organization.subscriptions.grace.description",
      confirm: "organization.subscriptions.grace.confirm",
      destructive: false,
    },
  };

  return (
    <div className="grid gap-3">
      {feedback ? (
        <Alert title={feedback.title} tone={feedback.tone} aria-live={feedback.tone === "danger" ? "assertive" : "polite"}>
          {feedback.detail}
        </Alert>
      ) : null}

      {subscriptions.length > 0 ? (
        <ul className="grid gap-2">
          {subscriptions.map((item) => {
            const plans = plansByProduct.get(item.productCode) ?? posPlansQuery.data ?? [];
            const plan = findCatalogPlan(plans, item.planId);
            const pendingPlan = findCatalogPlan(plans, item.pendingPlanId);
            const capabilities = subscriptionLifecycleCapabilities(item.status, item.pendingPlanId);
            const pos = isPinoyBusinessPosSubscription(item);
            return (
              <li key={item.id}>
                <Card className="grid gap-3">
                  <div className="flex flex-wrap items-start justify-between gap-2">
                    <div>
                      <p className="font-semibold">
                        {productLabel(item)}
                        {pos ? (
                          <span className="ml-2 rounded-full border border-border px-2 py-0.5 text-[length:var(--exits-text-xs)] font-medium text-muted">
                            {t("organization.subscriptions.posBadge")}
                          </span>
                        ) : null}
                      </p>
                      <p className="mt-0.5 text-[length:var(--exits-text-sm)] text-muted">
                        {planLabel(item, plan)}
                      </p>
                    </div>
                    <StatusIndicator
                      tone={organizationSubscriptionStatusTone(item.status)}
                      label={organizationSubscriptionStatusLabel(item.status, t)}
                    />
                  </div>
                  <dl className="grid gap-1 text-[length:var(--exits-text-sm)] sm:grid-cols-2">
                    <div>
                      <dt className="text-muted">{t("organization.subscriptions.summary.trialEnd")}</dt>
                      <dd>{formatInstant(item.trialEndUtc, language) || "—"}</dd>
                    </div>
                    <div>
                      <dt className="text-muted">{t("organization.subscriptions.summary.periodEnd")}</dt>
                      <dd>{formatInstant(subscriptionPeriodEnd(item), language) || "—"}</dd>
                    </div>
                    <div>
                      <dt className="text-muted">{t("organization.subscriptions.summary.devices")}</dt>
                      <dd>
                        {plan?.maxActivePosDevices != null
                          ? t("organization.subscriptions.summary.deviceCount").replace(
                              "{count}",
                              String(plan.maxActivePosDevices),
                            )
                          : "—"}
                      </dd>
                    </div>
                    <div>
                      <dt className="text-muted">{t("organization.subscriptions.summary.pending")}</dt>
                      <dd>
                        {item.pendingPlanId
                          ? `${pendingPlan?.displayName || item.pendingPlanId}${
                              item.pendingPlanEffectiveAtUtc
                                ? ` · ${formatInstant(item.pendingPlanEffectiveAtUtc, language)}`
                                : ""
                            }`
                          : t("organization.subscriptions.summary.pendingNone")}
                      </dd>
                    </div>
                  </dl>
                  {canManage ? (
                    <div className="flex flex-wrap gap-2">
                      {capabilities.changePlan ? (
                        <Button
                          type="button"
                          size="sm"
                          onClick={() => setChangePlanItem(item)}
                        >
                          {t("organization.subscriptions.changePlan")}
                        </Button>
                      ) : null}
                      {capabilities.applyPending ? (
                        <Button
                          type="button"
                          size="sm"
                          variant="secondary"
                          disabled={pending}
                          onClick={() => setConfirm({ kind: "applyPending", item })}
                        >
                          {t("organization.subscriptions.applyPending")}
                        </Button>
                      ) : null}
                      {capabilities.suspend ? (
                        <Button
                          type="button"
                          size="sm"
                          variant="outline"
                          disabled={pending}
                          onClick={() => setConfirm({ kind: "suspend", item })}
                        >
                          {t("organization.subscriptions.suspend")}
                        </Button>
                      ) : null}
                      {capabilities.reactivate ? (
                        <Button
                          type="button"
                          size="sm"
                          disabled={pending}
                          onClick={() => setConfirm({ kind: "reactivate", item })}
                        >
                          {t("organization.subscriptions.reactivate")}
                        </Button>
                      ) : null}
                      {capabilities.cancel ? (
                        <Button
                          type="button"
                          size="sm"
                          variant="destructive"
                          disabled={pending}
                          onClick={() => setConfirm({ kind: "cancel", item })}
                        >
                          {t("organization.subscriptions.cancel")}
                        </Button>
                      ) : null}
                      {capabilities.supportActions.length > 0 ? (
                        <DropdownMenu
                          label={t("organization.subscriptions.support")}
                          trigger={
                            <Button type="button" size="sm" variant="ghost">
                              {t("organization.subscriptions.support")}
                            </Button>
                          }
                        >
                          {capabilities.supportActions.includes("gracePeriod") ? (
                            <DropdownMenuItem
                              onSelect={() => {
                                setGraceEndLocal(defaultGraceEndLocal());
                                setConfirm({ kind: "gracePeriod", item });
                              }}
                            >
                              {t("organization.subscriptions.grace")}
                            </DropdownMenuItem>
                          ) : null}
                          {capabilities.supportActions.includes("pastDue") ? (
                            <DropdownMenuItem
                              onSelect={() => setConfirm({ kind: "pastDue", item })}
                            >
                              {t("organization.subscriptions.pastDue")}
                            </DropdownMenuItem>
                          ) : null}
                          {capabilities.supportActions.includes("expire") ? (
                            <DropdownMenuItem
                              onSelect={() => setConfirm({ kind: "expire", item })}
                            >
                              {t("organization.subscriptions.expire")}
                            </DropdownMenuItem>
                          ) : null}
                        </DropdownMenu>
                      ) : null}
                    </div>
                  ) : null}
                </Card>
              </li>
            );
          })}
        </ul>
      ) : null}

      {!hasPosSubscription ? (
        <Card className="grid gap-2">
          <p className="font-semibold">{t("organization.subscriptions.emptyPos.title")}</p>
          <p className="text-[length:var(--exits-text-sm)] text-muted">
            {t("organization.subscriptions.emptyPos.body")}
          </p>
          {canManage && eligibleTrialPlans.length > 0 ? (
            <div>
              <Button type="button" onClick={() => setStartTrialOpen(true)}>
                {t("organization.subscriptions.startTrial")}
              </Button>
            </div>
          ) : null}
        </Card>
      ) : null}

      {startTrialOpen ? (
        <StartTrialDialog
          plans={eligibleTrialPlans}
          pending={startTrial.isPending}
          error={
            startTrial.error ? commercialMutationFailureCopy(startTrial.error, t) : null
          }
          onCancel={() => {
            startTrial.reset();
            setStartTrialOpen(false);
          }}
          onSubmit={async (body) => {
            try {
              await startTrial.mutateAsync({ organizationId, body });
              setStartTrialOpen(false);
              showSuccess("organization.subscriptions.startTrial.success");
            } catch {
              // Dialog already shows startTrial.error via commercialMutationFailureCopy.
            }
          }}
        />
      ) : null}

      {changePlanItem ? (
        <ChangePlanDialog
          organizationId={organizationId}
          item={changePlanItem}
          plans={(plansByProduct.get(changePlanItem.productCode) ?? posPlansQuery.data ?? []).filter(
            (plan) => plan.status === "Active" && plan.id !== changePlanItem.planId,
          )}
          currentPlan={findCatalogPlan(
            plansByProduct.get(changePlanItem.productCode) ?? posPlansQuery.data ?? [],
            changePlanItem.planId,
          )}
          pending={upgrade.isPending || downgrade.isPending}
          onCancel={() => {
            upgrade.reset();
            downgrade.reset();
            setChangePlanItem(null);
          }}
          onSubmit={async (target, direction) => {
            try {
              if (direction === "upgrade") {
                await upgrade.mutateAsync({
                  organizationId,
                  subscriptionId: changePlanItem.id,
                  body: {
                    planId: target.id,
                    idempotencyKey: crypto.randomUUID(),
                  },
                });
                showSuccess("organization.subscriptions.upgrade.success");
              } else {
                await downgrade.mutateAsync({
                  organizationId,
                  subscriptionId: changePlanItem.id,
                  body: { planId: target.id },
                });
                showSuccess("organization.subscriptions.downgrade.success");
              }
              setChangePlanItem(null);
            } catch (error) {
              const kind = classifyCommercialMutationFailure(error).kind;
              if (kind === "payment_required") {
                setChangePlanItem(null);
                navigate(
                  `/admin/organizations/${organizationId}/billing?${buildBillingUpgradeSearchParams({
                    upgradeSubscriptionId: changePlanItem.id,
                    targetPlanId: target.id,
                    billingCycle: defaultBillingCycle(target),
                  }).toString()}`,
                );
                return;
              }
              showError(error);
            }
          }}
        />
      ) : null}

      {confirm ? (
        <ConfirmActionDialog
          open
          title={t(confirmCopy[confirm.kind].title)}
          description={t(confirmCopy[confirm.kind].description)}
          confirmLabel={t(confirmCopy[confirm.kind].confirm)}
          cancelLabel={t("organization.subscriptions.dialog.dismiss")}
          pendingLabel={t("organization.subscriptions.submitting")}
          destructive={confirmCopy[confirm.kind].destructive}
          pending={pending}
          onCancel={() => setConfirm(null)}
          onConfirm={() => void runConfirm()}
        >
          {confirm.kind === "gracePeriod" ? (
            <label
              className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium"
              htmlFor="org-sub-grace-end"
            >
              {t("organization.subscriptions.grace.end")}
              <Input
                id="org-sub-grace-end"
                type="datetime-local"
                value={graceEndLocal}
                onChange={(event) => setGraceEndLocal(event.target.value)}
              />
            </label>
          ) : null}
        </ConfirmActionDialog>
      ) : null}
    </div>
  );
}

function StartTrialDialog({
  plans,
  pending,
  error,
  onCancel,
  onSubmit,
}: {
  plans: CatalogPlan[];
  pending: boolean;
  error: { title: string; detail: string } | null;
  onCancel: () => void;
  onSubmit: (body: { planId: string; planVersionId: string; trialDefinitionId: string }) => Promise<void>;
}) {
  const { t } = usePreferences();
  const [planId, setPlanId] = useState(plans[0]?.id ?? "");
  const selected = plans.find((plan) => plan.id === planId) ?? plans[0];
  const versionsQuery = useCatalogPlanVersionsQuery(
    selected?.productCode ?? PINOY_BUSINESS_POS_PRODUCT_CODE,
    selected?.id ?? null,
  );
  const trialsQuery = useCatalogTrialsQuery(PINOY_BUSINESS_POS_PRODUCT_CODE, true);
  const versionId = publishedPlanVersionId(versionsQuery.data ?? []);
  const trial = selectActiveTrialDefinition(trialsQuery.data ?? [], selected?.id ?? "");
  const ready = Boolean(selected && versionId && trial);

  return (
    <ConfirmActionDialog
      open
      title={t("organization.subscriptions.startTrial.title")}
      description={t("organization.subscriptions.startTrial.description")}
      confirmLabel={t("organization.subscriptions.startTrial.confirm")}
      cancelLabel={t("organization.subscriptions.dialog.dismiss")}
      pendingLabel={t("organization.subscriptions.submitting")}
      pending={pending}
      confirmDisabled={!ready}
      error={error ? <Alert title={error.title} tone="danger">{error.detail}</Alert> : null}
      onCancel={onCancel}
      onConfirm={() => {
        if (!ready || !selected || !versionId || !trial || pending) {
          return;
        }
        void onSubmit({
          planId: selected.id,
          planVersionId: versionId,
          trialDefinitionId: trial.id,
        });
      }}
    >
      <label
        className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium"
        htmlFor="org-sub-trial-plan"
      >
        {t("organization.subscriptions.startTrial.plan")}
        <select
          id="org-sub-trial-plan"
          className="h-[var(--exits-control-height)] rounded-[var(--exits-density-radius)] border border-input bg-surface px-3"
          value={selected?.id ?? ""}
          onChange={(event) => setPlanId(event.target.value)}
        >
          {plans.map((plan) => (
            <option key={plan.id} value={plan.id}>
              {plan.displayName}
              {plan.defaultTrialDays != null ? ` · ${plan.defaultTrialDays}d` : ""}
            </option>
          ))}
        </select>
      </label>
      {selected ? (
        <p className="text-[length:var(--exits-text-sm)]">
          {t("organization.subscriptions.summary.deviceCount").replace(
            "{count}",
            String(selected.maxActivePosDevices ?? "—"),
          )}
        </p>
      ) : null}
      {!ready && !versionsQuery.isPending && !trialsQuery.isPending ? (
        <p className="text-[length:var(--exits-text-sm)] text-muted">
          {versionId
            ? t("organization.subscriptions.startTrial.noMatchingTrial")
            : t("organization.subscriptions.startTrial.unavailable")}
        </p>
      ) : null}
    </ConfirmActionDialog>
  );
}

function ChangePlanDialog({
  organizationId,
  item,
  plans,
  currentPlan,
  pending,
  onCancel,
  onSubmit,
}: {
  organizationId: string;
  item: OrganizationSubscription;
  plans: CatalogPlan[];
  currentPlan?: CatalogPlan;
  pending: boolean;
  onCancel: () => void;
  onSubmit: (target: CatalogPlan, direction: "upgrade" | "downgrade") => Promise<void>;
}) {
  const { t } = usePreferences();
  const [planId, setPlanId] = useState(plans[0]?.id ?? "");
  const target = plans.find((plan) => plan.id === planId) ?? plans[0];
  const direction = planChangeDirection(currentPlan, target);
  const previewQuery = useQuery({
    queryKey: ["organizations", "plan-change-preview", organizationId, item.id, target?.id],
    enabled: Boolean(target?.id),
    queryFn: ({ signal }) =>
      getSubscriptionPlanChangePreview(
        env.platformApiBaseUrl,
        organizationId,
        item.id,
        { planId: target!.id },
        signal,
      ),
  });

  return (
    <ConfirmActionDialog
      open
      title={t("organization.subscriptions.changePlan.title")}
      description={t("organization.subscriptions.changePlan.description")}
      confirmLabel={
        direction === "downgrade"
          ? t("organization.subscriptions.downgrade.confirm")
          : t("organization.subscriptions.upgrade.confirm")
      }
      cancelLabel={t("organization.subscriptions.dialog.dismiss")}
      pendingLabel={t("organization.subscriptions.submitting")}
      pending={pending}
      confirmDisabled={!target || direction === "same" || Boolean(previewQuery.data?.hasBlockingUsageConflicts)}
      onCancel={onCancel}
      onConfirm={() => {
        if (!target || direction === "same" || pending || previewQuery.data?.hasBlockingUsageConflicts) {
          return;
        }
        void onSubmit(target, direction);
      }}
    >
      <p className="text-[length:var(--exits-text-sm)]">
        {t("organization.subscriptions.changePlan.current")}: {currentPlan?.displayName || item.planDisplayName} ·{" "}
        {t("organization.subscriptions.summary.deviceCount").replace(
          "{count}",
          String(currentPlan?.maxActivePosDevices ?? "—"),
        )}
      </p>
      <label className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium" htmlFor="org-sub-change-plan">
        {t("organization.subscriptions.changePlan.target")}
        <select
          id="org-sub-change-plan"
          className="h-[var(--exits-control-height)] rounded-[var(--exits-density-radius)] border border-input bg-surface px-3"
          value={target?.id ?? ""}
          onChange={(event) => setPlanId(event.target.value)}
        >
          {plans.map((plan) => (
            <option key={plan.id} value={plan.id}>
              {plan.displayName}
            </option>
          ))}
        </select>
      </label>
      {target ? (
        <ul className="grid gap-1 text-[length:var(--exits-text-sm)]">
          <li>
            {t("organization.subscriptions.diff.devices")}: {currentPlan?.maxActivePosDevices ?? "—"} →{" "}
            {target.maxActivePosDevices ?? "—"}
          </li>
          <li>
            {t("organization.subscriptions.diff.price")}: {currentPlan?.monthlyPrice ?? "—"} →{" "}
            {target.monthlyPrice ?? "—"} {target.currencyCode || currentPlan?.currencyCode || ""}
          </li>
          <li>
            {t("organization.subscriptions.diff.branches")}: {currentPlan?.maxBranches ?? "—"} →{" "}
            {target.maxBranches ?? "—"}
          </li>
          <li>
            {t("organization.subscriptions.diff.staff")}: {currentPlan?.maxActiveStaff ?? "—"} →{" "}
            {target.maxActiveStaff ?? "—"}
          </li>
        </ul>
      ) : null}
      {direction === "downgrade" ? (
        <p className="text-[length:var(--exits-text-sm)] text-muted">
          {t("organization.subscriptions.downgrade.scheduled")}
        </p>
      ) : null}
      {previewQuery.data?.hasBlockingUsageConflicts ? (
        <Alert title={t("organization.subscriptions.changePlan.conflicts")} tone="danger">
          {previewQuery.data.usageConflicts.map((conflict) => conflict.message).join(" ")}
        </Alert>
      ) : null}
      {previewQuery.data && previewQuery.data.lostFeatures.length > 0 ? (
        <p className="text-[length:var(--exits-text-sm)]">
          {t("organization.subscriptions.changePlan.lost")}: {previewQuery.data.lostFeatures.join(", ")}
        </p>
      ) : null}
    </ConfirmActionDialog>
  );
}

import { useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import type {
  OrganizationPlanDto,
  OrganizationSubscriptionDto,
} from "@/api/platform/organization-current-plan-client";
import {
  createPlanChangeIdempotencyKey,
  downgradeOrganizationSubscription,
  getOrganizationPlanChangePreview,
  upgradeOrganizationSubscription,
  type PlanChangePaymentSimulation,
} from "@/api/platform/organization-plan-change-client";
import { LoadingState } from "@/components/exits/LoadingState";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { Notice } from "@/components/exits/Notice";
import { StatusChip } from "@/components/exits/StatusChip";
import { Button } from "@/components/ui/button";
import { isSimulatedOrganizationBilling } from "@/features/organization/subscription/organization-billing-mode";
import {
  CAPACITY_LABEL_KEYS,
  PLAN_FEATURE_LABEL_KEYS,
  buildPlanFeatureRows,
  buildPlanLimitDiffs,
  comparePlanTier,
  formatSubscriptionDate,
  listAvailablePlansForChange,
  quotePlanPriceForCycle,
  resolveDowngradeEffectiveAtUtc,
  type PlanChangeDirection,
} from "@/features/organization/subscription/subscription-billing-view";
import { useI18n } from "@/i18n/I18nProvider";

type Step = "select" | "review" | "payment" | "done";

export function OrgPlanChangePanel({
  organizationId,
  subscription,
  currentPlan,
  availablePlans,
  onCompleted,
}: {
  organizationId: string;
  subscription: OrganizationSubscriptionDto;
  currentPlan: OrganizationPlanDto | null;
  availablePlans: ReadonlyArray<OrganizationPlanDto>;
  onCompleted: () => Promise<void> | void;
}) {
  const { t } = useI18n();
  const queryClient = useQueryClient();
  const [step, setStep] = useState<Step>("select");
  const [selectedPlanId, setSelectedPlanId] = useState<string | null>(null);
  const [feedback, setFeedback] = useState<"upgrade" | "downgrade" | null>(null);
  const [errorDetail, setErrorDetail] = useState<string | null>(null);

  const plans = useMemo(() => listAvailablePlansForChange(availablePlans), [availablePlans]);
  const selectedPlan = useMemo(
    () => plans.find((plan) => plan.id === selectedPlanId) ?? null,
    [plans, selectedPlanId],
  );
  const direction: PlanChangeDirection = comparePlanTier(currentPlan, selectedPlan);
  const billingCycle = subscription.billingCycle ?? "Monthly";
  const currentPrice = quotePlanPriceForCycle(currentPlan, billingCycle);
  const newPrice = quotePlanPriceForCycle(selectedPlan, billingCycle);
  const limitDiffs = buildPlanLimitDiffs(currentPlan, selectedPlan);
  const downgradeEffectiveAt = resolveDowngradeEffectiveAtUtc(subscription);
  const currentFeatures = buildPlanFeatureRows(currentPlan);
  const targetFeatures = buildPlanFeatureRows(selectedPlan);
  const gainedFeatures = targetFeatures.filter(
    (row) => row.included && !currentFeatures.find((c) => c.id === row.id)?.included,
  );
  const lostFromPlans = currentFeatures.filter(
    (row) => row.included && !targetFeatures.find((c) => c.id === row.id)?.included,
  );

  const previewQuery = useQuery({
    queryKey: [
      "org-subscription",
      "plan-change-preview",
      organizationId,
      subscription.id,
      selectedPlan?.id,
    ],
    enabled: Boolean(subscription.id && selectedPlan?.id && step !== "select" && step !== "done"),
    staleTime: 0,
    queryFn: async ({ signal }) => {
      const result = await getOrganizationPlanChangePreview(
        {
          organizationId,
          subscriptionId: subscription.id!,
          planId: selectedPlan!.id!,
        },
        signal,
      );
      if (!result.ok) {
        throw new Error(result.body?.detail ?? "plan-change-preview");
      }
      return result.value;
    },
  });

  const blocked = Boolean(previewQuery.data?.hasBlockingUsageConflicts);
  const simulated = isSimulatedOrganizationBilling();

  const mutation = useMutation({
    mutationFn: async (simulation: PlanChangePaymentSimulation | "downgrade") => {
      if (!subscription.id || !selectedPlan?.id) {
        throw new Error("missing-selection");
      }
      if (direction === "downgrade" || simulation === "downgrade") {
        return downgradeOrganizationSubscription({
          organizationId,
          subscriptionId: subscription.id,
          planId: selectedPlan.id,
          effectiveAtUtc: downgradeEffectiveAt,
          idempotencyKey: createPlanChangeIdempotencyKey("pos-downgrade"),
        });
      }
      return upgradeOrganizationSubscription({
        organizationId,
        subscriptionId: subscription.id,
        planId: selectedPlan.id,
        billingCycle,
        idempotencyKey: createPlanChangeIdempotencyKey("pos-upgrade"),
        paymentSimulation: simulation,
      });
    },
  });

  function planLimitsLine(plan: OrganizationPlanDto): string {
    return t("orgSubscription.planLimits")
      .replace("{branches}", String(plan.maxBranches))
      .replace("{staff}", String(plan.maxActiveStaff))
      .replace("{devices}", String(plan.maxActivePosDevices))
      .replace("{areas}", String(plan.maxAreas));
  }

  async function completeChange(kind: "upgrade" | "downgrade") {
    setFeedback(kind);
    setStep("done");
    setSelectedPlanId(null);
    await queryClient.invalidateQueries({ queryKey: ["org-subscription"] });
    await onCompleted();
  }

  async function runUpgrade(outcome: PlanChangePaymentSimulation) {
    setErrorDetail(null);
    const result = await mutation.mutateAsync(outcome);
    if (!result.ok) {
      setErrorDetail(result.body?.detail ?? t("orgSubscription.paymentFailedDetail"));
      setStep("payment");
      return;
    }
    if (outcome === "fail" || outcome === "declined") {
      setErrorDetail(t("orgSubscription.paymentFailedDetail"));
      setStep("payment");
      return;
    }
    await completeChange("upgrade");
  }

  async function runDowngrade() {
    setErrorDetail(null);
    const result = await mutation.mutateAsync("downgrade");
    if (!result.ok) {
      setErrorDetail(result.body?.detail ?? t("orgSubscription.changeError"));
      return;
    }
    await completeChange("downgrade");
  }

  return (
    <div className="flex flex-col gap-4" data-testid="org-plan-change-panel">
      {feedback === "upgrade" ? (
        <Notice tone="success" testId="org-subscription-upgrade-success">
          {t("orgSubscription.upgradeSuccess")}
        </Notice>
      ) : null}
      {feedback === "downgrade" ? (
        <Notice tone="success" testId="org-subscription-downgrade-success">
          {t("orgSubscription.downgradeSuccess")}
        </Notice>
      ) : null}

      {step === "select" || step === "done" ? (
        <section className="flex flex-col gap-2">
          <h3 className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
            {t("orgSubscription.plansTitle")}
          </h3>
          {plans.length === 0 ? (
            <Notice tone="info">{t("orgSubscription.plansEmpty")}</Notice>
          ) : (
            <ul className="m-0 flex list-none flex-col gap-2 p-0" data-testid="org-subscription-plans">
              {plans.map((plan) => {
                const planDirection = comparePlanTier(currentPlan, plan);
                const isCurrent = Boolean(currentPlan?.id && plan.id === currentPlan.id);
                return (
                  <li
                    key={plan.id ?? plan.planKey ?? plan.displayName}
                    className="flex flex-wrap items-center justify-between gap-2 rounded-[var(--exits-radius-md)] border border-border px-3 py-2"
                    data-testid={`org-subscription-plan-${plan.planKey ?? plan.code ?? plan.id}`}
                    data-direction={planDirection}
                    data-current={isCurrent ? "true" : "false"}
                  >
                    <div className="flex min-w-0 flex-col gap-1">
                      <span className="font-medium">{plan.displayName}</span>
                      <span className="text-[length:var(--exits-text-xs)] text-muted">
                        {planLimitsLine(plan)}
                      </span>
                      {plan.monthlyPrice != null ? (
                        <span className="text-[length:var(--exits-text-xs)]">
                          <MoneyDisplay amount={plan.monthlyPrice} />
                        </span>
                      ) : null}
                    </div>
                    <div className="flex items-center gap-2">
                      {isCurrent ? (
                        <StatusChip tone="success" shape="auto" data-testid="org-subscription-current-badge">
                          {t("orgSubscription.planCurrent")}
                        </StatusChip>
                      ) : (
                        <StatusChip
                          tone={planDirection === "downgrade" ? "warning" : "info"}
                          shape="auto"
                        >
                          {planDirection === "downgrade"
                            ? t("orgSubscription.planDowngrade")
                            : t("orgSubscription.planUpgrade")}
                        </StatusChip>
                      )}
                      {!isCurrent ? (
                        <Button
                          intent="neutral"
                          appearance="outline"
                          onClick={() => {
                            setSelectedPlanId(plan.id);
                            setFeedback(null);
                            setErrorDetail(null);
                            setStep("review");
                          }}
                          disabled={!plan.id || !subscription.id}
                          data-testid={`org-subscription-check-${plan.planKey ?? plan.code ?? plan.id}`}
                        >
                          {t("orgSubscription.selectPlan")}
                        </Button>
                      ) : null}
                    </div>
                  </li>
                );
              })}
            </ul>
          )}
        </section>
      ) : null}

      {step === "review" && selectedPlan ? (
        <section className="flex flex-col gap-3" data-testid="org-subscription-preview">
          <h3 className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
            {t("orgSubscription.reviewTitle")}
          </h3>
          {previewQuery.isLoading ? <LoadingState label={t("orgSubscription.comparing")} /> : null}
          {previewQuery.isError ? (
            <Notice tone="danger" testId="org-subscription-preview-error">
              {t("orgSubscription.previewError")}
            </Notice>
          ) : null}
          {previewQuery.data?.hasBlockingUsageConflicts ? (
            <Notice
              tone="danger"
              title={t("orgSubscription.previewBlockedTitle")}
              testId="org-subscription-preview-blocked"
            >
              <span className="block">
                {t("orgSubscription.previewBlockedDetail").replace(
                  "{plan}",
                  selectedPlan.displayName ?? "",
                )}
              </span>
              <ul className="m-0 mt-1 list-disc pl-4">
                {previewQuery.data.usageConflicts.map((conflict) => (
                  <li key={`${conflict.resource}-${conflict.targetLimit}`}>{conflict.message}</li>
                ))}
              </ul>
            </Notice>
          ) : null}
          {previewQuery.data && !previewQuery.data.hasBlockingUsageConflicts ? (
            <Notice
              tone="success"
              title={t("orgSubscription.previewOkTitle")}
              testId="org-subscription-preview-ok"
            >
              {t("orgSubscription.previewOkDetail").replace(
                "{plan}",
                selectedPlan.displayName ?? "",
              )}
            </Notice>
          ) : null}

          <dl className="grid gap-2 text-[length:var(--exits-text-sm)] sm:grid-cols-2">
            <div>
              <dt className="text-muted">{t("orgSubscription.reviewCurrentPlan")}</dt>
              <dd className="m-0 font-medium">{currentPlan?.displayName}</dd>
            </div>
            <div>
              <dt className="text-muted">{t("orgSubscription.reviewNewPlan")}</dt>
              <dd className="m-0 font-medium">{selectedPlan.displayName}</dd>
            </div>
            <div>
              <dt className="text-muted">{t("orgSubscription.reviewBillingCycle")}</dt>
              <dd className="m-0">{billingCycle}</dd>
            </div>
            <div>
              <dt className="text-muted">{t("orgSubscription.reviewEffective")}</dt>
              <dd className="m-0">
                {direction === "downgrade"
                  ? t("orgSubscription.reviewEffectiveRenewal").replace(
                      "{date}",
                      formatSubscriptionDate(downgradeEffectiveAt) ??
                        t("orgSubscription.notAvailable"),
                    )
                  : t("orgSubscription.reviewEffectiveImmediate")}
              </dd>
            </div>
            <div>
              <dt className="text-muted">{t("orgSubscription.reviewCurrentPrice")}</dt>
              <dd className="m-0">
                {currentPrice != null ? (
                  <MoneyDisplay amount={currentPrice} />
                ) : (
                  t("orgSubscription.notAvailable")
                )}
              </dd>
            </div>
            <div>
              <dt className="text-muted">{t("orgSubscription.reviewNewPrice")}</dt>
              <dd className="m-0">
                {newPrice != null ? <MoneyDisplay amount={newPrice} /> : t("orgSubscription.notAvailable")}
              </dd>
            </div>
          </dl>

          <div>
            <p className="m-0 mb-1 text-[length:var(--exits-text-sm)] font-semibold">
              {t("orgSubscription.reviewLimitsTitle")}
            </p>
            <ul className="m-0 list-disc pl-4 text-[length:var(--exits-text-sm)]">
              {limitDiffs.map((row) => (
                <li key={row.dimension}>
                  {t("orgSubscription.reviewLimitRow")
                    .replace("{label}", t(CAPACITY_LABEL_KEYS[row.dimension]))
                    .replace("{current}", String(row.current))
                    .replace("{target}", String(row.target))}
                </li>
              ))}
            </ul>
          </div>

          {gainedFeatures.length > 0 ? (
            <div data-testid="org-subscription-preview-gained">
              <strong>{t("orgSubscription.previewGainedFeatures")}</strong>
              <ul className="m-0 mt-1 list-disc pl-4">
                {gainedFeatures.map((row) => (
                  <li key={row.id}>{t(PLAN_FEATURE_LABEL_KEYS[row.id])}</li>
                ))}
              </ul>
            </div>
          ) : null}

          {(previewQuery.data?.lostFeatures.length || lostFromPlans.length) > 0 ? (
            <div data-testid="org-subscription-preview-lost">
              <strong>{t("orgSubscription.previewLostFeatures")}</strong>
              <ul className="m-0 mt-1 list-disc pl-4">
                {(previewQuery.data?.lostFeatures.length
                  ? previewQuery.data.lostFeatures
                  : lostFromPlans.map((row) => t(PLAN_FEATURE_LABEL_KEYS[row.id]))
                ).map((feature) => (
                  <li key={feature}>{feature}</li>
                ))}
              </ul>
            </div>
          ) : null}

          <div className="flex flex-wrap gap-2">
            <Button
              intent="neutral"
              appearance="outline"
              onClick={() => {
                setStep("select");
                setSelectedPlanId(null);
              }}
              data-testid="org-subscription-back-plans"
            >
              {t("orgSubscription.backToPlans")}
            </Button>
            {direction === "upgrade" ? (
              <Button
                intent="primary"
                disabled={blocked || previewQuery.isLoading || previewQuery.isError}
                onClick={() => setStep("payment")}
                data-testid="org-subscription-continue-payment"
              >
                {t("orgSubscription.continueToPayment")}
              </Button>
            ) : null}
            {direction === "downgrade" ? (
              <Button
                intent="primary"
                disabled={blocked || previewQuery.isLoading || previewQuery.isError || mutation.isPending}
                onClick={() => void runDowngrade()}
                data-testid="org-subscription-schedule-downgrade"
              >
                {mutation.isPending
                  ? t("orgSubscription.submitting")
                  : t("orgSubscription.scheduleDowngrade")}
              </Button>
            ) : null}
          </div>
          {errorDetail ? (
            <Notice tone="danger" testId="org-subscription-change-error">
              {errorDetail}
            </Notice>
          ) : null}
        </section>
      ) : null}

      {step === "payment" && selectedPlan ? (
        <section className="flex flex-col gap-3" data-testid="org-subscription-simulated-payment">
          <Notice
            tone="info"
            title={t("orgSubscription.simulatedPaymentTitle")}
            testId="org-subscription-simulated-banner"
          >
            {t("orgSubscription.simulatedPaymentDetail")}
          </Notice>
          <dl className="grid gap-2 text-[length:var(--exits-text-sm)] sm:grid-cols-2">
            <div>
              <dt className="text-muted">{t("orgSubscription.simulatedAmountDue")}</dt>
              <dd className="m-0">
                {newPrice != null ? (
                  <MoneyDisplay amount={newPrice} testId="org-subscription-amount-due" />
                ) : (
                  t("orgSubscription.notAvailable")
                )}
              </dd>
            </div>
            <div>
              <dt className="text-muted">{t("orgSubscription.simulatedMethod")}</dt>
              <dd className="m-0">{t("orgSubscription.simulatedMethodValue")}</dd>
            </div>
            <div>
              <dt className="text-muted">{t("orgSubscription.billingModeLabel")}</dt>
              <dd className="m-0" data-testid="org-subscription-billing-mode">
                {simulated
                  ? t("orgSubscription.billingModeSimulated")
                  : t("orgSubscription.notAvailable")}
              </dd>
            </div>
          </dl>
          {errorDetail ? (
            <Notice
              tone="danger"
              title={t("orgSubscription.paymentFailedTitle")}
              testId="org-subscription-payment-failed"
            >
              {errorDetail}
            </Notice>
          ) : null}
          <div className="flex flex-wrap gap-2">
            <Button
              intent="neutral"
              appearance="outline"
              onClick={() => setStep("review")}
              disabled={mutation.isPending}
            >
              {t("orgSubscription.backToPlans")}
            </Button>
            <Button
              intent="primary"
              disabled={mutation.isPending}
              onClick={() => void runUpgrade("succeed")}
              data-testid="org-subscription-simulate-success"
            >
              {mutation.isPending
                ? t("orgSubscription.submitting")
                : t("orgSubscription.simulateSuccess")}
            </Button>
            <Button
              intent="danger"
              appearance="outline"
              disabled={mutation.isPending}
              onClick={() => void runUpgrade("fail")}
              data-testid="org-subscription-simulate-failure"
            >
              {t("orgSubscription.simulateFailure")}
            </Button>
          </div>
        </section>
      ) : null}
    </div>
  );
}

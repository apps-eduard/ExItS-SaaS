import { Check, Users } from "lucide-react";
import { useMemo, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useMutation, useQuery } from "@tanstack/react-query";
import {
  listCommercialPlans,
  type CommercialPlanDto,
} from "@/api/platform/commercial-plans-client";
import { createPersonalSubscriptionPayment } from "@/api/platform/subscription-payment-client";
import { PlatformApiError } from "@/api/platform/platform-http";
import { isFrontendLocalValidationMode } from "@/api/platform/local-validation-gate";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { Notice } from "@/components/exits/Notice";
import { PageHeader } from "@/components/exits/PageHeader";
import {
  billingCycleDiscountPercentFromQuotes,
  buildPlanCompareRows,
  getPlanBillingQuote,
  getPlanDisplayMeta,
  getServerBillingQuote,
  PLAN_BILLING_CYCLES,
  resolvePlanCtaKind,
  resolvePlanKey,
  type PlanBillingCycle,
  type PlanCtaKind,
} from "@/features/personal/start-business/plan-selection-meta";
import { PlanPaymentSummary } from "@/features/personal/start-business/PlanPaymentSummary";
import { writePendingSubscriptionCheckout } from "@/features/subscription-checkout/pending-subscription-checkout";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { personalPageBackNav } from "@/navigation/page-back-nav";

type ExplorePosPageProps = {
  /** When set, CTAs become Current / Upgrade / Change instead of generic choose. */
  currentPlanKey?: string | null;
};

function formatMoney(amount: number, currency: string): string {
  return `${amount.toLocaleString()} ${currency}`;
}

function includesLabel(
  includesKey: string | undefined,
  plans: CommercialPlanDto[],
  t: (key: MessageKey) => string,
): string | null {
  if (!includesKey) return null;
  const baseline = plans.find((p) => resolvePlanKey(p) === includesKey);
  const name = baseline?.displayName ?? includesKey;
  return t("personal.explore.includesEverythingIn").replace("{plan}", name);
}

function ctaLabel(
  kind: PlanCtaKind,
  displayName: string,
  t: (key: MessageKey) => string,
): string {
  switch (kind) {
    case "current":
      return t("personal.explore.cta.current");
    case "upgrade":
      return t("personal.explore.cta.upgrade").replace("{plan}", displayName);
    case "downgrade":
      return t("personal.explore.cta.change").replace("{plan}", displayName);
    default:
      return t("personal.explore.cta.choose").replace("{plan}", displayName);
  }
}

function billingCycleShortLabel(cycle: PlanBillingCycle, t: (key: MessageKey) => string): string {
  switch (cycle) {
    case "Quarterly":
      return t("personal.explore.billingQuarterly");
    case "SixMonths":
      return t("personal.explore.billingSixMonths");
    case "Annual":
      return t("personal.explore.billingAnnual");
    default:
      return t("personal.explore.billingMonthly");
  }
}

function billingCyclePeriodLabel(cycle: PlanBillingCycle, t: (key: MessageKey) => string): string {
  switch (cycle) {
    case "Quarterly":
      return t("personal.explore.billingEvery3Months");
    case "SixMonths":
      return t("personal.explore.billingEvery6Months");
    case "Annual":
      return t("personal.explore.billingEveryYear");
    default:
      return t("personal.explore.billingEveryMonth");
  }
}

function billingToggleSecondary(
  cycle: PlanBillingCycle,
  plans: CommercialPlanDto[],
  t: (key: MessageKey) => string,
): string | null {
  const savePct = billingCycleDiscountPercentFromQuotes(plans, cycle);
  if (cycle === "Annual") {
    if (savePct != null) {
      return t("personal.explore.billingBestValueSave").replace("{percent}", String(savePct));
    }
    return t("personal.explore.billingBestValue");
  }
  if (savePct != null) {
    return t("personal.explore.billingSavePercent").replace("{percent}", String(savePct));
  }
  return null;
}

export function PersonalExplorePosPage({ currentPlanKey = null }: ExplorePosPageProps) {
  const { t } = useI18n();
  const navigate = useNavigate();
  const localValidation = isFrontendLocalValidationMode();
  const [billing, setBilling] = useState<PlanBillingCycle>("Monthly");
  const [compareOpen, setCompareOpen] = useState(false);
  const compareRef = useRef<HTMLDivElement>(null);

  const plansQuery = useQuery({
    queryKey: ["commercial", "plans", "pinoy-business-pos"],
    queryFn: ({ signal }) => listCommercialPlans(undefined, signal),
  });

  const plans = plansQuery.data ?? [];
  const currentPlan = useMemo(() => {
    if (!currentPlanKey) return null;
    return plans.find((p) => resolvePlanKey(p) === currentPlanKey.trim().toLowerCase()) ?? null;
  }, [plans, currentPlanKey]);

  const compareRows = useMemo(() => buildPlanCompareRows(plans), [plans]);
  const [checkoutError, setCheckoutError] = useState<string | null>(null);
  const [checkoutPlanKey, setCheckoutPlanKey] = useState<string | null>(null);

  const startCheckoutMutation = useMutation({
    mutationFn: async (args: { planKey: string; billingCycle: PlanBillingCycle }) => {
      const payment = await createPersonalSubscriptionPayment({
        planKey: args.planKey,
        billingCycle: args.billingCycle,
      });
      writePendingSubscriptionCheckout({
        paymentId: payment.id,
        planKey: payment.planKey,
        billingCycle: payment.billingCycle,
      });
      return payment;
    },
    onSuccess: (payment) => {
      setCheckoutError(null);
      setCheckoutPlanKey(null);
      navigate(`/subscription-checkout/${payment.id}`, { replace: false });
    },
    onError: (error) => {
      setCheckoutPlanKey(null);
      if (error instanceof PlatformApiError) {
        if (error.status === 404) {
          setCheckoutError(
            error.problem.detail?.trim()
            || t("personal.explore.checkoutApiMissing"),
          );
          return;
        }
        setCheckoutError(
          error.problem.detail?.trim()
          || error.message
          || t("subscriptionCheckout.errorDetail"),
        );
        return;
      }
      setCheckoutError(error instanceof Error ? error.message : t("subscriptionCheckout.errorDetail"));
    },
  });

  function openCompare() {
    setCompareOpen(true);
    requestAnimationFrame(() => {
      const el = compareRef.current;
      if (el && typeof el.scrollIntoView === "function") {
        el.scrollIntoView({ behavior: "smooth", block: "start" });
      }
    });
  }

  if (plansQuery.isPending) {
    return <LoadingSkeleton label={t("personal.explore.loading")} />;
  }

  if (plansQuery.isError) {
    return (
      <div
        className="personal-page exits-page flex flex-col gap-3"
        data-testid="personal-explore-pos-error"
      >
        <PageHeader
          title={t("personal.explore.title")}
          backTo={personalPageBackNav.more.to}
          backLabel={t(personalPageBackNav.more.labelKey)}
          backTestId="page-header-back-explore-pos"
        />
        <ErrorState
          title={t("personal.explore.errorTitle")}
          detail={t("personal.explore.errorDetail")}
        />
        <Button type="button" className="w-fit" onClick={() => void plansQuery.refetch()}>
          {t("personal.explore.retry")}
        </Button>
      </div>
    );
  }

  return (
    <div className="personal-page exits-page flex min-w-0 flex-col gap-3" data-testid="personal-explore-pos-page">
      <div className="plan-explore-shell mx-auto flex w-full min-w-0 flex-col gap-3">
        <PageHeader
          title={t("personal.explore.title")}
          description={t("personal.explore.lede")}
          backTo={personalPageBackNav.more.to}
          backLabel={t(personalPageBackNav.more.labelKey)}
          backTestId="page-header-back-explore-pos"
        />

        {checkoutError ? (
          <Notice tone="danger" title={t("personal.explore.checkoutFailedTitle")} testId="explore-checkout-error">
            {checkoutError}
          </Notice>
        ) : null}

        {plans.length === 0 ? (
          <EmptyState
            align="center"
            icon={<Users className="size-5" strokeWidth={1.75} />}
            title={t("personal.explore.emptyTitle")}
            detail={t("personal.explore.emptyDetail")}
          />
        ) : (
          <>
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted exits-animate-panel">
              {t("personal.explore.selectHint")}
            </p>

            <Notice
              tone="info"
              testId="explore-simulated-payments-notice"
              className="max-w-3xl"
            >
              {t("personal.explore.simulatedPaymentsNotice")}
            </Notice>

            <div
              className="plan-billing-toggle"
              role="group"
              aria-label={t("personal.explore.billingToggleAria")}
              data-testid="explore-billing-toggle"
            >
              {PLAN_BILLING_CYCLES.map((cycle) => {
                const secondary = billingToggleSecondary(cycle, plans, t);
                return (
                  <button
                    key={cycle}
                    type="button"
                    className={billing === cycle ? "is-active" : undefined}
                    data-testid={`explore-billing-${cycle.toLowerCase()}`}
                    aria-pressed={billing === cycle}
                    onClick={() => setBilling(cycle)}
                  >
                    <span>{billingCycleShortLabel(cycle, t)}</span>
                    {secondary ? (
                      <span className="plan-billing-toggle__hint">{secondary}</span>
                    ) : null}
                  </button>
                );
              })}
            </div>

            <ul className="plan-selection-grid m-0 list-none p-0" role="list">
              {plans.map((plan) => {
                const planKey = resolvePlanKey(plan);
                const meta = getPlanDisplayMeta(plan);
                const trialAvailable = plan.trialAllowed && plan.defaultTrialDays > 0;
                const quote = getPlanBillingQuote(plan, billing);
                const serverQuote = getServerBillingQuote(plan, billing);
                const price = quote?.finalAmount ?? plan.monthlyPrice;
                const periodLabel = billingCyclePeriodLabel(billing, t);
                const savingsPct =
                  serverQuote && serverQuote.discountPercent > 0
                    ? Math.round(serverQuote.discountPercent)
                    : null;
                const savingsAmount =
                  serverQuote && serverQuote.discountAmount > 0
                    ? serverQuote.discountAmount
                    : null;
                const showEquivalent =
                  billing !== "Monthly" && quote && quote.equivalentMonthlyAmount > 0;
                const includes = includesLabel(meta.includesEverythingIn, plans, t);
                const ctaKind = resolvePlanCtaKind(
                  planKey,
                  currentPlanKey,
                  plan.sortOrder,
                  currentPlan?.sortOrder,
                );
                const highlightKeys = meta.highlightKeys.slice(0, 5);
                const cardClass = [
                  "plan-selection-card",
                  "plan-selection-card--compact",
                  "exits-list__card",
                  "flex",
                  "flex-col",
                  "gap-1.5",
                  "p-3.5",
                  meta.badge === "most_popular" ? "plan-selection-card--popular" : "",
                  meta.badge === "complete" ? "plan-selection-card--complete" : "",
                  ctaKind === "current" ? "plan-selection-card--current" : "",
                ]
                  .filter(Boolean)
                  .join(" ");

                return (
                  <li key={plan.id} className="min-w-0">
                    <article className={cardClass} data-testid={`explore-plan-${planKey}`}>
                      <header className="flex flex-wrap items-start justify-between gap-2">
                        <div className="min-w-0">
                          <h2 className="m-0 text-[length:var(--exits-text-base)] font-semibold leading-tight">
                            {plan.displayName}
                          </h2>
                          <p className="m-0 mt-0.5 text-[length:var(--exits-text-xs)] text-muted">
                            {t(meta.taglineKey)}
                          </p>
                        </div>
                        <div className="flex flex-col items-end gap-1">
                          {meta.badge === "most_popular" ? (
                            <span
                              className="plan-badge plan-badge--popular"
                              data-testid="explore-badge-most-popular"
                            >
                              {t("personal.explore.badge.mostPopular")}
                            </span>
                          ) : null}
                          {meta.badge === "complete" ? (
                            <span
                              className="plan-badge plan-badge--complete"
                              data-testid="explore-badge-complete"
                            >
                              {t("personal.explore.badge.complete")}
                            </span>
                          ) : null}
                        </div>
                      </header>

                      <p
                        className="m-0 text-[length:var(--exits-text-xl)] font-semibold leading-tight"
                        data-testid={`explore-price-${planKey}`}
                      >
                        {formatMoney(price, plan.currencyCode)}
                        <span className="ml-1 text-[length:var(--exits-text-xs)] font-normal text-muted">
                          {periodLabel}
                        </span>
                      </p>
                      {showEquivalent ? (
                        <p
                          className="m-0 text-[length:var(--exits-text-xs)] text-muted"
                          data-testid={`explore-equiv-${planKey}`}
                        >
                          {t("personal.explore.equivalentMonthly").replace(
                            "{amount}",
                            formatMoney(quote!.equivalentMonthlyAmount, plan.currencyCode),
                          )}
                        </p>
                      ) : null}
                      {savingsPct != null && savingsAmount != null ? (
                        <p
                          className="m-0 text-[length:var(--exits-text-xs)] font-medium text-primary"
                          data-testid={`explore-savings-${planKey}`}
                        >
                          {t("personal.explore.cycleSavingsDetail")
                            .replace("{amount}", formatMoney(savingsAmount, plan.currencyCode))
                            .replace("{percent}", String(savingsPct))}
                        </p>
                      ) : savingsPct != null ? (
                        <p className="m-0 text-[length:var(--exits-text-xs)] text-primary">
                          {t("personal.explore.cycleSavings").replace("{percent}", String(savingsPct))}
                        </p>
                      ) : null}

                      <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
                        {trialAvailable
                          ? t("personal.explore.trialDays").replace(
                              "{days}",
                              String(plan.defaultTrialDays),
                            )
                          : t("personal.explore.noTrial")}
                      </p>

                      <p
                        className="m-0 text-[length:var(--exits-text-xs)] text-muted"
                        data-testid={`explore-capacity-${planKey}`}
                      >
                        {t("personal.explore.capacityLine")
                          .replace("{branches}", String(plan.maxBranches))
                          .replace("{staff}", String(plan.maxActiveStaff))
                          .replace("{devices}", String(plan.maxActivePosDevices))}
                      </p>

                      {includes ? (
                        <p className="m-0 text-[length:var(--exits-text-xs)] font-medium">{includes}</p>
                      ) : null}

                      <ul className="plan-feature-checks m-0 list-none space-y-1 p-0">
                        {highlightKeys.map((key) => (
                          <li
                            key={key}
                            className="flex items-start gap-1.5 text-[length:var(--exits-text-xs)] text-muted"
                          >
                            <Check
                              className="mt-0.5 size-3.5 shrink-0 text-primary"
                              aria-hidden
                              strokeWidth={2.25}
                            />
                            <span>{t(key)}</span>
                          </li>
                        ))}
                      </ul>

                      <PlanPaymentSummary planKey={planKey} t={t} />

                      <div className="mt-auto flex flex-col gap-1.5 pt-1">
                        <div className="flex flex-wrap gap-2">
                          {ctaKind === "current" ? (
                            <Button
                              type="button"
                              variant="ghost"
                              disabled
                              data-testid={`explore-current-${planKey}`}
                            >
                              {ctaLabel(ctaKind, plan.displayName, t)}
                            </Button>
                          ) : (
                            <>
                              {trialAvailable ? (
                                <Button
                                  type="button"
                                  data-testid={`explore-start-trial-${planKey}`}
                                  onClick={() =>
                                    navigate(
                                      `/personal/start-business?planKey=${encodeURIComponent(planKey)}&trial=1&payNow=0&billing=${billing}`,
                                    )
                                  }
                                >
                                  {t("personal.explore.startTrial")}
                                </Button>
                              ) : null}
                              {localValidation ? (
                                <Button
                                  type="button"
                                  variant={trialAvailable ? "ghost" : "default"}
                                  data-testid={`explore-subscribe-${planKey}`}
                                  disabled={startCheckoutMutation.isPending}
                                  onClick={() => {
                                    setCheckoutPlanKey(planKey);
                                    startCheckoutMutation.mutate({
                                      planKey,
                                      billingCycle: billing,
                                    });
                                  }}
                                >
                                  {startCheckoutMutation.isPending && checkoutPlanKey === planKey
                                    ? t("subscriptionCheckout.processing")
                                    : ctaLabel(ctaKind, plan.displayName, t)}
                                </Button>
                              ) : null}
                              {!trialAvailable && !localValidation ? (
                                <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                                  {t("personal.explore.paymentUnavailable")}
                                </p>
                              ) : null}
                            </>
                          )}
                        </div>
                        <button
                          type="button"
                          className="plan-view-all-features"
                          data-testid={`explore-view-all-${planKey}`}
                          onClick={openCompare}
                        >
                          {t("personal.explore.viewAllFeatures")}
                        </button>
                      </div>
                    </article>
                  </li>
                );
              })}
            </ul>

            <div className="flex flex-col gap-2" ref={compareRef}>
              <Button
                type="button"
                variant="ghost"
                className="w-fit"
                data-testid="explore-compare-toggle"
                aria-expanded={compareOpen}
                onClick={() => setCompareOpen((open) => !open)}
              >
                {compareOpen
                  ? t("personal.explore.compare.hide")
                  : t("personal.explore.compare.show")}
              </Button>

              {compareOpen ? (
                <div className="plan-compare-scroll" data-testid="explore-compare-matrix">
                  <table className="plan-compare-table">
                    <thead>
                      <tr>
                        <th scope="col">{t("personal.explore.compare.feature")}</th>
                        {plans.map((plan) => (
                          <th key={plan.id} scope="col">
                            {plan.displayName}
                          </th>
                        ))}
                      </tr>
                    </thead>
                    <tbody>
                      {compareRows.map((row) => (
                        <tr key={row.id}>
                          <th scope="row">{t(row.labelKey)}</th>
                          {plans.map((plan) => {
                            const key = resolvePlanKey(plan);
                            const value = row.values[key];
                            return (
                              <td key={`${row.id}-${key}`}>
                                {typeof value === "boolean" ? (
                                  value ? (
                                    <span aria-label={t("personal.explore.compare.yes")}>✓</span>
                                  ) : (
                                    <span
                                      className="text-muted"
                                      aria-label={t("personal.explore.compare.no")}
                                    >
                                      —
                                    </span>
                                  )
                                ) : (
                                  value
                                )}
                              </td>
                            );
                          })}
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              ) : null}
            </div>

            {!localValidation ? (
              <p
                className="m-0 text-[length:var(--exits-text-xs)] text-muted"
                data-testid="explore-payment-note"
              >
                {t("personal.explore.paymentNote")}
              </p>
            ) : null}
          </>
        )}
      </div>
    </div>
  );
}

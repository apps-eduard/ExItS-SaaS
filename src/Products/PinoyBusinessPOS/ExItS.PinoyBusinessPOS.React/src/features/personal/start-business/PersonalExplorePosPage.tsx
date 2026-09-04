import { useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import {
  listCommercialPlans,
  type CommercialPlanDto,
} from "@/api/platform/commercial-plans-client";
import { isFrontendLocalValidationMode } from "@/api/platform/local-validation-gate";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { PageHeader } from "@/components/exits/PageHeader";
import {
  annualSavingsPercent,
  buildPlanCompareRows,
  getPlanDisplayMeta,
  planPriceForCycle,
  resolvePlanCtaKind,
  resolvePlanKey,
  type PlanBillingCycle,
  type PlanCtaKind,
} from "@/features/personal/start-business/plan-selection-meta";
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

export function PersonalExplorePosPage({ currentPlanKey = null }: ExplorePosPageProps) {
  const { t } = useI18n();
  const navigate = useNavigate();
  const localValidation = isFrontendLocalValidationMode();
  const [billing, setBilling] = useState<PlanBillingCycle>("Monthly");
  const [compareOpen, setCompareOpen] = useState(false);

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
        <Button type="button" className="min-h-11 w-fit" onClick={() => void plansQuery.refetch()}>
          {t("personal.explore.retry")}
        </Button>
      </div>
    );
  }

  return (
    <div className="personal-page exits-page flex min-w-0 flex-col gap-3" data-testid="personal-explore-pos-page">
      <PageHeader
        title={t("personal.explore.title")}
        description={t("personal.explore.lede")}
        backTo={personalPageBackNav.more.to}
        backLabel={t(personalPageBackNav.more.labelKey)}
        backTestId="page-header-back-explore-pos"
      />

      {plans.length === 0 ? (
        <EmptyState
          title={t("personal.explore.emptyTitle")}
          detail={t("personal.explore.emptyDetail")}
        />
      ) : (
        <>
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted exits-animate-panel">
            {t("personal.explore.selectHint")}
          </p>

          <div
            className="plan-billing-toggle"
            role="group"
            aria-label={t("personal.explore.billingToggleAria")}
            data-testid="explore-billing-toggle"
          >
            <button
              type="button"
              className={billing === "Monthly" ? "is-active" : undefined}
              data-testid="explore-billing-monthly"
              aria-pressed={billing === "Monthly"}
              onClick={() => setBilling("Monthly")}
            >
              {t("personal.explore.billingMonthly")}
            </button>
            <button
              type="button"
              className={billing === "Annual" ? "is-active" : undefined}
              data-testid="explore-billing-annual"
              aria-pressed={billing === "Annual"}
              onClick={() => setBilling("Annual")}
            >
              {t("personal.explore.billingAnnual")}
            </button>
          </div>

          <ul className="plan-selection-grid m-0 list-none p-0" role="list">
            {plans.map((plan) => {
              const planKey = resolvePlanKey(plan);
              const meta = getPlanDisplayMeta(plan);
              const trialAvailable = plan.trialAllowed && plan.defaultTrialDays > 0;
              const price = planPriceForCycle(plan, billing);
              const periodLabel =
                billing === "Annual"
                  ? t("personal.explore.billingYear")
                  : t("personal.explore.billingMonth");
              const savingsPct = billing === "Annual" ? annualSavingsPercent(plan) : null;
              const includes = includesLabel(meta.includesEverythingIn, plans, t);
              const ctaKind = resolvePlanCtaKind(
                planKey,
                currentPlanKey,
                plan.sortOrder,
                currentPlan?.sortOrder,
              );
              const cardClass = [
                "plan-selection-card",
                "exits-list__card",
                "flex",
                "flex-col",
                "gap-2",
                "p-4",
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
                        <h2 className="m-0 text-[length:var(--exits-text-lg)] font-semibold">
                          {plan.displayName}
                        </h2>
                        <p className="m-0 mt-0.5 text-[length:var(--exits-text-sm)] text-muted">
                          {t(meta.taglineKey)}
                        </p>
                      </div>
                      <div className="flex flex-col items-end gap-1">
                        {meta.badge === "most_popular" ? (
                          <span className="plan-badge plan-badge--popular" data-testid="explore-badge-most-popular">
                            {t("personal.explore.badge.mostPopular")}
                          </span>
                        ) : null}
                        {meta.badge === "complete" ? (
                          <span className="plan-badge plan-badge--complete" data-testid="explore-badge-complete">
                            {t("personal.explore.badge.complete")}
                          </span>
                        ) : null}
                        <span className="text-[length:var(--exits-text-xs)] text-muted">
                          {trialAvailable
                            ? t("personal.explore.trialDays").replace(
                                "{days}",
                                String(plan.defaultTrialDays),
                              )
                            : t("personal.explore.noTrial")}
                        </span>
                      </div>
                    </header>

                    <p className="m-0 text-[length:var(--exits-text-xl)] font-semibold" data-testid={`explore-price-${planKey}`}>
                      {formatMoney(price, plan.currencyCode)}
                      <span className="ml-1 text-[length:var(--exits-text-sm)] font-normal text-muted">
                        / {periodLabel}
                      </span>
                    </p>
                    {savingsPct != null ? (
                      <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
                        {t("personal.explore.annualSavings").replace("{percent}", String(savingsPct))}
                      </p>
                    ) : null}

                    <ul className="m-0 mt-1 list-none space-y-1 p-0 text-[length:var(--exits-text-sm)] text-muted">
                      <li>{t("personal.explore.featureBranches").replace("{count}", String(plan.maxBranches))}</li>
                      <li>{t("personal.explore.featureStaff").replace("{count}", String(plan.maxActiveStaff))}</li>
                      <li>
                        {t("personal.explore.featureDevices").replace(
                          "{count}",
                          String(plan.maxActivePosDevices),
                        )}
                      </li>
                      <li>
                        {t("personal.explore.featureBusinessTypes").replace(
                          "{count}",
                          String(plan.maxActiveBusinessTypes),
                        )}
                      </li>
                      {plan.maxAreas > 0 ? (
                        <li>
                          {t("personal.explore.featureAreas").replace("{count}", String(plan.maxAreas))}
                        </li>
                      ) : null}
                      {meta.warehouseIncluded ? (
                        <li>{t("personal.explore.featureWarehouse")}</li>
                      ) : null}
                    </ul>

                    {includes ? (
                      <p className="m-0 text-[length:var(--exits-text-sm)] font-medium">{includes}</p>
                    ) : null}

                    <ul className="m-0 list-disc space-y-1 pl-5 text-[length:var(--exits-text-sm)] text-muted">
                      {meta.highlightKeys.map((key) => (
                        <li key={key}>{t(key)}</li>
                      ))}
                    </ul>

                    <div className="mt-auto flex flex-wrap gap-2 pt-2">
                      {ctaKind === "current" ? (
                        <Button
                          type="button"
                          variant="ghost"
                          className="min-h-11"
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
                              className="min-h-11"
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
                              className="min-h-11"
                              data-testid={`explore-subscribe-${planKey}`}
                              onClick={() =>
                                navigate(
                                  `/personal/start-business?planKey=${encodeURIComponent(planKey)}&trial=0&payNow=1&billing=${billing}`,
                                )
                              }
                            >
                              {ctaLabel(ctaKind, plan.displayName, t)}
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
                  </article>
                </li>
              );
            })}
          </ul>

          <div className="flex flex-col gap-2">
            <Button
              type="button"
              variant="ghost"
              className="min-h-11 w-fit"
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
                                  <span className="text-muted" aria-label={t("personal.explore.compare.no")}>
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
          ) : (
            <p
              className="m-0 text-[length:var(--exits-text-xs)] text-muted"
              data-testid="explore-local-validation-note"
            >
              {t("personal.explore.localValidationNote")}
            </p>
          )}
        </>
      )}
    </div>
  );
}

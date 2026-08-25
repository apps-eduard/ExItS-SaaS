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
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { personalPageBackNav } from "@/navigation/page-back-nav";

function planFeatureLines(plan: CommercialPlanDto, t: (key: MessageKey) => string): string[] {
  const lines = [
    t("personal.explore.featureBranches").replace("{count}", String(plan.maxBranches)),
    t("personal.explore.featureStaff").replace("{count}", String(plan.maxActiveStaff)),
    t("personal.explore.featureDevices").replace("{count}", String(plan.maxActivePosDevices)),
    t("personal.explore.featureBusinessTypes").replace(
      "{count}",
      String(plan.maxActiveBusinessTypes),
    ),
  ];
  if (plan.customerCreditEnabled) lines.push(t("personal.explore.featureCredit"));
  if (plan.advancedReportsEnabled) lines.push(t("personal.explore.featureReports"));
  if (plan.exportEnabled) lines.push(t("personal.explore.featureExport"));
  if (plan.description) lines.push(plan.description);
  return lines;
}

export function PersonalExplorePosPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const localValidation = isFrontendLocalValidationMode();
  const plansQuery = useQuery({
    queryKey: ["commercial", "plans", "pinoy-business-pos"],
    queryFn: ({ signal }) => listCommercialPlans(undefined, signal),
  });

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

  const plans = plansQuery.data;

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
          <ul className="exits-list m-0 grid list-none gap-2 p-0" role="list">
            {plans.map((plan) => {
              const trialAvailable = plan.trialAllowed && plan.defaultTrialDays > 0;
              const planKey = plan.planKey ?? plan.code;
              return (
                <li key={plan.id}>
                <article
                  className="exits-list__card flex flex-col gap-2 p-4"
                  data-testid={`explore-plan-${planKey}`}
                >
                  <header className="mb-2 flex flex-wrap items-baseline justify-between gap-2">
                    <h2 className="m-0 text-[length:var(--exits-text-lg)] font-semibold">
                      {plan.displayName}
                    </h2>
                    <span className="text-[length:var(--exits-text-xs)] text-muted">
                      {trialAvailable
                        ? t("personal.explore.trialDays").replace(
                            "{days}",
                            String(plan.defaultTrialDays),
                          )
                        : t("personal.explore.noTrial")}
                    </span>
                  </header>
                  <p className="m-0 text-[length:var(--exits-text-base)] font-semibold">
                    {plan.monthlyPrice.toLocaleString()} {plan.currencyCode} /{" "}
                    {t("personal.explore.billingMonth")}
                  </p>
                  <p className="m-0 mt-0.5 text-[length:var(--exits-text-sm)] text-muted">
                    {plan.annualPrice.toLocaleString()} {plan.currencyCode} /{" "}
                    {t("personal.explore.billingYear")}
                  </p>
                  <ul className="mt-3 list-disc space-y-1 pl-5 text-[length:var(--exits-text-sm)] text-muted">
                    {planFeatureLines(plan, t).map((line) => (
                      <li key={line}>{line}</li>
                    ))}
                  </ul>
                  <div className="mt-4 flex flex-wrap gap-2">
                    {trialAvailable ? (
                      <Button
                        type="button"
                        className="min-h-11"
                        data-testid={`explore-start-trial-${planKey}`}
                        onClick={() =>
                          navigate(
                            `/personal/start-business?planKey=${encodeURIComponent(planKey)}&trial=1&payNow=0&billing=Monthly`,
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
                            `/personal/start-business?planKey=${encodeURIComponent(planKey)}&trial=0&payNow=1&billing=Monthly`,
                          )
                        }
                      >
                        {t("personal.explore.subscribe")}
                      </Button>
                    ) : null}
                    {!trialAvailable && !localValidation ? (
                      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                        {t("personal.explore.paymentUnavailable")}
                      </p>
                    ) : null}
                  </div>
                </article>
                </li>
              );
            })}
          </ul>
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

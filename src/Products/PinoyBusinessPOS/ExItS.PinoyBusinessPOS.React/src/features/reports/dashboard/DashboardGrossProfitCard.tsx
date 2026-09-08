import type { ReactNode } from "react";
import { Link } from "react-router-dom";
import type { PosProfitabilityReportDto } from "@/api/pos/pos-reporting-client";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { LoadingState } from "@/components/exits/LoadingState";
import { DashboardPanel } from "@/features/reports/dashboard/DashboardToolbar";
import { GrossMarginRadial } from "@/features/reports/dashboard/RadialKpis";
import { productionCostStatusLabelKey } from "@/features/inventory/production-labels";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";

function CogsStatusBadge({ status }: { status: string }) {
  const { t } = useI18n();
  const tone =
    status === "Complete" ? "complete" : status === "Partial" ? "partial" : "unavailable";
  return (
    <span
      className={cn(
        "inline-flex items-center whitespace-nowrap rounded-full px-2 py-0.5 text-[0.68rem] font-medium leading-tight",
        tone === "complete" &&
          "bg-[color-mix(in_srgb,var(--exits-success,#15803d)_14%,transparent)] text-[var(--exits-success,#15803d)]",
        tone === "partial" && "bg-[color-mix(in_srgb,#f59e0b_18%,transparent)] text-[#b45309]",
        tone === "unavailable" &&
          "bg-[color-mix(in_srgb,var(--exits-surface-muted)_90%,transparent)] text-muted",
      )}
      data-testid="dashboard-gross-profit-status"
      data-cogs-status={status}
    >
      {t(productionCostStatusLabelKey(status))}
    </span>
  );
}

function MetricRow({
  label,
  children,
  testId,
}: {
  label: string;
  children: ReactNode;
  testId?: string;
}) {
  return (
    <div
      className="flex items-baseline justify-between gap-3 text-[length:var(--exits-text-sm)]"
      data-testid={testId}
    >
      <span className="text-muted">{label}</span>
      <span className="tabular-nums">{children}</span>
    </div>
  );
}

export function DashboardGrossProfitCard({
  data,
  loading,
  scopeLabel,
  animationKey,
}: {
  data: PosProfitabilityReportDto | undefined;
  loading: boolean;
  scopeLabel: string;
  animationKey: string;
}) {
  const { t } = useI18n();

  const badge = data ? <CogsStatusBadge status={data.cogsStatus} /> : null;

  return (
    <DashboardPanel
      title={t("dashboard.grossProfit")}
      scopeLabel={scopeLabel}
      scopeTestId="scope-gross-margin"
      testId="dashboard-gross-profit"
      badge={badge}
    >
      {loading && !data ? (
        <LoadingState label={t("reports.loading")} />
      ) : data ? (
        <div className="flex flex-col gap-2" data-testid="dashboard-gross-profit-body">
          {data.cogsStatus === "Complete" &&
          data.grossProfit != null &&
          data.grossMarginPercent != null &&
          data.totalCogs != null ? (
            <>
              <GrossMarginRadial
                marginPercent={data.grossMarginPercent}
                grossProfit={data.grossProfit}
                revenue={data.netSales}
                marginLabel={t("dashboard.grossMargin")}
                profitLabel={t("dashboard.grossProfit")}
                animationKey={animationKey}
              />
              <div className="mt-2 flex flex-col gap-1.5" data-testid="dashboard-gross-profit-complete">
                <MetricRow label={t("reports.metric.netSales")}>
                  <MoneyDisplay amount={data.netSales} />
                </MetricRow>
                <MetricRow label={t("reports.metric.cogs")}>
                  <MoneyDisplay amount={data.totalCogs} />
                </MetricRow>
                <MetricRow
                  label={t("dashboard.costCoverage")}
                  testId="dashboard-gross-profit-coverage"
                >
                  {Math.round(data.costCompletenessPercent)}%
                </MetricRow>
              </div>
            </>
          ) : null}

          {data.cogsStatus === "Partial" ? (
            <div className="flex flex-col gap-2" data-testid="dashboard-gross-profit-partial">
              <p className="m-0 text-[length:var(--exits-text-md)] font-semibold">
                {t("dashboard.grossProfitCostIncomplete")}
              </p>
              <div className="flex flex-col gap-1.5">
                <MetricRow label={t("reports.metric.knownCogs")}>
                  <MoneyDisplay amount={data.knownCogs} />
                </MetricRow>
                <MetricRow
                  label={t("dashboard.costCoverage")}
                  testId="dashboard-gross-profit-coverage"
                >
                  {Math.round(data.costCompletenessPercent)}%
                </MetricRow>
                <MetricRow
                  label={t("dashboard.salesCosted")}
                  testId="dashboard-gross-profit-sales-costed"
                >
                  {t("dashboard.salesCostedCount")
                    .replace("{complete}", String(data.completeCostSaleCount))
                    .replace("{total}", String(data.completedSaleCount))}
                </MetricRow>
                {data.partialCostSaleCount + data.unavailableCostSaleCount > 0 ? (
                  <MetricRow
                    label={t("dashboard.incompleteCostSales")}
                    testId="dashboard-gross-profit-incomplete-count"
                  >
                    {data.partialCostSaleCount + data.unavailableCostSaleCount}
                  </MetricRow>
                ) : null}
              </div>
              <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
                {t("dashboard.grossProfitAppearsWhenComplete")}
              </p>
            </div>
          ) : null}

          {data.cogsStatus === "Unavailable" ? (
            <div className="flex flex-col gap-2" data-testid="dashboard-gross-profit-unavailable">
              <p className="m-0 text-[length:var(--exits-text-md)] font-semibold">
                {t("dashboard.grossProfitCostUnavailable")}
              </p>
              <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
                {t("dashboard.grossProfitCostUnavailableDetail")}
              </p>
              <div className="flex flex-col gap-1.5">
                <MetricRow label={t("reports.metric.netSales")}>
                  <MoneyDisplay amount={data.netSales} />
                </MetricRow>
                <MetricRow
                  label={t("dashboard.costCoverage")}
                  testId="dashboard-gross-profit-coverage"
                >
                  {Math.round(data.costCompletenessPercent)}%
                </MetricRow>
              </div>
            </div>
          ) : null}

          <div className="mt-2">
            <Link
              to="/reports/operational/profitability"
              className="dashboard-chart-link text-[length:var(--exits-text-sm)]"
              data-testid="dashboard-view-profitability"
            >
              {t("dashboard.viewProfitability")}
            </Link>
          </div>
        </div>
      ) : (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
          {t("reports.loadError")}
        </p>
      )}
    </DashboardPanel>
  );
}

import { useMemo } from "react";
import {
  PolarAngleAxis,
  RadialBar,
  RadialBarChart,
  ResponsiveContainer,
} from "recharts";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { Link } from "react-router-dom";
import {
  CHART_INTRO_MS,
  readDashboardChartTheme,
} from "@/features/reports/dashboard/chart-theme";
import { DashboardQuietEmpty } from "@/features/reports/dashboard/DashboardToolbar";
import { usePrefersReducedMotion } from "@/features/reports/dashboard/useChartMotion";

export function UtangOverdueRadial({
  outstanding,
  overdue,
  overdueLabel,
  outstandingLabel,
  ofLabel,
  clearTitle,
  clearDetail,
  animationKey,
  customersHref,
}: {
  outstanding: number;
  overdue: number;
  overdueLabel: string;
  outstandingLabel: string;
  ofLabel: string;
  clearTitle: string;
  clearDetail?: string;
  animationKey: string;
  customersHref?: string;
}) {
  const reduced = usePrefersReducedMotion();
  const theme = useMemo(() => readDashboardChartTheme(), []);
  const pct =
    outstanding > 0
      ? Math.min(100, Math.round((Math.max(0, overdue) / outstanding) * 100))
      : 0;
  const data = useMemo(
    () => [{ name: "overdue", value: pct, fill: pct > 0 ? theme.danger : theme.success }],
    [pct, theme.danger, theme.success],
  );

  if (outstanding <= 0) {
    return (
      <DashboardQuietEmpty
        title={clearTitle}
        detail={clearDetail}
        testId="dashboard-utang-clear"
      />
    );
  }

  const body = (
    <div className="dashboard-radial" data-testid="dashboard-utang-radial" key={animationKey}>
      <div className="dashboard-radial__chart">
        <ResponsiveContainer width="100%" height="100%" minHeight={160}>
          <RadialBarChart
            cx="50%"
            cy="50%"
            innerRadius="68%"
            outerRadius="100%"
            data={data}
            startAngle={90}
            endAngle={-270}
          >
            <PolarAngleAxis type="number" domain={[0, 100]} tick={false} />
            <RadialBar
              background={{ fill: theme.border }}
              dataKey="value"
              cornerRadius={8}
              isAnimationActive={reduced ? false : "auto"}
              animationDuration={CHART_INTRO_MS}
              animationEasing="ease-out"
            />
          </RadialBarChart>
        </ResponsiveContainer>
        <div className="dashboard-radial__center" aria-hidden>
          <span className="dashboard-radial__pct">{pct}%</span>
          <span className="dashboard-radial__pct-label">{overdueLabel}</span>
        </div>
      </div>
      <div className="dashboard-radial__meta">
        <p className="m-0 dashboard-radial__amounts">
          <MoneyDisplay amount={overdue} />{" "}
          <span className="text-muted">
            {ofLabel} <MoneyDisplay amount={outstanding} />
          </span>
        </p>
        <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">{outstandingLabel}</p>
      </div>
    </div>
  );

  if (customersHref) {
    return (
      <Link to={customersHref} className="dashboard-chart-link">
        {body}
      </Link>
    );
  }

  return body;
}

export function GrossMarginRadial({
  marginPercent,
  grossProfit,
  revenue,
  marginLabel,
  profitLabel,
  animationKey,
}: {
  marginPercent: number;
  grossProfit: number;
  revenue: number;
  marginLabel: string;
  profitLabel: string;
  animationKey: string;
}) {
  const reduced = usePrefersReducedMotion();
  const theme = useMemo(() => readDashboardChartTheme(), []);
  const clamped = Math.max(0, Math.min(100, marginPercent));
  const data = useMemo(
    () => [{ name: "margin", value: clamped, fill: theme.primary }],
    [clamped, theme.primary],
  );

  return (
    <div className="dashboard-radial" data-testid="dashboard-gross-margin-radial" key={animationKey}>
      <div className="dashboard-radial__chart">
        <ResponsiveContainer width="100%" height="100%" minHeight={160}>
          <RadialBarChart
            cx="50%"
            cy="50%"
            innerRadius="68%"
            outerRadius="100%"
            data={data}
            startAngle={90}
            endAngle={-270}
          >
            <PolarAngleAxis type="number" domain={[0, 100]} tick={false} />
            <RadialBar
              background={{ fill: theme.border }}
              dataKey="value"
              cornerRadius={8}
              isAnimationActive={reduced ? false : "auto"}
              animationDuration={CHART_INTRO_MS}
              animationEasing="ease-out"
            />
          </RadialBarChart>
        </ResponsiveContainer>
        <div className="dashboard-radial__center" aria-hidden>
          <span className="dashboard-radial__pct">{clamped.toFixed(clamped % 1 === 0 ? 0 : 1)}%</span>
          <span className="dashboard-radial__pct-label">{marginLabel}</span>
        </div>
      </div>
      <div className="dashboard-radial__meta">
        <p className="m-0 dashboard-radial__amounts">
          <MoneyDisplay amount={grossProfit} />
        </p>
        <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
          {profitLabel} · <MoneyDisplay amount={revenue} />
        </p>
      </div>
    </div>
  );
}

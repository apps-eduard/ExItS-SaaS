import { useMemo } from "react";
import {
  Area,
  AreaChart,
  CartesianGrid,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import { EmptyState } from "@/components/exits/EmptyState";
import { formatPeso } from "@/lib/format-money";
import { CHART_INTRO_MS, readDashboardChartTheme } from "@/features/reports/dashboard/chart-theme";
import { usePrefersReducedMotion } from "@/features/reports/dashboard/useChartMotion";

export type SalesTrendPoint = {
  date: string;
  label: string;
  amount: number;
  count: number;
};

function shortDayLabel(date: string): string {
  const parts = date.split("-");
  return parts[2] ?? date;
}

export function SalesTrendAreaChart({
  points,
  emptyTitle,
  emptyDetail,
  animationKey,
  ariaLabel,
}: {
  points: ReadonlyArray<{ date: string; amount: number; count: number }>;
  emptyTitle: string;
  emptyDetail: string;
  animationKey: string;
  ariaLabel: string;
}) {
  const reduced = usePrefersReducedMotion();
  const theme = useMemo(() => readDashboardChartTheme(), []);

  const data = useMemo<SalesTrendPoint[]>(
    () =>
      points.map((p) => ({
        date: p.date,
        label: shortDayLabel(p.date),
        amount: p.amount,
        count: p.count,
      })),
    [points],
  );

  if (data.length === 0) {
    return <EmptyState title={emptyTitle} detail={emptyDetail} />;
  }

  return (
    <div
      className="dashboard-rechart"
      data-testid="dashboard-sales-trend-chart"
      role="img"
      aria-label={ariaLabel}
      key={animationKey}
    >
      <ResponsiveContainer width="100%" height="100%" minHeight={220}>
        <AreaChart data={data} margin={{ top: 8, right: 8, left: 0, bottom: 0 }}>
          <defs>
            <linearGradient id={`salesFill-${animationKey}`} x1="0" y1="0" x2="0" y2="1">
              <stop offset="0%" stopColor={theme.primary} stopOpacity={0.28} />
              <stop offset="100%" stopColor={theme.primary} stopOpacity={0.02} />
            </linearGradient>
          </defs>
          <CartesianGrid strokeDasharray="3 3" vertical={false} stroke={theme.border} />
          <XAxis
            dataKey="label"
            tick={{ fill: theme.muted, fontSize: 11 }}
            axisLine={false}
            tickLine={false}
            minTickGap={16}
          />
          <YAxis
            tick={{ fill: theme.muted, fontSize: 11 }}
            axisLine={false}
            tickLine={false}
            width={56}
            tickFormatter={(v: number) =>
              v >= 1000 ? `${Math.round(v / 1000)}k` : String(Math.round(v))
            }
          />
          <Tooltip
            cursor={{ stroke: theme.primary, strokeOpacity: 0.35 }}
            contentStyle={{
              borderRadius: 8,
              border: `1px solid ${theme.border}`,
              background: theme.surface,
              fontSize: 12,
            }}
            formatter={(value) => [formatPeso(Number(value ?? 0)), "Sales"]}
            labelFormatter={(_, payload) => {
              const row = payload?.[0]?.payload as SalesTrendPoint | undefined;
              if (!row) {
                return "";
              }
              return `${row.date} · ${row.count} txn${row.count === 1 ? "" : "s"}`;
            }}
          />
          <Area
            type="monotone"
            dataKey="amount"
            stroke={theme.primary}
            strokeWidth={2.25}
            fill={`url(#salesFill-${animationKey})`}
            isAnimationActive={reduced ? false : "auto"}
            animationDuration={CHART_INTRO_MS}
            animationEasing="ease-out"
            dot={false}
            activeDot={{ r: 4, strokeWidth: 0, fill: theme.primary }}
          />
        </AreaChart>
      </ResponsiveContainer>
    </div>
  );
}

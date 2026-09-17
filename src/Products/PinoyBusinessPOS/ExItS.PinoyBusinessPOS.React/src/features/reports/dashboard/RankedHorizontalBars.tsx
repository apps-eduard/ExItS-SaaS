import { useMemo } from "react";
import {
  Bar,
  BarChart,
  Cell,
  LabelList,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import { formatPeso } from "@/lib/format-money";
import {
  CHART_INTRO_MS,
  readDashboardChartTheme,
} from "@/features/reports/dashboard/chart-theme";
import { DashboardQuietEmpty } from "@/features/reports/dashboard/DashboardToolbar";
import { usePrefersReducedMotion } from "@/features/reports/dashboard/useChartMotion";

export type RankedBarRow = {
  id: string;
  name: string;
  value: number;
  display?: string;
};

export function RankedHorizontalBars({
  rows,
  emptyTitle,
  emptyDetail,
  animationKey,
  valueFormatter = formatPeso,
  testId = "dashboard-ranked-bars",
  emptyTestId,
  ariaLabel,
}: {
  rows: ReadonlyArray<RankedBarRow>;
  emptyTitle: string;
  emptyDetail?: string;
  animationKey: string;
  valueFormatter?: (value: number) => string;
  testId?: string;
  emptyTestId?: string;
  ariaLabel: string;
}) {
  const reduced = usePrefersReducedMotion();
  const theme = useMemo(() => readDashboardChartTheme(), []);

  const data = useMemo(
    () =>
      rows.slice(0, 8).map((r) => ({
        ...r,
        shortName: r.name.length > 18 ? `${r.name.slice(0, 16)}…` : r.name,
        labelText: r.display ?? valueFormatter(r.value),
      })),
    [rows, valueFormatter],
  );

  if (data.length === 0) {
    return (
      <DashboardQuietEmpty
        title={emptyTitle}
        detail={emptyDetail}
        testId={emptyTestId ?? `${testId}-empty`}
      />
    );
  }

  const height = Math.max(140, data.length * 34 + 16);

  return (
    <div
      className="dashboard-rechart dashboard-rechart--ranked"
      style={{ height }}
      data-testid={testId}
      role="img"
      aria-label={ariaLabel}
      key={animationKey}
    >
      <ResponsiveContainer width="100%" height="100%">
        <BarChart
          data={data}
          layout="vertical"
          margin={{ top: 4, right: 64, left: 4, bottom: 4 }}
        >
          <XAxis type="number" hide domain={[0, "dataMax"]} />
          <YAxis
            type="category"
            dataKey="shortName"
            width={108}
            tick={{ fill: theme.muted, fontSize: 11 }}
            axisLine={false}
            tickLine={false}
          />
          <Tooltip
            formatter={(value) => valueFormatter(Number(value ?? 0))}
            labelFormatter={(_, payload) => {
              const row = payload?.[0]?.payload as RankedBarRow | undefined;
              return row?.name ?? "";
            }}
            contentStyle={{
              borderRadius: 8,
              border: `1px solid ${theme.border}`,
              background: theme.surface,
              fontSize: 12,
            }}
          />
          <Bar
            dataKey="value"
            radius={[0, 6, 6, 0]}
            barSize={14}
            isAnimationActive={reduced ? false : "auto"}
            animationDuration={CHART_INTRO_MS}
            animationEasing="ease-out"
          >
            {data.map((row, index) => (
              <Cell key={row.id} fill={theme.primary} fillOpacity={1 - index * 0.08} />
            ))}
            <LabelList dataKey="labelText" position="right" fill={theme.text} fontSize={11} />
          </Bar>
        </BarChart>
      </ResponsiveContainer>
    </div>
  );
}

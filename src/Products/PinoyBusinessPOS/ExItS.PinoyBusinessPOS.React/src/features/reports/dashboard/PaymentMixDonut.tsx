import { useMemo, useState } from "react";
import { Cell, Pie, PieChart, ResponsiveContainer, Tooltip } from "recharts";
import { EmptyState } from "@/components/exits/EmptyState";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { formatReportPaymentMethod } from "@/api/pos/pos-reporting-client";
import { formatPeso } from "@/lib/format-money";
import { cn } from "@/lib/cn";
import {
  CHART_INTRO_MS,
  paymentMethodColor,
  readDashboardChartTheme,
} from "@/features/reports/dashboard/chart-theme";
import { usePrefersReducedMotion } from "@/features/reports/dashboard/useChartMotion";

type PaymentRow = {
  paymentMethod: string;
  amount: number;
  count: number;
};

type Slice = PaymentRow & {
  name: string;
  fill: string;
  pct: number;
};

export function PaymentMixDonut({
  rows,
  totalLabel,
  emptyTitle,
  emptyDetail,
  animationKey,
}: {
  rows: ReadonlyArray<PaymentRow>;
  totalLabel: string;
  emptyTitle: string;
  emptyDetail: string;
  animationKey: string;
}) {
  const reduced = usePrefersReducedMotion();
  const theme = useMemo(() => readDashboardChartTheme(), []);
  const [activeIndex, setActiveIndex] = useState<number | null>(null);

  const { data, total } = useMemo(() => {
    const sum = rows.reduce((acc, r) => acc + r.amount, 0);
    const mapped: Slice[] = rows.map((r) => ({
      ...r,
      name: formatReportPaymentMethod(r.paymentMethod),
      fill: paymentMethodColor(r.paymentMethod, theme),
      pct: sum > 0 ? (r.amount / sum) * 100 : 0,
    }));
    return { data: mapped, total: sum };
  }, [rows, theme]);

  if (data.length === 0 || total <= 0) {
    return <EmptyState title={emptyTitle} detail={emptyDetail} />;
  }

  return (
    <div className="dashboard-payment-mix" data-testid="dashboard-payment-mix">
      <div className="dashboard-payment-mix__chart" key={animationKey}>
        <ResponsiveContainer width="100%" height="100%" minHeight={200}>
          <PieChart>
            <Pie
              data={data}
              dataKey="amount"
              nameKey="name"
              cx="50%"
              cy="50%"
              innerRadius="58%"
              outerRadius="82%"
              paddingAngle={2}
              isAnimationActive={reduced ? false : "auto"}
              animationDuration={CHART_INTRO_MS}
              animationEasing="ease-out"
              onMouseEnter={(_, index) => setActiveIndex(index)}
              onMouseLeave={() => setActiveIndex(null)}
              onClick={(_, index) => setActiveIndex(index)}
            >
              {data.map((entry, index) => (
                <Cell
                  key={entry.paymentMethod}
                  fill={entry.fill}
                  stroke={theme.surface}
                  strokeWidth={2}
                  fillOpacity={activeIndex === null || activeIndex === index ? 1 : 0.4}
                />
              ))}
            </Pie>
            <Tooltip
              formatter={(value) => formatPeso(Number(value ?? 0))}
              contentStyle={{
                borderRadius: 8,
                border: `1px solid ${theme.border}`,
                background: theme.surface,
                fontSize: 12,
              }}
            />
          </PieChart>
        </ResponsiveContainer>
        <div className="dashboard-payment-mix__center" aria-hidden>
          <span className="dashboard-payment-mix__center-label">{totalLabel}</span>
          <MoneyDisplay amount={total} className="dashboard-payment-mix__center-value" />
        </div>
      </div>
      <ul className="dashboard-payment-mix__legend m-0 list-none p-0" data-testid="payment-breakdown">
        {data.map((row, index) => (
          <li
            key={row.paymentMethod}
            className={cn(
              "dashboard-payment-mix__legend-row",
              activeIndex === index && "dashboard-payment-mix__legend-row--active",
            )}
            data-testid={`payment-share-${row.paymentMethod}`}
            onMouseEnter={() => setActiveIndex(index)}
            onMouseLeave={() => setActiveIndex(null)}
            onFocus={() => setActiveIndex(index)}
            onBlur={() => setActiveIndex(null)}
          >
            <span
              className="dashboard-payment-mix__swatch"
              style={{ background: row.fill }}
              aria-hidden
            />
            <span className="dashboard-payment-mix__legend-label">{row.name}</span>
            <MoneyDisplay amount={row.amount} className="dashboard-payment-mix__legend-amount" />
            <span className="dashboard-payment-mix__legend-pct">{Math.round(row.pct)}%</span>
          </li>
        ))}
      </ul>
    </div>
  );
}
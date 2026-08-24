import type { ReactNode } from "react";
import type { LucideIcon } from "lucide-react";
import { ArrowDownRight, ArrowUpRight, Minus } from "lucide-react";
import { cn } from "@/lib/cn";

export type DashboardMetricTone = "default" | "emphasis" | "attention" | "success";

export type PeriodComparisonFacts = {
  absoluteChange?: number | null;
  percentageChange?: number | null;
  percentageAvailable: boolean;
  comparisonFromDate: string;
  comparisonToDate: string;
};

export function DashboardHeroMetric({
  label,
  children,
  meta,
  trend,
  testId,
  className,
}: {
  label: string;
  children: ReactNode;
  meta?: ReactNode;
  trend?: ReactNode;
  testId: string;
  className?: string;
}) {
  return (
    <article
      className={cn("dashboard-hero-metric exits-animate-panel", className)}
      data-testid={testId}
      role="listitem"
    >
      <span className="dashboard-hero-metric__label">{label}</span>
      <div className="dashboard-hero-metric__value">{children}</div>
      {meta ? <div className="dashboard-hero-metric__meta">{meta}</div> : null}
      {trend ? <div className="dashboard-hero-metric__trend">{trend}</div> : null}
    </article>
  );
}

export function DashboardMetricCard({
  label,
  children,
  meta,
  icon: Icon,
  tone = "default",
  testId,
  className,
}: {
  label: string;
  children: ReactNode;
  meta?: ReactNode;
  icon?: LucideIcon;
  tone?: DashboardMetricTone;
  testId: string;
  className?: string;
}) {
  return (
    <article
      className={cn(
        "dashboard-metric-card",
        tone === "emphasis" && "dashboard-metric-card--emphasis",
        tone === "attention" && "dashboard-metric-card--attention",
        tone === "success" && "dashboard-metric-card--success",
        className,
      )}
      data-testid={testId}
      role="listitem"
    >
      <div className="dashboard-metric-card__head">
        {Icon ? (
          <span className="dashboard-metric-card__icon" aria-hidden>
            <Icon />
          </span>
        ) : null}
        <span className="dashboard-metric-card__label">{label}</span>
      </div>
      <div className="dashboard-metric-card__value">{children}</div>
      {meta ? <div className="dashboard-metric-card__meta">{meta}</div> : null}
    </article>
  );
}

export function DashboardComparisonTrend({
  comparison,
  absoluteLabel,
  pctUnavailableLabel,
  vsPriorLabel,
}: {
  comparison: PeriodComparisonFacts;
  absoluteLabel: ReactNode;
  pctUnavailableLabel: string;
  vsPriorLabel: string;
}) {
  const pct = comparison.percentageChange;
  const available = comparison.percentageAvailable && pct != null;
  const absolute = comparison.absoluteChange ?? 0;
  const direction = absolute > 0 ? "up" : absolute < 0 ? "down" : "flat";
  const Icon = direction === "up" ? ArrowUpRight : direction === "down" ? ArrowDownRight : Minus;

  return (
    <div
      className={cn(
        "dashboard-trend",
        direction === "up" && "dashboard-trend--up",
        direction === "down" && "dashboard-trend--down",
        direction === "flat" && "dashboard-trend--flat",
      )}
      data-testid="dashboard-comparison-trend"
    >
      <span className="dashboard-trend__icon" aria-hidden>
        <Icon />
      </span>
      <span className="dashboard-trend__copy">
        <span className="dashboard-trend__absolute">{absoluteLabel}</span>
        {available ? (
          <span className="dashboard-trend__pct">
            {pct! > 0 ? "+" : ""}
            {pct!.toFixed(pct! % 1 === 0 ? 0 : 1)}%
          </span>
        ) : (
          <span className="dashboard-trend__pct dashboard-trend__pct--muted">{pctUnavailableLabel}</span>
        )}
        <span className="dashboard-trend__vs">
          {vsPriorLabel
            .replace("{from}", comparison.comparisonFromDate)
            .replace("{to}", comparison.comparisonToDate)}
        </span>
      </span>
    </div>
  );
}

export function DashboardSparkBars({
  points,
  ariaLabel,
  emptyLabel,
  testId = "dashboard-spark-bars",
}: {
  points: ReadonlyArray<{ key: string; value: number; title: string }>;
  ariaLabel: string;
  emptyLabel: string;
  testId?: string;
}) {
  if (points.length === 0) {
    return (
      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted" data-testid={`${testId}-empty`}>
        {emptyLabel}
      </p>
    );
  }

  const max = Math.max(...points.map((p) => p.value), 0);
  return (
    <div
      className="dashboard-spark"
      role="img"
      aria-label={ariaLabel}
      data-testid={testId}
    >
      {points.map((point) => {
        const ratio = max <= 0 ? 0 : point.value / max;
        const height = `${Math.max(ratio * 100, point.value > 0 ? 8 : 2)}%`;
        return (
          <div key={point.key} className="dashboard-spark__col" title={point.title}>
            <span className="dashboard-spark__bar" style={{ height }} />
            <span className="dashboard-spark__tick">{point.key}</span>
          </div>
        );
      })}
    </div>
  );
}

export function DashboardShareRow({
  label,
  meta,
  amount,
  share,
  testId,
}: {
  label: string;
  meta?: string;
  amount: ReactNode;
  share: number;
  testId?: string;
}) {
  const clamped = Math.max(0, Math.min(share, 1));
  return (
    <li className="dashboard-share-row" data-testid={testId}>
      <div className="dashboard-share-row__top">
        <div className="min-w-0">
          <strong className="dashboard-share-row__label">{label}</strong>
          {meta ? <p className="dashboard-share-row__meta m-0">{meta}</p> : null}
        </div>
        <div className="dashboard-share-row__amount">{amount}</div>
      </div>
      <div
        className="dashboard-share-row__track"
        role="progressbar"
        aria-valuemin={0}
        aria-valuemax={100}
        aria-valuenow={Math.round(clamped * 100)}
        aria-label={label}
      >
        <span className="dashboard-share-row__fill" style={{ width: `${clamped * 100}%` }} />
      </div>
    </li>
  );
}

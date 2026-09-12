import type { ReactNode } from "react";
import type { LucideIcon } from "lucide-react";
import { ChevronRight } from "lucide-react";
import { Link } from "react-router-dom";
import { cn } from "@/lib/cn";

export function ManagerHomeSection({
  title,
  children,
  testId,
}: {
  title: string;
  children: ReactNode;
  testId?: string;
}) {
  return (
    <section
      className="manager-ops-home__section catalog-form-section exits-animate-panel flex min-w-0 flex-col gap-2"
      data-testid={testId}
    >
      <h2 className="catalog-form-section__title m-0 text-muted">{title}</h2>
      {children}
    </section>
  );
}

export type ManagerMetricTone = "default" | "primary" | "attention" | "success" | "info" | "warning";

export function ManagerMetricCard({
  label,
  value,
  hint,
  badge,
  icon: Icon,
  testId,
  tone = "default",
  valueScale = "kpi",
  to,
}: {
  label: string;
  value?: ReactNode;
  hint?: string;
  /** Optional top-right status (e.g. Open chip on Shift). */
  badge?: ReactNode;
  icon?: LucideIcon;
  testId?: string;
  tone?: ManagerMetricTone;
  /** `kpi` = sales-scale; `restrained` = Shift/Register (~text-xl, weight 600). */
  valueScale?: "kpi" | "restrained";
  /** When set, the metric cell is a navigable link. */
  to?: string;
}) {
  const classes = cn(
    "manager-metric-cell",
    tone === "primary" && "manager-metric-cell--primary",
    tone === "attention" && "manager-metric-cell--attention",
    tone === "success" && "manager-metric-cell--success",
    tone === "info" && "manager-metric-cell--info",
    tone === "warning" && "manager-metric-cell--warning",
    to && "manager-metric-cell--clickable no-underline text-inherit",
  );

  const content = (
    <>
      <div className="manager-metric-cell__top">
        <span className="manager-metric-cell__label">
          {Icon ? <Icon className="manager-metric-cell__icon" aria-hidden /> : null}
          {label}
        </span>
        {badge ? <span className="manager-metric-cell__badge shrink-0">{badge}</span> : null}
      </div>
      {value != null && value !== "" ? (
        <span
          className={cn(
            "manager-metric-cell__value m-0 text-foreground",
            valueScale === "restrained"
              ? "manager-metric-value--restrained"
              : "exits-type-kpi manager-metric-value--kpi",
          )}
        >
          {value}
        </span>
      ) : null}
      {hint ? (
        <span className="manager-metric-cell__hint m-0 text-[length:var(--exits-text-xs)] font-normal text-muted">
          {hint}
        </span>
      ) : null}
    </>
  );

  if (to) {
    return (
      <Link to={to} className={classes} data-testid={testId} data-value-scale={valueScale}>
        {content}
      </Link>
    );
  }

  return (
    <div className={classes} data-testid={testId} data-value-scale={valueScale}>
      {content}
    </div>
  );
}

export function ManagerMetricStrip({ children }: { children: ReactNode }) {
  return (
    <div className="manager-metric-strip min-w-0" role="group">
      {children}
    </div>
  );
}

export type ManagerActionCardProps = {
  label: string;
  /** Optional secondary line under the label (e.g. "View my shift history"). */
  detail?: string;
  icon: LucideIcon;
  testId?: string;
  /** Quieter styling for secondary Insights cards. */
  quiet?: boolean;
  badge?: ReactNode;
} & ({ to: string; onClick?: never } | { to?: never; onClick: () => void });

/**
 * Neutral surface action/nav card: [icon] label … [ChevronRight]
 * Start selling uses the same family (no solid primary fill).
 */
export function ManagerActionCard(props: ManagerActionCardProps) {
  const { label, detail, icon: Icon, testId, quiet = false, badge } = props;
  const classes = cn(
    "manager-action-card inline-flex w-full min-w-0 items-center gap-2 border border-[var(--exits-border)] bg-[var(--exits-surface)] px-3 py-2 text-left no-underline text-foreground",
    "rounded-[var(--exits-radius-md)] transition-[background-color,border-color] duration-[var(--exits-motion-fast)]",
    "hover:border-[color-mix(in_srgb,var(--exits-primary)_28%,var(--exits-border))] hover:bg-[color-mix(in_srgb,var(--exits-primary-soft)_45%,var(--exits-surface))]",
    "focus-visible:outline-none focus-visible:border-[var(--exits-primary)] focus-visible:shadow-[0_0_0_1px_color-mix(in_srgb,var(--exits-primary)_28%,transparent)]",
    quiet && "manager-action-card--quiet",
  );

  const content = (
    <>
      <span
        className="manager-action-card__icon inline-flex size-5 shrink-0 items-center justify-center text-[var(--exits-primary)]"
        aria-hidden
      >
        <Icon className="size-5" />
      </span>
      <span className="manager-action-card__copy min-w-0 flex-1">
        <span className="manager-action-card__label block wrap-break-word text-[length:var(--exits-text-sm)] font-medium">
          {label}
        </span>
        {detail ? (
          <span className="manager-action-card__detail mt-0.5 block wrap-break-word text-[length:var(--exits-text-xs)] font-normal text-muted">
            {detail}
          </span>
        ) : null}
      </span>
      {badge != null ? (
        <span
          className="inline-flex min-w-5 shrink-0 items-center justify-center rounded-full bg-[var(--exits-primary)] px-1.5 text-[length:var(--exits-text-xs)] font-semibold text-primary-foreground"
          data-testid={testId ? `${testId}-badge` : undefined}
        >
          {badge}
        </span>
      ) : null}
      <ChevronRight
        className="manager-action-card__chevron size-4 shrink-0 text-muted"
        aria-hidden
      />
    </>
  );

  if ("to" in props && props.to) {
    return (
      <Link to={props.to} data-testid={testId} className={classes}>
        {content}
      </Link>
    );
  }

  return (
    <button type="button" data-testid={testId} className={classes} onClick={props.onClick}>
      {content}
    </button>
  );
}

export function ManagerActionGrid({ children }: { children: ReactNode }) {
  return (
    <div
      className="manager-action-grid grid min-w-0 grid-cols-1 gap-1.5 sm:grid-cols-2 lg:grid-cols-3"
      role="group"
    >
      {children}
    </div>
  );
}

export function ManagerAttentionLink({
  title,
  detail,
  href,
  testId,
}: {
  title: string;
  detail: string;
  href: string;
  testId: string;
}) {
  return (
    <Link
      to={href}
      className="exits-alert-surface manager-nav-row flex min-w-0 items-center justify-between gap-2 px-3 py-2 no-underline"
      data-testid={testId}
    >
      <span className="min-w-0">
        <span className="block font-medium text-foreground">{title}</span>
        <span className="block text-[length:var(--exits-text-sm)] text-muted">{detail}</span>
      </span>
      <ChevronRight className="size-4 shrink-0 text-muted" aria-hidden />
    </Link>
  );
}

export function ManagerHealthyAttention({ title, detail }: { title: string; detail: string }) {
  return (
    <div
      className="exits-alert-surface--success exits-alert-surface flex min-w-0 flex-col gap-0.5 px-3 py-2"
      data-testid="manager-attention-healthy"
    >
      <span className="font-medium text-foreground">{title}</span>
      <span className="text-[length:var(--exits-text-sm)] text-muted">{detail}</span>
    </div>
  );
}

export function ManagerSnapshotLink({
  title,
  detail,
  href,
  testId,
  icon: Icon,
  tone = "default",
}: {
  title: string;
  detail: string;
  href: string;
  testId: string;
  icon: LucideIcon;
  /** When snapshot helper indicates attention (e.g. low stock), use warning accent. */
  tone?: "default" | "attention";
}) {
  return (
    <Link
      to={href}
      className={cn(
        "manager-snapshot-card exits-card exits-card--interactive flex min-w-0 items-start gap-2.5 px-3 py-2.5 no-underline text-inherit",
        tone === "attention" && "manager-snapshot-card--attention",
      )}
      data-testid={testId}
      role="listitem"
      aria-label={`${title} — ${detail}`}
    >
      <span className="manager-snapshot-card__icon shrink-0" aria-hidden>
        <Icon className="size-4" strokeWidth={1.75} />
      </span>
      <span className="manager-snapshot-card__copy min-w-0 flex-1">
        <span className="manager-snapshot-card__title block text-[length:var(--exits-text-sm)] font-semibold text-foreground">
          {title}
        </span>
        <span className="manager-snapshot-card__detail mt-0.5 block text-[length:var(--exits-text-xs)] leading-snug text-muted">
          {detail}
        </span>
      </span>
      <ChevronRight
        className="manager-snapshot-card__chevron size-4 shrink-0 text-muted rtl:rotate-180"
        aria-hidden
      />
    </Link>
  );
}

export function ManagerSnapshotTable({ children }: { children: ReactNode }) {
  return (
    <div className="manager-snapshot-grid min-w-0" role="list">
      {children}
    </div>
  );
}

export function ManagerInsightCard({
  label,
  href,
  icon,
  testId,
}: {
  label: string;
  href: string;
  icon: LucideIcon;
  testId: string;
}) {
  return <ManagerActionCard label={label} to={href} icon={icon} testId={testId} quiet />;
}

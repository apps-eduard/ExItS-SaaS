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
      className="manager-ops-home__section exits-animate-panel flex min-w-0 flex-col gap-2"
      data-testid={testId}
    >
      <h2 className="exits-type-section-title m-0 text-muted">{title}</h2>
      {children}
    </section>
  );
}

export function ManagerMetricCard({
  label,
  value,
  hint,
  testId,
  tone,
  valueScale = "kpi",
}: {
  label: string;
  value: ReactNode;
  hint?: string;
  testId?: string;
  tone?: "default" | "attention" | "success";
  /** `kpi` = sales-scale; `restrained` = Shift/Register (~text-xl, weight 600). */
  valueScale?: "kpi" | "restrained";
}) {
  return (
    <div
      className={cn(
        "exits-metric-surface flex min-w-0 flex-col gap-0.5 px-3 py-2.5",
        tone === "attention" && "exits-alert-surface",
        tone === "success" && "exits-alert-surface--success",
      )}
      data-testid={testId}
      data-value-scale={valueScale}
    >
      <span className="exits-type-label m-0 text-muted">{label}</span>
      <span
        className={cn(
          "m-0 text-foreground",
          valueScale === "restrained"
            ? "manager-metric-value--restrained"
            : "exits-type-kpi",
        )}
      >
        {value}
      </span>
      {hint ? (
        <span className="m-0 text-[length:var(--exits-text-sm)] font-normal text-muted">{hint}</span>
      ) : null}
    </div>
  );
}

export type ManagerActionCardProps = {
  label: string;
  icon: LucideIcon;
  testId?: string;
  /** Quieter styling for secondary Insights cards. */
  quiet?: boolean;
} & ({ to: string; onClick?: never } | { to?: never; onClick: () => void });

/**
 * Neutral surface action/nav card: [icon] label … [ChevronRight]
 * Start selling uses the same family (no solid primary fill).
 */
export function ManagerActionCard(props: ManagerActionCardProps) {
  const { label, icon: Icon, testId, quiet = false } = props;
  const classes = cn(
    "manager-action-card inline-flex w-full min-w-0 items-center gap-2 border border-[var(--exits-border)] bg-[var(--exits-surface)] px-3 py-2.5 text-left no-underline text-foreground",
    "rounded-[var(--exits-radius-md)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
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
      <span className="manager-action-card__label min-w-0 flex-1 wrap-break-word text-[length:var(--exits-text-sm)] font-medium">
        {label}
      </span>
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
    <div className="manager-action-grid grid min-w-0 grid-cols-1 gap-2 sm:grid-cols-2" role="group">
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
      className="exits-alert-surface manager-nav-row flex min-w-0 items-center justify-between gap-2 px-3 py-2.5 no-underline"
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
      className="exits-alert-surface--success exits-alert-surface flex min-w-0 flex-col gap-0.5 px-3 py-2.5"
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
}: {
  title: string;
  detail: string;
  href: string;
  testId: string;
}) {
  return (
    <Link
      to={href}
      className="exits-metric-surface manager-nav-row flex min-w-0 items-start justify-between gap-2 px-3 py-2.5 no-underline"
      data-testid={testId}
    >
      <span className="min-w-0">
        <span className="block font-medium text-foreground">{title}</span>
        <span className="block text-[length:var(--exits-text-sm)] text-muted">{detail}</span>
      </span>
      <ChevronRight className="mt-0.5 size-4 shrink-0 text-muted" aria-hidden />
    </Link>
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

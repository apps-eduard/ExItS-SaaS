import type { ReactNode } from "react";
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
}: {
  label: string;
  value: ReactNode;
  hint?: string;
  testId?: string;
  tone?: "default" | "attention" | "success";
}) {
  return (
    <div
      className={cn(
        "exits-metric-surface flex min-w-0 flex-col gap-0.5 px-3 py-2.5",
        tone === "attention" && "exits-alert-surface",
        tone === "success" && "exits-alert-surface--success",
      )}
      data-testid={testId}
    >
      <span className="exits-type-label m-0 text-muted">{label}</span>
      <span className="exits-type-kpi m-0 text-foreground">{value}</span>
      {hint ? (
        <span className="m-0 text-[length:var(--exits-text-sm)] font-normal text-muted">{hint}</span>
      ) : null}
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
      className="exits-alert-surface flex min-w-0 items-center justify-between gap-2 px-3 py-2.5 no-underline"
      data-testid={testId}
    >
      <span className="min-w-0">
        <span className="block font-medium text-foreground">{title}</span>
        <span className="block text-[length:var(--exits-text-sm)] text-muted">{detail}</span>
      </span>
      <span className="shrink-0 text-[length:var(--exits-text-sm)] font-medium text-primary" aria-hidden>
        →
      </span>
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
      className="exits-metric-surface flex min-w-0 flex-col gap-0.5 px-3 py-2.5 no-underline"
      data-testid={testId}
    >
      <span className="font-medium text-foreground">{title}</span>
      <span className="text-[length:var(--exits-text-sm)] text-muted">{detail}</span>
    </Link>
  );
}

export function ManagerInsightLink({
  label,
  href,
  testId,
}: {
  label: string;
  href: string;
  testId: string;
}) {
  return (
    <Link
      to={href}
      className="text-[length:var(--exits-text-sm)] font-medium text-primary no-underline hover:underline"
      data-testid={testId}
    >
      {label}
    </Link>
  );
}

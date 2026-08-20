import type { ReactNode } from "react";
import { cn } from "@/lib/cn";
import { EmptyState } from "@/components/exits/EmptyState";

export function LoadingSkeleton({ count = 3, className }: { count?: number; className?: string }) {
  return (
    <div className={cn("grid gap-3", className)} aria-hidden="true" data-testid="loading-skeleton">
      {Array.from({ length: count }).map((_, index) => (
        <div
          key={index}
          className="h-16 animate-pulse rounded-[var(--exits-radius-md)] bg-[var(--exits-surface-muted)]"
        />
      ))}
    </div>
  );
}

export function AccessDeniedState({ title, detail }: { title: string; detail: string }) {
  return <EmptyState title={title} detail={detail} />;
}

export function ConflictState({ title, detail }: { title: string; detail: string }) {
  return <EmptyState title={title} detail={detail} />;
}

export function OfflineBanner({
  title,
  detail,
  offline,
}: {
  title: string;
  detail: string;
  offline: boolean;
}) {
  if (!offline) {
    return null;
  }

  return (
    <div
      className="pointer-events-none fixed inset-x-0 top-[max(0.75rem,env(safe-area-inset-top))] z-[1200] flex justify-center px-4"
      role="status"
      aria-live="polite"
      data-testid="offline-banner"
    >
      <div className="pointer-events-auto max-w-sm min-w-0 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 py-2 text-center shadow-sm">
        <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold">{title}</p>
        <p className="m-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">{detail}</p>
      </div>
    </div>
  );
}

export function FormSection({
  title,
  description,
  children,
  className,
}: {
  title: string;
  description?: string;
  children: ReactNode;
  className?: string;
}) {
  return (
    <section
      className={cn(
        "flex min-w-0 flex-col gap-3 rounded-[var(--exits-radius-md)] border border-border bg-surface p-4",
        className,
      )}
    >
      <header className="min-w-0">
        <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">{title}</h2>
        {description ? (
          <p className="m-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">{description}</p>
        ) : null}
      </header>
      <div className="flex min-w-0 flex-col gap-3">{children}</div>
    </section>
  );
}

export function StickyActionBar({
  children,
  className,
}: {
  children: ReactNode;
  className?: string;
}) {
  return (
    <div
      className={cn(
        "sticky bottom-[max(0.75rem,env(safe-area-inset-bottom))] z-20 mt-4 flex w-full items-center justify-between gap-3 rounded-[var(--exits-radius-lg)] border border-border bg-surface px-4 py-3 shadow-[0_-4px_24px_rgba(0,0,0,0.08)]",
        className,
      )}
      data-testid="sticky-action-bar"
    >
      {children}
    </div>
  );
}

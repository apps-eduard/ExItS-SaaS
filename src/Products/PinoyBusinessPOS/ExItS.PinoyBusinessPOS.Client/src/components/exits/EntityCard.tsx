import type { ReactNode } from "react";
import { cn } from "@/lib/cn";

export function EntityCard({
  title,
  subtitle,
  meta,
  trailing,
  footer,
  onClick,
  className,
  testId,
}: {
  title: ReactNode;
  subtitle?: ReactNode;
  meta?: ReactNode;
  trailing?: ReactNode;
  footer?: ReactNode;
  onClick?: () => void;
  className?: string;
  testId?: string;
}) {
  const interactive = typeof onClick === "function";
  const Comp = interactive ? "button" : "div";

  return (
    <Comp
      type={interactive ? "button" : undefined}
      data-testid={testId}
      onClick={onClick}
      className={cn(
        "flex min-h-[var(--exits-touch-target-min)] w-full min-w-0 flex-col gap-2 rounded-[var(--exits-radius-md)] border border-border bg-surface p-3 text-left",
        interactive &&
          "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring hover:border-primary",
        className,
      )}
    >
      <div className="flex min-w-0 items-start justify-between gap-3">
        <div className="min-w-0">
          <div className="truncate text-[length:var(--exits-text-sm)] font-semibold">{title}</div>
          {subtitle ? (
            <div className="mt-0.5 text-[length:var(--exits-text-xs)] text-muted">{subtitle}</div>
          ) : null}
        </div>
        {trailing}
      </div>
      {meta ? <div className="text-[length:var(--exits-text-sm)] text-muted">{meta}</div> : null}
      {footer}
    </Comp>
  );
}

export function ResponsiveEntityList({
  children,
  className,
  ariaLabel,
}: {
  children: ReactNode;
  className?: string;
  ariaLabel?: string;
}) {
  return (
    <div
      className={cn("grid min-w-0 gap-3", "grid-cols-1 md:grid-cols-2 xl:grid-cols-1", className)}
      role="list"
      aria-label={ariaLabel}
      data-testid="responsive-entity-list"
    >
      {children}
    </div>
  );
}

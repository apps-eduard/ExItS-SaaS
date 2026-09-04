import type { ReactNode } from "react";
import { cn } from "@/lib/cn";

/**
 * Lightweight wrapper around the global `.exits-entity-card` foundation.
 * Prefer semantic CSS classes directly on management list pages when layout
 * needs more structure (see BranchManagementListPage).
 */
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
        "exits-entity-card",
        interactive && "exits-entity-card--interactive",
        className,
      )}
    >
      <div className="exits-entity-card__header">
        <div className="exits-entity-card__identity">
          <div className="exits-entity-card__title-row">
            <div className="exits-entity-card__title">{title}</div>
          </div>
          {subtitle ? <div className="exits-entity-card__subtitle">{subtitle}</div> : null}
        </div>
        {trailing ? <div className="exits-entity-card__badges">{trailing}</div> : null}
      </div>
      {meta ? <div className="text-[length:var(--exits-text-sm)] text-muted">{meta}</div> : null}
      {footer ? <div className="exits-entity-card__actions">{footer}</div> : null}
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
      className={cn("grid min-w-0 gap-[var(--exits-entity-card-gap)]", "grid-cols-1", className)}
      role="list"
      aria-label={ariaLabel}
      data-testid="responsive-entity-list"
    >
      {children}
    </div>
  );
}

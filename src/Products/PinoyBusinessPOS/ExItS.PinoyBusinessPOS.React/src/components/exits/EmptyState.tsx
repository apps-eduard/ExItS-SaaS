import type { ReactNode } from "react";
import { cn } from "@/lib/cn";

export type EmptyStateVariant = "default" | "filtered" | "setup";
export type EmptyStateSize = "standard" | "compact";
export type EmptyStateAlign = "start" | "center";

export type EmptyStateProps = {
  title: string;
  /** Why / what to do next. Omit when title alone is enough. */
  detail?: string;
  /** Optional next-step control (permission-gated by caller). */
  action?: ReactNode;
  /** Optional secondary control beside/under primary. */
  secondaryAction?: ReactNode;
  /** Optional leading icon shown above the title. */
  icon?: ReactNode;
  align?: EmptyStateAlign;
  /**
   * Semantic empty kind for analytics / styling hooks.
   * default = dataset empty; filtered = no matches; setup = prerequisite missing.
   */
  variant?: EmptyStateVariant;
  size?: EmptyStateSize;
  className?: string;
  testId?: string;
};

/**
 * Canonical empty content region (APPROVED / LOCKED).
 * LOADING / ERROR / PERMISSION DENIED are not empty — use LoadingState, ErrorState, or Notice.
 */
export function EmptyState({
  title,
  detail,
  action,
  secondaryAction,
  icon,
  align = "start",
  variant = "default",
  size = "standard",
  className,
  testId,
}: EmptyStateProps) {
  const hasActions = Boolean(action || secondaryAction);
  return (
    <div
      className={cn(
        "flex flex-col gap-2 rounded-[var(--exits-radius-md)] border border-dashed border-border",
        "bg-[color-mix(in_srgb,var(--exits-surface-muted)_55%,var(--exits-surface))]",
        size === "standard" && "px-4 py-8",
        size === "compact" && "px-3 py-4",
        align === "center" && "items-center justify-center text-center",
        align === "center" && size === "standard" && "min-h-[12rem]",
        className,
      )}
      data-testid={testId ?? "exits-empty-state"}
      data-align={align}
      data-variant={variant}
      data-size={size}
    >
      {icon ? (
        <span
          className={cn(
            "mb-1 inline-flex shrink-0 items-center justify-center rounded-[var(--exits-radius-md)]",
            "bg-[color-mix(in_srgb,var(--exits-primary)_10%,var(--exits-surface))] text-primary",
            size === "standard" && "size-11",
            size === "compact" && "size-9",
          )}
          aria-hidden
        >
          {icon}
        </span>
      ) : null}
      <p className="exits-type-label m-0 text-foreground">{title}</p>
      {detail ? <p className="exits-type-muted m-0 max-w-sm">{detail}</p> : null}
      {hasActions ? (
        <div
          className={cn(
            "mt-1 flex flex-wrap gap-2",
            align === "center" && "justify-center",
          )}
        >
          {action}
          {secondaryAction}
        </div>
      ) : null}
    </div>
  );
}

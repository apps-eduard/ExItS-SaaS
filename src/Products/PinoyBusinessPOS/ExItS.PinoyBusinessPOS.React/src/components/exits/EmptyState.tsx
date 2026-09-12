import type { ReactNode } from "react";
import { cn } from "@/lib/cn";

export function EmptyState({
  title,
  detail,
  action,
  icon,
  align = "start",
}: {
  title: string;
  detail: string;
  /** Optional next-step control (permission-gated by caller). */
  action?: ReactNode;
  /** Optional leading icon shown above the title. */
  icon?: ReactNode;
  align?: "start" | "center";
}) {
  return (
    <div
      className={cn(
        "flex flex-col gap-2 rounded-[var(--exits-radius-md)] border border-dashed border-border bg-[color-mix(in_srgb,var(--exits-surface-muted)_55%,var(--exits-surface))] px-4 py-8",
        align === "center" && "min-h-[12rem] items-center justify-center text-center",
      )}
      data-testid="exits-empty-state"
      data-align={align}
    >
      {icon ? (
        <span
          className={cn(
            "mb-1 inline-flex size-11 shrink-0 items-center justify-center rounded-[var(--exits-radius-md)]",
            "bg-[color-mix(in_srgb,var(--exits-primary)_10%,var(--exits-surface))] text-primary",
          )}
          aria-hidden
        >
          {icon}
        </span>
      ) : null}
      <p className="exits-type-label m-0 text-foreground">{title}</p>
      {detail ? <p className="exits-type-muted m-0 max-w-sm">{detail}</p> : null}
      {action ? <div className="mt-1">{action}</div> : null}
    </div>
  );
}

import type { ReactNode } from "react";
import { cn } from "@/lib/cn";

export type StatusChipTone = "info" | "success" | "warning" | "danger" | "neutral";

export function StatusChip({
  children,
  tone = "info",
  className,
  icon,
}: {
  children: ReactNode;
  tone?: StatusChipTone;
  className?: string;
  /** Optional leading icon — scales via --exits-status-chip-icon-size. */
  icon?: ReactNode;
}) {
  return (
    <span
      className={cn("exits-status-chip", `exits-status-chip--${tone}`, className)}
      data-tone={tone}
    >
      {icon ? (
        <span className="exits-status-chip__icon" aria-hidden>
          {icon}
        </span>
      ) : null}
      {children}
    </span>
  );
}

/** Alias for StatusChip — same slim read-only status/attribute pill. */
export const StatusPill = StatusChip;

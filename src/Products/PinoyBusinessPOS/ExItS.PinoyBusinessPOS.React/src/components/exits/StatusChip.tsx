import type { ReactNode } from "react";
import { cn } from "@/lib/cn";
import type { ChipShape } from "@/components/exits/chip-variants";

export type StatusChipTone =
  | "info"
  | "success"
  | "warning"
  | "danger"
  | "neutral"
  /** Brand / selected emphasis — uses --exits-primary (Preferences-ready). */
  | "primary";

export type StatusChipShape = ChipShape;

export function StatusChip({
  children,
  tone = "info",
  shape = "pill",
  className,
  icon,
}: {
  children: ReactNode;
  tone?: StatusChipTone;
  /** Visual shape — independent from tone. Default remains pill for compatibility. */
  shape?: StatusChipShape;
  className?: string;
  /** Optional leading icon — scales via --exits-status-chip-icon-size / square icon token. */
  icon?: ReactNode;
}) {
  return (
    <span
      className={cn(
        "exits-status-chip",
        `exits-status-chip--${tone}`,
        shape !== "pill" && `exits-status-chip--shape-${shape}`,
        className,
      )}
      data-tone={tone}
      data-shape={shape}
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

/** Alias for StatusChip — same slim read-only status/attribute chip. */
export const StatusPill = StatusChip;

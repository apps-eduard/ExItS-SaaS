import type { ComponentPropsWithoutRef, ReactNode } from "react";
import { cn } from "@/lib/cn";

export type StatusChipTone =
  | "info"
  | "success"
  | "warning"
  | "danger"
  | "neutral"
  /** Brand / selected emphasis — uses --exits-primary (Preferences-ready). */
  | "primary";

/**
 * Geometry — independent from tone and appearance.
 * - auto: follows Preferences Control Shape via `--exits-control-radius`
 * - standard / soft / pill: fixed Control Shape vocabulary (match Button)
 * - square: special-case compact metadata (not a global Control Shape option)
 */
export type StatusChipShape = "auto" | "standard" | "soft" | "pill" | "square";

/** Fill treatment — independent from tone and shape. Soft is the locked default. */
export type StatusChipAppearance = "soft" | "outline" | "solid";

export type StatusChipProps = {
  children: ReactNode;
  tone?: StatusChipTone;
  /**
   * Visual shape — independent from tone and appearance.
   * Default remains pill for existing call-site compatibility; prefer `auto` for new code.
   */
  shape?: StatusChipShape;
  /** Soft (default) / Outline / Solid — independent from tone and shape. */
  appearance?: StatusChipAppearance;
  className?: string;
  /** Optional leading icon — scales via --exits-status-chip-icon-size / square icon token. */
  icon?: ReactNode;
} & Omit<ComponentPropsWithoutRef<"span">, "children" | "color">;

export function StatusChip({
  children,
  tone = "info",
  shape = "pill",
  appearance = "soft",
  className,
  icon,
  ...rest
}: StatusChipProps) {
  return (
    <span
      {...rest}
      className={cn(
        "exits-status-chip",
        `exits-status-chip--${tone}`,
        appearance !== "soft" && `exits-status-chip--appearance-${appearance}`,
        shape !== "pill" && `exits-status-chip--shape-${shape}`,
        className,
      )}
      data-tone={tone}
      data-appearance={appearance}
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

import type { ReactNode } from "react";
import { cn } from "@/lib/cn";
import {
  chipSurfaceVariants,
  type ChipShape,
  type ChipTone,
} from "@/components/exits/chip-variants";

export type CountChipLayout = "inline" | "split";

export type CountChipProps = {
  label: ReactNode;
  count: ReactNode;
  tone?: ChipTone;
  shape?: ChipShape;
  /** `inline` = [ Pending 3 ] · `split` = Pending [ 3 ] (both candidates). */
  layout?: CountChipLayout;
  className?: string;
};

/**
 * Compact label + count chip (PILOT — layout/shape not locked).
 */
export function CountChip({
  label,
  count,
  tone = "neutral",
  shape = "soft",
  layout = "inline",
  className,
}: CountChipProps) {
  if (layout === "split") {
    return (
      <span className={cn("inline-flex max-w-full items-center gap-1.5", className)}>
        <span className="text-[length:var(--exits-status-chip-font-size)] text-muted">{label}</span>
        <span
          className={cn(
            chipSurfaceVariants({ tone, shape }),
            "pointer-events-none cursor-default select-none tabular-nums",
          )}
          data-tone={tone}
          data-shape={shape}
          data-layout="split"
        >
          {count}
        </span>
      </span>
    );
  }

  return (
    <span
      className={cn(
        chipSurfaceVariants({ tone, shape }),
        "pointer-events-none cursor-default select-none gap-1.5",
        className,
      )}
      data-tone={tone}
      data-shape={shape}
      data-layout="inline"
    >
      <span className="min-w-0 truncate">{label}</span>
      <span className="tabular-nums font-semibold">{count}</span>
    </span>
  );
}

export type CountBadgeProps = {
  count: ReactNode;
  tone?: Extract<ChipTone, "neutral" | "primary" | "danger">;
  className?: string;
};

/**
 * Tiny count-only badge candidate (menu / notification / tab counts) — PILOT.
 * Remains round/pill by design (not square).
 */
export function CountBadge({ count, tone = "neutral", className }: CountBadgeProps) {
  return (
    <span
      className={cn(
        "inline-flex min-w-[1.25rem] shrink-0 items-center justify-center rounded-full border border-solid px-1.5",
        "h-[1.25rem] text-[0.6875rem] font-semibold leading-none tabular-nums",
        "pointer-events-none cursor-default select-none",
        tone === "neutral" &&
          "border-[color-mix(in_srgb,var(--exits-border-strong)_55%,transparent)] bg-[color-mix(in_srgb,var(--exits-surface-muted)_45%,transparent)] text-[var(--exits-text-muted)]",
        tone === "primary" &&
          "border-[color-mix(in_srgb,var(--exits-primary)_35%,transparent)] bg-[color-mix(in_srgb,var(--exits-primary)_14%,transparent)] text-[var(--exits-primary)]",
        tone === "danger" &&
          "border-[color-mix(in_srgb,var(--exits-danger)_35%,transparent)] bg-[color-mix(in_srgb,var(--exits-danger)_12%,transparent)] text-[var(--exits-danger)]",
        className,
      )}
      data-tone={tone}
      data-shape="pill"
    >
      {count}
    </span>
  );
}

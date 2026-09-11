import type { ReactNode } from "react";
import { cn } from "@/lib/cn";
import {
  chipSurfaceVariants,
  type ChipShape,
  type ChipTone,
} from "@/components/exits/chip-variants";

export type TagChipProps = {
  children: ReactNode;
  tone?: ChipTone;
  shape?: ChipShape;
  className?: string;
  icon?: ReactNode;
  title?: string;
};

/**
 * Read-only descriptive attribute chip (PILOT).
 * Not a status and not interactive. Square is the compact metadata candidate.
 */
export function TagChip({
  children,
  tone = "neutral",
  shape = "pill",
  className,
  icon,
  title,
}: TagChipProps) {
  return (
    <span
      className={cn(
        chipSurfaceVariants({ tone, shape }),
        "pointer-events-none cursor-default select-none",
        className,
      )}
      data-tone={tone}
      data-shape={shape}
      title={title}
    >
      {icon ? (
        <span
          className="inline-flex size-[var(--exits-chip-icon-size,var(--exits-status-chip-icon-size))] shrink-0 items-center justify-center [&_svg]:size-full"
          aria-hidden
        >
          {icon}
        </span>
      ) : null}
      <span className="min-w-0 truncate">{children}</span>
    </span>
  );
}

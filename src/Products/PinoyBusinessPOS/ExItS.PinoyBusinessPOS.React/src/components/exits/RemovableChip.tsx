import type { ReactNode } from "react";
import { X } from "lucide-react";
import { cn } from "@/lib/cn";
import {
  chipSurfaceVariants,
  type ChipTone,
  type FilterChipShape,
} from "@/components/exits/chip-variants";

export type RemovableChipProps = {
  children: ReactNode;
  tone?: ChipTone;
  /** Default `auto` follows Preferences Control Shape. Explicit shapes win. */
  shape?: FilterChipShape;
  className?: string;
  onRemove: () => void;
  /** Accessible name for the remove control, e.g. "Remove Branch: Main filter". */
  removeLabel: string;
  disabled?: boolean;
};

/**
 * Selected value / active filter chip with trailing remove (APPROVED / LOCKED).
 * Default shape follows global Control Shape (`auto`).
 */
export function RemovableChip({
  children,
  tone = "neutral",
  shape = "auto",
  className,
  onRemove,
  removeLabel,
  disabled,
}: RemovableChipProps) {
  const surfaceShape = shape === "auto" ? "soft" : shape;
  return (
    <span
      className={cn(
        chipSurfaceVariants({ tone, shape: surfaceShape }),
        shape === "auto" && "rounded-[var(--exits-control-radius)]",
        "pointer-events-auto max-w-[16rem] gap-1 pr-1",
        className,
      )}
      data-tone={tone}
      data-shape={shape}
    >
      <span className="min-w-0 truncate pl-0.5">{children}</span>
      <button
        type="button"
        className={cn(
          "inline-flex size-[1.125rem] shrink-0 items-center justify-center rounded-full",
          "text-current opacity-55 transition-[opacity,background-color,transform]",
          "duration-[var(--exits-motion-fast)] ease-[var(--exits-ease-standard)]",
          "hover:bg-[color-mix(in_srgb,currentColor_12%,transparent)] hover:opacity-100",
          "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--exits-ring)]",
          "active:scale-[0.96] motion-reduce:active:scale-100",
          "disabled:pointer-events-none disabled:opacity-40",
        )}
        aria-label={removeLabel}
        disabled={disabled}
        onClick={(e) => {
          e.stopPropagation();
          onRemove();
        }}
      >
        <X className="size-3" aria-hidden strokeWidth={2.25} />
      </button>
    </span>
  );
}

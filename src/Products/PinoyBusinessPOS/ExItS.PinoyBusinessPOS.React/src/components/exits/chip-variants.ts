import { cva, type VariantProps } from "class-variance-authority";

/**
 * Shared ExItS chip visual foundation (PILOT / NOT LOCKED).
 * Semantic components choose HTML roles; this only organizes tone/size classes.
 */
export const chipToneClasses = {
  neutral:
    "border-[color-mix(in_srgb,var(--exits-border-strong)_55%,transparent)] bg-[color-mix(in_srgb,var(--exits-surface-muted)_35%,transparent)] text-[var(--exits-text-muted)]",
  primary:
    "border-[color-mix(in_srgb,var(--exits-primary)_28%,transparent)] bg-[color-mix(in_srgb,var(--exits-primary)_8%,transparent)] text-[var(--exits-primary)]",
  info: "border-[color-mix(in_srgb,var(--exits-info)_28%,transparent)] bg-[color-mix(in_srgb,var(--exits-info)_5%,transparent)] text-[var(--exits-info)]",
  success:
    "border-[color-mix(in_srgb,var(--exits-success)_28%,transparent)] bg-[color-mix(in_srgb,var(--exits-success)_5%,transparent)] text-[var(--exits-success)]",
  warning:
    "border-[color-mix(in_srgb,var(--exits-warning)_28%,transparent)] bg-[color-mix(in_srgb,var(--exits-warning)_5%,transparent)] text-[var(--exits-warning)]",
  danger:
    "border-[color-mix(in_srgb,var(--exits-danger)_28%,transparent)] bg-[color-mix(in_srgb,var(--exits-danger)_5%,transparent)] text-[var(--exits-danger)]",
} as const;

export type ChipTone = keyof typeof chipToneClasses;

/** Compact read-only / tag / count surface (status-chip density tokens). */
export const chipSurfaceVariants = cva(
  [
    "inline-flex max-w-full shrink-0 items-center justify-center gap-[var(--exits-status-chip-gap)]",
    "box-border h-[var(--exits-status-chip-height)] min-h-[var(--exits-status-chip-height)]",
    "rounded-full border border-solid px-[var(--exits-status-chip-padding-x)]",
    "text-[length:var(--exits-status-chip-font-size)] font-medium leading-none",
    "transition-[background-color,border-color,color,box-shadow,opacity,transform]",
    "duration-[var(--exits-motion-fast)] ease-[var(--exits-ease-standard)]",
  ].join(" "),
  {
    variants: {
      tone: chipToneClasses,
    },
    defaultVariants: {
      tone: "neutral",
    },
  },
);

/** Interactive filter density (toolbar chip height tokens), pill by default. */
export const filterChipVariants = cva(
  [
    "group/filter-chip inline-flex max-w-full shrink-0 items-center justify-center gap-[var(--exits-chip-gap)]",
    "box-border h-[var(--exits-chip-min-height)] min-h-[var(--exits-chip-min-height)]",
    "rounded-full border border-solid px-[var(--exits-chip-padding-x)]",
    "text-[length:var(--exits-chip-font-size)] font-medium leading-none",
    "transition-[background-color,border-color,color,box-shadow,transform]",
    "duration-[var(--exits-motion-fast)] ease-[var(--exits-ease-standard)]",
    "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--exits-ring)] focus-visible:ring-offset-2 focus-visible:ring-offset-[var(--exits-bg)]",
    "disabled:pointer-events-none disabled:opacity-50 disabled:scale-100",
    "active:scale-[0.985] motion-reduce:active:scale-100",
  ].join(" "),
  {
    variants: {
      selected: {
        true: [
          "border-[color-mix(in_srgb,var(--exits-primary)_45%,var(--exits-border))]",
          "bg-[color-mix(in_srgb,var(--exits-primary)_12%,var(--exits-surface))]",
          "text-[var(--exits-primary)]",
          "hover:border-[color-mix(in_srgb,var(--exits-primary)_55%,var(--exits-border))]",
          "hover:bg-[color-mix(in_srgb,var(--exits-primary)_16%,var(--exits-surface))]",
        ].join(" "),
        false: [
          "border-border bg-surface text-foreground",
          "hover:border-[color-mix(in_srgb,var(--exits-primary)_28%,var(--exits-border))]",
          "hover:bg-[var(--exits-surface-muted)]",
        ].join(" "),
      },
    },
    defaultVariants: {
      selected: false,
    },
  },
);

export type ChipSurfaceVariantProps = VariantProps<typeof chipSurfaceVariants>;
export type FilterChipVariantProps = VariantProps<typeof filterChipVariants>;

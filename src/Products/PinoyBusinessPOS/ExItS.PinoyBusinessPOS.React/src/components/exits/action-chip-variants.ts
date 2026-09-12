import { cva, type VariantProps } from "class-variance-authority";
import type { ChipTone } from "@/components/exits/chip-variants";

/**
 * ExItS Action Chip visual foundation (PILOT).
 * Formalizes production ExitsChipBar variant="actions" with richer candidates.
 * See Docs/UI/exits-action-chip-standard.md.
 */

export type ActionChipVisual =
  | "soft"
  | "outline"
  | "ghost"
  | "solidPrimary"
  | "tintedPrimary"
  | "elevated"
  | "gradient";

export type ActionChipShape = "pill" | "soft" | "square";

export type ActionChipGroupLayout =
  | "inline"
  | "wrap"
  | "scroll"
  | "responsiveAuto"
  | "grid";

export type ActionChipTone = ChipTone;

const baseItem = [
  "exits-action-chip inline-flex max-w-full shrink-0 items-center justify-center",
  "box-border border border-solid",
  "h-[var(--exits-chip-min-height)] min-h-[var(--exits-chip-min-height)]",
  "px-[var(--exits-chip-padding-x)] gap-[var(--exits-chip-gap)]",
  "text-[length:var(--exits-chip-font-size)] font-medium leading-none",
  "whitespace-nowrap no-underline cursor-pointer",
  "transition-[background-color,border-color,color,box-shadow,transform,opacity]",
  "duration-[var(--exits-motion-fast)] ease-[var(--exits-ease-standard)]",
  "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--exits-ring)]",
  "focus-visible:ring-offset-2 focus-visible:ring-offset-[var(--exits-bg)]",
  "disabled:pointer-events-none disabled:opacity-[0.55] disabled:cursor-not-allowed",
  "active:scale-[0.985] motion-reduce:active:scale-100",
  "hover:cursor-pointer",
].join(" ");

export const actionChipItemVariants = cva(baseItem, {
  variants: {
    visual: {
      soft: [
        "border-[color-mix(in_srgb,var(--exits-border-strong)_40%,transparent)]",
        "bg-[color-mix(in_srgb,var(--exits-surface-muted)_55%,var(--exits-surface))]",
        "text-foreground",
        "hover:bg-[color-mix(in_srgb,var(--exits-surface-muted)_80%,var(--exits-surface))]",
        "hover:border-[color-mix(in_srgb,var(--exits-primary)_28%,var(--exits-border))]",
      ].join(" "),
      outline: [
        "border-border bg-surface text-foreground",
        "hover:bg-[var(--exits-surface-muted)]",
        "hover:border-[color-mix(in_srgb,var(--exits-primary)_35%,var(--exits-border))]",
      ].join(" "),
      ghost: [
        "border-transparent bg-transparent text-foreground",
        "hover:bg-[color-mix(in_srgb,var(--exits-surface-muted)_70%,transparent)]",
        "hover:border-[color-mix(in_srgb,var(--exits-border)_60%,transparent)]",
      ].join(" "),
      solidPrimary: [
        "border-[var(--exits-primary)] bg-[var(--exits-primary)] text-[var(--exits-primary-contrast)]",
        "hover:brightness-[1.04]",
        "font-semibold",
      ].join(" "),
      tintedPrimary: [
        "border-[color-mix(in_srgb,var(--exits-primary)_55%,var(--exits-border))]",
        "bg-[color-mix(in_srgb,var(--exits-primary)_14%,var(--exits-surface))]",
        "text-[var(--exits-primary)] font-semibold",
        "hover:bg-[color-mix(in_srgb,var(--exits-primary)_22%,var(--exits-surface))]",
        "hover:border-[var(--exits-primary)]",
      ].join(" "),
      elevated: [
        "border-border bg-surface text-foreground",
        "shadow-[0_1px_2px_color-mix(in_srgb,#000_10%,transparent),0_4px_10px_color-mix(in_srgb,#000_8%,transparent)]",
        "hover:-translate-y-px hover:shadow-[0_2px_4px_color-mix(in_srgb,#000_12%,transparent),0_8px_16px_color-mix(in_srgb,#000_10%,transparent)]",
        "motion-reduce:hover:translate-y-0",
      ].join(" "),
      gradient: [
        "border-[color-mix(in_srgb,var(--exits-primary)_50%,transparent)]",
        "bg-[linear-gradient(135deg,color-mix(in_srgb,var(--exits-primary)_88%,white),var(--exits-primary))]",
        "text-[var(--exits-primary-contrast)] font-semibold",
        "hover:brightness-[1.03]",
      ].join(" "),
    },
    shape: {
      pill: "rounded-full",
      soft: "rounded-[var(--exits-radius-md)]",
      square: "rounded-[var(--exits-radius-xs)]",
    },
    tone: {
      neutral: "",
      primary: "",
      info: [
        "border-[color-mix(in_srgb,var(--exits-info)_35%,var(--exits-border))]",
        "bg-[color-mix(in_srgb,var(--exits-info)_8%,var(--exits-surface))]",
        "text-[var(--exits-info)]",
      ].join(" "),
      success: [
        "border-[color-mix(in_srgb,var(--exits-success)_35%,var(--exits-border))]",
        "bg-[color-mix(in_srgb,var(--exits-success)_8%,var(--exits-surface))]",
        "text-[var(--exits-success)]",
      ].join(" "),
      warning: [
        "border-[color-mix(in_srgb,var(--exits-warning)_35%,var(--exits-border))]",
        "bg-[color-mix(in_srgb,var(--exits-warning)_8%,var(--exits-surface))]",
        "text-[var(--exits-warning)]",
      ].join(" "),
      danger: [
        "border-[color-mix(in_srgb,var(--exits-danger)_35%,var(--exits-border))]",
        "bg-[color-mix(in_srgb,var(--exits-danger)_8%,var(--exits-surface))]",
        "text-[var(--exits-danger)]",
      ].join(" "),
    },
    iconOnly: {
      true: "aspect-square w-[var(--exits-chip-min-height)] min-w-[var(--exits-chip-min-height)] px-0",
      false: "",
    },
    fullWidthMobile: {
      true: "max-sm:w-full max-sm:justify-center",
      false: "",
    },
  },
  defaultVariants: {
    visual: "soft",
    shape: "soft",
    tone: "neutral",
    iconOnly: false,
    fullWidthMobile: false,
  },
});

export const actionChipGroupVariants = cva("exits-action-chip-bar flex min-w-0 gap-[var(--exits-chip-gap)]", {
  variants: {
    layout: {
      inline: "flex-nowrap items-center",
      wrap: "flex-wrap items-center",
      scroll: [
        "flex-nowrap items-center overflow-x-auto overscroll-x-contain",
        "pb-0.5 [-webkit-overflow-scrolling:touch] [scrollbar-width:thin]",
        "focus-within:scroll-ps-1 focus-within:scroll-pe-1",
      ].join(" "),
      responsiveAuto: [
        "flex-wrap items-center",
        "max-[420px]:flex-nowrap max-[420px]:overflow-x-auto max-[420px]:overscroll-x-contain",
        "max-[420px]:pb-0.5 max-[420px]:[-webkit-overflow-scrolling:touch] max-[420px]:[scrollbar-width:thin]",
      ].join(" "),
      grid: "grid w-full grid-cols-1 gap-[var(--exits-chip-gap)] min-[360px]:grid-cols-2",
    },
  },
  defaultVariants: {
    layout: "wrap",
  },
});

export type ActionChipItemVariantProps = VariantProps<typeof actionChipItemVariants>;
export type ActionChipGroupVariantProps = VariantProps<typeof actionChipGroupVariants>;

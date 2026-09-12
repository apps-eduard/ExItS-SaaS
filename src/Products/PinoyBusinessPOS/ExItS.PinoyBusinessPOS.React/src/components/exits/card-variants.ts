import { cva, type VariantProps } from "class-variance-authority";

/**
 * ExItS Card visual foundation (APPROVED / LOCKED).
 * Treatment / motion / radius / padding / accent are independent of usage patterns.
 * @see Docs/UI/exits-card-standard.md
 */
export type ExitsCardTreatment =
  | "surface"
  | "bordered"
  | "elevated"
  | "interactive"
  | "selected"
  | "accent"
  | "featured";

/** Hover / micro-interaction vocabulary — default none (STATIC). */
export type ExitsCardMotion =
  | "none"
  | "lift"
  | "expand"
  | "accent"
  | "featured";

/** Expand intensity — default standard ≈ 1.02 (APPROVED / LOCKED). */
export type ExitsCardExpandScale = "subtle" | "standard" | "strong";

export type ExitsCardRadius = "standard" | "soft";
export type ExitsCardPadding = "default" | "compact";
export type ExitsCardLayout = "vertical" | "horizontal";
export type ExitsCardAccentTone = "neutral" | "primary" | "success" | "warning" | "danger" | "info";
/** start = inline-start accent (RTL-aware); top = block-start; tint = soft fill */
export type ExitsCardAccentPosition = "start" | "top" | "tint";

const motionBase = [
  "exits-motion-lift origin-center",
  "duration-[var(--exits-motion-fast)]",
  "motion-reduce:hover:translate-y-0 motion-reduce:hover:scale-100 motion-reduce:hover:shadow-[var(--exits-shadow-sm)]",
].join(" ");

export const exitsCardVariants = cva(
  [
    "exits-card group/card relative min-w-0 text-foreground",
    "transition-[color,background-color,border-color,box-shadow,transform,opacity] duration-[var(--exits-motion-fast)] ease-[var(--exits-ease-standard)]",
    "motion-reduce:transition-none motion-reduce:transform-none",
  ].join(" "),
  {
    variants: {
      treatment: {
        surface: "border border-transparent bg-surface shadow-none",
        bordered:
          "border border-border bg-surface shadow-none disabled:cursor-not-allowed disabled:opacity-60",
        elevated:
          "border border-border bg-[var(--exits-surface-elevated)] shadow-[var(--exits-shadow-sm)]",
        interactive: [
          "cursor-pointer border border-border bg-surface shadow-none",
          "hover:border-[color-mix(in_srgb,var(--exits-primary)_35%,var(--exits-border))]",
          "hover:bg-[color-mix(in_srgb,var(--exits-surface-muted)_55%,var(--exits-surface))]",
          "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--exits-ring)] focus-visible:ring-offset-2 focus-visible:ring-offset-[var(--exits-bg)]",
        ].join(" "),
        selected: [
          "border border-[color-mix(in_srgb,var(--exits-primary)_55%,var(--exits-border))]",
          "bg-[color-mix(in_srgb,var(--exits-primary)_10%,var(--exits-surface))]",
          "shadow-none",
          "disabled:cursor-not-allowed disabled:opacity-60",
        ].join(" "),
        accent: "border border-border bg-surface shadow-none",
        featured: [
          "border border-[color-mix(in_srgb,var(--exits-primary)_45%,var(--exits-border))]",
          "bg-[color-mix(in_srgb,var(--exits-primary)_6%,var(--exits-surface))]",
          "shadow-[var(--exits-shadow-sm)]",
        ].join(" "),
      },
      motion: {
        none: "",
        lift: [
          motionBase,
          "hover:z-[1] hover:-translate-y-0.5 hover:shadow-[var(--exits-shadow-md)]",
          "hover:border-[color-mix(in_srgb,var(--exits-primary)_28%,var(--exits-border))]",
          "active:translate-y-0 active:shadow-[var(--exits-shadow-sm)]",
        ].join(" "),
        expand: [
          motionBase,
          "hover:z-[2] hover:-translate-y-0.5 hover:shadow-[var(--exits-shadow-md)]",
          "hover:border-[color-mix(in_srgb,var(--exits-primary)_35%,var(--exits-border))]",
          "active:translate-y-0 active:scale-100 active:shadow-[var(--exits-shadow-sm)]",
        ].join(" "),
        accent: [
          motionBase,
          "hover:z-[1] hover:border-[var(--exits-primary)]",
          "hover:shadow-[0_0_0_1px_color-mix(in_srgb,var(--exits-primary)_22%,transparent),var(--exits-shadow-sm)]",
        ].join(" "),
        featured: [
          motionBase,
          "hover:z-[2] hover:-translate-y-0.5 hover:scale-[1.02]",
          "hover:border-[var(--exits-primary)] hover:shadow-[var(--exits-shadow-md)]",
          "active:translate-y-0 active:scale-100",
        ].join(" "),
      },
      expandScale: {
        subtle: "",
        standard: "",
        strong: "",
      },
      radius: {
        standard: "rounded-[var(--exits-radius-md)]",
        soft: "rounded-[var(--exits-radius-soft)]",
      },
      padding: {
        default: "px-4 py-4",
        compact: "px-3 py-2.5",
      },
      layout: {
        vertical: "flex flex-col gap-3",
        horizontal: "flex flex-row items-stretch gap-3",
      },
      accentTone: {
        neutral: "",
        primary: "",
        success: "",
        warning: "",
        danger: "",
        info: "",
      },
      accentPosition: {
        start: "",
        top: "",
        tint: "",
      },
      /** Reserves footer space so hover-reveal actions do not change card height. */
      reveal: {
        true: "pb-12",
        false: "",
      },
    },
    compoundVariants: [
      { motion: "expand", expandScale: "subtle", class: "hover:scale-[1.01]" },
      { motion: "expand", expandScale: "standard", class: "hover:scale-[1.02]" },
      { motion: "expand", expandScale: "strong", class: "hover:scale-[1.03]" },
      {
        treatment: "accent",
        accentTone: "warning",
        accentPosition: "start",
        class: "border-s-[3px] border-s-[var(--exits-warning)] ps-[calc(1rem-1px)]",
      },
      {
        treatment: "accent",
        accentTone: "danger",
        accentPosition: "start",
        class: "border-s-[3px] border-s-[var(--exits-danger)] ps-[calc(1rem-1px)]",
      },
      {
        treatment: "accent",
        accentTone: "success",
        accentPosition: "start",
        class: "border-s-[3px] border-s-[var(--exits-success)] ps-[calc(1rem-1px)]",
      },
      {
        treatment: "accent",
        accentTone: "primary",
        accentPosition: "start",
        class: "border-s-[3px] border-s-[var(--exits-primary)] ps-[calc(1rem-1px)]",
      },
      {
        treatment: "accent",
        accentTone: "info",
        accentPosition: "start",
        class: "border-s-[3px] border-s-[var(--exits-info)] ps-[calc(1rem-1px)]",
      },
      {
        treatment: "accent",
        accentTone: "warning",
        accentPosition: "top",
        class: "border-t-[3px] border-t-[var(--exits-warning)] pt-[calc(1rem-1px)]",
      },
      {
        treatment: "accent",
        accentTone: "danger",
        accentPosition: "top",
        class: "border-t-[3px] border-t-[var(--exits-danger)] pt-[calc(1rem-1px)]",
      },
      {
        treatment: "accent",
        accentTone: "success",
        accentPosition: "top",
        class: "border-t-[3px] border-t-[var(--exits-success)] pt-[calc(1rem-1px)]",
      },
      {
        treatment: "accent",
        accentTone: "primary",
        accentPosition: "top",
        class: "border-t-[3px] border-t-[var(--exits-primary)] pt-[calc(1rem-1px)]",
      },
      {
        treatment: "accent",
        accentTone: "info",
        accentPosition: "top",
        class: "border-t-[3px] border-t-[var(--exits-info)] pt-[calc(1rem-1px)]",
      },
      {
        treatment: "accent",
        accentTone: "warning",
        accentPosition: "tint",
        class:
          "border-[color-mix(in_srgb,var(--exits-warning)_35%,var(--exits-border))] bg-[color-mix(in_srgb,var(--exits-warning)_8%,var(--exits-surface))]",
      },
      {
        treatment: "accent",
        accentTone: "danger",
        accentPosition: "tint",
        class:
          "border-[color-mix(in_srgb,var(--exits-danger)_35%,var(--exits-border))] bg-[color-mix(in_srgb,var(--exits-danger)_8%,var(--exits-surface))]",
      },
      {
        treatment: "accent",
        accentTone: "success",
        accentPosition: "tint",
        class:
          "border-[color-mix(in_srgb,var(--exits-success)_35%,var(--exits-border))] bg-[color-mix(in_srgb,var(--exits-success)_8%,var(--exits-surface))]",
      },
      {
        treatment: "accent",
        accentTone: "primary",
        accentPosition: "tint",
        class:
          "border-[color-mix(in_srgb,var(--exits-primary)_35%,var(--exits-border))] bg-[color-mix(in_srgb,var(--exits-primary)_8%,var(--exits-surface))]",
      },
      {
        treatment: "accent",
        accentTone: "info",
        accentPosition: "tint",
        class:
          "border-[color-mix(in_srgb,var(--exits-info)_35%,var(--exits-border))] bg-[color-mix(in_srgb,var(--exits-info)_8%,var(--exits-surface))]",
      },
      {
        padding: "compact",
        treatment: "accent",
        accentPosition: "start",
        class: "ps-[calc(0.75rem-1px)]",
      },
      {
        padding: "compact",
        treatment: "accent",
        accentPosition: "top",
        class: "pt-[calc(0.625rem-1px)]",
      },
    ],
    defaultVariants: {
      treatment: "bordered",
      motion: "none",
      expandScale: "standard",
      radius: "standard",
      padding: "default",
      layout: "vertical",
      accentTone: "neutral",
      accentPosition: "start",
      reveal: false,
    },
  },
);

export const exitsCardMediaVariants = cva(
  "exits-card__media shrink-0 overflow-hidden rounded-[var(--exits-radius-sm)] bg-[var(--exits-surface-muted)]",
  {
    variants: {
      zoom: {
        true: [
          "[&>*]:origin-center [&>*]:transition-transform [&>*]:duration-[180ms] [&>*]:ease-[var(--exits-ease-standard)]",
          "group-hover/card:[&>*]:scale-[1.04]",
          "motion-reduce:group-hover/card:[&>*]:scale-100",
        ].join(" "),
        false: "",
      },
    },
    defaultVariants: { zoom: false },
  },
);

export type ExitsCardVariantProps = VariantProps<typeof exitsCardVariants>;

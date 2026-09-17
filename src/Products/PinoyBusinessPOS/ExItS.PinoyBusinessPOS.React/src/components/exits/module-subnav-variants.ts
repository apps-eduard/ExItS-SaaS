import { cva, type VariantProps } from "class-variance-authority";

/**
 * ExItS Module Subnav visual foundation (PILOT / CANDIDATE).
 * Route navigation between related pages inside one module — NOT Tabs.
 * See Docs/UI/exits-module-subnav-standard.md.
 */
export type ModuleSubnavVariant =
  | "underline"
  | "soft"
  | "pill"
  | "pillBar"
  | "compact"
  | "vertical";

export type ModuleSubnavLayout = "equal" | "content";

/** Active treatment for pill / pillBar / soft. */
export type ModuleSubnavActiveTreatment = "solid" | "soft";

export const moduleSubnavListVariants = cva("exits-module-subnav__list flex", {
  variants: {
    variant: {
      underline: "relative gap-1 border-b border-border",
      soft: "gap-1 rounded-[var(--exits-radius-md)] border border-border bg-[var(--exits-surface-muted)]/35 p-1",
      pill: "flex-wrap gap-2",
      pillBar: [
        "gap-0.5 rounded-full border border-[color-mix(in_srgb,var(--exits-primary)_18%,var(--exits-border))]",
        "bg-[color-mix(in_srgb,var(--exits-primary)_8%,var(--exits-surface-muted))]",
        "p-1",
      ].join(" "),
      compact:
        "gap-0.5 rounded-[var(--exits-radius-sm)] border border-border bg-[var(--exits-surface-muted)]/25 p-0.5",
      vertical: "w-52 shrink-0 flex-col gap-0.5 border-e border-border pe-2",
    },
    scrollable: {
      true: "max-w-full overflow-x-auto overscroll-x-contain [scrollbar-width:thin]",
      false: "",
    },
    layout: {
      equal: "",
      content: "",
    },
  },
  compoundVariants: [
    { variant: "vertical", scrollable: true, class: "max-h-72 overflow-y-auto overflow-x-hidden" },
    {
      variant: "underline",
      scrollable: true,
      class: "flex-nowrap whitespace-nowrap [-ms-overflow-style:none] [&::-webkit-scrollbar]:h-1",
    },
    { variant: "soft", scrollable: true, class: "flex-nowrap whitespace-nowrap" },
    { variant: "pill", scrollable: true, class: "flex-nowrap whitespace-nowrap !flex-nowrap" },
    { variant: "pillBar", scrollable: true, class: "flex-nowrap whitespace-nowrap" },
    { variant: "compact", scrollable: true, class: "flex-nowrap whitespace-nowrap" },
    { variant: "pillBar", layout: "equal", class: "w-full" },
    { variant: "soft", layout: "equal", class: "w-full" },
    { variant: "compact", layout: "equal", class: "w-full" },
  ],
  defaultVariants: {
    variant: "soft",
    scrollable: false,
    layout: "content",
  },
});

export const moduleSubnavItemVariants = cva(
  [
    "exits-module-subnav__item group/subnav relative inline-flex shrink-0 items-center justify-center",
    "gap-1.5 font-medium outline-none no-underline",
    "h-[var(--exits-control-height)] min-h-[var(--exits-control-height)]",
    "px-[var(--exits-control-padding-x)]",
    "text-[length:var(--exits-text-sm)] leading-none",
    "transition-[color,background-color,border-color,box-shadow,opacity] duration-[var(--exits-motion-fast)] ease-[var(--exits-ease-standard)]",
    "focus-visible:ring-2 focus-visible:ring-[var(--exits-ring)] focus-visible:ring-offset-2 focus-visible:ring-offset-[var(--exits-bg)]",
    "aria-disabled:pointer-events-none aria-disabled:opacity-45",
    "motion-reduce:transition-none",
  ].join(" "),
  {
    variants: {
      variant: {
        underline: [
          "rounded-none border-b-2 border-transparent text-muted",
          "hover:text-foreground",
          "aria-[current=page]:border-[var(--exits-primary)] aria-[current=page]:text-foreground",
        ].join(" "),
        soft: [
          "rounded-[var(--exits-radius-sm)] text-muted",
          "hover:bg-[color-mix(in_srgb,var(--exits-surface)_70%,transparent)] hover:text-foreground",
          "aria-[current=page]:bg-[color-mix(in_srgb,var(--exits-primary)_12%,var(--exits-surface))]",
          "aria-[current=page]:text-[var(--exits-primary)]",
          "aria-[current=page]:shadow-sm",
        ].join(" "),
        pill: [
          "rounded-full border border-transparent text-muted",
          "bg-[color-mix(in_srgb,var(--exits-surface-muted)_40%,transparent)]",
          "hover:bg-[var(--exits-surface-muted)] hover:text-foreground",
          "aria-[current=page]:border-[color-mix(in_srgb,var(--exits-primary)_45%,var(--exits-border))]",
          "aria-[current=page]:bg-[color-mix(in_srgb,var(--exits-primary)_14%,var(--exits-surface))]",
          "aria-[current=page]:text-[var(--exits-primary)]",
          "aria-[current=page]:shadow-sm",
        ].join(" "),
        pillBar: [
          "rounded-full border border-transparent bg-transparent text-muted",
          "hover:bg-[color-mix(in_srgb,var(--exits-surface)_55%,transparent)] hover:text-foreground",
        ].join(" "),
        compact: [
          "h-8 min-h-8 rounded-[var(--exits-radius-sm)] px-2 text-[length:var(--exits-text-xs)] text-muted",
          "hover:bg-[color-mix(in_srgb,var(--exits-surface)_70%,transparent)] hover:text-foreground",
          "aria-[current=page]:bg-[color-mix(in_srgb,var(--exits-primary)_12%,var(--exits-surface))]",
          "aria-[current=page]:text-[var(--exits-primary)]",
        ].join(" "),
        vertical: [
          "w-full justify-start rounded-[var(--exits-radius-sm)] border-s-2 border-transparent text-muted",
          "hover:bg-[var(--exits-surface-muted)]/60 hover:text-foreground",
          "aria-[current=page]:border-s-[var(--exits-primary)]",
          "aria-[current=page]:bg-[color-mix(in_srgb,var(--exits-primary)_10%,var(--exits-surface))]",
          "aria-[current=page]:text-[var(--exits-primary)]",
        ].join(" "),
      },
      activeTreatment: {
        solid: "",
        soft: "",
      },
      layout: {
        equal: "",
        content: "",
      },
    },
    compoundVariants: [
      {
        variant: "pillBar",
        activeTreatment: "solid",
        class: [
          "aria-[current=page]:bg-[var(--exits-primary)]",
          "aria-[current=page]:text-[var(--exits-primary-contrast)]",
          "aria-[current=page]:shadow-sm",
        ].join(" "),
      },
      {
        variant: "pillBar",
        activeTreatment: "soft",
        class: [
          "aria-[current=page]:border-[color-mix(in_srgb,var(--exits-primary)_40%,transparent)]",
          "aria-[current=page]:bg-[color-mix(in_srgb,var(--exits-primary)_16%,var(--exits-surface))]",
          "aria-[current=page]:text-[var(--exits-primary)]",
          "aria-[current=page]:shadow-sm",
        ].join(" "),
      },
      {
        variant: "pill",
        activeTreatment: "solid",
        class: [
          "aria-[current=page]:border-transparent",
          "aria-[current=page]:bg-[var(--exits-primary)]",
          "aria-[current=page]:text-[var(--exits-primary-contrast)]",
        ].join(" "),
      },
      { variant: "pillBar", layout: "equal", class: "min-w-0 flex-1" },
      { variant: "soft", layout: "equal", class: "min-w-0 flex-1" },
      { variant: "compact", layout: "equal", class: "min-w-0 flex-1" },
    ],
    defaultVariants: {
      variant: "soft",
      activeTreatment: "solid",
      layout: "content",
    },
  },
);

export type ModuleSubnavListVariantProps = VariantProps<typeof moduleSubnavListVariants>;
export type ModuleSubnavItemVariantProps = VariantProps<typeof moduleSubnavItemVariants>;

import { cva, type VariantProps } from "class-variance-authority";

/**
 * ExItS Tabs visual foundation (PILOT / NOT LOCKED).
 * Variant / orientation / icon / count / layout are independent dimensions.
 */
export type ExitsTabsVariant =
  | "underline"
  | "soft"
  | "pill"
  | "pillBar"
  | "segmented"
  | "enclosed"
  | "vertical";

/** Equal shares available width; content sizes to label. */
export type ExitsTabsLayout = "equal" | "content";

/** Pill-bar active inner pill treatment candidates (NOT LOCKED). */
export type ExitsTabsActiveTreatment = "solid" | "accent";

export const exitsTabsListVariants = cva("exits-tabs__list flex", {
  variants: {
    variant: {
      underline: "relative gap-1 border-b border-border",
      soft: "gap-1 rounded-[var(--exits-radius-md)] border border-border bg-[var(--exits-surface-muted)]/35 p-1",
      /** Independent rounded items with gaps — NOT a shared segmented container. */
      pill: "flex-wrap gap-2",
      /**
       * Continuous fully-rounded outer bar; selected item is an inner filled pill.
       * Distinct from PILL (gaps) and SEGMENTED (segment boundaries).
       */
      pillBar: [
        "gap-0.5 rounded-full border border-[color-mix(in_srgb,var(--exits-primary)_18%,var(--exits-border))]",
        "bg-[color-mix(in_srgb,var(--exits-primary)_8%,var(--exits-surface-muted))]",
        "p-1",
      ].join(" "),
      segmented:
        "gap-0.5 rounded-[var(--exits-radius-md)] border border-border bg-[var(--exits-surface-muted)] p-0.5",
      enclosed: "gap-0 border-b border-border",
      vertical: "w-44 shrink-0 flex-col gap-0.5 border-e border-border pe-2",
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
    { variant: "vertical", scrollable: true, class: "max-h-64 overflow-y-auto overflow-x-hidden" },
    {
      variant: "underline",
      scrollable: true,
      class: "flex-nowrap whitespace-nowrap [-ms-overflow-style:none] [&::-webkit-scrollbar]:h-1",
    },
    {
      variant: "soft",
      scrollable: true,
      class: "flex-nowrap whitespace-nowrap",
    },
    {
      variant: "pill",
      scrollable: true,
      class: "flex-nowrap whitespace-nowrap",
    },
    {
      variant: "pillBar",
      scrollable: true,
      class: "flex-nowrap whitespace-nowrap",
    },
    {
      variant: "pillBar",
      layout: "equal",
      class: "w-full",
    },
  ],
  defaultVariants: {
    variant: "underline",
    scrollable: false,
    layout: "content",
  },
});

export const exitsTabTriggerVariants = cva(
  [
    "exits-tabs__trigger group/tab relative inline-flex shrink-0 items-center justify-center",
    "gap-1.5 font-medium outline-none",
    "h-[var(--exits-control-height)] min-h-[var(--exits-control-height)]",
    "px-[var(--exits-control-padding-x)]",
    "text-[length:var(--exits-text-sm)] leading-none",
    "transition-[color,background-color,border-color,box-shadow,opacity,transform] duration-[var(--exits-motion-fast)] ease-[var(--exits-ease-standard)]",
    "focus-visible:ring-2 focus-visible:ring-[var(--exits-ring)] focus-visible:ring-offset-2 focus-visible:ring-offset-[var(--exits-bg)]",
    "disabled:pointer-events-none disabled:opacity-45",
    "motion-reduce:transition-none",
  ].join(" "),
  {
    variants: {
      variant: {
        underline: [
          "rounded-none border-b-2 border-transparent text-muted",
          "hover:text-foreground",
          "data-[selected=true]:border-[var(--exits-primary)] data-[selected=true]:text-foreground",
        ].join(" "),
        soft: [
          "rounded-[var(--exits-radius-sm)] text-muted",
          "hover:bg-[color-mix(in_srgb,var(--exits-surface)_70%,transparent)] hover:text-foreground",
          "data-[selected=true]:bg-[color-mix(in_srgb,var(--exits-primary)_12%,var(--exits-surface))]",
          "data-[selected=true]:text-[var(--exits-primary)]",
          "data-[selected=true]:shadow-sm",
        ].join(" "),
        pill: [
          "rounded-full border border-transparent text-muted",
          "bg-[color-mix(in_srgb,var(--exits-surface-muted)_40%,transparent)]",
          "hover:bg-[var(--exits-surface-muted)] hover:text-foreground",
          "data-[selected=true]:border-[color-mix(in_srgb,var(--exits-primary)_45%,var(--exits-border))]",
          "data-[selected=true]:bg-[color-mix(in_srgb,var(--exits-primary)_14%,var(--exits-surface))]",
          "data-[selected=true]:text-[var(--exits-primary)]",
          "data-[selected=true]:shadow-sm",
        ].join(" "),
        pillBar: [
          "rounded-full border border-transparent bg-transparent text-muted",
          "hover:bg-[color-mix(in_srgb,var(--exits-surface)_55%,transparent)] hover:text-foreground",
          "active:scale-[0.99] motion-reduce:active:scale-100",
        ].join(" "),
        segmented: [
          "flex-1 rounded-[calc(var(--exits-radius-md)-2px)] text-muted sm:flex-none",
          "hover:text-foreground",
          "data-[selected=true]:bg-surface data-[selected=true]:text-foreground data-[selected=true]:shadow-sm",
        ].join(" "),
        enclosed: [
          "mb-[-1px] rounded-t-[var(--exits-radius-sm)] border border-transparent text-muted",
          "hover:text-foreground",
          "data-[selected=true]:border-border data-[selected=true]:border-b-[var(--exits-surface)]",
          "data-[selected=true]:bg-surface data-[selected=true]:text-foreground",
        ].join(" "),
        vertical: [
          "w-full justify-start rounded-[var(--exits-radius-sm)] border-s-2 border-transparent text-muted",
          "hover:bg-[var(--exits-surface-muted)]/60 hover:text-foreground",
          "data-[selected=true]:border-s-[var(--exits-primary)]",
          "data-[selected=true]:bg-[color-mix(in_srgb,var(--exits-primary)_10%,var(--exits-surface))]",
          "data-[selected=true]:text-[var(--exits-primary)]",
        ].join(" "),
      },
      activeTreatment: {
        solid: "",
        accent: "",
      },
      layout: {
        equal: "",
        content: "",
      },
      iconOnly: {
        true: "min-w-[var(--exits-control-height)] px-0",
        false: "",
      },
    },
    compoundVariants: [
      {
        variant: "pillBar",
        activeTreatment: "solid",
        class: [
          "data-[selected=true]:bg-[var(--exits-primary)]",
          "data-[selected=true]:text-[var(--exits-primary-contrast)]",
          "data-[selected=true]:shadow-sm",
        ].join(" "),
      },
      {
        variant: "pillBar",
        activeTreatment: "accent",
        class: [
          "data-[selected=true]:border-[color-mix(in_srgb,var(--exits-primary)_40%,transparent)]",
          "data-[selected=true]:bg-[color-mix(in_srgb,var(--exits-primary)_16%,var(--exits-surface))]",
          "data-[selected=true]:text-[var(--exits-primary)]",
          "data-[selected=true]:shadow-sm",
        ].join(" "),
      },
      {
        variant: "pillBar",
        layout: "equal",
        class: "min-w-0 flex-1",
      },
    ],
    defaultVariants: {
      variant: "underline",
      activeTreatment: "solid",
      layout: "content",
      iconOnly: false,
    },
  },
);

export const exitsTabsPanelVariants = cva(
  [
    "exits-tabs__panel min-w-0 text-[length:var(--exits-text-sm)] text-foreground",
    "motion-safe:animate-[exits-tab-panel-in_var(--exits-motion-fast)_var(--exits-ease-standard)]",
    "motion-reduce:animate-none",
  ].join(" "),
  {
    variants: {
      variant: {
        underline: "pt-3",
        soft: "pt-3",
        pill: "pt-3",
        pillBar: "pt-3",
        segmented: "pt-3",
        enclosed:
          "rounded-b-[var(--exits-radius-md)] rounded-se-[var(--exits-radius-md)] border border-t-0 border-border bg-surface p-3",
        vertical: "min-w-0 flex-1 ps-3",
      },
    },
    defaultVariants: {
      variant: "underline",
    },
  },
);

export type ExItsTabsListVariantProps = VariantProps<typeof exitsTabsListVariants>;
export type ExItsTabTriggerVariantProps = VariantProps<typeof exitsTabTriggerVariants>;

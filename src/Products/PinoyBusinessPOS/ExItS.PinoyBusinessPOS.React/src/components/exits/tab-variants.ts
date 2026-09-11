import { cva, type VariantProps } from "class-variance-authority";

/**
 * ExItS Tabs visual foundation (PILOT / NOT LOCKED).
 * Variant / orientation / icon / count are independent dimensions.
 */
export type ExitsTabsVariant = "underline" | "soft" | "segmented" | "enclosed" | "vertical";

export const exitsTabsListVariants = cva("exits-tabs__list flex", {
  variants: {
    variant: {
      underline: "relative gap-1 border-b border-border",
      soft: "gap-1 rounded-[var(--exits-radius-md)] border border-border bg-[var(--exits-surface-muted)]/35 p-1",
      segmented:
        "gap-0.5 rounded-[var(--exits-radius-md)] border border-border bg-[var(--exits-surface-muted)] p-0.5",
      enclosed: "gap-0 border-b border-border",
      vertical: "w-44 shrink-0 flex-col gap-0.5 border-e border-border pe-2",
    },
    scrollable: {
      true: "max-w-full overflow-x-auto overscroll-x-contain [scrollbar-width:thin]",
      false: "",
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
  ],
  defaultVariants: {
    variant: "underline",
    scrollable: false,
  },
});

export const exitsTabTriggerVariants = cva(
  [
    "exits-tabs__trigger group/tab relative inline-flex shrink-0 items-center justify-center",
    "gap-1.5 font-medium outline-none",
    "h-[var(--exits-control-height)] min-h-[var(--exits-control-height)]",
    "px-[var(--exits-control-padding-x)]",
    "text-[length:var(--exits-text-sm)] leading-none",
    "transition-[color,background-color,border-color,box-shadow,opacity] duration-[var(--exits-motion-fast)] ease-[var(--exits-ease-standard)]",
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
      iconOnly: {
        true: "min-w-[var(--exits-control-height)] px-0",
        false: "",
      },
    },
    defaultVariants: {
      variant: "underline",
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

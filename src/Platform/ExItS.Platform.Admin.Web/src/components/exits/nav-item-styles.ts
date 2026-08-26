import { cn } from "@/lib/utils";

export const SIDEBAR_ICON_RAIL_WIDTH = "5rem";
export const SIDEBAR_ICON_RAIL_WIDTH_CLASS = "w-[5rem]";

/** One step below `--exits-text-xs` for nested nav rows. */
export const navNestedTextClass = "text-[0.75rem] leading-snug";

export const navRowBase = cn(
  "flex w-full min-h-11 items-center gap-2 rounded-lg px-2 text-[length:var(--exits-text-sm)] font-medium lg:min-h-9",
  "transition-[background-color,color,box-shadow] duration-[var(--exits-motion-fast)] ease-[var(--exits-ease)]",
);

export const navRowNested = cn(
  "flex w-full min-h-9 items-center gap-1.5 rounded-lg px-2 pl-3 font-medium text-muted lg:min-h-8",
  navNestedTextClass,
  "transition-[background-color,color,box-shadow] duration-[var(--exits-motion-fast)] ease-[var(--exits-ease)]",
);

/** Same typography as section items; extra inset for items under a group (e.g. By Product). */
export const navRowNestedChild = cn(navRowNested, "pl-5");

export function navLinkClass(active: boolean, nested = false) {
  return cn(
    "block rounded-lg font-medium",
    nested ? navNestedTextClass : "text-[length:var(--exits-text-sm)]",
    "transition-[background-color,color,box-shadow] duration-[var(--exits-motion-fast)] ease-[var(--exits-ease)]",
    active
      ? "bg-[var(--exits-primary-soft)] text-foreground shadow-[inset_3px_0_0_0_var(--exits-primary)]"
      : nested
        ? "text-muted hover:bg-surface-muted/70 hover:text-foreground"
        : "text-muted hover:bg-surface-muted/70 hover:text-foreground hover:shadow-sm",
  );
}

export function navSectionHeaderClass(active: boolean) {
  return cn(
    "flex w-full items-center justify-between gap-2 rounded-md px-2 py-1.5 text-left text-[11px] font-semibold tracking-wide uppercase",
    "transition-[background-color,color] duration-[var(--exits-motion-fast)] ease-[var(--exits-ease)]",
    active ? "text-primary" : "text-muted hover:bg-surface-muted/60 hover:text-foreground",
  );
}

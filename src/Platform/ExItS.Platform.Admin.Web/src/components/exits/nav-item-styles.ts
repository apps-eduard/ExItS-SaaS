import { cn } from "@/lib/utils";

export const navRowBase = cn(
  "flex w-full min-h-11 items-center gap-2 rounded-lg px-2 text-[length:var(--exits-text-sm)] font-medium lg:min-h-9",
  "transition-[background-color,color,box-shadow] duration-[var(--exits-motion-fast)] ease-[var(--exits-ease)]",
);

export function navLinkClass(active: boolean) {
  return cn(
    "block rounded-lg text-[length:var(--exits-text-sm)] font-medium",
    "transition-[background-color,color,box-shadow] duration-[var(--exits-motion-fast)] ease-[var(--exits-ease)]",
    active
      ? "bg-[var(--exits-primary-soft)] text-foreground shadow-[inset_3px_0_0_0_var(--exits-primary)]"
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

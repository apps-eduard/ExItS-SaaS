import type { ReactNode } from "react";
import { cn } from "@/lib/utils";

type NavExpandableProps = {
  open: boolean;
  children: ReactNode;
  className?: string;
  contentClassName?: string;
};

/** Height + opacity accordion panel (Material-style submenu motion without keeping items focusable when closed). */
export function NavExpandable({ open, children, className, contentClassName }: NavExpandableProps) {
  return (
    <div
      className={cn(
        "grid transition-[grid-template-rows,opacity] duration-[var(--exits-motion-base)] ease-[var(--exits-ease-out)]",
        open ? "grid-rows-[1fr] opacity-100" : "grid-rows-[0fr] opacity-0",
        !open && "pointer-events-none",
        className,
      )}
      aria-hidden={!open}
    >
      <div className={cn("min-h-0 overflow-hidden", contentClassName)}>{children}</div>
    </div>
  );
}

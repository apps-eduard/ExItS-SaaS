import type { ReactNode } from "react";
import { cn } from "@/lib/cn";

export function Card({
  className,
  children,
  as: Comp = "section",
}: {
  className?: string;
  children: ReactNode;
  as?: "section" | "div" | "article";
}) {
  return (
    <Comp
      className={cn(
        "rounded-[var(--exits-radius-md)] border border-border bg-surface px-4 py-4",
        className,
      )}
    >
      {children}
    </Comp>
  );
}

import type { ReactNode } from "react";
import { cn } from "@/lib/cn";

export function Card({ className, children }: { className?: string; children: ReactNode }) {
  return (
    <div
      className={cn(
        "rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 py-3",
        className,
      )}
    >
      {children}
    </div>
  );
}

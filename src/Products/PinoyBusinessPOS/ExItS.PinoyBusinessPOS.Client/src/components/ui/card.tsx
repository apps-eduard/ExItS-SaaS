import type { HTMLAttributes } from "react";
import { cn } from "@/lib/cn";

export function Card({ className, ...props }: HTMLAttributes<HTMLDivElement>) {
  return (
    <div
      className={cn(
        "rounded-[var(--exits-radius-lg)] border border-border bg-surface p-[var(--exits-density-card-padding)] shadow-sm",
        className,
      )}
      {...props}
    />
  );
}

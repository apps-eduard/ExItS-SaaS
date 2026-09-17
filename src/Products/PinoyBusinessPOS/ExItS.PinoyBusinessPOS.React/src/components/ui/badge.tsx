import type { HTMLAttributes } from "react";
import { cn } from "@/lib/cn";

export function Badge({ className, ...props }: HTMLAttributes<HTMLSpanElement>) {
  return (
    <span
      className={cn(
        "inline-flex items-center gap-1 rounded-full border border-border bg-surface-muted px-2.5 py-1 text-[length:var(--exits-text-xs)] font-medium text-muted",
        className,
      )}
      {...props}
    />
  );
}

export function StatusChip({
  tone = "neutral",
  className,
  ...props
}: HTMLAttributes<HTMLSpanElement> & { tone?: "neutral" | "success" | "warning" | "info" }) {
  const toneClass =
    tone === "success"
      ? "border-transparent bg-[var(--exits-success-bg)] text-[var(--exits-success)]"
      : tone === "warning"
        ? "border-transparent bg-[var(--exits-warning-bg)] text-[var(--exits-warning)]"
        : tone === "info"
          ? "border-transparent bg-[var(--exits-info-bg)] text-[var(--exits-info)]"
          : "";
  return <Badge className={cn(toneClass, className)} {...props} />;
}

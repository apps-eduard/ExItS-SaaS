import type { ReactNode } from "react";
import { cn } from "@/lib/utils";

export function DashboardSection({
  title,
  description,
  children,
  className,
  variant = "panel",
}: {
  title: string;
  description?: string;
  children: ReactNode;
  className?: string;
  variant?: "panel" | "metric" | "quiet";
}) {
  return (
    <section
      className={cn(
        "min-w-0",
        variant === "panel" &&
          "rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3",
        variant === "metric" &&
          "rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3",
        variant === "quiet" && "px-0 py-0",
        className,
      )}
    >
      <header className={cn("min-w-0", variant === "quiet" ? "mb-2" : "mb-2.5")}>
        <h2
          className={cn(
            "break-words",
            variant === "metric"
              ? "text-[length:var(--exits-text-xs)] font-medium text-muted"
              : "text-[length:var(--exits-text-sm)] font-semibold",
          )}
        >
          {title}
        </h2>
        {description && variant !== "metric" ? (
          <p className="mt-0.5 text-[length:var(--exits-text-xs)] text-muted break-words">
            {description}
          </p>
        ) : null}
      </header>
      {children}
    </section>
  );
}

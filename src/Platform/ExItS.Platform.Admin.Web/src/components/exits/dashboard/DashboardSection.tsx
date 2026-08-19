import type { ReactNode } from "react";
import { cn } from "@/lib/utils";

export function DashboardSection({
  title,
  description,
  children,
  className,
}: {
  title: string;
  description?: string;
  children: ReactNode;
  className?: string;
}) {
  return (
    <section
      className={cn(
        "min-w-0 rounded-[var(--exits-density-radius)] border border-border bg-surface p-[var(--exits-density-card-padding)] shadow-sm",
        className,
      )}
    >
      <header className="mb-3 min-w-0">
        <h2 className="text-[length:var(--exits-text-md)] font-semibold break-words">{title}</h2>
        {description ? (
          <p className="mt-1 text-[length:var(--exits-text-sm)] text-muted break-words">
            {description}
          </p>
        ) : null}
      </header>
      {children}
    </section>
  );
}

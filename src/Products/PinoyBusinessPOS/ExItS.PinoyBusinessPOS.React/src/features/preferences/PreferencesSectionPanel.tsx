import type { ReactNode } from "react";
import { cn } from "@/lib/cn";

export function PreferencesSectionPanel({
  title,
  titleId,
  children,
  testId,
  className,
}: {
  title: string;
  titleId: string;
  children: ReactNode;
  testId?: string;
  className?: string;
}) {
  return (
    <section
      className={cn(
        "min-w-0 rounded-[var(--exits-radius-md)] border border-border bg-surface",
        className,
      )}
      aria-labelledby={titleId}
      data-testid={testId}
    >
      <div className="border-b border-border px-4 py-3">
        <h3
          id={titleId}
          className="m-0 text-[length:var(--exits-text-md)] font-semibold tracking-tight text-foreground"
        >
          {title}
        </h3>
      </div>
      <div className="min-w-0">{children}</div>
    </section>
  );
}

export function PreferencesEmptyState({
  message,
  testId,
}: {
  message: string;
  testId?: string;
}) {
  return (
    <p
      className="m-0 px-4 py-4 text-[length:var(--exits-text-sm)] leading-relaxed text-muted"
      data-testid={testId}
    >
      {message}
    </p>
  );
}

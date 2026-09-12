import type { ReactNode } from "react";
import { cn } from "@/lib/cn";

export function PreferencesSectionPanel({
  title,
  titleId,
  children,
  testId,
  className,
  /** plain — lightweight settings panel (Appearance); card — bordered section surface */
  surface = "card",
}: {
  title: string;
  titleId: string;
  children: ReactNode;
  testId?: string;
  className?: string;
  surface?: "card" | "plain";
}) {
  return (
    <section
      className={cn(
        "min-w-0",
        surface === "card" &&
          "rounded-[var(--exits-radius-md)] border border-border bg-surface",
        surface === "plain" && "bg-transparent",
        className,
      )}
      aria-labelledby={titleId}
      data-testid={testId}
      data-surface={surface}
    >
      <div
        className={cn(
          "px-4 py-2.5",
          surface === "card" && "border-b border-border py-3",
        )}
      >
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

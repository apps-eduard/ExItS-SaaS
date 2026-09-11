import { useId, type HTMLAttributes, type ReactNode } from "react";
import { ChevronDown } from "lucide-react";
import { cn } from "@/lib/cn";

export type UiStandardsSectionProps = {
  id: string;
  title: string;
  description?: string;
  /** Muted one-line summary shown under the title (also when collapsed). */
  summary?: string;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  children: ReactNode;
  testId?: string;
  /** 1 = major Card section; 2 = nested semantic group. Max depth = 2. */
  level?: 1 | 2;
};

/**
 * UI Standards gallery disclosure.
 * Full-header toggle, chevron indicator, restrained expand/collapse motion.
 */
export function UiStandardsSection({
  id,
  title,
  description,
  summary,
  open,
  onOpenChange,
  children,
  testId,
  level = 1,
}: UiStandardsSectionProps) {
  const reactId = useId();
  const panelId = `${id}-${reactId}-panel`;
  const resolvedTestId = testId ?? `ui-standards-section-${id}`;

  return (
    <section
      className={cn(
        level === 1
          ? "rounded-[var(--exits-radius-md)] border border-border bg-surface"
          : "border-t border-border pt-3 first:border-t-0 first:pt-0",
      )}
      data-testid={resolvedTestId}
      data-open={open ? "true" : "false"}
      data-level={level}
    >
      <div className={cn("m-0", level === 1 ? "p-0" : "")}>
        <button
          type="button"
          className={cn(
            "flex w-full min-h-[var(--exits-control-height)] items-start gap-3 text-left transition-colors duration-[var(--exits-motion-fast)] ease-[var(--exits-ease-standard)]",
            "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--exits-ring)] focus-visible:ring-offset-2 focus-visible:ring-offset-[var(--exits-bg)]",
            level === 1
              ? "rounded-[var(--exits-radius-md)] px-3 py-3 hover:bg-[var(--exits-surface-muted)]/50"
              : "rounded-[var(--exits-radius-md)] px-1 py-1.5 hover:bg-[var(--exits-surface-muted)]/40",
          )}
          aria-expanded={open}
          aria-controls={panelId}
          data-testid={`${resolvedTestId}-toggle`}
          onClick={() => onOpenChange(!open)}
        >
          <span className="min-w-0 flex-1">
            <span
              className={cn(
                "block font-semibold text-foreground",
                level === 1
                  ? "text-[length:var(--exits-text-md)]"
                  : "text-[length:var(--exits-text-sm)] text-muted",
              )}
            >
              {title}
            </span>
            {description ? (
              <span className="mt-0.5 block text-[length:var(--exits-text-sm)] text-muted">
                {description}
              </span>
            ) : null}
            {summary ? (
              <span className="mt-0.5 block text-[length:var(--exits-text-xs)] text-muted">
                {summary}
              </span>
            ) : null}
          </span>
          <ChevronDown
            className={cn(
              "mt-0.5 size-4 shrink-0 text-muted transition-transform duration-[var(--exits-motion-fast)] ease-[var(--exits-ease-standard)] motion-reduce:transition-none",
              open && "rotate-180",
            )}
            aria-hidden
          />
        </button>
      </div>

      <div
        id={panelId}
        className={cn(
          "grid transition-[grid-template-rows,opacity] duration-[var(--exits-motion-normal)] ease-[var(--exits-ease-standard)] motion-reduce:transition-none",
          open ? "grid-rows-[1fr] opacity-100" : "grid-rows-[0fr] opacity-0",
        )}
        data-testid={`${resolvedTestId}-panel`}
        aria-hidden={!open}
        {...(!open ? ({ inert: true } as HTMLAttributes<HTMLDivElement>) : {})}
      >
        <div className="min-h-0 overflow-hidden">
          <div
            className={cn(
              level === 1 ? "grid gap-4 border-t border-border px-3 pb-3 pt-3" : "grid gap-2 pt-1",
            )}
          >
            {children}
          </div>
        </div>
      </div>
    </section>
  );
}

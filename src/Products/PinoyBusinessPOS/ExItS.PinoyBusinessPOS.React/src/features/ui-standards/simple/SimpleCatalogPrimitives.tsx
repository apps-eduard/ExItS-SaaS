import { useState, type ReactNode } from "react";
import { ChevronDown } from "lucide-react";
import { cn } from "@/lib/cn";
import { UiStandardsSampleCard } from "@/features/ui-standards/UiStandardsSampleCard";
import type { UiStandardsStandardName } from "@/features/ui-standards/UiStandardsCopyCommand";

export type SimpleSampleProps = {
  label: string;
  command: string;
  standard: UiStandardsStandardName | UiStandardsStandardName[];
  standardStatus?: "locked" | "pilot";
  children: ReactNode;
  testId?: string;
  contentClassName?: string;
  commandContext?: string;
};

/** Compact Simple-catalog sample with Cursor copy. */
export function SimpleSample({
  label,
  command,
  standard,
  standardStatus = "locked",
  children,
  testId,
  contentClassName = "flex min-w-0 flex-wrap items-center gap-2",
  commandContext,
}: SimpleSampleProps) {
  return (
    <UiStandardsSampleCard
      label={label}
      density="compact"
      standard={standard}
      standardStatus={standardStatus}
      command={command}
      commandContext={commandContext}
      testId={testId}
      contentClassName={contentClassName}
    >
      {children}
    </UiStandardsSampleCard>
  );
}

export function SimpleGroup({
  title,
  children,
  testId,
}: {
  title: string;
  children: ReactNode;
  testId?: string;
}) {
  return (
    <div className="grid min-w-0 gap-2" data-testid={testId}>
      <h4 className="m-0 text-[length:var(--exits-text-xs)] font-semibold uppercase tracking-wide text-muted">
        {title}
      </h4>
      <div className="grid min-w-0 gap-2 sm:grid-cols-2 xl:grid-cols-3">{children}</div>
    </div>
  );
}

export function SimpleCatalogSection({
  id,
  title,
  status,
  open,
  onOpenChange,
  children,
}: {
  id: string;
  title: string;
  status?: "LOCKED" | "PILOT";
  open: boolean;
  onOpenChange: (open: boolean) => void;
  children: ReactNode;
}) {
  return (
    <section
      id={`simple-${id}`}
      data-testid={`ui-standards-simple-section-${id}`}
      className="min-w-0 rounded-[var(--exits-radius-md)] border border-border bg-surface"
    >
      <button
        type="button"
        className="flex w-full items-center justify-between gap-2 px-3 py-2.5 text-start"
        aria-expanded={open}
        onClick={() => onOpenChange(!open)}
      >
        <span className="flex min-w-0 items-center gap-2">
          <span className="text-[length:var(--exits-text-md)] font-semibold text-foreground">{title}</span>
          {status ? (
            <span
              className={cn(
                "rounded-full px-1.5 py-0.5 text-[0.625rem] font-semibold uppercase tracking-wide",
                status === "PILOT"
                  ? "bg-[color-mix(in_srgb,var(--exits-warning)_16%,transparent)] text-[var(--exits-warning)]"
                  : "bg-[color-mix(in_srgb,var(--exits-success)_14%,transparent)] text-[var(--exits-success)]",
              )}
            >
              {status}
            </span>
          ) : null}
        </span>
        <ChevronDown
          className={cn(
            "size-4 shrink-0 text-muted transition-transform duration-[var(--exits-motion-fast)]",
            open && "rotate-180",
          )}
          aria-hidden
        />
      </button>
      {open ? <div className="grid gap-4 border-t border-border px-3 py-3">{children}</div> : null}
    </section>
  );
}

export function useSimpleSectionOpen(defaults: Record<string, boolean>) {
  const [open, setOpen] = useState(defaults);
  return {
    isOpen: (id: string) => open[id] ?? true,
    setOpen: (id: string, next: boolean) => setOpen((prev) => ({ ...prev, [id]: next })),
  };
}

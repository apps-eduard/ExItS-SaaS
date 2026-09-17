import type { ReactNode } from "react";
import { cn } from "@/lib/cn";

export function SegmentedControl({ label, children }: { label: string; children: ReactNode }) {
  return (
    <fieldset className="m-0 min-w-0 border-0 p-0">
      <legend className="exits-type-label mb-2 text-muted">
        {label}
      </legend>
      <div
        className="flex min-w-0 flex-wrap gap-0.5 rounded-[var(--exits-radius-md)] bg-background p-0.5"
        role="radiogroup"
        aria-label={label}
      >
        {children}
      </div>
    </fieldset>
  );
}

export function SegmentedOption({
  selected,
  onSelect,
  children,
}: {
  selected: boolean;
  onSelect: () => void;
  children: ReactNode;
}) {
  return (
    <button
      type="button"
      role="radio"
      aria-checked={selected}
      onClick={onSelect}
      className={cn(
        "inline-flex h-[var(--exits-control-height)] min-h-[var(--exits-control-height)] min-w-[5.25rem] flex-1 items-center justify-center gap-1 rounded-[calc(var(--exits-radius-md)-2px)] px-2 text-[length:var(--exits-text-xs)] font-medium transition-[background-color,color,box-shadow] duration-[var(--exits-motion-fast)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring sm:flex-none sm:min-w-[6.5rem] sm:text-[length:var(--exits-text-sm)]",
        selected
          ? "bg-surface font-semibold text-foreground shadow-sm"
          : "bg-transparent text-muted hover:text-foreground",
      )}
    >
      {children}
    </button>
  );
}

import type { ReactNode } from "react";
import { cn } from "@/lib/cn";

export function SegmentedControl({ label, children }: { label: string; children: ReactNode }) {
  return (
    <fieldset className="m-0 min-w-0 border-0 p-0">
      <legend className="mb-2 text-[length:var(--exits-text-sm)] font-semibold text-muted">
        {label}
      </legend>
      <div
        className="grid auto-cols-fr grid-flow-col gap-1 rounded-[var(--exits-radius-md)] bg-surface-muted p-1"
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
        "inline-flex min-h-11 min-w-0 items-center justify-center gap-1.5 rounded-[calc(var(--exits-radius-md)-2px)] px-2 text-[length:var(--exits-text-sm)] font-semibold transition-[background-color,color,box-shadow] duration-[var(--exits-motion-fast)] ease-[var(--exits-ease)]",
        selected
          ? "bg-surface text-foreground shadow-sm"
          : "bg-transparent text-muted hover:text-foreground",
      )}
    >
      {children}
    </button>
  );
}

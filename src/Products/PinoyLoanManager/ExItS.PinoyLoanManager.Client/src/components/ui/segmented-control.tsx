import type { ReactNode } from "react";
import { cn } from "@/lib/cn";

export function SegmentedControl({ label, children }: { label: string; children: ReactNode }) {
  return (
    <fieldset className="m-0 min-w-0 border-0 p-0">
      <legend className="mb-2 text-[length:var(--exits-text-sm)] font-semibold text-muted">
        {label}
      </legend>
      <div
        className="grid auto-cols-fr grid-flow-col gap-0.5 rounded-[var(--exits-radius-md)] bg-background p-0.5"
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
        "inline-flex min-h-11 min-w-0 items-center justify-center rounded-[calc(var(--exits-radius-md)-2px)] px-2 text-[length:var(--exits-text-sm)] font-semibold",
        selected ? "bg-surface text-foreground shadow-sm" : "bg-transparent text-muted",
      )}
    >
      {children}
    </button>
  );
}

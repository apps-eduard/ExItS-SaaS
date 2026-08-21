import { Search, X } from "lucide-react";
import type { InputHTMLAttributes } from "react";
import { cn } from "@/lib/cn";

export type SearchFieldProps = Omit<InputHTMLAttributes<HTMLInputElement>, "type"> & {
  label: string;
  onClear?: () => void;
  containerClassName?: string;
};

export function SearchField({
  label,
  value,
  onClear,
  className,
  containerClassName,
  id,
  ...props
}: SearchFieldProps) {
  const fieldId = id ?? props.name ?? "search-field";
  const hasValue = typeof value === "string" && value.length > 0;

  return (
    <div className={cn("flex min-w-0 flex-col gap-1", containerClassName)}>
      <label htmlFor={fieldId} className="sr-only">
        {label}
      </label>
      <div className="relative min-w-0">
        <Search
          aria-hidden
          className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-muted"
        />
        <input
          id={fieldId}
          // Use text (not search): browsers draw a native clear "x" that duplicates our button.
          type="text"
          inputMode="search"
          enterKeyHint="search"
          value={value}
          className={cn(
            "h-[var(--exits-control-height)] min-h-[var(--exits-touch-target-min)] w-full rounded-full border border-border bg-surface py-2 pr-10 pl-10 text-[length:var(--exits-text-md)] text-foreground outline-none focus-visible:ring-2 focus-visible:ring-ring",
            className,
          )}
          {...props}
        />
        {hasValue && onClear ? (
          <button
            type="button"
            className="absolute top-1/2 right-1.5 inline-flex size-[var(--exits-touch-target-min)] -translate-y-1/2 items-center justify-center rounded-full text-muted hover:bg-[var(--exits-surface-muted)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
            aria-label="Clear search"
            onClick={onClear}
          >
            <X className="size-4" aria-hidden />
          </button>
        ) : null}
      </div>
    </div>
  );
}

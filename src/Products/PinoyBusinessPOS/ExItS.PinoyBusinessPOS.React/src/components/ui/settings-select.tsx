import { Check } from "lucide-react";
import { useId, type ReactNode } from "react";
import { cn } from "@/lib/cn";

export type SettingsOption<T extends string> = {
  value: T;
  label: string;
  icon?: ReactNode;
};

type SettingsSelectProps<T extends string> = {
  label: string;
  value: T;
  options: SettingsOption<T>[];
  onChange: (value: T) => void;
};

/**
 * In-flow preference choices (not an absolute dropdown).
 * Avoids covering the next settings row on narrow phone viewports.
 */
export function SettingsSelect<T extends string>({
  label,
  value,
  options,
  onChange,
}: SettingsSelectProps<T>) {
  const labelId = useId();
  // Theme/Density (3): stacked on small screens, one row from ~480px.
  // Language (5+): 2 columns; odd last option spans full width.
  const layoutClass =
    options.length === 3
      ? "grid grid-cols-1 gap-2 min-[480px]:grid-cols-3"
      : options.length === 2
        ? "grid grid-cols-2 gap-2"
        : "grid grid-cols-2 gap-2";
  const oddLastSpansFull = options.length > 3 && options.length % 2 === 1;

  return (
    <div className="flex min-w-0 flex-col gap-3 py-4">
      <span
        id={labelId}
        className="text-[length:var(--exits-text-sm)] font-semibold text-foreground"
      >
        {label}
      </span>
      <div role="radiogroup" aria-labelledby={labelId} className={layoutClass}>
        {options.map((option, index) => {
          const selected = option.value === value;
          const isLastOdd = oddLastSpansFull && index === options.length - 1;
          return (
            <button
              key={option.value}
              type="button"
              role="radio"
              aria-checked={selected}
              aria-label={`${label}: ${option.label}`}
              className={cn(
                "flex min-h-[var(--exits-touch-target-min)] min-w-0 items-center gap-2.5 rounded-[var(--exits-radius-md)] border px-3 py-2.5 text-left text-[length:var(--exits-text-sm)] font-semibold transition-[background-color,border-color,color,box-shadow] duration-[var(--exits-motion-fast)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
                isLastOdd && "col-span-2",
                selected
                  ? "border-primary bg-[color-mix(in_srgb,var(--exits-primary)_10%,var(--exits-surface))] text-foreground shadow-[inset_0_0_0_1px_color-mix(in_srgb,var(--exits-primary)_35%,transparent)]"
                  : "border-border bg-background text-foreground hover:bg-[var(--exits-surface-muted)]",
              )}
              onClick={() => {
                onChange(option.value);
              }}
            >
              <span className="flex min-w-0 flex-1 items-center gap-2 truncate">
                {option.icon}
                <span className="truncate">{option.label}</span>
              </span>
              {selected ? (
                <Check className="size-4 shrink-0 text-primary" aria-hidden="true" />
              ) : (
                <span className="size-4 shrink-0" aria-hidden="true" />
              )}
            </button>
          );
        })}
      </div>
    </div>
  );
}

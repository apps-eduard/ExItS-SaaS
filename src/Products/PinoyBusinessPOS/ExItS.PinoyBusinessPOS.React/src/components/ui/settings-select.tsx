import { Check } from "lucide-react";
import { useId, type ReactNode } from "react";
import { cn } from "@/lib/cn";

export type SettingsOption<T extends string> = {
  value: T;
  label: string;
  icon?: ReactNode;
  testId?: string;
};

type SettingsSelectProps<T extends string> = {
  label: string;
  value: T;
  options: SettingsOption<T>[];
  onChange: (value: T) => void;
  /**
   * cards — taller option tiles (Language, etc.)
   * segmented — compact single-row control (Appearance Theme / Density / Shape / Motion)
   */
  variant?: "cards" | "segmented";
  testId?: string;
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
  variant = "cards",
  testId,
}: SettingsSelectProps<T>) {
  const labelId = useId();

  if (variant === "segmented") {
    return (
      <div
        className="@container flex min-w-0 flex-col gap-2"
        data-testid={testId}
        data-settings-variant="segmented"
      >
        <span id={labelId} className="exits-type-label text-foreground">
          {label}
        </span>
        <div
          role="radiogroup"
          aria-labelledby={labelId}
          className="flex min-w-0 flex-wrap gap-1 rounded-[var(--exits-control-radius)] border border-border bg-[color-mix(in_srgb,var(--exits-surface-muted)_55%,var(--exits-surface))] p-1"
        >
          {options.map((option) => {
            const selected = option.value === value;
            return (
              <button
                key={option.value}
                type="button"
                role="radio"
                aria-checked={selected}
                aria-label={`${label}: ${option.label}`}
                data-testid={option.testId}
                className={cn(
                  "inline-flex min-h-8 min-w-0 flex-1 items-center justify-center gap-1.5 rounded-[var(--exits-control-radius)] px-2.5 py-1.5",
                  "text-[length:var(--exits-text-sm)] font-medium transition-[background-color,color,box-shadow,border-color] duration-[var(--exits-motion-fast)]",
                  "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--exits-primary)]",
                  selected
                    ? "bg-surface text-[var(--exits-primary)] shadow-sm ring-1 ring-[var(--exits-primary)]"
                    : "text-muted hover:bg-[color-mix(in_srgb,var(--exits-surface)_70%,transparent)] hover:text-foreground",
                )}
                onClick={() => {
                  onChange(option.value);
                }}
              >
                {option.icon}
                <span className="min-w-0 truncate">{option.label}</span>
              </button>
            );
          })}
        </div>
      </div>
    );
  }

  // Prefer container width over viewport: preferences live in a narrow right drawer,
  // where viewport-based 3-up columns truncates labels (Sy… / Li… / D…).
  // Language (5+): single column when narrow; 2 columns only when comfortable.
  const layoutClass =
    options.length === 3
      ? "grid grid-cols-1 gap-2 @min-[28rem]:grid-cols-3"
      : options.length === 2
        ? "grid grid-cols-2 gap-2"
        : "grid grid-cols-1 gap-2 @min-[20rem]:grid-cols-2";
  const oddLastSpansFull = options.length > 3 && options.length % 2 === 1;

  return (
    <div
      className="@container flex min-w-0 flex-col gap-3 py-4"
      data-testid={testId}
      data-settings-variant="cards"
    >
      <span id={labelId} className="exits-type-label text-foreground">
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
                "flex min-h-[var(--exits-row-min-height)] min-w-0 items-center gap-2.5 rounded-[var(--exits-control-radius)] border px-3 py-2.5 text-left text-[length:var(--exits-text-sm)] font-medium transition-[background-color,border-color,color,box-shadow] duration-[var(--exits-motion-fast)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
                isLastOdd && "col-span-2",
                selected
                  ? "border-primary bg-[color-mix(in_srgb,var(--exits-primary)_10%,var(--exits-surface))] font-semibold text-foreground shadow-[inset_0_0_0_1px_color-mix(in_srgb,var(--exits-primary)_35%,transparent)]"
                  : "border-border bg-background text-foreground hover:bg-[var(--exits-surface-muted)]",
              )}
              onClick={() => {
                onChange(option.value);
              }}
            >
              <span className="flex min-w-0 flex-1 items-center gap-2">
                {option.icon}
                <span className="min-w-0 wrap-break-word">{option.label}</span>
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

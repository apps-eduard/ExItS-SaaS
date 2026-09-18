import { cn } from "@/lib/cn";

export type ExitsPillSelectOption<T extends string = string> = {
  value: T;
  label: string;
  disabled?: boolean;
};

/** `pill` = compact rounded chips; `tile` = full-width bordered tiles (forms / cards). */
export type ExitsPillSelectAppearance = "pill" | "tile";

type ExitsPillSelectBaseProps<T extends string> = {
  options: ReadonlyArray<ExitsPillSelectOption<T>>;
  disabled?: boolean;
  className?: string;
  testId?: string;
  "aria-label"?: string;
  /** Visual shape. Default `pill`. Use `tile` for equal-width attribute grids in cards. */
  appearance?: ExitsPillSelectAppearance;
};

export type ExitsPillSelectProps<T extends string = string> =
  | (ExitsPillSelectBaseProps<T> & {
      mode?: "single";
      value: T;
      onChange: (value: T) => void;
    })
  | (ExitsPillSelectBaseProps<T> & {
      mode: "multi";
      value: ReadonlyArray<T>;
      onChange: (value: T[]) => void;
    });

/**
 * In-flow attribute select (size / variant / payment choices).
 * Double-ring selected treatment; Primary tokens (not page-local green).
 * Use for short fixed option sets — not long searchable lists.
 */
export function ExitsPillSelect<T extends string>(props: ExitsPillSelectProps<T>) {
  const {
    options,
    disabled = false,
    className,
    testId,
    "aria-label": ariaLabel,
    appearance = "pill",
  } = props;
  const multi = props.mode === "multi";
  const tile = appearance === "tile";

  function isSelected(value: T): boolean {
    if (multi) {
      return props.value.includes(value);
    }
    return props.value === value;
  }

  function onToggle(value: T) {
    if (disabled) {
      return;
    }
    if (multi) {
      if (props.value.includes(value)) {
        props.onChange(props.value.filter((item) => item !== value));
        return;
      }
      props.onChange([...props.value, value]);
      return;
    }
    props.onChange(value);
  }

  return (
    <div
      role={multi ? "group" : "radiogroup"}
      aria-label={ariaLabel}
      aria-disabled={disabled || undefined}
      className={cn(tile ? "grid gap-2" : "flex flex-wrap gap-2", className)}
      data-testid={testId}
      data-appearance={appearance}
    >
      {options.map((option) => {
        const selected = isSelected(option.value);
        const optionDisabled = disabled || option.disabled;
        return (
          <button
            key={option.value}
            type="button"
            role={multi ? "button" : "radio"}
            aria-checked={multi ? undefined : selected}
            aria-pressed={multi ? selected : undefined}
            disabled={optionDisabled}
            data-selected={selected ? "true" : "false"}
            data-testid={testId ? `${testId}-option-${option.value}` : undefined}
            className={cn(
              "border p-0.5 transition-[border-color,box-shadow] duration-[var(--exits-motion-fast)]",
              "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--exits-primary)] focus-visible:ring-offset-1",
              tile
                ? "min-w-0 w-full rounded-[var(--exits-radius-md)]"
                : "rounded-full",
              selected
                ? "border-[var(--exits-primary)]"
                : "border-[var(--exits-border)]",
              optionDisabled && "cursor-not-allowed opacity-50",
            )}
            onClick={() => onToggle(option.value)}
          >
            <span
              className={cn(
                "inline-flex items-center justify-center",
                "text-[length:var(--exits-text-sm)] font-normal",
                "transition-[background-color,color] duration-[var(--exits-motion-fast)]",
                tile
                  ? "min-h-10 w-full rounded-[calc(var(--exits-radius-md)-2px)] px-3 py-2 text-center"
                  : "min-h-8 min-w-9 rounded-full px-3 tabular-nums",
                selected
                  ? "bg-[var(--exits-primary)] text-[var(--exits-primary-foreground)]"
                  : "bg-[color-mix(in_srgb,var(--exits-surface-muted)_88%,var(--exits-surface))] text-foreground",
              )}
            >
              {option.label}
            </span>
          </button>
        );
      })}
    </div>
  );
}

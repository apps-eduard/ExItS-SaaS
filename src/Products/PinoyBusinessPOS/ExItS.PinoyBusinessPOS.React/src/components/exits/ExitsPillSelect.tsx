import { cn } from "@/lib/cn";

export type ExitsPillSelectOption<T extends string = string> = {
  value: T;
  label: string;
  disabled?: boolean;
};

type ExitsPillSelectBaseProps<T extends string> = {
  options: ReadonlyArray<ExitsPillSelectOption<T>>;
  disabled?: boolean;
  className?: string;
  testId?: string;
  "aria-label"?: string;
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
 * In-flow pill attribute select (size / variant chips).
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
  } = props;
  const multi = props.mode === "multi";

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
      className={cn("flex flex-wrap gap-2", className)}
      data-testid={testId}
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
              "rounded-full border p-0.5 transition-[border-color,box-shadow] duration-[var(--exits-motion-fast)]",
              "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--exits-primary)] focus-visible:ring-offset-1",
              selected
                ? "border-[var(--exits-primary)]"
                : "border-[var(--exits-border)]",
              optionDisabled && "cursor-not-allowed opacity-50",
            )}
            onClick={() => onToggle(option.value)}
          >
            <span
              className={cn(
                "inline-flex min-h-8 min-w-9 items-center justify-center rounded-full px-3",
                "text-[length:var(--exits-text-sm)] font-semibold tabular-nums",
                "transition-[background-color,color] duration-[var(--exits-motion-fast)]",
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

import { Loader2, Search, X } from "lucide-react";
import { useId, type ChangeEvent, type InputHTMLAttributes, type KeyboardEvent } from "react";
import { cn } from "@/lib/cn";

export type SearchFieldShape = "auto" | "standard" | "pill";

export type SearchFieldProps = Omit<InputHTMLAttributes<HTMLInputElement>, "type" | "size"> & {
  /** Accessible name (rendered as sr-only label). */
  label: string;
  /** Clears the value. Prefer providing this for controlled search state. */
  onClear?: () => void;
  containerClassName?: string;
  /**
   * Shape participation in Preferences → Control Shape.
   * - auto (default): follows global `data-control-shape` via `--exits-control-radius`
   * - standard / pill: explicit override
   */
  shape?: SearchFieldShape;
  /** Optional restrained loading indicator (replaces search icon). */
  loading?: boolean;
  /** Test id on the outer field shell. */
  testId?: string;
};

/**
 * Canonical ExItS Search Field — dataset / list filter control.
 * Follows Control Shape. Not a form data-entry input (`exits-input`).
 */
export function SearchField({
  label,
  value,
  onClear,
  onChange,
  onKeyDown,
  className,
  containerClassName,
  id,
  disabled,
  shape = "auto",
  loading = false,
  testId = "exits-search-field",
  ...props
}: SearchFieldProps) {
  const reactId = useId();
  const fieldId = id ?? props.name ?? `exits-search-${reactId}`;
  const hasValue = typeof value === "string" && value.length > 0;
  const canClear = hasValue && !disabled;

  function handleClear() {
    if (onClear) {
      onClear();
      return;
    }
    if (onChange) {
      const target = { value: "" } as HTMLInputElement;
      onChange({ target, currentTarget: target } as ChangeEvent<HTMLInputElement>);
    }
  }

  function handleKeyDown(event: KeyboardEvent<HTMLInputElement>) {
    onKeyDown?.(event);
    if (event.defaultPrevented) return;
    // Clear only when focused field has value — does not stop modal Escape if empty.
    if (event.key === "Escape" && hasValue && !disabled) {
      event.preventDefault();
      handleClear();
    }
  }

  return (
    <div className={cn("exits-search-field-root flex min-w-0 flex-col gap-1", containerClassName)}>
      <label htmlFor={fieldId} className="sr-only">
        {label}
      </label>
      <div
        className={cn(
          "exits-search-field",
          disabled && "exits-search-field--disabled",
          loading && "exits-search-field--loading",
        )}
        data-shape={shape}
        data-testid={testId}
      >
        <span className="exits-search-field__leading" aria-hidden>
          {loading ? (
            <Loader2 className="exits-search-field__spinner size-4 animate-spin" strokeWidth={2} />
          ) : (
            <Search className="exits-search-field__icon size-4" />
          )}
        </span>
        <input
          id={fieldId}
          // text (not search): avoid native clear control duplicating our button.
          type="text"
          inputMode="search"
          enterKeyHint="search"
          value={value}
          disabled={disabled}
          aria-busy={loading || undefined}
          onChange={onChange}
          onKeyDown={handleKeyDown}
          className={cn("exits-search-field__input", className)}
          {...props}
        />
        {canClear ? (
          <button
            type="button"
            className="exits-search-field__clear"
            aria-label="Clear search"
            onClick={handleClear}
            tabIndex={0}
          >
            <X className="size-4" aria-hidden />
          </button>
        ) : null}
      </div>
    </div>
  );
}

/** Alias matching ExItS naming in standards docs. */
export { SearchField as ExitsSearchField };

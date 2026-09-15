import { useEffect, useId, useState, type KeyboardEvent } from "react";
import { formatPeso } from "@/lib/format-money";
import { cn } from "@/lib/cn";
import {
  clampQuantityToPrecision,
  formatQuantityValue,
  parseQuantityTyping,
  stepQuantity,
} from "@/lib/quantity-rules";

export function MoneyDisplay({
  amount,
  className,
  testId,
}: {
  amount: number;
  className?: string;
  testId?: string;
}) {
  return (
    <span
      data-testid={testId}
      className={cn("tabular-nums text-[length:var(--exits-text-sm)] font-semibold", className)}
    >
      {formatPeso(amount)}
    </span>
  );
}

export function QuantityDisplay({
  value,
  unit,
  className,
  testId,
}: {
  value: number | string;
  unit?: string;
  className?: string;
  testId?: string;
}) {
  return (
    <span
      data-testid={testId}
      className={cn("tabular-nums text-[length:var(--exits-text-sm)] font-semibold", className)}
    >
      {value}
      {unit ? ` ${unit}` : ""}
    </span>
  );
}

export type QuantityStepperProps = {
  value: number | string;
  increaseLabel: string;
  decreaseLabel: string;
  /** Legacy: parent owns step math (Sell / PO / returns). */
  onIncrement?: () => void;
  onDecrement?: () => void;
  /**
   * Controlled editable mode: stepper emits quantity only.
   * When set, center is a real numeric input and +/- use step/min/precision.
   */
  onChange?: (next: number) => void;
  min?: number;
  max?: number;
  step?: number;
  precision?: number;
  disabled?: boolean;
  invalid?: boolean;
  unit?: string;
  ariaLabel?: string;
  valueTestId?: string;
  className?: string;
  compact?: boolean;
  incrementDisabled?: boolean;
  decrementDisabled?: boolean;
};

function numericValue(value: number | string): number {
  if (typeof value === "number") {
    return value;
  }
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : 0;
}

/**
 * Canonical ExItS quantity control: [ − ][ quantity ][ + ]
 * Editable center when `onChange` is provided; display-only when using increment/decrement callbacks.
 */
export function QuantityStepper({
  value,
  onIncrement,
  onDecrement,
  onChange,
  min = 0,
  max,
  step = 1,
  precision = 0,
  disabled = false,
  invalid = false,
  unit,
  ariaLabel,
  increaseLabel,
  decreaseLabel,
  valueTestId,
  className,
  compact = false,
  incrementDisabled = false,
  decrementDisabled = false,
}: QuantityStepperProps) {
  const inputId = useId();
  const editable = typeof onChange === "function";
  const numeric = numericValue(value);
  const displayValue = editable
    ? formatQuantityValue(numeric, precision)
    : String(value);

  const [draft, setDraft] = useState<string | null>(null);
  const [focused, setFocused] = useState(false);

  useEffect(() => {
    if (!focused) {
      setDraft(null);
    }
  }, [value, focused]);

  const atMin = numeric <= min + 1e-12;
  const atMax = max != null && numeric >= max - 1e-12;
  const minusDisabled =
    disabled || decrementDisabled || (editable ? atMin : false);
  const plusDisabled =
    disabled || incrementDisabled || (editable ? atMax : false);

  const valueClass = compact
    ? "min-w-[1.5rem] text-center text-[length:var(--exits-text-xs)]"
    : "min-w-[2rem] text-center";

  function commit(next: number) {
    if (!onChange || !Number.isFinite(next)) {
      return;
    }
    const clamped = clampQuantityToPrecision(next, precision);
    if (!Number.isFinite(clamped)) {
      return;
    }
    let resolved = clamped;
    if (resolved < min) {
      resolved = min;
    }
    if (max != null && resolved > max) {
      resolved = max;
    }
    onChange(resolved);
  }

  function handleMinus() {
    if (minusDisabled) {
      return;
    }
    if (editable && onChange) {
      commit(
        stepQuantity({
          value: numeric,
          direction: -1,
          step,
          precision,
          min,
          max,
        }),
      );
      return;
    }
    onDecrement?.();
  }

  function handlePlus() {
    if (plusDisabled) {
      return;
    }
    if (editable && onChange) {
      commit(
        stepQuantity({
          value: numeric,
          direction: 1,
          step,
          precision,
          min,
          max,
        }),
      );
      return;
    }
    onIncrement?.();
  }

  function handleInputKeyDown(event: KeyboardEvent<HTMLInputElement>) {
    if (event.key === "ArrowUp") {
      event.preventDefault();
      handlePlus();
      return;
    }
    if (event.key === "ArrowDown") {
      event.preventDefault();
      handleMinus();
    }
  }

  return (
    <div
      className={cn(
        "quantity-stepper flex min-w-0 items-center gap-1.5",
        compact && "quantity-stepper--compact",
        invalid && "quantity-stepper--invalid",
        disabled && "quantity-stepper--disabled",
        className,
      )}
      data-testid="quantity-stepper"
    >
      <div className="quantity-stepper__group">
        <button
          type="button"
          className="quantity-stepper__btn quantity-stepper__btn--minus"
          aria-label={decreaseLabel}
          disabled={minusDisabled}
          onClick={handleMinus}
        >
          −
        </button>
        {editable ? (
          <input
            id={inputId}
            className={cn(
              "quantity-stepper__input tabular-nums",
              compact && "quantity-stepper__input--compact",
            )}
            value={draft ?? displayValue}
            inputMode={precision > 0 ? "decimal" : "numeric"}
            disabled={disabled}
            aria-invalid={invalid || undefined}
            aria-label={ariaLabel ?? "Quantity"}
            data-testid={valueTestId}
            onFocus={() => {
              setFocused(true);
              setDraft(displayValue);
            }}
            onChange={(event) => {
              const nextText = event.target.value;
              const parsed = parseQuantityTyping(nextText, precision);
              if (parsed.kind === "invalid") {
                return;
              }
              setDraft(nextText);
              if (parsed.kind === "value" && parsed.value > 0) {
                commit(parsed.value);
              }
            }}
            onBlur={() => {
              setFocused(false);
              const raw = draft ?? displayValue;
              const parsed = parseQuantityTyping(raw, precision);
              if (parsed.kind === "value" && parsed.value > 0) {
                commit(parsed.value);
              } else {
                // Snap back to last committed value — never emit 0/NaN from blur.
                setDraft(null);
              }
            }}
            onKeyDown={handleInputKeyDown}
          />
        ) : (
          <span
            className={cn("quantity-stepper__value tabular-nums", valueClass)}
            data-testid={valueTestId}
          >
            {value}
          </span>
        )}
        <button
          type="button"
          className="quantity-stepper__btn quantity-stepper__btn--plus"
          aria-label={increaseLabel}
          disabled={plusDisabled}
          onClick={handlePlus}
        >
          +
        </button>
      </div>
      {unit ? (
        <span className="quantity-stepper__unit shrink-0 text-[length:var(--exits-text-xs)] text-muted">
          {unit}
        </span>
      ) : null}
    </div>
  );
}

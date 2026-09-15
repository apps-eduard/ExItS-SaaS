import { useEffect, useId, useState, type KeyboardEvent } from "react";
import { formatPeso } from "@/lib/format-money";
import { cn } from "@/lib/cn";
import {
  clampQuantityToPrecision,
  formatQuantityValue,
  parseQuantityTyping,
  stepQuantity,
  stripQuantityGrouping,
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

/** Default / compact ch clamps for auto-width quantity input (buttons stay fixed). */
export const QUANTITY_STEPPER_INPUT_MIN_CH = 12;
export const QUANTITY_STEPPER_INPUT_MAX_CH = 20;
export const QUANTITY_STEPPER_INPUT_COMPACT_MIN_CH = 12;
export const QUANTITY_STEPPER_INPUT_COMPACT_MAX_CH = 20;
const QUANTITY_STEPPER_INPUT_PAD_CH = 0.5;

/** Width in `ch` from current text, clamped so layout stays compact and mobile-safe. */
export function quantityStepperInputWidthCh(
  text: string,
  options?: { compact?: boolean },
): number {
  const compact = options?.compact === true;
  const minCh = compact ? QUANTITY_STEPPER_INPUT_COMPACT_MIN_CH : QUANTITY_STEPPER_INPUT_MIN_CH;
  const maxCh = compact ? QUANTITY_STEPPER_INPUT_COMPACT_MAX_CH : QUANTITY_STEPPER_INPUT_MAX_CH;
  const len = Math.max(1, text.length);
  return Math.min(maxCh, Math.max(minCh, len + QUANTITY_STEPPER_INPUT_PAD_CH));
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

  // Quantity never commits below 1 (empty/0/0.5 → 1). Parent min may be higher.
  const floor = Math.max(1, min > 0 ? min : 1);
  const atMin = numeric <= floor + 1e-12;
  const atMax = max != null && numeric >= max - 1e-12;
  // Allow minus while draft is empty/0 so empty → floor (1) and 0 → floor, even if committed value is already at min.
  const draftRequestsFloor =
    draft != null &&
    (() => {
      const parsed = parseQuantityTyping(draft, precision);
      if (parsed.kind === "empty") {
        return true;
      }
      if (parsed.kind === "value") {
        return parsed.value <= 0 || parsed.value < floor;
      }
      if (parsed.kind === "incomplete") {
        const cleaned = stripQuantityGrouping(draft).replace(/\.$/u, "");
        if (cleaned === "") {
          return true;
        }
        const asNumber = Number(cleaned);
        return Number.isFinite(asNumber) && (asNumber <= 0 || asNumber < floor);
      }
      return false;
    })();
  const minusDisabled =
    disabled ||
    decrementDisabled ||
    (editable ? atMin && !draftRequestsFloor : false);
  const plusDisabled =
    disabled || incrementDisabled || (editable ? atMax : false);

  const visibleText = editable ? (draft ?? displayValue) : String(value);
  const inputWidthCh = quantityStepperInputWidthCh(visibleText, { compact });
  const inputWidthStyle = {
    ["--quantity-stepper-input-width" as string]: `${inputWidthCh}ch`,
    width: `${inputWidthCh}ch`,
  } as const;

  function commit(next: number) {
    if (!onChange || !Number.isFinite(next)) {
      return;
    }
    const clamped = clampQuantityToPrecision(next, precision);
    if (!Number.isFinite(clamped)) {
      return;
    }
    let resolved = clamped;
    if (resolved <= 0 || resolved < floor) {
      resolved = floor;
    }
    if (max != null && resolved > max) {
      resolved = max;
    }
    onChange(resolved);
    setDraft(null);
  }

  /** Normalize typed draft on blur / Enter — never on every keystroke. */
  function commitFromDraft() {
    const raw = (draft ?? displayValue).trim();
    const parsed = parseQuantityTyping(raw, precision);
    if (parsed.kind === "value") {
      // empty/0 → floor (1 when min is 1); values below floor snap up.
      commit(parsed.value <= 0 || parsed.value < floor ? floor : parsed.value);
      return;
    }
    if (parsed.kind === "incomplete" || parsed.kind === "empty") {
      const cleaned = stripQuantityGrouping(raw).replace(/\.$/u, "");
      if (cleaned === "") {
        commit(floor);
        return;
      }
      const asNumber = Number(cleaned);
      if (Number.isFinite(asNumber)) {
        commit(asNumber <= 0 || asNumber < floor ? floor : asNumber);
        return;
      }
    }
    // Invalid text → revert to last committed value.
    setDraft(null);
  }

  function handleMinus() {
    if (minusDisabled) {
      return;
    }
    if (editable && onChange) {
      const base = resolveDraftNumber();
      // empty → 1 (floor), 0 → 1 (floor), 1 → 1 (stay at floor)
      if (base <= floor + 1e-12) {
        commit(floor);
        return;
      }
      commit(
        stepQuantity({
          value: base,
          direction: -1,
          step,
          precision,
          min: floor,
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
          value: resolveDraftNumber(),
          direction: 1,
          step,
          precision,
          min: floor,
          max,
        }),
      );
      return;
    }
    onIncrement?.();
  }

  function resolveDraftNumber(): number {
    const raw = (draft ?? displayValue).trim();
    const parsed = parseQuantityTyping(raw, precision);
    if (parsed.kind === "value" && Number.isFinite(parsed.value)) {
      if (parsed.value <= 0) {
        return floor;
      }
      return Math.max(parsed.value, floor);
    }
    if (parsed.kind === "incomplete" || parsed.kind === "empty") {
      const cleaned = stripQuantityGrouping(raw).replace(/\.$/u, "");
      if (cleaned === "") {
        return floor;
      }
      const asNumber = Number(cleaned);
      if (Number.isFinite(asNumber)) {
        if (asNumber <= 0) {
          return floor;
        }
        return Math.max(asNumber, floor);
      }
    }
    return numeric > 0 ? Math.max(numeric, floor) : floor;
  }

  function handleInputKeyDown(event: KeyboardEvent<HTMLInputElement>) {
    if (event.key === "Enter") {
      event.preventDefault();
      commitFromDraft();
      event.currentTarget.blur();
      return;
    }
    if (event.key === "ArrowUp") {
      event.preventDefault();
      commit(
        stepQuantity({
          value: resolveDraftNumber(),
          direction: 1,
          step,
          precision,
          min: floor,
          max,
        }),
      );
      return;
    }
    if (event.key === "ArrowDown") {
      event.preventDefault();
      const base = resolveDraftNumber();
      if (base <= floor + 1e-12) {
        commit(floor);
        return;
      }
      commit(
        stepQuantity({
          value: base,
          direction: -1,
          step,
          precision,
          min: floor,
          max,
        }),
      );
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
          onMouseDown={(event) => event.preventDefault()}
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
            style={inputWidthStyle}
            value={visibleText}
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
              // Whole units: reject decimal point entirely while typing.
              if (precision <= 0 && nextText.includes(".")) {
                return;
              }
              const parsed = parseQuantityTyping(nextText, precision);
              if (parsed.kind === "invalid") {
                return;
              }
              // Draft-only while typing — allow "1.", "0.", "1.5" without clamping.
              setDraft(nextText);
            }}
            onBlur={() => {
              commitFromDraft();
              setFocused(false);
            }}
            onKeyDown={handleInputKeyDown}
          />
        ) : (
          <span
            className={cn(
              "quantity-stepper__value tabular-nums",
              compact && "quantity-stepper__input--compact",
            )}
            style={inputWidthStyle}
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
          onMouseDown={(event) => event.preventDefault()}
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

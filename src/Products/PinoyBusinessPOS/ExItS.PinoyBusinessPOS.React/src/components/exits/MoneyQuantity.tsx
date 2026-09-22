import { useEffect, useId, useRef, useState, type KeyboardEvent } from "react";
import { formatPeso } from "@/lib/format-money";
import { cn } from "@/lib/cn";
import {
  clampQuantityToPrecision,
  formatQuantityValue,
  maxQuantityDecimals,
  minPositiveQuantity,
  parseQuantityTyping,
  quantityInputMinimum,
  quantityStepperWholeStep,
  stepQuantity,
  stripQuantityGrouping,
} from "@/lib/quantity-rules";
import {
  nextWeightLineQuantityKg,
  usesAdaptiveWeightSteps,
} from "@/cart/sell-cart-helpers";

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
  /**
   * Legacy display-only mode: parent owns step math (Sell cart, Stock Request, returns).
   * Prefer `onChange` for editable purchasing / catalog quantity lines.
   */
  onIncrement?: () => void;
  onDecrement?: () => void;
  /**
   * Controlled editable mode: center is a real numeric input; +/- use step/min/precision.
   * Owns typing, thousands formatting, and decimal rules — do not reimplement in features.
   */
  onChange?: (next: number) => void;
  min?: number;
  max?: number;
  step?: number;
  precision?: number;
  /**
   * Optional catalog profile — when set with sellingMode (and precision/min omitted),
   * resolves whole vs divisible rules via shared quantity-rules metadata (not Kg-only).
   */
  unitOfMeasure?: string;
  sellingMode?: string;
  disabled?: boolean;
  invalid?: boolean;
  unit?: string;
  ariaLabel?: string;
  valueTestId?: string;
  className?: string;
  compact?: boolean;
  /**
   * Visual variant:
   * - **default** — form field group [ neutral − ][ qty ][ primary + ]
   * - **standard / soft / pill** — cart primary capsule with explicit radius
   * - **auto** — cart primary capsule; radius follows Preferences → Control Shape
   *   (`data-control-shape` → `--exits-control-radius`)
   */
  variant?: "default" | "standard" | "soft" | "pill" | "auto";
  /**
   * When set in display-only mode, the center quantity becomes a button
   * (e.g. open weight entry from sell cart).
   */
  onValueClick?: () => void;
  valueClickLabel?: string;
  /**
   * With `onChange`: center starts as a tap target; first tap reveals the numeric input.
   * ± still prefer `onIncrement` / `onDecrement` when provided (sell cart).
   */
  editOnClick?: boolean;
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
export const QUANTITY_STEPPER_INPUT_PILL_MIN_CH = 7;
export const QUANTITY_STEPPER_INPUT_PILL_MAX_CH = 12;
const QUANTITY_STEPPER_INPUT_PAD_CH = 0.5;

/** Width in `ch` from current text, clamped so layout stays compact and mobile-safe. */
export function quantityStepperInputWidthCh(
  text: string,
  options?: { compact?: boolean; cart?: boolean; /** @deprecated use cart */ pill?: boolean },
): number {
  if (options?.cart === true || options?.pill === true) {
    const len = Math.max(1, text.length);
    return Math.min(
      QUANTITY_STEPPER_INPUT_PILL_MAX_CH,
      Math.max(QUANTITY_STEPPER_INPUT_PILL_MIN_CH, len + 0.25),
    );
  }
  const compact = options?.compact === true;
  const minCh = compact ? QUANTITY_STEPPER_INPUT_COMPACT_MIN_CH : QUANTITY_STEPPER_INPUT_MIN_CH;
  const maxCh = compact ? QUANTITY_STEPPER_INPUT_COMPACT_MAX_CH : QUANTITY_STEPPER_INPUT_MAX_CH;
  const len = Math.max(1, text.length);
  return Math.min(maxCh, Math.max(minCh, len + QUANTITY_STEPPER_INPUT_PAD_CH));
}

/**
 * Canonical ExItS quantity control.
 * - **default** — form field group: [ neutral − ][ editable qty ][ primary + ]
 * - **standard / soft / pill** — cart primary capsule with explicit Control Shape radius
 * - **auto** — cart primary capsule; adopts global Control Shape via `--exits-control-radius`
 * - **editOnClick** — center tap reveals numeric input (sell cart non-kg)
 * - **onValueClick** — center tap opens parent dialog (sell cart kg / `1.5kg`)
 * Editable when `onChange` is set; display-only when using increment/decrement callbacks.
 * Interaction (typing, ±1 with remainder, measured 2dp trailing zeros, thousands) lives here + quantity-rules.
 */
export function QuantityStepper({
  value,
  onIncrement,
  onDecrement,
  onChange,
  min: minProp,
  max,
  step: stepProp,
  precision: precisionProp,
  unitOfMeasure,
  sellingMode,
  disabled = false,
  invalid = false,
  unit,
  ariaLabel,
  increaseLabel,
  decreaseLabel,
  valueTestId,
  className,
  compact = false,
  variant = "default",
  onValueClick,
  valueClickLabel,
  editOnClick = false,
  incrementDisabled = false,
  decrementDisabled = false,
}: QuantityStepperProps) {
  const inputId = useId();
  const inputRef = useRef<HTMLInputElement | null>(null);
  const [clickEditing, setClickEditing] = useState(false);
  const hasParentSteps =
    typeof onIncrement === "function" || typeof onDecrement === "function";
  const canEditValue = typeof onChange === "function";
  const showInput = canEditValue && (!editOnClick || clickEditing);
  const editable = showInput;
  const cartCapsule =
    variant === "pill" ||
    variant === "soft" ||
    variant === "standard" ||
    variant === "auto";
  const hasCatalogProfile = unitOfMeasure != null && sellingMode != null;
  const precision =
    precisionProp ??
    (hasCatalogProfile ? maxQuantityDecimals(unitOfMeasure, sellingMode) : 0);
  const min =
    minProp ??
    (hasCatalogProfile
      ? quantityInputMinimum(unitOfMeasure, sellingMode)
      : precision > 0
        ? minPositiveQuantity(precision)
        : 1);
  const step = stepProp ?? quantityStepperWholeStep();
  const numeric = numericValue(value);
  const formattedValue = formatQuantityValue(numeric, precision);
  const displayValue = canEditValue ? formattedValue : String(value);

  const [draft, setDraft] = useState<string | null>(null);
  const [focused, setFocused] = useState(false);

  useEffect(() => {
    if (!focused) {
      setDraft(null);
    }
  }, [value, focused]);

  useEffect(() => {
    if (!editOnClick || !clickEditing) {
      return;
    }
    const node = inputRef.current;
    if (!node) {
      return;
    }
    node.focus();
    node.select();
  }, [editOnClick, clickEditing]);

  // Effective minimum: respect explicit `min` (including 0 for classification / write-off).
  // When omitted, `min` already defaults to 1 (whole) or measured min-positive.
  const floor = min;
  const adaptiveWeightSteps = usesAdaptiveWeightSteps({ unitOfMeasure, sellingMode });
  const atMin = numeric <= floor + 1e-12;
  const atMax = max != null && numeric >= max - 1e-12;
  // Allow minus while draft is empty/0 so empty → floor, even if committed value is already at min.
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
  // Parent-owned ± (sell cart): never lock minus/plus from local floor — parent may remove at ≤0.
  // Controlled onChange (PO items, etc.): lock at floor/max whether idle or click-editing.
  const minusDisabled =
    disabled ||
    decrementDisabled ||
    (canEditValue && !hasParentSteps
      ? editable
        ? atMin && !draftRequestsFloor
        : atMin
      : false);
  const plusDisabled =
    disabled ||
    incrementDisabled ||
    (canEditValue && !hasParentSteps ? atMax : false);

  const buttonLabel = canEditValue ? formattedValue : String(value);
  const visibleText = editable ? (draft ?? displayValue) : buttonLabel;
  const inputWidthCh = quantityStepperInputWidthCh(visibleText, { compact, cart: cartCapsule });
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
    if (hasParentSteps) {
      // Sell cart: 0 (or less) lets the parent remove the line.
      if (resolved < 0) {
        resolved = 0;
      }
      if (max != null && resolved > max) {
        resolved = max;
      }
      onChange(resolved);
      setDraft(null);
      return;
    }
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

  function endClickEdit() {
    commitFromDraft();
    setFocused(false);
    setClickEditing(false);
  }

  function stepControlledValue(base: number, direction: 1 | -1): void {
    if (adaptiveWeightSteps) {
      commit(nextWeightLineQuantityKg(base, direction, floor));
      return;
    }
    if (direction < 0 && base <= floor + 1e-12) {
      commit(floor);
      return;
    }
    commit(
      stepQuantity({
        value: base,
        direction,
        step,
        precision,
        min: floor,
        max,
      }),
    );
  }

  function handleMinus() {
    if (minusDisabled) {
      return;
    }
    if (hasParentSteps) {
      onDecrement?.();
      return;
    }
    if (onChange) {
      stepControlledValue(editable ? resolveDraftNumber() : numeric, -1);
      return;
    }
    onDecrement?.();
  }

  function handlePlus() {
    if (plusDisabled) {
      return;
    }
    if (hasParentSteps) {
      onIncrement?.();
      return;
    }
    if (onChange) {
      stepControlledValue(editable ? resolveDraftNumber() : numeric, 1);
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
      if (editOnClick) {
        endClickEdit();
        event.currentTarget.blur();
        return;
      }
      commitFromDraft();
      event.currentTarget.blur();
      return;
    }
    if (event.key === "ArrowUp") {
      event.preventDefault();
      if (hasParentSteps) {
        onIncrement?.();
        return;
      }
      if (onChange) {
        stepControlledValue(resolveDraftNumber(), 1);
      }
      return;
    }
    if (event.key === "ArrowDown") {
      event.preventDefault();
      if (hasParentSteps) {
        onDecrement?.();
        return;
      }
      if (onChange) {
        stepControlledValue(resolveDraftNumber(), -1);
      }
    }
  }

  return (
    <div
      className={cn(
        "quantity-stepper flex min-w-0 items-center gap-1.5",
        compact && "quantity-stepper--compact",
        cartCapsule && "quantity-stepper--cart",
        variant !== "default" && `quantity-stepper--${variant}`,
        invalid && "quantity-stepper--invalid",
        disabled && "quantity-stepper--disabled",
        className,
      )}
      data-testid="quantity-stepper"
      data-variant={variant}
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
            ref={inputRef}
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
              if (editOnClick) {
                endClickEdit();
                return;
              }
              commitFromDraft();
              setFocused(false);
            }}
            onKeyDown={handleInputKeyDown}
          />
        ) : canEditValue && editOnClick ? (
          <button
            type="button"
            className={cn(
              "quantity-stepper__value quantity-stepper__value--button tabular-nums",
              compact && "quantity-stepper__input--compact",
            )}
            style={inputWidthStyle}
            data-testid={valueTestId}
            aria-label={valueClickLabel ?? ariaLabel ?? "Edit quantity"}
            disabled={disabled}
            onClick={() => {
              setClickEditing(true);
              setDraft(formattedValue);
            }}
          >
            {buttonLabel}
          </button>
        ) : onValueClick ? (
          <button
            type="button"
            className={cn(
              "quantity-stepper__value quantity-stepper__value--button tabular-nums",
              compact && "quantity-stepper__input--compact",
            )}
            style={inputWidthStyle}
            data-testid={valueTestId}
            aria-label={valueClickLabel ?? ariaLabel ?? "Edit quantity"}
            disabled={disabled}
            onClick={onValueClick}
          >
            {buttonLabel}
          </button>
        ) : (
          <span
            className={cn(
              "quantity-stepper__value tabular-nums",
              compact && "quantity-stepper__input--compact",
            )}
            style={inputWidthStyle}
            data-testid={valueTestId}
          >
            {buttonLabel}
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

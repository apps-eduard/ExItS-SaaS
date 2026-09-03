import { Button } from "@/components/ui/button";
import { formatPeso } from "@/lib/format-money";
import { cn } from "@/lib/cn";

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

export function QuantityStepper({
  value,
  onIncrement,
  onDecrement,
  increaseLabel,
  decreaseLabel,
  valueTestId,
  className,
  compact = false,
  incrementDisabled = false,
  decrementDisabled = false,
}: {
  value: number | string;
  onIncrement: () => void;
  onDecrement: () => void;
  increaseLabel: string;
  decreaseLabel: string;
  valueTestId?: string;
  className?: string;
  compact?: boolean;
  incrementDisabled?: boolean;
  decrementDisabled?: boolean;
}) {
  const buttonClass = compact
    ? "size-8 min-h-8 shrink-0 p-0 text-[length:var(--exits-text-sm)]"
    : "border border-border";
  const valueClass = compact
    ? "min-w-[1.5rem] text-center text-[length:var(--exits-text-xs)]"
    : "min-w-[2rem] text-center";

  return (
    <div
      className={cn("flex items-center", compact ? "gap-1" : "gap-2", className)}
      data-testid="quantity-stepper"
    >
      <Button
        type="button"
        variant="ghost"
        className={cn(buttonClass, compact && "border border-border")}
        aria-label={decreaseLabel}
        disabled={decrementDisabled}
        onClick={onDecrement}
      >
        −
      </Button>
      <QuantityDisplay value={value} testId={valueTestId} className={valueClass} />
      <Button
        type="button"
        variant="ghost"
        className={cn(buttonClass, compact && "border border-border")}
        aria-label={increaseLabel}
        disabled={incrementDisabled}
        onClick={onIncrement}
      >
        +
      </Button>
    </div>
  );
}

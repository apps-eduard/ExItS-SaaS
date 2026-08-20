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
}: {
  value: number | string;
  onIncrement: () => void;
  onDecrement: () => void;
  increaseLabel: string;
  decreaseLabel: string;
  valueTestId?: string;
  className?: string;
}) {
  return (
    <div className={cn("flex items-center gap-2", className)} data-testid="quantity-stepper">
      <Button
        type="button"
        variant="ghost"
        className="border border-border"
        aria-label={decreaseLabel}
        onClick={onDecrement}
      >
        −
      </Button>
      <QuantityDisplay value={value} testId={valueTestId} className="min-w-[2rem] text-center" />
      <Button
        type="button"
        variant="ghost"
        className="border border-border"
        aria-label={increaseLabel}
        onClick={onIncrement}
      >
        +
      </Button>
    </div>
  );
}

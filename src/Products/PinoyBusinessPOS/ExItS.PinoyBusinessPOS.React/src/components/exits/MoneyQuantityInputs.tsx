import type { ChangeEvent, FocusEvent, InputHTMLAttributes } from "react";
import { Input } from "@/components/ui/input";
import { cn } from "@/lib/cn";
import {
  formatMoneyAmountInput,
  normalizeMoneyAmountTyping,
  parseMoneyAmountInput,
} from "@/lib/money-input";

export function MoneyInput({
  label,
  className,
  value,
  onChange,
  onBlur,
  ...props
}: InputHTMLAttributes<HTMLInputElement> & { label: string }) {
  function emitChange(e: ChangeEvent<HTMLInputElement>, next: string) {
    if (!onChange) {
      return;
    }
    onChange({
      ...e,
      target: { ...e.target, value: next },
      currentTarget: { ...e.currentTarget, value: next },
    });
  }

  return (
    <Input
      label={label}
      {...props}
      inputMode="decimal"
      className={cn("tabular-nums", className)}
      value={value}
      onChange={(e) => emitChange(e, normalizeMoneyAmountTyping(e.target.value))}
      onBlur={(e: FocusEvent<HTMLInputElement>) => {
        const parsed = parseMoneyAmountInput(String(e.target.value ?? ""));
        if (parsed !== null) {
          emitChange(e as unknown as ChangeEvent<HTMLInputElement>, formatMoneyAmountInput(parsed));
        }
        onBlur?.(e);
      }}
    />
  );
}

export function QuantityInput({
  label,
  className,
  ...props
}: InputHTMLAttributes<HTMLInputElement> & { label: string }) {
  return (
    <Input label={label} inputMode="decimal" className={cn("tabular-nums", className)} {...props} />
  );
}

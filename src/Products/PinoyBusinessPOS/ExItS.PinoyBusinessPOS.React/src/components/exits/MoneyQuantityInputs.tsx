import type { InputHTMLAttributes } from "react";
import { Input } from "@/components/ui/input";
import { cn } from "@/lib/cn";

export function MoneyInput({
  label,
  className,
  ...props
}: InputHTMLAttributes<HTMLInputElement> & { label: string }) {
  return (
    <Input label={label} inputMode="decimal" className={cn("tabular-nums", className)} {...props} />
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

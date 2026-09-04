import type { InputHTMLAttributes, ReactNode } from "react";
import { cn } from "@/lib/cn";

export function Input({
  label,
  labelAccessory,
  className,
  id,
  ...props
}: InputHTMLAttributes<HTMLInputElement> & {
  label: string;
  labelAccessory?: ReactNode;
}) {
  const fieldId = id ?? props.name;
  return (
    <div className="flex min-w-0 flex-col gap-1.5">
      <div className="flex items-center gap-1.5">
        <label htmlFor={fieldId} className="text-[length:var(--exits-text-sm)] font-semibold">
          {label}
        </label>
        {labelAccessory}
      </div>
      <input
        id={fieldId}
        className={cn(
          "h-[var(--exits-control-height)] min-h-[var(--exits-control-height)] w-full rounded-[var(--exits-radius-md)] border border-border bg-surface px-[var(--exits-control-padding-x)] text-[length:var(--exits-text-md)] text-foreground transition-[border-color,box-shadow] duration-[var(--exits-motion-fast)] placeholder:text-[var(--exits-text-subtle)] hover:border-[var(--exits-border-strong)] focus-visible:outline-none focus-visible:border-[var(--exits-ring)] focus-visible:ring-2 focus-visible:ring-[var(--exits-ring)] disabled:cursor-not-allowed disabled:opacity-50 disabled:bg-[var(--exits-surface-muted)]",
          className,
        )}
        {...props}
      />
    </div>
  );
}

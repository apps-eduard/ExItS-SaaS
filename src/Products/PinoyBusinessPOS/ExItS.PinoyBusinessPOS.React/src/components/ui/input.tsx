import type { InputHTMLAttributes, ReactNode } from "react";
import { cn } from "@/lib/cn";

/**
 * Shared ExItS text Input — form field radius (not Control Shape).
 * Focus: Form Field Focus Standard (Primary 1px border + 1px soft ring).
 */
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
        <label htmlFor={fieldId} className="exits-type-label">
          {label}
        </label>
        {labelAccessory}
      </div>
      <input
        id={fieldId}
        className={cn(
          "exits-input h-[var(--exits-control-height)] min-h-[var(--exits-control-height)] w-full rounded-[var(--exits-field-radius)] border border-border bg-surface px-[var(--exits-control-padding-x)] text-[length:var(--exits-text-md)] text-foreground transition-[border-color,box-shadow] duration-[var(--exits-motion-fast)] ease-[var(--exits-ease-standard)] placeholder:text-[var(--exits-text-subtle)] hover:border-[var(--exits-field-border-hover)] focus:outline-none focus:border-[var(--exits-field-border-focus)] focus:shadow-[0_0_0_var(--exits-field-focus-ring-width)_var(--exits-field-focus-ring)] focus-visible:outline-none focus-visible:border-[var(--exits-field-border-focus)] focus-visible:shadow-[0_0_0_var(--exits-field-focus-ring-width)_var(--exits-field-focus-ring)] aria-invalid:border-[var(--exits-field-border-error)] aria-invalid:focus:border-[var(--exits-field-border-error)] aria-invalid:focus:shadow-[0_0_0_var(--exits-field-focus-ring-width)_var(--exits-field-focus-ring-error)] aria-invalid:focus-visible:border-[var(--exits-field-border-error)] aria-invalid:focus-visible:shadow-[0_0_0_var(--exits-field-focus-ring-width)_var(--exits-field-focus-ring-error)] disabled:cursor-not-allowed disabled:opacity-50 disabled:bg-[var(--exits-surface-muted)]",
          className,
        )}
        {...props}
      />
    </div>
  );
}

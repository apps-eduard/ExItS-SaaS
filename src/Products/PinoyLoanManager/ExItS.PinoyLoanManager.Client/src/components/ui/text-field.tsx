import type { InputHTMLAttributes, ReactNode } from "react";
import { cn } from "@/lib/cn";

export function TextField({
  label,
  error,
  trailing,
  className,
  id,
  ...props
}: InputHTMLAttributes<HTMLInputElement> & {
  label: string;
  error?: string;
  trailing?: ReactNode;
}) {
  const fieldId = id ?? props.name;
  const errorId = error && fieldId ? `${fieldId}-error` : undefined;
  return (
    <div className="flex min-w-0 flex-col gap-1.5">
      <label htmlFor={fieldId} className="text-[length:var(--exits-text-sm)] font-semibold">
        {label}
      </label>
      <div className="relative">
        <input
          id={fieldId}
          aria-invalid={error ? true : undefined}
          aria-describedby={errorId}
          className={cn(
            "h-[var(--exits-control-height)] min-h-[var(--exits-touch-target-min)] w-full rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 text-[length:var(--exits-text-md)] text-foreground",
            trailing ? "pr-12" : "",
            className,
          )}
          {...props}
        />
        {trailing ? (
          <div className="absolute inset-y-0 right-0 flex items-center pr-1">{trailing}</div>
        ) : null}
      </div>
      {error ? (
        <p
          id={errorId}
          className="m-0 text-[length:var(--exits-text-sm)] text-destructive"
          role="alert"
        >
          {error}
        </p>
      ) : null}
    </div>
  );
}

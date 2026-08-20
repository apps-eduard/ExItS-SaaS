import type { InputHTMLAttributes } from "react";
import { cn } from "@/lib/cn";

export function Input({
  label,
  className,
  id,
  ...props
}: InputHTMLAttributes<HTMLInputElement> & { label: string }) {
  const fieldId = id ?? props.name;
  return (
    <div className="flex min-w-0 flex-col gap-1.5">
      <label htmlFor={fieldId} className="text-[length:var(--exits-text-sm)] font-semibold">
        {label}
      </label>
      <input
        id={fieldId}
        className={cn(
          "h-[var(--exits-control-height)] min-h-[var(--exits-touch-target-min)] w-full rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 text-[length:var(--exits-text-md)] text-foreground",
          className,
        )}
        {...props}
      />
    </div>
  );
}

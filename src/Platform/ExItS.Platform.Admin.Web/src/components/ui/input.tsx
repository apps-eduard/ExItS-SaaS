import type { InputHTMLAttributes } from "react";
import { cn } from "@/lib/utils";

export function Input({ className, ...props }: InputHTMLAttributes<HTMLInputElement>) {
  return (
    <input
      className={cn(
        "h-[var(--exits-control-height)] min-h-[var(--exits-touch-target-min)] w-full rounded-[var(--exits-density-radius)] border border-input bg-surface px-3 text-[length:var(--exits-text-md)] text-foreground shadow-sm transition-[border-color,box-shadow] duration-[var(--exits-motion-fast)] placeholder:text-muted disabled:bg-[var(--exits-disabled-bg)] disabled:text-[var(--exits-disabled-text)]",
        className,
      )}
      {...props}
    />
  );
}

import * as React from "react";

import { cn } from "@/lib/utils";

export type InputProps = React.InputHTMLAttributes<HTMLInputElement>;

export function Input({ className, type, ...props }: InputProps) {
  return (
    <input
      type={type}
      className={cn(
        "flex h-11 w-full rounded-md border border-borderDefault bg-surface px-4 py-3 text-primary placeholder:text-muted shadow-none",
        "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brandBright",
        "disabled:cursor-not-allowed disabled:opacity-50",
        className,
      )}
      {...props}
    />
  );
}


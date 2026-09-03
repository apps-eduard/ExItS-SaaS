import type { ReactNode } from "react";

import { cn } from "@/lib/utils";

export function ExItsFormField({
  id,
  label,
  error,
  description,
  className,
  children,
}: {
  id: string;
  label: string;
  error?: string;
  description?: string;
  className?: string;
  children: ReactNode;
}) {
  const errorId = `${id}-error`;
  const descriptionId = `${id}-description`;

  return (
    <div className={cn("space-y-2", className)}>
      <label htmlFor={id} className="block text-sm font-medium text-primary">
        {label}
      </label>
      {description ? (
        <p id={descriptionId} className="text-sm text-muted">
          {description}
        </p>
      ) : null}
      {children}
      {error ? (
        <p id={errorId} role="alert" className="text-sm text-red-300">
          {error}
        </p>
      ) : null}
    </div>
  );
}

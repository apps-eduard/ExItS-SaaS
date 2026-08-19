import type { HTMLAttributes } from "react";
import { cn } from "@/lib/utils";

type AlertProps = HTMLAttributes<HTMLDivElement> & {
  title: string;
};

export function Alert({ title, className, children, ...props }: AlertProps) {
  return (
    <div
      role="status"
      className={cn(
        "rounded-[var(--exits-density-radius)] border border-border bg-[var(--exits-info-bg)] p-[var(--exits-density-space-unit)] text-info",
        className,
      )}
      {...props}
    >
      <p className="font-semibold text-foreground">{title}</p>
      <div className="mt-1 text-[length:var(--exits-text-sm)] text-foreground">{children}</div>
    </div>
  );
}

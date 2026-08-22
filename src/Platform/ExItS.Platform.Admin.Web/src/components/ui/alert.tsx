import type { HTMLAttributes } from "react";
import { cn } from "@/lib/utils";

type AlertProps = HTMLAttributes<HTMLDivElement> & {
  title: string;
  tone?: "info" | "danger" | "success";
};

export function Alert({ title, className, children, tone = "info", ...props }: AlertProps) {
  return (
    <div
      role={tone === "danger" ? "alert" : "status"}
      className={cn(
        "rounded-[var(--exits-density-radius)] border p-[var(--exits-density-space-unit)]",
        tone === "danger" && "border-destructive bg-[var(--exits-danger-bg)] text-destructive",
        tone === "success" && "border-success bg-[var(--exits-success-bg)] text-success",
        tone === "info" && "border-border bg-[var(--exits-info-bg)] text-info",
        className,
      )}
      {...props}
    >
      <p className="font-semibold text-foreground">{title}</p>
      {children ? (
        <div className="mt-1 text-[length:var(--exits-text-sm)] text-foreground">{children}</div>
      ) : null}
    </div>
  );
}

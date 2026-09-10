import type { ReactNode } from "react";
import { cn } from "@/lib/cn";

export function EmptyState({
  title,
  detail,
  action,
  align = "start",
}: {
  title: string;
  detail: string;
  /** Optional next-step control (permission-gated by caller). */
  action?: ReactNode;
  align?: "start" | "center";
}) {
  return (
    <div
      className={cn(
        "flex flex-col gap-2 rounded-[var(--exits-radius-md)] border border-dashed border-border px-4 py-6",
        align === "center" && "items-center text-center",
      )}
    >
      <p className="exits-type-label m-0">{title}</p>
      <p className="exits-type-muted m-0">{detail}</p>
      {action ? <div className="mt-1">{action}</div> : null}
    </div>
  );
}

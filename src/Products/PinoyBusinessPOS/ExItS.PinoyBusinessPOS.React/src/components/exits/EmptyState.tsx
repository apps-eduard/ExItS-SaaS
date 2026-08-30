import type { ReactNode } from "react";

export function EmptyState({
  title,
  detail,
  action,
}: {
  title: string;
  detail: string;
  /** Optional next-step control (permission-gated by caller). */
  action?: ReactNode;
}) {
  return (
    <div className="flex flex-col gap-2 rounded-[var(--exits-radius-md)] border border-dashed border-border px-4 py-6">
      <p className="m-0 font-semibold">{title}</p>
      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{detail}</p>
      {action ? <div className="mt-1">{action}</div> : null}
    </div>
  );
}

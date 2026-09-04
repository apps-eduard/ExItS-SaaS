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
      <p className="exits-type-label m-0">{title}</p>
      <p className="exits-type-muted m-0">{detail}</p>
      {action ? <div className="mt-1">{action}</div> : null}
    </div>
  );
}

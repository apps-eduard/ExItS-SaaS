import type { ReactNode } from "react";
import { Button } from "@/components/ui/button";

export function EmptyState({
  title,
  description,
  actionLabel,
  onAction,
  children,
}: {
  title: string;
  description?: string;
  actionLabel?: string;
  onAction?: () => void;
  children?: ReactNode;
}) {
  return (
    <section
      className="grid max-w-xl gap-2 rounded-[var(--exits-density-radius)] border border-dashed border-border bg-surface p-[var(--exits-density-space-unit)]"
      data-state="empty"
    >
      <h2 className="text-[length:var(--exits-text-base)] font-semibold text-foreground">{title}</h2>
      {description ? (
        <p className="text-[length:var(--exits-text-sm)] text-muted break-words">{description}</p>
      ) : null}
      {children}
      {actionLabel && onAction ? (
        <div className="mt-2">
          <Button type="button" size="sm" variant="outline" onClick={onAction}>
            {actionLabel}
          </Button>
        </div>
      ) : null}
    </section>
  );
}

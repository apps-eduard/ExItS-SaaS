import type { LucideIcon } from "lucide-react";
import { Link } from "react-router-dom";
import { cn } from "@/lib/cn";

export type ActionCardProps = {
  to: string;
  title: string;
  subtitle?: string;
  icon: LucideIcon;
  testId?: string;
  className?: string;
};

/**
 * Flat navigational action tile — icon left, title + subtitle stacked on the right.
 */
export function ActionCard({
  to,
  title,
  subtitle,
  icon: Icon,
  testId,
  className,
}: ActionCardProps) {
  return (
    <Link
      to={to}
      data-testid={testId}
      className={cn(
        "flex min-w-0 items-start gap-2.5 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 py-3 text-foreground no-underline shadow-[0_1px_2px_color-mix(in_srgb,var(--exits-foreground)_6%,transparent)] transition-colors hover:bg-[var(--exits-surface-muted)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
        className,
      )}
    >
      <Icon className="mt-0.5 size-5 shrink-0 text-primary" aria-hidden />
      <span className="min-w-0 flex-1">
        <span className="exits-type-card-title block wrap-break-word text-[length:var(--exits-text-sm)]">
          {title}
        </span>
        {subtitle ? (
          <span className="exits-type-muted mt-0.5 block wrap-break-word text-[length:var(--exits-text-xs)]">
            {subtitle}
          </span>
        ) : null}
      </span>
    </Link>
  );
}

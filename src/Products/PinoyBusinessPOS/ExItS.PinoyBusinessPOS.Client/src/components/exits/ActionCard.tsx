import type { LucideIcon } from "lucide-react";
import { ChevronRight } from "lucide-react";
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
 * Flat navigational action tile — icon + title + chevron, entire card clickable.
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
        "group flex min-h-[4.5rem] min-w-0 items-center gap-3 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 py-3 text-foreground no-underline shadow-[0_1px_2px_color-mix(in_srgb,var(--exits-foreground)_6%,transparent)] transition-colors hover:bg-[var(--exits-surface-muted)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
        className,
      )}
    >
      <span className="inline-flex size-10 shrink-0 items-center justify-center rounded-[var(--exits-radius-md)] bg-[var(--exits-surface-muted)] text-primary">
        <Icon className="size-5" aria-hidden />
      </span>
      <span className="min-w-0 flex-1">
        <span className="block text-[length:var(--exits-text-sm)] font-semibold wrap-break-word">
          {title}
        </span>
        {subtitle ? (
          <span className="mt-0.5 block text-[length:var(--exits-text-xs)] text-muted wrap-break-word">
            {subtitle}
          </span>
        ) : null}
      </span>
      <ChevronRight
        className="size-5 shrink-0 text-muted group-hover:text-foreground"
        aria-hidden
      />
    </Link>
  );
}

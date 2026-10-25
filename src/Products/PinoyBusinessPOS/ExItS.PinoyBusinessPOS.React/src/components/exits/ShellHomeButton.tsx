import { Home } from "lucide-react";
import { Link } from "react-router-dom";
import { ExitsTooltip } from "@/components/exits/ExitsTooltip";
import { cn } from "@/lib/cn";

export type ShellHomeButtonProps = {
  to: string;
  label: string;
  testId?: string;
  className?: string;
};

/**
 * Compact shell Home control — icon only; Soft Glass tooltip.
 * Caller supplies Personal (`/personal`) or Organization experience home href.
 */
export function ShellHomeButton({
  to,
  label,
  testId = "shell-home-button",
  className,
}: ShellHomeButtonProps) {
  return (
    <ExitsTooltip content={label}>
      <Link
        to={to}
        data-testid={testId}
        aria-label={label}
        className={cn(
          "relative inline-flex size-11 min-w-11 shrink-0 items-center justify-center rounded-full text-foreground no-underline transition-colors hover:bg-[var(--exits-surface-muted)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
          className,
        )}
      >
        <Home className="size-5" aria-hidden strokeWidth={2.25} />
        <span className="sr-only">{label}</span>
      </Link>
    </ExitsTooltip>
  );
}

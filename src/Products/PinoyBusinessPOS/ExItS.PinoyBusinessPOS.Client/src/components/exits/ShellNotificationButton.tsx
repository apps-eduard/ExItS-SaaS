import { Bell } from "lucide-react";
import { Link } from "react-router-dom";
import { cn } from "@/lib/cn";

export type ShellNotificationButtonProps = {
  to: string;
  label: string;
  unreadLabel: string;
  badge: string | null;
  testId?: string;
  className?: string;
};

/**
 * Compact shell notifications control — presentation only.
 * Unread badge text is caller-supplied from authoritative data.
 */
export function ShellNotificationButton({
  to,
  label,
  unreadLabel,
  badge,
  testId = "shell-notification-bell",
  className,
}: ShellNotificationButtonProps) {
  const accessibleName = badge ? unreadLabel.replace("{count}", badge) : label;

  return (
    <Link
      to={to}
      data-testid={testId}
      aria-label={accessibleName}
      className={cn(
        "relative inline-flex size-11 min-h-11 min-w-11 shrink-0 items-center justify-center rounded-[var(--exits-radius-md)] text-foreground no-underline transition-colors hover:bg-[var(--exits-surface-muted)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
        className,
      )}
    >
      <Bell className="size-5" aria-hidden />
      {badge ? (
        <span
          data-testid={`${testId}-badge`}
          className="absolute top-1 right-1 inline-flex min-w-[1.1rem] items-center justify-center rounded-full bg-primary px-1 text-[0.65rem] font-semibold leading-4 text-primary-foreground"
          aria-hidden
        >
          {badge}
        </span>
      ) : null}
      <span className="sr-only">{accessibleName}</span>
    </Link>
  );
}

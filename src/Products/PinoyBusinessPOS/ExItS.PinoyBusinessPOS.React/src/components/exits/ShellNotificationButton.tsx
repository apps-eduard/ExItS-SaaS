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
  /** When set, click runs this instead of a plain route Link (e.g. switch profile then go). */
  onNavigate?: () => void | Promise<void>;
  disabled?: boolean;
  /** Router location state passed through the Link. */
  linkState?: unknown;
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
  onNavigate,
  disabled = false,
  linkState,
}: ShellNotificationButtonProps) {
  const accessibleName = badge ? unreadLabel.replace("{count}", badge) : label;
  const sharedClassName = cn(
    "relative inline-flex size-11 min-h-11 min-w-11 shrink-0 items-center justify-center rounded-full text-foreground no-underline transition-colors hover:bg-[var(--exits-surface-muted)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
    disabled && "pointer-events-none opacity-60",
    className,
  );

  const content = (
    <>
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
    </>
  );

  if (onNavigate) {
    return (
      <button
        type="button"
        data-testid={testId}
        aria-label={accessibleName}
        className={sharedClassName}
        disabled={disabled}
        onClick={() => {
          void onNavigate();
        }}
      >
        {content}
      </button>
    );
  }

  return (
    <Link
      to={to}
      state={linkState}
      data-testid={testId}
      aria-label={accessibleName}
      className={sharedClassName}
    >
      {content}
    </Link>
  );
}

import type { CSSProperties } from "react";
import type { LucideIcon } from "lucide-react";
import { Link } from "react-router-dom";
import { cn } from "@/lib/cn";

export type RoleActionTileProps = {
  label: string;
  icon: LucideIcon;
  testId?: string;
  primary?: boolean;
  disabled?: boolean;
  className?: string;
  style?: CSSProperties;
} & ({ to: string; onClick?: never } | { to?: never; onClick: () => void });

/**
 * Owner-dashboard-style action tile: icon left of label, no chevron.
 * Used by Manager (and related) role homes for touch-friendly grids.
 */
export function RoleActionTile(props: RoleActionTileProps) {
  const {
    label,
    icon: Icon,
    testId,
    primary = false,
    disabled = false,
    className,
    style,
  } = props;
  const classes = cn(
    "role-action-tile inline-flex min-h-11 w-full items-center gap-2 rounded-[var(--exits-radius-md)] border px-3 py-2.5 text-left text-[length:var(--exits-text-sm)] font-semibold no-underline focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
    primary
      ? "role-action-tile--primary border-primary bg-primary text-primary-foreground"
      : "border-border bg-surface text-foreground",
    disabled && "pointer-events-none opacity-50",
    className,
  );
  const content = (
    <>
      <span
        className={cn(
          "role-action-tile__icon inline-flex size-5 shrink-0 items-center justify-center",
          primary ? "text-primary-foreground" : "text-primary",
        )}
        aria-hidden
      >
        <Icon className="size-5" />
      </span>
      <span className="role-action-tile__label min-w-0 wrap-break-word">{label}</span>
    </>
  );

  if ("to" in props && props.to) {
    if (disabled) {
      return (
        <span
          data-testid={testId}
          className={classes}
          style={style}
          aria-disabled="true"
          role="link"
        >
          {content}
        </span>
      );
    }
    return (
      <Link to={props.to} data-testid={testId} className={classes} style={style}>
        {content}
      </Link>
    );
  }

  return (
    <button
      type="button"
      data-testid={testId}
      className={classes}
      style={style}
      disabled={disabled}
      onClick={props.onClick}
    >
      {content}
    </button>
  );
}

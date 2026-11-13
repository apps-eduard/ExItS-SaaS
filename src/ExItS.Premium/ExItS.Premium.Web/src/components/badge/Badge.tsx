import type { HTMLAttributes, ReactNode } from "react";
import { AppIcon } from "../../icons/ExIcon";
import styles from "./Badge.module.scss";

/** Neutral plus the same semantic set as Button (ghost stays an appearance). */
export const BADGE_TONES = [
  "neutral",
  "primary",
  "secondary",
  "accent",
  "success",
  "info",
  "warning",
  "help",
  "danger",
  "contrast",
] as const;

export const BADGE_APPEARANCES = ["soft", "emphasis", "solid", "outline", "ghost"] as const;

export type BadgeTone = (typeof BADGE_TONES)[number];
export type BadgeAppearance = (typeof BADGE_APPEARANCES)[number];

/** Compact count label — caps at 99+. */
export function formatBadgeCount(count: number) {
  const value = Number.isFinite(count) ? Math.max(0, Math.floor(count)) : 0;
  return value > 99 ? "99+" : String(value);
}

type BadgeProps = {
  tone?: BadgeTone;
  appearance?: BadgeAppearance;
  /** Uppercase the label. */
  allCaps?: boolean;
  /** Leading status pip (or pip-only when there is no label). */
  indicator?: boolean;
  /** Compact numeric count chip (sample / nav counts — not a playground Cursor flag). */
  count?: number;
  /** Show a trailing remove control. */
  removable?: boolean;
  /** Called when the remove control is activated. */
  onRemove?: () => void;
  /** Accessible name for the remove control. */
  removeLabel?: string;
  children?: ReactNode;
} & Omit<HTMLAttributes<HTMLSpanElement>, "children" | "color">;

export function Badge({
  tone = "neutral",
  appearance = "soft",
  allCaps = false,
  indicator = false,
  count,
  removable = false,
  onRemove,
  removeLabel = "Remove",
  children,
  className,
  ...rest
}: BadgeProps) {
  const isCount = typeof count === "number";
  const countLabel = isCount ? formatBadgeCount(count) : null;
  const hasLabel = isCount || (children != null && children !== false && children !== "");
  const indicatorOnly = indicator && !hasLabel;
  const showRemove = removable && !indicatorOnly && !isCount;

  return (
    <span
      {...rest}
      className={[styles.badge, styles[tone], className].filter(Boolean).join(" ")}
      data-tone={tone}
      data-appearance={appearance}
      data-all-caps={allCaps ? "true" : "false"}
      data-indicator={indicator ? "true" : "false"}
      data-indicator-only={indicatorOnly ? "true" : "false"}
      data-count={isCount ? "true" : "false"}
      data-removable={showRemove ? "true" : "false"}
    >
      {indicator && !isCount ? <span className={styles.indicator} aria-hidden="true" /> : null}
      {isCount || hasLabel ? <span className={styles.label}>{isCount ? countLabel : children}</span> : null}
      {showRemove ? (
        <button
          type="button"
          className={styles.remove}
          aria-label={removeLabel}
          onClick={(event) => {
            event.stopPropagation();
            onRemove?.();
          }}
        >
          <AppIcon name="close" size={12} label="" />
        </button>
      ) : null}
    </span>
  );
}

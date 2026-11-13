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

type BadgeProps = {
  tone?: BadgeTone;
  appearance?: BadgeAppearance;
  /** Uppercase the label. */
  allCaps?: boolean;
  /** Leading status pip (or pip-only when there is no label). */
  indicator?: boolean;
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
  removable = false,
  onRemove,
  removeLabel = "Remove",
  children,
  className,
  ...rest
}: BadgeProps) {
  const hasLabel = children != null && children !== false && children !== "";
  const indicatorOnly = indicator && !hasLabel;
  const showRemove = removable && !indicatorOnly;

  return (
    <span
      {...rest}
      className={[styles.badge, styles[tone], className].filter(Boolean).join(" ")}
      data-tone={tone}
      data-appearance={appearance}
      data-all-caps={allCaps ? "true" : "false"}
      data-indicator={indicator ? "true" : "false"}
      data-indicator-only={indicatorOnly ? "true" : "false"}
      data-removable={showRemove ? "true" : "false"}
    >
      {indicator ? <span className={styles.indicator} aria-hidden="true" /> : null}
      {hasLabel ? children : null}
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

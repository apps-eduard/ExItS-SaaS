import type { ReactNode } from "react";
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

export function Badge({
  tone = "neutral",
  appearance = "soft",
  children,
}: {
  tone?: BadgeTone;
  appearance?: BadgeAppearance;
  children: ReactNode;
}) {
  return (
    <span className={`${styles.badge} ${styles[tone]}`} data-tone={tone} data-appearance={appearance}>
      {children}
    </span>
  );
}

import type { ReactNode } from "react";
import styles from "./Badge.module.scss";

export const BADGE_TONES = ["neutral", "primary", "success", "warning", "danger", "info"] as const;

export type BadgeTone = (typeof BADGE_TONES)[number];

export function Badge({ tone = "neutral", children }: { tone?: BadgeTone; children: ReactNode }) {
  return (
    <span className={`${styles.badge} ${styles[tone]}`} data-tone={tone}>
      {children}
    </span>
  );
}

import type { ReactNode } from "react";
import styles from "./Badge.module.scss";

export type BadgeTone = "neutral" | "primary" | "success" | "warning" | "danger" | "info";

export function Badge({ tone = "neutral", children }: { tone?: BadgeTone; children: ReactNode }) {
  return <span className={`${styles.badge} ${styles[tone]}`}>{children}</span>;
}

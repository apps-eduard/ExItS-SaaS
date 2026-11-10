import type { CSSProperties } from "react";
import styles from "./Skeleton.module.scss";

type SkeletonProps = {
  width?: string;
  height?: string;
  label?: string;
};

export function Skeleton({ width = "100%", height = "0.875rem", label = "Loading" }: SkeletonProps) {
  const style = { "--skeleton-width": width, "--skeleton-height": height } as CSSProperties;
  return <span className={styles.skeleton} style={style} role="status" aria-label={label} />;
}

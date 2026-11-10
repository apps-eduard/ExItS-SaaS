import type { CSSProperties } from "react";
import type { IconName } from "./registry";
import styles from "./ExIcon.module.scss";

export type ExIconProps = {
  name: IconName;
  size?: number;
  color?: string;
  fill?: 0 | 1;
  weight?: number;
  grade?: number;
  opticalSize?: number;
  label?: string;
  className?: string;
};

function opticalSizeFor(size: number, opticalSize?: number) {
  if (opticalSize) return Math.min(48, Math.max(20, opticalSize));
  return Math.min(48, Math.max(20, Math.round(size)));
}

export function ExIcon({
  name,
  size = 20,
  color,
  fill = 0,
  weight = 400,
  grade = 0,
  opticalSize,
  label,
  className,
}: ExIconProps) {
  const style = {
    "--icon-size": `${size}px`,
    "--icon-fill": fill,
    "--icon-weight": weight,
    "--icon-grade": grade,
    "--icon-opsz": opticalSizeFor(size, opticalSize),
    color,
  } as CSSProperties;

  const classNames = className ? `${styles.glyph} ${className}` : styles.glyph;

  if (label) {
    return (
      <span role="img" aria-label={label} className={classNames} style={style}>
        {name}
      </span>
    );
  }

  return (
    <span aria-hidden="true" className={classNames} style={style}>
      {name}
    </span>
  );
}

export function AppIcon({ size = 24, ...props }: ExIconProps) {
  return <ExIcon size={size} {...props} />;
}

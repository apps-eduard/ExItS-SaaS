import type { CSSProperties } from "react";
import { ExIcon, type ExIconProps } from "../../icons/ExIcon";
import type { FeatureTone, IconName } from "../../icons/registry";
import styles from "./IconTile.module.scss";

export type IconTileVariant = "neutral" | "primary" | "feature" | "solid";

type IconTileProps = {
  name: IconName;
  variant?: IconTileVariant;
  tone?: FeatureTone;
  label?: string;
  size?: number;
  fill?: ExIconProps["fill"];
};

export function IconTile({ name, variant = "neutral", tone, label, size = 22, fill = 0 }: IconTileProps) {
  const style =
    tone && (variant === "feature" || variant === "solid")
      ? ({
          "--tile-fill": `var(--color-feature-${tone})`,
          "--tile-ink": variant === "solid" ? `var(--color-feature-${tone}-foreground)` : `var(--color-feature-${tone}-ink)`,
        } as CSSProperties)
      : undefined;

  return (
    <span className={`${styles.tile} ${styles[variant]}`} style={style}>
      <ExIcon name={name} size={size} fill={fill} label={label} />
    </span>
  );
}

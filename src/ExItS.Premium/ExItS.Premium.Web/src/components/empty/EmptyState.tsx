import type { ReactNode } from "react";
import { IconTile } from "../icon-tile/IconTile";
import type { IconName } from "../../icons/registry";
import styles from "./EmptyState.module.scss";

type EmptyStateProps = {
  icon?: IconName;
  title: string;
  description: string;
  action?: ReactNode;
};

export function EmptyState({ icon = "inbox", title, description, action }: EmptyStateProps) {
  return (
    <div className={styles.empty}>
      <IconTile name={icon} variant="primary" />
      <div className={styles.copy}>
        <h2 className="type-card">{title}</h2>
        <p className="type-secondary">{description}</p>
      </div>
      {action}
    </div>
  );
}

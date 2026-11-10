import type { ReactNode } from "react";
import styles from "./Card.module.scss";

type CardProps = {
  eyebrow?: string;
  title?: string;
  elevation?: 0 | 1 | 2 | 3 | 4;
  children?: ReactNode;
};

export function Card({ eyebrow, title, elevation = 1, children }: CardProps) {
  return (
    <article className={styles.card} data-elevation={elevation}>
      {eyebrow ? <p className={styles.eyebrow}>{eyebrow}</p> : null}
      {title ? <h3 className={styles.title}>{title}</h3> : null}
      {children}
    </article>
  );
}

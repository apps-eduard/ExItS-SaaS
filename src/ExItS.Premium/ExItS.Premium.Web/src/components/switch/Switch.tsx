import type { InputHTMLAttributes } from "react";
import styles from "./Switch.module.scss";

type SwitchProps = Omit<InputHTMLAttributes<HTMLInputElement>, "type" | "role"> & {
  label: string;
};

export function Switch({ label, ...props }: SwitchProps) {
  return (
    <label className={styles.switch}>
      <input {...props} className={styles.input} type="checkbox" role="switch" />
      <span className={styles.track} aria-hidden="true">
        <span className={styles.thumb} />
      </span>
      <span className={styles.text}>{label}</span>
    </label>
  );
}

import type { InputHTMLAttributes } from "react";
import styles from "./Radio.module.scss";

type RadioProps = Omit<InputHTMLAttributes<HTMLInputElement>, "type"> & {
  label: string;
};

export function Radio({ label, ...props }: RadioProps) {
  return (
    <label className={styles.choice}>
      <input {...props} className={styles.input} type="radio" />
      <span className={styles.dot} aria-hidden="true" />
      <span className={styles.text}>{label}</span>
    </label>
  );
}

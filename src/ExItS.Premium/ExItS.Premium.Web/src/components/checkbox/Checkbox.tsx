import { useEffect, useRef, type InputHTMLAttributes } from "react";
import styles from "./Checkbox.module.scss";

type CheckboxProps = Omit<InputHTMLAttributes<HTMLInputElement>, "type"> & {
  label: string;
  indeterminate?: boolean;
};

export function Checkbox({ label, indeterminate = false, ...props }: CheckboxProps) {
  const ref = useRef<HTMLInputElement>(null);

  useEffect(() => {
    if (ref.current) ref.current.indeterminate = indeterminate && !props.checked;
  }, [indeterminate, props.checked]);

  return (
    <label className={styles.choice}>
      <input {...props} ref={ref} className={styles.input} type="checkbox" />
      <span className={styles.box} aria-hidden="true" />
      <span className={styles.text}>{label}</span>
    </label>
  );
}

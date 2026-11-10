import type { ReactNode } from "react";
import styles from "./Field.module.scss";

type FieldProps = {
  id: string;
  label: string;
  hint?: string;
  error?: string;
  success?: string;
  children: ReactNode;
};

export function Field({ id, label, hint, error, success, children }: FieldProps) {
  const message = error ?? success ?? hint;
  const state = error ? "error" : success ? "success" : undefined;
  return (
    <div className={styles.field} data-state={state}>
      <label className={styles.label} htmlFor={id}>
        {label}
      </label>
      {children}
      {message ? (
        <p className={styles.message} id={`${id}-message`}>
          {message}
        </p>
      ) : null}
    </div>
  );
}

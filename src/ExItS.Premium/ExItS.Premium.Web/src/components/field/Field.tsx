import type { ReactNode } from "react";
import styles from "./Field.module.scss";

type FieldProps = {
  id: string;
  label: string;
  hint?: string;
  error?: string;
  success?: string;
  required?: boolean;
  readOnly?: boolean;
  disabled?: boolean;
  /** Stacked is the shipped label. Float rests on the border. Rise starts as a placeholder and moves up. */
  placement?: "stacked" | "float";
  labelMotion?: "rise";
  icon?: boolean;
  children: ReactNode;
};

function LabelText({ label, required, readOnly }: { label: string; required: boolean; readOnly: boolean }) {
  return (
    <>
      <span className={styles.labelText}>{label}</span>
      {!readOnly && required ? (
        <span className={styles.required} aria-hidden="true">
          *
        </span>
      ) : null}
    </>
  );
}

export function Field({
  id,
  label,
  hint,
  error,
  success,
  required = false,
  readOnly = false,
  disabled = false,
  placement = "stacked",
  labelMotion,
  icon = false,
  children,
}: FieldProps) {
  const message = error ?? success ?? hint;
  const state = error ? "error" : success ? "success" : undefined;
  const floating = placement === "float";
  const rise = labelMotion === "rise";
  const labelNode = (
    <label className={floating ? styles.floatLabel : styles.label} htmlFor={id}>
      {floating || rise ? (
        <LabelText label={label} required={required} readOnly={readOnly} />
      ) : (
        <>
          <span>
            <LabelText label={label} required={required} readOnly={readOnly} />
          </span>
          {readOnly ? <span className={styles.flag}>Read-only</span> : null}
        </>
      )}
    </label>
  );

  return (
    <div
      className={styles.field}
      data-state={state}
      data-placement={floating ? "float" : undefined}
      data-label-motion={rise ? "rise" : undefined}
      data-disabled={(floating || rise) && disabled ? "true" : undefined}
      data-icon={(floating || rise) && icon ? "true" : undefined}
    >
      {rise ? (
        <>
          {readOnly ? <span className={styles.flag}>Read-only</span> : null}
          {labelNode}
          {children}
        </>
      ) : floating ? (
        <>
          {readOnly ? <span className={styles.flag}>Read-only</span> : null}
          <div className={styles.floatShell}>
            {labelNode}
            {children}
          </div>
        </>
      ) : (
        <>
          {labelNode}
          {children}
        </>
      )}
      {message ? (
        <p className={styles.message} id={`${id}-message`}>
          {message}
        </p>
      ) : null}
    </div>
  );
}

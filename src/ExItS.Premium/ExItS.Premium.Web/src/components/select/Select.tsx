import { useId, type ReactNode, type SelectHTMLAttributes } from "react";
import { Field } from "../field/Field";
import { ExIcon } from "../../icons/ExIcon";
import styles from "./Select.module.scss";

type SelectProps = SelectHTMLAttributes<HTMLSelectElement> & {
  label: string;
  hint?: string;
  error?: string;
  success?: string;
  children: ReactNode;
};

export function Select({ label, hint, error, success, id, children, ...props }: SelectProps) {
  const autoId = useId();
  const fieldId = id ?? autoId;
  const message = error ?? success ?? hint;
  const state = error ? "error" : success ? "success" : undefined;
  return (
    <Field id={fieldId} label={label} hint={hint} error={error} success={success}>
      <div className={styles.wrap}>
        <select
          {...props}
          id={fieldId}
          className={styles.control}
          data-state={state}
          aria-invalid={error ? true : undefined}
          aria-describedby={message ? `${fieldId}-message` : undefined}
        >
          {children}
        </select>
        <ExIcon name="expand_more" className={styles.chevron} />
      </div>
    </Field>
  );
}

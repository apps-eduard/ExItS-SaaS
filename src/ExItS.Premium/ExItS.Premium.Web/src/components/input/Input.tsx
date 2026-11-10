import { useId, type InputHTMLAttributes } from "react";
import { Field } from "../field/Field";
import styles from "./Input.module.scss";

type InputProps = Omit<InputHTMLAttributes<HTMLInputElement>, "size"> & {
  label: string;
  hint?: string;
  error?: string;
  success?: string;
};

export function Input({ label, hint, error, success, id, ...props }: InputProps) {
  const autoId = useId();
  const fieldId = id ?? autoId;
  const message = error ?? success ?? hint;
  const state = error ? "error" : success ? "success" : undefined;
  return (
    <Field id={fieldId} label={label} hint={hint} error={error} success={success}>
      <input
        {...props}
        id={fieldId}
        className={styles.control}
        data-state={state}
        aria-invalid={error ? true : undefined}
        aria-describedby={message ? `${fieldId}-message` : undefined}
      />
    </Field>
  );
}

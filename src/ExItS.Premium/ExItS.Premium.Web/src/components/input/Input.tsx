import { useId, useState, type ChangeEvent, type InputHTMLAttributes, type Ref } from "react";
import { Field } from "../field/Field";
import { isInputFocusMotion, type InputFocusMotion } from "./focus-motion";
import { InputMotion } from "./InputMotion";
import styles from "./Input.module.scss";

type InputValue = InputHTMLAttributes<HTMLInputElement>["value"];

type InputProps = Omit<InputHTMLAttributes<HTMLInputElement>, "size"> & {
  ref?: Ref<HTMLInputElement>;
  label: string;
  hint?: string;
  error?: string;
  success?: string;
  /** Experimental focus treatment. Omitted fields keep the shipped focus style. */
  focusMotion?: InputFocusMotion;
};

function hasText(value: InputValue) {
  if (value == null) return false;
  if (Array.isArray(value)) return value.join("").length > 0;
  return String(value).length > 0;
}

export function Input({
  ref,
  label,
  hint,
  error,
  success,
  id,
  focusMotion = "none",
  className,
  value,
  defaultValue,
  onChange,
  disabled,
  readOnly,
  required,
  "aria-label": ariaLabel,
  ...props
}: InputProps) {
  const autoId = useId();
  const fieldId = id ?? autoId;
  const message = error ?? success ?? hint;
  const state = error ? "error" : success ? "success" : undefined;
  const motion = isInputFocusMotion(focusMotion) ? focusMotion : "none";
  const [uncontrolledFilled, setUncontrolledFilled] = useState(() => hasText(defaultValue));
  const filled = value !== undefined ? hasText(value) : uncontrolledFilled;

  function handleChange(event: ChangeEvent<HTMLInputElement>) {
    if (value === undefined) setUncontrolledFilled(event.target.value.length > 0);
    onChange?.(event);
  }

  return (
    <Field
      id={fieldId}
      label={label}
      hint={hint}
      error={error}
      success={success}
      required={Boolean(required)}
      readOnly={Boolean(readOnly)}
    >
      <InputMotion motion={motion} disabled={Boolean(disabled)} readOnly={Boolean(readOnly)} state={state}>
        <input
          {...props}
          ref={ref}
          id={fieldId}
          className={[styles.control, className].filter(Boolean).join(" ")}
          value={value}
          defaultValue={value === undefined ? defaultValue : undefined}
          disabled={disabled}
          readOnly={readOnly}
          required={required}
          aria-label={readOnly ? `${ariaLabel ?? label}, read-only` : ariaLabel}
          onChange={handleChange}
          data-focus-motion={motion}
          data-filled={filled ? "true" : undefined}
          data-state={state}
          aria-invalid={error ? true : undefined}
          aria-describedby={message ? `${fieldId}-message` : undefined}
        />
      </InputMotion>
    </Field>
  );
}

import type { ReactNode, Ref } from "react";
import { ExIcon } from "../../icons/ExIcon";
import { Field } from "../field/Field";
import { CalendarGlyph } from "./glyphs";
import styles from "./DateField.module.scss";

type DateFieldProps = {
  id: string;
  label: string;
  text: string | null;
  placeholder: string;
  open: boolean;
  controls: string;
  disabled?: boolean;
  hint?: string;
  error?: string;
  success?: string;
  name?: string;
  hiddenValue?: string;
  clearable?: boolean;
  clearLabel: string;
  viewport: "compact" | "regular";
  triggerRef?: Ref<HTMLButtonElement>;
  anchorRef?: Ref<HTMLDivElement>;
  onToggle: () => void;
  onClear: () => void;
  children?: ReactNode;
};

export function DateField({
  id,
  label,
  text,
  placeholder,
  open,
  controls,
  disabled = false,
  hint,
  error,
  success,
  name,
  hiddenValue = "",
  clearable = true,
  clearLabel,
  viewport,
  triggerRef,
  anchorRef,
  onToggle,
  onClear,
  children,
}: DateFieldProps) {
  const message = error ?? success ?? hint;
  const state = error ? "error" : success ? "success" : undefined;
  const showClear = clearable && Boolean(text) && !disabled;

  return (
    <Field id={id} label={label} hint={hint} error={error} success={success}>
      <div ref={anchorRef} className={styles.shell} data-viewport={viewport}>
        <span className={styles.icon}>
          <CalendarGlyph />
        </span>
        <button
          ref={triggerRef}
          id={id}
          type="button"
          className={styles.trigger}
          disabled={disabled}
          data-state={state}
          data-open={open ? "true" : "false"}
          aria-haspopup="dialog"
          aria-expanded={open}
          aria-controls={controls}
          aria-invalid={error ? true : undefined}
          aria-describedby={message ? `${id}-message` : undefined}
          aria-label={text ? `${label}, ${text}` : label}
          onClick={onToggle}
        >
          <span className={text ? styles.value : `${styles.value} ${styles.placeholder}`}>{text ?? placeholder}</span>
        </button>
        {showClear ? (
          <button type="button" className={styles.clear} aria-label={clearLabel} onClick={onClear}>
            <ExIcon name="close" />
          </button>
        ) : null}
        {name ? <input className={styles.hidden} type="hidden" name={name} value={hiddenValue} /> : null}
      </div>
      {children}
    </Field>
  );
}

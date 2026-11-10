import { useId } from "react";
import { Field } from "../field/Field";
import { ExIcon } from "../../icons/ExIcon";
import styles from "./SearchField.module.scss";

type SearchFieldProps = {
  label: string;
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
  disabled?: boolean;
  error?: string;
};

export function SearchField({ label, value, onChange, placeholder, disabled, error }: SearchFieldProps) {
  const id = useId();
  return (
    <Field id={id} label={label} error={error}>
      <div className={styles.wrap}>
        <ExIcon name="search" className={styles.icon} />
        <input
          id={id}
          type="search"
          className={styles.control}
          value={value}
          placeholder={placeholder}
          disabled={disabled}
          data-state={error ? "error" : undefined}
          aria-invalid={error ? true : undefined}
          aria-describedby={error ? `${id}-message` : undefined}
          onChange={(event) => onChange(event.target.value)}
        />
        {value && !disabled ? (
          <button type="button" className={styles.clear} aria-label="Clear search" onClick={() => onChange("")}>
            <ExIcon name="close" />
          </button>
        ) : null}
      </div>
    </Field>
  );
}

import type { CSSProperties } from "react";
import styles from "./SegmentedControl.module.scss";

export type SegmentedOption<T extends string> = {
  value: T;
  label: string;
  disabled?: boolean;
};

type SegmentedControlProps<T extends string> = {
  label: string;
  name: string;
  value: T;
  options: Array<SegmentedOption<T>>;
  onChange: (value: T) => void;
  disabled?: boolean;
};

export function SegmentedControl<T extends string>({
  label,
  name,
  value,
  options,
  onChange,
  disabled = false,
}: SegmentedControlProps<T>) {
  const index = Math.max(0, options.findIndex((option) => option.value === value));
  const style = {
    "--count": String(options.length),
    "--index": String(index),
  } as CSSProperties;

  return (
    <div className={styles.wrap}>
      <div className={styles.label} id={`${name}-label`}>
        {label}
      </div>
      <div className={styles.group} role="radiogroup" aria-labelledby={`${name}-label`} style={style} data-disabled={disabled ? "true" : undefined}>
        <span className={styles.thumb} aria-hidden="true" />
        {options.map((option) => {
          const selected = option.value === value;
          return (
            <label key={option.value} className={styles.option} data-selected={selected ? "true" : "false"}>
              <input
                type="radio"
                name={name}
                value={option.value}
                checked={selected}
                disabled={disabled || option.disabled}
                onChange={() => onChange(option.value)}
              />
              <span>{option.label}</span>
            </label>
          );
        })}
      </div>
    </div>
  );
}

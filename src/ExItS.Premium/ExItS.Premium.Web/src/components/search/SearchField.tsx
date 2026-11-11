import { useId } from "react";
import { Field } from "../field/Field";
import { ExIcon } from "../../icons/ExIcon";
import appearanceStyles from "../input/appearance.module.scss";
import { inputFocusFrame, isInputAppearance, type InputAppearance } from "../input/input-appearance";
import { isInputFocusMotion, type InputFocusMotion } from "../input/focus-motion";
import { InputMotion } from "../input/InputMotion";
import styles from "./SearchField.module.scss";

type SearchFieldProps = {
  label: string;
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
  disabled?: boolean;
  error?: string;
  /** Experimental focus treatment. Omitted fields keep the shipped focus style. */
  focusMotion?: InputFocusMotion;
  /** Omitted fields keep the shipped stacked label. Other surfaces are opt-in. */
  appearance?: InputAppearance;
};

export function SearchField({ label, value, onChange, placeholder, disabled, error, focusMotion = "none", appearance: appearanceProp = "stacked" }: SearchFieldProps) {
  const id = useId();
  const motion = isInputFocusMotion(focusMotion) ? focusMotion : "none";
  const appearance = isInputAppearance(appearanceProp) ? appearanceProp : "stacked";
  const floating = appearance !== "stacked";
  return (
    <Field id={id} label={label} error={error} disabled={Boolean(disabled)} placement={floating ? "float" : "stacked"} icon={floating}>
      <div className={styles.wrap} data-appearance={floating ? appearance : undefined}>
        <ExIcon name="search" className={styles.icon} />
        <InputMotion motion={motion} disabled={Boolean(disabled)} state={error ? "error" : undefined} frame={inputFocusFrame(appearance)}>
          <input
            id={id}
            type="search"
            className={[styles.control, floating ? appearanceStyles.control : ""].filter(Boolean).join(" ")}
            data-appearance={floating ? appearance : undefined}
            value={value}
            placeholder={placeholder}
            disabled={disabled}
            data-state={error ? "error" : undefined}
            data-focus-motion={motion}
            data-filled={value.length > 0 ? "true" : undefined}
            aria-invalid={error ? true : undefined}
            aria-describedby={error ? `${id}-message` : undefined}
            onChange={(event) => onChange(event.target.value)}
          />
        </InputMotion>
        {value && !disabled ? (
          <button type="button" className={styles.clear} aria-label="Clear search" onClick={() => onChange("")}>
            <ExIcon name="close" />
          </button>
        ) : null}
      </div>
    </Field>
  );
}

import { useId } from "react";
import { Field } from "../field/Field";
import { ExIcon } from "../../icons/ExIcon";
import appearanceStyles from "../input/appearance.module.scss";
import { inputRestsOnBorder, isInputAppearance, type InputAppearance } from "../input/input-appearance";
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
  /** Omitted fields keep the shipped standard label. Other surfaces are opt-in. */
  appearance?: InputAppearance;
};

export function SearchField({ label, value, onChange, placeholder, disabled, error, focusMotion = "none", appearance: appearanceProp = "standard" }: SearchFieldProps) {
  const id = useId();
  const motion = isInputFocusMotion(focusMotion) ? focusMotion : "none";
  const appearance = isInputAppearance(appearanceProp) ? appearanceProp : "standard";
  const onBorder = inputRestsOnBorder(appearance);
  const rise = appearance === "floating";
  const surfaced = onBorder || rise;
  return (
    <Field id={id} label={label} error={error} disabled={Boolean(disabled)} placement={onBorder ? "float" : "stacked"} labelMotion={rise ? "rise" : undefined} icon={surfaced}>
      <div className={styles.wrap} data-appearance={surfaced ? appearance : undefined}>
        <ExIcon name="search" className={styles.icon} />
        <InputMotion motion={motion} disabled={Boolean(disabled)} state={error ? "error" : undefined}>
          <input
            id={id}
            type="search"
            className={[styles.control, surfaced ? appearanceStyles.control : ""].filter(Boolean).join(" ")}
            data-appearance={surfaced ? appearance : undefined}
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

import { useState, type CSSProperties, type ReactNode } from "react";
import { INPUT_SAMPLES, type InputSample } from "@/features/ui-standards/UiStandardsInputGallery";
import { radioAccent } from "@/features/ui-standards/UiStandardsRadioGallery";

/**
 * Quasar QCheckbox color samples. Colors match the radio palette.
 * Showcase only — not a product checkbox.
 * @see https://quasar.dev/vue-components/checkbox
 */
const TRUTHY_PATH = "M1.73,12.91 8.1,19.28 22.79,4.59";
const INDET_PATH = "M4,14H20V10H4";

type CheckboxValue = true | false | null;
type ToggleOrder = "tf" | "ft";

function nextCheckboxValue(value: CheckboxValue, order: ToggleOrder, toggleIndeterminate: boolean): CheckboxValue {
  if (value === true) {
    if (order !== "ft" || !toggleIndeterminate) return false;
  } else if (value === false) {
    if (order === "ft" || !toggleIndeterminate) return true;
  } else {
    return order !== "ft" ? true : false;
  }
  return null;
}

export function UiStandardsQcheckbox({
  label,
  color,
  checked = false,
  indeterminate = false,
  dense = false,
  disabled = false,
  darkInk = false,
  onDark = false,
  onSelect,
  testId,
}: {
  label: string;
  color: string;
  checked?: boolean;
  indeterminate?: boolean;
  dense?: boolean;
  disabled?: boolean;
  darkInk?: boolean;
  onDark?: boolean;
  onSelect?: () => void;
  testId?: string;
}) {
  const style = { "--qcheckbox-color": color } as CSSProperties;
  return (
    <div
      role="checkbox"
      aria-checked={indeterminate ? "mixed" : checked}
      aria-disabled={disabled || undefined}
      aria-label={label}
      tabIndex={disabled ? -1 : 0}
      className={
        "ui-standards-qcheckbox" +
        (dense ? " ui-standards-qcheckbox--dense" : "") +
        (disabled ? " ui-standards-qcheckbox--disabled" : "") +
        (darkInk ? " ui-standards-qcheckbox--dark-ink" : "") +
        (onDark ? " ui-standards-qcheckbox--on-dark" : "")
      }
      style={style}
      data-testid={testId}
      onClick={() => {
        if (!disabled) onSelect?.();
      }}
      onKeyDown={(event) => {
        if (disabled) return;
        if (event.key === "Enter" || event.key === " ") {
          event.preventDefault();
          onSelect?.();
        }
      }}
    >
      <span
        className={
          "ui-standards-qcheckbox__inner" +
          (indeterminate
            ? " ui-standards-qcheckbox__inner--indet"
            : checked
              ? " ui-standards-qcheckbox__inner--truthy"
              : "")
        }
        aria-hidden
      >
        <span className="ui-standards-qcheckbox__bg">
          <svg className="ui-standards-qcheckbox__svg" viewBox="0 0 24 24">
            <path className="ui-standards-qcheckbox__truthy" d={TRUTHY_PATH} />
            <path className="ui-standards-qcheckbox__indet" d={INDET_PATH} />
          </svg>
        </span>
      </span>
      <span className="ui-standards-qcheckbox__label">{label}</span>
    </div>
  );
}

function CheckboxColor({
  sample,
  testId,
  onDark = false,
}: {
  sample: InputSample;
  testId: string;
  onDark?: boolean;
}) {
  const [checked, setChecked] = useState(true);
  return (
    <UiStandardsQcheckbox
      label={sample.label}
      color={radioAccent(sample)}
      checked={checked}
      onDark={onDark}
      darkInk={sample.id === "black"}
      onSelect={() => setChecked((value) => !value)}
      testId={`${testId}-${sample.id}`}
    />
  );
}

const TOGGLE_ORDER_DEFAULTS = {
  tf: null,
  ft: false,
  tfIndet: true,
  ftIndet: false,
} as const;

function CheckboxToggleOrder() {
  const [tf, setTf] = useState<CheckboxValue>(TOGGLE_ORDER_DEFAULTS.tf);
  const [ft, setFt] = useState<CheckboxValue>(TOGGLE_ORDER_DEFAULTS.ft);
  const [tfIndet, setTfIndet] = useState<CheckboxValue>(TOGGLE_ORDER_DEFAULTS.tfIndet);
  const [ftIndet, setFtIndet] = useState<CheckboxValue>(TOGGLE_ORDER_DEFAULTS.ftIndet);

  function resetModels() {
    setTf(null);
    setFt(null);
    setTfIndet(null);
    setFtIndet(null);
  }

  return (
    <div className="ui-standards-q-example" data-testid="ui-standard-checkbox-toggle">
      <div className="ui-standards-q-example__label">Toggle order</div>
      <button type="button" className="ui-standards-qcheckbox-reset" data-testid="ui-standard-checkbox-toggle-reset" onClick={resetModels}>
        Reset models
      </button>
      <div className="ui-standards-qcheckbox-toggle__rows">
        <div className="ui-standards-qcheckbox-toggle__row">
          <UiStandardsQcheckbox
            label="'tf' order"
            color="#009688"
            checked={tf === true}
            indeterminate={tf === null}
            onSelect={() => setTf(nextCheckboxValue(tf, "tf", false))}
            testId="ui-standard-checkbox-toggle-tf"
          />
          <UiStandardsQcheckbox
            label="'ft' order"
            color="#ff9800"
            checked={ft === true}
            indeterminate={ft === null}
            onSelect={() => setFt(nextCheckboxValue(ft, "ft", false))}
            testId="ui-standard-checkbox-toggle-ft"
          />
        </div>
        <div className="ui-standards-qcheckbox-toggle__row">
          <UiStandardsQcheckbox
            label="'tf' order + toggle-indeterminate"
            color="#f44336"
            checked={tfIndet === true}
            indeterminate={tfIndet === null}
            onSelect={() => setTfIndet(nextCheckboxValue(tfIndet, "tf", true))}
            testId="ui-standard-checkbox-toggle-tf-indet"
          />
          <UiStandardsQcheckbox
            label="'ft' order + toggle-indeterminate"
            color="#00bcd4"
            checked={ftIndet === true}
            indeterminate={ftIndet === null}
            onSelect={() => setFtIndet(nextCheckboxValue(ftIndet, "ft", true))}
            testId="ui-standard-checkbox-toggle-ft-indet"
          />
        </div>
      </div>
    </div>
  );
}

export function UiStandardsCheckboxGallery(): ReactNode {
  return (
    <div className="flex flex-col gap-4" data-testid="ui-standard-checkbox-gallery">
      <div className="ui-standards-q-example" data-testid="ui-standard-checkbox-colors">
        <div className="ui-standards-q-example__label">Colors</div>
        <div className="ui-standards-q-example__row">
          {INPUT_SAMPLES.map((sample) => (
            <CheckboxColor key={sample.id} sample={sample} testId="ui-standard-checkbox" />
          ))}
        </div>
      </div>
      <CheckboxToggleOrder />
    </div>
  );
}

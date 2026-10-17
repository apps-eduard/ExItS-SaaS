import { useState, type CSSProperties, type ReactNode } from "react";
import { INPUT_SAMPLES, type InputSample } from "@/features/ui-standards/UiStandardsInputGallery";
import { radioAccent } from "@/features/ui-standards/UiStandardsRadioGallery";

/** Same gray as the checkbox Standard. */
export function toggleAccent(sample: Pick<InputSample, "id" | "bg">): string {
  return radioAccent(sample);
}

/**
 * Locked ExItS Toggle visual standard. Standard is gray, matching the checkbox.
 * The playground is the authority. This lock does not restyle the product Switch.
 * @see https://quasar.dev/vue-components/toggle
 */
const CHECK_PATH = "M9 16.17 4.83 12l-1.41 1.41L9 19 21 7l-1.41-1.41z";

type ToggleValue = true | false | null;
type ToggleOrder = "tf" | "ft";

function nextToggleValue(value: ToggleValue, order: ToggleOrder, toggleIndeterminate: boolean): ToggleValue {
  if (value === true) {
    if (order !== "ft" || !toggleIndeterminate) return false;
  } else if (value === false) {
    if (order === "ft" || !toggleIndeterminate) return true;
  } else {
    return order !== "ft" ? true : false;
  }
  return null;
}

export function UiStandardsQtoggle({
  label,
  color,
  checked = false,
  indeterminate = false,
  dense = false,
  disabled = false,
  icons = false,
  darkInk = false,
  onDark = false,
  standard = false,
  appearance = "standard",
  glyph,
  onSelect,
  testId,
}: {
  label: string;
  color: string;
  checked?: boolean;
  indeterminate?: boolean;
  dense?: boolean;
  disabled?: boolean;
  icons?: boolean;
  darkInk?: boolean;
  onDark?: boolean;
  standard?: boolean;
  appearance?: string;
  glyph?: ReactNode;
  onSelect?: () => void;
  testId?: string;
}) {
  const style = { "--qtoggle-color": color } as CSSProperties;
  return (
    <div
      role="switch"
      aria-checked={indeterminate ? false : checked}
      aria-disabled={disabled || undefined}
      aria-label={label}
      tabIndex={disabled ? -1 : 0}
      data-state={indeterminate ? "indet" : checked ? "on" : "off"}
      className={
        "ui-standards-qtoggle" +
        (dense ? " ui-standards-qtoggle--dense" : "") +
        (icons ? " ui-standards-qtoggle--icons" : "") +
        (disabled ? " ui-standards-qtoggle--disabled" : "") +
        (darkInk ? " ui-standards-qtoggle--dark-ink" : "") +
        (onDark ? " ui-standards-qtoggle--on-dark" : "") +
        (standard ? " ui-standards-qtoggle--standard" : "") +
        (appearance !== "standard" ? ` ui-standards-qtoggle--appearance-${appearance}` : "")
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
          "ui-standards-qtoggle__inner" +
          (indeterminate ? " ui-standards-qtoggle__inner--indet" : checked ? " ui-standards-qtoggle__inner--truthy" : "")
        }
        aria-hidden
      >
        <span className="ui-standards-qtoggle__track" />
        <span className="ui-standards-qtoggle__thumb">
          {glyph ? (
            <span className="ui-standards-qtoggle__glyph" data-testid={testId ? `${testId}-icon` : undefined}>
              {glyph}
            </span>
          ) : icons ? (
            <svg className="ui-standards-qtoggle__icon" viewBox="0 0 24 24">
              <path d={CHECK_PATH} />
            </svg>
          ) : null}
        </span>
      </span>
      <span className="ui-standards-qtoggle__label">{label}</span>
    </div>
  );
}

function ToggleColor({
  sample,
  testId,
  icons = false,
  onDark = false,
  appearance = "standard",
}: {
  sample: InputSample;
  testId: string;
  icons?: boolean;
  onDark?: boolean;
  appearance?: string;
}) {
  const [checked, setChecked] = useState(true);
  return (
    <UiStandardsQtoggle
      label={sample.label}
      color={toggleAccent(sample)}
      checked={checked}
      icons={icons}
      onDark={onDark}
      appearance={appearance}
      standard={sample.id === "standard"}
      darkInk={sample.id === "black"}
      onSelect={() => setChecked((value) => !value)}
      testId={`${testId}-${sample.id}`}
    />
  );
}

const TOGGLE_ORDER_DEFAULTS = {
  tf: null,
  ft: null,
  tfIndet: true,
  ftIndet: false,
} as const;

function ToggleOrderSample() {
  const [tf, setTf] = useState<ToggleValue>(TOGGLE_ORDER_DEFAULTS.tf);
  const [ft, setFt] = useState<ToggleValue>(TOGGLE_ORDER_DEFAULTS.ft);
  const [tfIndet, setTfIndet] = useState<ToggleValue>(TOGGLE_ORDER_DEFAULTS.tfIndet);
  const [ftIndet, setFtIndet] = useState<ToggleValue>(TOGGLE_ORDER_DEFAULTS.ftIndet);

  function resetModels() {
    setTf(TOGGLE_ORDER_DEFAULTS.tf);
    setFt(TOGGLE_ORDER_DEFAULTS.ft);
    setTfIndet(TOGGLE_ORDER_DEFAULTS.tfIndet);
    setFtIndet(TOGGLE_ORDER_DEFAULTS.ftIndet);
  }

  return (
    <div className="ui-standards-q-example" data-testid="ui-standard-toggle-order">
      <div className="ui-standards-q-example__label">Toggle order</div>
      <button type="button" className="ui-standards-qcheckbox-reset" data-testid="ui-standard-toggle-order-reset" onClick={resetModels}>
        Reset models
      </button>
      <div className="ui-standards-qcheckbox-toggle__row">
        <UiStandardsQtoggle
          label="'tf' order"
          color="#009688"
          checked={tf === true}
          indeterminate={tf === null}
          onSelect={() => setTf(nextToggleValue(tf, "tf", false))}
          testId="ui-standard-toggle-order-tf"
        />
        <UiStandardsQtoggle
          label="'ft' order"
          color="#ff9800"
          checked={ft === true}
          indeterminate={ft === null}
          onSelect={() => setFt(nextToggleValue(ft, "ft", false))}
          testId="ui-standard-toggle-order-ft"
        />
        <UiStandardsQtoggle
          label="'tf' order + toggle-indeterminate"
          color="#f44336"
          checked={tfIndet === true}
          indeterminate={tfIndet === null}
          onSelect={() => setTfIndet(nextToggleValue(tfIndet, "tf", true))}
          testId="ui-standard-toggle-order-tf-indet"
        />
        <UiStandardsQtoggle
          label="'ft' order + toggle-indeterminate"
          color="#00bcd4"
          checked={ftIndet === true}
          indeterminate={ftIndet === null}
          onSelect={() => setFtIndet(nextToggleValue(ftIndet, "ft", true))}
          testId="ui-standard-toggle-order-ft-indet"
        />
      </div>
    </div>
  );
}

export function UiStandardsPillToggle({
  label,
  color,
  ink = "#ffffff",
  icons = false,
  labeled = false,
  outline = false,
  standard = false,
  disabled = false,
  checked = true,
  appearance = "standard",
  glyph,
  onSelect,
  testId,
}: {
  label: string;
  color: string;
  ink?: string;
  icons?: boolean;
  labeled?: boolean;
  outline?: boolean;
  standard?: boolean;
  disabled?: boolean;
  checked?: boolean;
  appearance?: string;
  glyph?: ReactNode;
  onSelect?: () => void;
  testId?: string;
}) {
  const style = { "--qpill-color": color, "--qpill-ink": ink } as CSSProperties;
  return (
    <div
      role="switch"
      aria-checked={checked}
      aria-disabled={disabled || undefined}
      aria-label={label}
      tabIndex={disabled ? -1 : 0}
      data-state={checked ? "on" : "off"}
      className={
        "ui-standards-pill-switch" +
        (checked ? " ui-standards-pill-switch--on" : "") +
        (icons ? " ui-standards-pill-switch--icons" : "") +
        (labeled ? " ui-standards-pill-switch--label" : "") +
        (outline ? " ui-standards-pill-switch--outline" : "") +
        (standard ? " ui-standards-pill-switch--standard" : "") +
        (disabled ? " ui-standards-pill-switch--disabled" : "") +
        (appearance !== "standard" ? ` ui-standards-pill-switch--appearance-${appearance}` : "")
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
      <span className="ui-standards-pill-switch__track">
        {labeled ? <span className="ui-standards-pill-switch__word">{checked ? "ON" : "OFF"}</span> : null}
        <span className="ui-standards-pill-switch__knob">
          {glyph && checked ? (
            <span className="ui-standards-pill-switch__glyph" data-testid={testId ? `${testId}-icon` : undefined}>
              {glyph}
            </span>
          ) : icons && checked ? (
            <svg className="ui-standards-pill-switch__check" viewBox="0 0 24 24" aria-hidden>
              <path d={CHECK_PATH} />
            </svg>
          ) : null}
        </span>
      </span>
      <span className="ui-standards-pill-switch__label">{label}</span>
    </div>
  );
}

function PillSwitch({
  label,
  color,
  ink = "#ffffff",
  icons = false,
  labeled = false,
  outline = false,
  standard = false,
  appearance = "standard",
  testId,
}: {
  label: string;
  color: string;
  ink?: string;
  icons?: boolean;
  labeled?: boolean;
  outline?: boolean;
  standard?: boolean;
  appearance?: string;
  testId: string;
}) {
  const [on, setOn] = useState(true);
  const style = { "--qpill-color": color, "--qpill-ink": ink } as CSSProperties;
  return (
    <div
      role="switch"
      aria-checked={on}
      aria-label={label}
      tabIndex={0}
      data-state={on ? "on" : "off"}
      className={
        "ui-standards-pill-switch" +
        (on ? " ui-standards-pill-switch--on" : "") +
        (icons ? " ui-standards-pill-switch--icons" : "") +
        (labeled ? " ui-standards-pill-switch--label" : "") +
        (outline ? " ui-standards-pill-switch--outline" : "") +
        (standard ? " ui-standards-pill-switch--standard" : "") +
        (appearance !== "standard" ? ` ui-standards-pill-switch--appearance-${appearance}` : "")
      }
      style={style}
      data-testid={testId}
      onClick={() => setOn((value) => !value)}
      onKeyDown={(event) => {
        if (event.key === "Enter" || event.key === " ") {
          event.preventDefault();
          setOn((value) => !value);
        }
      }}
    >
      <span className="ui-standards-pill-switch__track">
        {labeled ? <span className="ui-standards-pill-switch__word">{on ? "ON" : "OFF"}</span> : null}
        <span className="ui-standards-pill-switch__knob">
          {icons && on ? (
            <svg className="ui-standards-pill-switch__check" viewBox="0 0 24 24" aria-hidden>
              <path d={CHECK_PATH} />
            </svg>
          ) : null}
        </span>
      </span>
      <span className="ui-standards-pill-switch__label">{label}</span>
    </div>
  );
}

function PillSwitchRow({
  title,
  testId,
  icons = false,
  labeled = false,
  outline = false,
  appearance = "standard",
}: {
  title: string;
  testId: string;
  icons?: boolean;
  labeled?: boolean;
  outline?: boolean;
  appearance?: string;
}) {
  return (
    <div className="ui-standards-q-example" data-testid={testId}>
      <div className="ui-standards-q-example__label">{title}</div>
      <div className="ui-standards-q-example__row">
        {INPUT_SAMPLES.map((sample) => (
          <PillSwitch
            key={sample.id}
            label={sample.label}
            color={toggleAccent(sample)}
            ink={sample.fg}
            icons={icons}
            labeled={labeled}
            outline={outline}
            appearance={appearance}
            standard={sample.id === "standard"}
            testId={`${testId}-${sample.id}`}
          />
        ))}
      </div>
    </div>
  );
}

export function UiStandardsToggleGallery(): ReactNode {
  return (
    <div className="flex flex-col gap-4" data-testid="ui-standard-toggle-gallery">
      <div className="ui-standards-q-example" data-testid="ui-standard-toggle-colors">
        <div className="ui-standards-q-example__label">Standard</div>
        <div className="ui-standards-q-example__row">
          {INPUT_SAMPLES.map((sample) => (
            <ToggleColor key={sample.id} sample={sample} testId="ui-standard-toggle" />
          ))}
        </div>
      </div>
      <div className="ui-standards-q-example" data-testid="ui-standard-toggle-icons">
        <div className="ui-standards-q-example__label">Standard with icons</div>
        <div className="ui-standards-q-example__row">
          {INPUT_SAMPLES.map((sample) => (
            <ToggleColor key={sample.id} sample={sample} icons testId="ui-standard-toggle-icon" />
          ))}
        </div>
      </div>
      <div className="ui-standards-q-example" data-testid="ui-standard-toggle-soft">
        <div className="ui-standards-q-example__label">Soft</div>
        <div className="ui-standards-q-example__row">
          {INPUT_SAMPLES.map((sample) => (
            <ToggleColor key={sample.id} sample={sample} appearance="soft" testId="ui-standard-toggle-soft" />
          ))}
        </div>
      </div>
      <div className="ui-standards-q-example" data-testid="ui-standard-toggle-soft-outline">
        <div className="ui-standards-q-example__label">Soft outline</div>
        <div className="ui-standards-q-example__row">
          {INPUT_SAMPLES.map((sample) => (
            <ToggleColor key={sample.id} sample={sample} appearance="soft-outline" testId="ui-standard-toggle-soft-outline" />
          ))}
        </div>
      </div>
      <PillSwitchRow title="Solid" testId="ui-standard-toggle-pill" />
      <PillSwitchRow title="Solid soft" testId="ui-standard-toggle-pill-soft" appearance="soft" />
      <PillSwitchRow title="Solid soft outline" testId="ui-standard-toggle-pill-soft-outline" appearance="soft-outline" />
      <PillSwitchRow title="Outline" testId="ui-standard-toggle-pill-outline" outline />
      <PillSwitchRow title="Solid with icons" testId="ui-standard-toggle-pill-icon" icons />
      <PillSwitchRow title="Solid with label" testId="ui-standard-toggle-pill-label" labeled />
      <ToggleOrderSample />
    </div>
  );
}

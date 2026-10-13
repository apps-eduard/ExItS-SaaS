import { useState, type CSSProperties, type ReactNode } from "react";
import { INPUT_SAMPLES, inputAccent, type InputSample } from "@/features/ui-standards/UiStandardsInputGallery";

/**
 * Quasar QRadio color samples. Colors match the button palette.
 * Showcase only — not a product radio.
 * @see https://quasar.dev/vue-components/radio
 */
const OUTER_PATH =
  "M12,22a10,10 0 0 1 -10,-10a10,10 0 0 1 10,-10a10,10 0 0 1 10,10a10,10 0 0 1 -10,10m0,-22a12,12 0 0 0 -12,12a12,12 0 0 0 12,12a12,12 0 0 0 12,-12a12,12 0 0 0 -12,-12";
const CHECK_PATH = "M12,6a6,6 0 0 0 -6,6a6,6 0 0 0 6,6a6,6 0 0 0 6,-6a6,6 0 0 0 -6,-6";
const TASK_ALT_PATH =
  "M22 5.18 10.59 16.6l-4.24-4.24 1.41-1.41 2.83 2.83 10-10L22 5.18zM12 20c-4.41 0-8-3.59-8-8s3.59-8 8-8c1.57 0 3.04.46 4.28 1.25l1.45-1.45C16.1 2.67 14.13 2 12 2 6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10h-2c0 3.31-2.69 6-6 6z";
const FISH_EYE_PATH =
  "M12 2C6.47 2 2 6.47 2 12s4.47 10 10 10 10-4.47 10-10S17.53 2 12 2zm0 18c-4.41 0-8-3.59-8-8s3.59-8 8-8 8 3.59 8 8-3.59 8-8 8z";

/** Quasar grey. Standard stays this gray in light and dark mode. */
export const RADIO_STANDARD_COLOR = "#9e9e9e";

export function radioAccent(sample: Pick<InputSample, "id" | "bg">): string {
  return sample.id === "standard" ? RADIO_STANDARD_COLOR : inputAccent(sample);
}

export function UiStandardsQradio({
  label,
  color,
  checked = false,
  dense = false,
  disabled = false,
  icons = false,
  darkInk = false,
  onDark = false,
  onSelect,
  testId,
}: {
  label: string;
  color: string;
  checked?: boolean;
  dense?: boolean;
  disabled?: boolean;
  icons?: boolean;
  darkInk?: boolean;
  onDark?: boolean;
  onSelect?: () => void;
  testId?: string;
}) {
  const style = { "--qradio-color": color } as CSSProperties;
  return (
    <div
      role="radio"
      aria-checked={checked}
      aria-disabled={disabled || undefined}
      aria-label={label}
      tabIndex={disabled ? -1 : 0}
      className={
        "ui-standards-qradio" +
        (dense ? " ui-standards-qradio--dense" : "") +
        (icons ? " ui-standards-qradio--icons" : "") +
        (disabled ? " ui-standards-qradio--disabled" : "") +
        (darkInk ? " ui-standards-qradio--dark-ink" : "") +
        (onDark ? " ui-standards-qradio--on-dark" : "")
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
      <span className={"ui-standards-qradio__inner" + (checked ? " ui-standards-qradio__inner--truthy" : "")} aria-hidden>
        {icons ? (
          <svg className="ui-standards-qradio__glyph" viewBox="0 0 24 24">
            <path d={checked ? TASK_ALT_PATH : FISH_EYE_PATH} />
          </svg>
        ) : (
          <svg className="ui-standards-qradio__bg" viewBox="0 0 24 24">
            <path d={OUTER_PATH} />
            <path className="ui-standards-qradio__check" d={CHECK_PATH} />
          </svg>
        )}
      </span>
      <span className="ui-standards-qradio__label">{label}</span>
    </div>
  );
}

function RadioColor({
  sample,
  testId,
  icons = false,
  onDark = false,
}: {
  sample: InputSample;
  testId: string;
  icons?: boolean;
  onDark?: boolean;
}) {
  const [checked, setChecked] = useState(true);
  return (
    <UiStandardsQradio
      label={sample.label}
      color={radioAccent(sample)}
      checked={checked}
      icons={icons}
      onDark={onDark}
      darkInk={sample.id === "black"}
      onSelect={() => setChecked((value) => !value)}
      testId={`${testId}-${sample.id}`}
    />
  );
}

function RadioForceDark() {
  return (
    <div className="ui-standards-q-example" data-testid="ui-standard-radio-dark">
      <div className="ui-standards-q-example__label">Force dark mode</div>
      <div className="ui-standards-qradio-dark" data-testid="ui-standard-radio-dark-panel">
        <div className="ui-standards-qradio-dark__row">
          {INPUT_SAMPLES.map((sample) => (
            <RadioColor key={sample.id} sample={sample} onDark testId="ui-standard-radio-dark" />
          ))}
        </div>
      </div>
    </div>
  );
}

export function UiStandardsRadioGallery(): ReactNode {
  return (
    <div className="flex flex-col gap-4" data-testid="ui-standard-radio-gallery">
      <div className="ui-standards-q-example" data-testid="ui-standard-radio-colors">
        <div className="ui-standards-q-example__label">Colors</div>
        <div className="ui-standards-q-example__row">
          {INPUT_SAMPLES.map((sample) => (
            <RadioColor key={sample.id} sample={sample} testId="ui-standard-radio" />
          ))}
        </div>
      </div>
      <div className="ui-standards-q-example" data-testid="ui-standard-radio-icons">
        <div className="ui-standards-q-example__label">With icons</div>
        <div className="ui-standards-q-example__row">
          {INPUT_SAMPLES.map((sample) => (
            <RadioColor key={sample.id} sample={sample} icons testId="ui-standard-radio-icon" />
          ))}
        </div>
      </div>
      <RadioForceDark />
    </div>
  );
}

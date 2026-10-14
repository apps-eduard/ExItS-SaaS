import { useRef, useState, type CSSProperties, type PointerEvent as ReactPointerEvent, type ReactNode } from "react";
import { INPUT_SAMPLES, type InputSample } from "@/features/ui-standards/UiStandardsInputGallery";
import { radioAccent } from "@/features/ui-standards/UiStandardsRadioGallery";

/**
 * Quasar-style slider samples for the Form Components pilot.
 * Showcase only — not a product slider.
 * @see https://quasar.dev/vue-components/slider
 */
export function sliderAccent(sample: Pick<InputSample, "id" | "bg">): string {
  return radioAccent(sample);
}

export function clampSlider(value: number, min: number, max: number): number {
  return Math.min(max, Math.max(min, value));
}

export function quantizeSlider(value: number, min: number, max: number, step: number, snap: boolean): number {
  const clamped = clampSlider(value, min, max);
  if (!snap) return Math.round(clamped);
  const size = step > 0 ? step : 1;
  const snapped = Math.round((clamped - min) / size) * size + min;
  return clampSlider(Number(snapped.toFixed(4)), min, max);
}

export function sliderMarkers(min: number, max: number, step: number): number[] {
  const size = step > 0 ? step : 10;
  const marks: number[] = [];
  for (let value = min; value <= max + size / 1000; value += size) {
    marks.push(clampSlider(Number(value.toFixed(4)), min, max));
    if (marks.length > 21) break;
  }
  if (marks[marks.length - 1] !== max) marks.push(max);
  return marks;
}

export function sliderValueFromPoint(
  clientX: number,
  clientY: number,
  rect: DOMRect,
  vertical: boolean,
  min: number,
  max: number,
): number {
  const ratio = vertical
    ? 1 - (rect.height === 0 ? 0 : (clientY - rect.top) / rect.height)
    : rect.width === 0
      ? 0
      : (clientX - rect.left) / rect.width;
  return min + clampSlider(ratio, 0, 1) * (max - min);
}

export function UiStandardsQslider({
  color,
  ink = "#ffffff",
  value,
  min = 0,
  max = 100,
  step = 1,
  snap = false,
  showLabel = false,
  labelAlways = false,
  markers = false,
  vertical = false,
  disabled = false,
  onDark = false,
  onChange,
  testId,
}: {
  color: string;
  ink?: string;
  value: number;
  min?: number;
  max?: number;
  step?: number;
  snap?: boolean;
  showLabel?: boolean;
  labelAlways?: boolean;
  markers?: boolean;
  vertical?: boolean;
  disabled?: boolean;
  onDark?: boolean;
  onChange?: (value: number) => void;
  testId?: string;
}) {
  const trackRef = useRef<HTMLDivElement>(null);
  const [dragging, setDragging] = useState(false);
  const percent = max === min ? 0 : ((value - min) / (max - min)) * 100;
  const markerStep = markers ? Math.max(step, 10) : step;
  const marks = markers ? sliderMarkers(min, max, markerStep) : [];
  const style = { "--qslider-color": color, "--qslider-ink": ink } as CSSProperties;

  function commit(next: number) {
    onChange?.(quantizeSlider(next, min, max, step, snap || markers));
  }

  function read(event: { clientX: number; clientY: number }) {
    const rect = trackRef.current?.getBoundingClientRect();
    if (!rect) return value;
    return sliderValueFromPoint(event.clientX, event.clientY, rect, vertical, min, max);
  }

  function onPointerDown(event: ReactPointerEvent<HTMLDivElement>) {
    if (disabled) return;
    event.currentTarget.setPointerCapture(event.pointerId);
    setDragging(true);
    commit(read(event));
  }

  function onPointerMove(event: ReactPointerEvent<HTMLDivElement>) {
    if (!dragging || disabled) return;
    commit(read(event));
  }

  function onKeyDown(event: React.KeyboardEvent<HTMLDivElement>) {
    if (disabled) return;
    const size = snap || markers ? Math.max(step, 1) : 1;
    if (event.key === "ArrowRight" || event.key === "ArrowUp") {
      event.preventDefault();
      commit(value + size);
    } else if (event.key === "ArrowLeft" || event.key === "ArrowDown") {
      event.preventDefault();
      commit(value - size);
    } else if (event.key === "Home") {
      event.preventDefault();
      commit(min);
    } else if (event.key === "End") {
      event.preventDefault();
      commit(max);
    }
  }

  return (
    <div
      className={
        "ui-standards-qslider" +
        (vertical ? " ui-standards-qslider--vertical" : "") +
        (disabled ? " ui-standards-qslider--disabled" : "") +
        (onDark ? " ui-standards-qslider--on-dark" : "")
      }
      style={style}
      data-testid={testId ? `${testId}-root` : undefined}
    >
      <div
        ref={trackRef}
        className="ui-standards-qslider__track"
        onPointerDown={onPointerDown}
        onPointerMove={onPointerMove}
        onPointerUp={() => setDragging(false)}
        onPointerCancel={() => setDragging(false)}
      >
        <div className="ui-standards-qslider__selection" style={vertical ? { height: `${percent}%` } : { width: `${percent}%` }} />
        {marks.map((mark) => {
          const markPercent = max === min ? 0 : ((mark - min) / (max - min)) * 100;
          return (
            <span
              key={mark}
              className="ui-standards-qslider__marker"
              style={vertical ? { bottom: `${markPercent}%` } : { left: `${markPercent}%` }}
            />
          );
        })}
        <div
          role="slider"
          tabIndex={disabled ? -1 : 0}
          className="ui-standards-qslider__thumb"
          style={vertical ? { bottom: `${percent}%` } : { left: `${percent}%` }}
          aria-valuemin={min}
          aria-valuemax={max}
          aria-valuenow={value}
          aria-disabled={disabled || undefined}
          aria-orientation={vertical ? "vertical" : "horizontal"}
          data-testid={testId}
          onKeyDown={onKeyDown}
        >
          {showLabel && (labelAlways || dragging) ? <span className="ui-standards-qslider__bubble">{value}</span> : null}
        </div>
      </div>
    </div>
  );
}

function SliderColor({
  sample,
  testId,
  value,
  ...rest
}: {
  sample: InputSample;
  testId: string;
  value: number;
  step?: number;
  snap?: boolean;
  showLabel?: boolean;
  labelAlways?: boolean;
  markers?: boolean;
  vertical?: boolean;
  disabled?: boolean;
  onDark?: boolean;
}) {
  const [current, setCurrent] = useState(value);
  return (
    <UiStandardsQslider
      color={sliderAccent(sample)}
      ink={sample.fg}
      value={current}
      onChange={setCurrent}
      testId={`${testId}-${sample.id}`}
      {...rest}
    />
  );
}

function SliderRow({
  title,
  testId,
  samples = INPUT_SAMPLES,
  onDark = false,
  ...rest
}: {
  title: string;
  testId: string;
  samples?: readonly InputSample[];
  onDark?: boolean;
  step?: number;
  snap?: boolean;
  showLabel?: boolean;
  labelAlways?: boolean;
  markers?: boolean;
  vertical?: boolean;
  disabled?: boolean;
}) {
  const row = (
    <div className={"ui-standards-q-example__row" + (rest.vertical ? " ui-standards-qslider-row--vertical" : "")}>
      {samples.map((sample, index) => (
        <SliderColor
          key={sample.id}
          sample={sample}
          testId={testId}
          value={20 + index * 7}
          onDark={onDark}
          {...rest}
        />
      ))}
    </div>
  );
  return (
    <div className="ui-standards-q-example" data-testid={testId}>
      <div className="ui-standards-q-example__label">{title}</div>
      {onDark ? <div className="ui-standards-qslider-dark">{row}</div> : row}
    </div>
  );
}

export function UiStandardsSliderGallery(): ReactNode {
  const verticalSamples = INPUT_SAMPLES.filter((sample) =>
    ["standard", "primary", "secondary", "amber"].includes(sample.id),
  );
  return (
    <div className="flex flex-col gap-4" data-testid="ui-standard-slider-gallery">
      <SliderRow title="Standard" testId="ui-standard-slider" />
      <SliderRow title="Label" testId="ui-standard-slider-label" showLabel />
      <SliderRow title="Label always" testId="ui-standard-slider-label-always" showLabel labelAlways />
      <SliderRow title="Markers" testId="ui-standard-slider-markers" markers step={10} snap />
      <SliderRow title="Snap" testId="ui-standard-slider-snap" snap step={20} />
      <SliderRow title="Disabled" testId="ui-standard-slider-disabled" disabled />
      <SliderRow title="Vertical" testId="ui-standard-slider-vertical" samples={verticalSamples} vertical showLabel labelAlways />
    </div>
  );
}

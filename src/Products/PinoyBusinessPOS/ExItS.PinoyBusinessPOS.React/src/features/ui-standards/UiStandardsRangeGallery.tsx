import { useRef, useState, type CSSProperties, type PointerEvent as ReactPointerEvent, type ReactNode } from "react";
import { INPUT_SAMPLES, type InputSample } from "@/features/ui-standards/UiStandardsInputGallery";
import {
  clampSlider,
  quantizeSlider,
  sliderAccent,
  sliderMarkers,
  sliderValueFromPoint,
} from "@/features/ui-standards/UiStandardsSliderGallery";

/**
 * Quasar-style range samples for the locked Range standard.
 * Showcase only — not a product range.
 * @see https://quasar.dev/vue-components/range
 */
export type SliderRangeValue = { min: number; max: number };

export function UiStandardsQrange({
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
  dragRange = false,
  vertical = false,
  disabled = false,
  onDark = false,
  onChange,
  testId,
}: {
  color: string;
  ink?: string;
  value: SliderRangeValue;
  min?: number;
  max?: number;
  step?: number;
  snap?: boolean;
  showLabel?: boolean;
  labelAlways?: boolean;
  markers?: boolean;
  dragRange?: boolean;
  vertical?: boolean;
  disabled?: boolean;
  onDark?: boolean;
  onChange?: (value: SliderRangeValue) => void;
  testId?: string;
}) {
  const trackRef = useRef<HTMLDivElement>(null);
  const dragRef = useRef<{ mode: "min" | "max" | "range"; origin: number; start: SliderRangeValue } | null>(null);
  const [dragging, setDragging] = useState<"min" | "max" | "range" | null>(null);
  const toPercent = (point: number) => (max === min ? 0 : ((point - min) / (max - min)) * 100);
  const marks = markers ? sliderMarkers(min, max, Math.max(step, 10)) : [];
  const style = { "--qslider-color": color, "--qslider-ink": ink } as CSSProperties;

  function quantize(next: number) {
    return quantizeSlider(next, min, max, step, snap || markers);
  }

  function read(event: { clientX: number; clientY: number }) {
    const rect = trackRef.current?.getBoundingClientRect();
    if (!rect) return value.min;
    return sliderValueFromPoint(event.clientX, event.clientY, rect, vertical, min, max);
  }

  function apply(mode: "min" | "max" | "range", raw: number, origin = raw, start = value) {
    if (mode === "range") {
      const delta = quantize(raw) - quantize(origin);
      const width = start.max - start.min;
      let nextMin = clampSlider(start.min + delta, min, max - width);
      nextMin = quantize(nextMin);
      const nextMax = clampSlider(nextMin + width, nextMin, max);
      onChange?.({ min: nextMin, max: nextMax });
      return;
    }
    const next = quantize(raw);
    if (mode === "min") onChange?.({ min: Math.min(next, value.max), max: value.max });
    else onChange?.({ min: value.min, max: Math.max(next, value.min) });
  }

  function onPointerDown(event: ReactPointerEvent<HTMLDivElement>) {
    if (disabled) return;
    event.currentTarget.setPointerCapture(event.pointerId);
    const raw = read(event);
    const between = raw > value.min && raw < value.max;
    const mode: "min" | "max" | "range" =
      dragRange && between && Math.abs(raw - value.min) > step && Math.abs(raw - value.max) > step
        ? "range"
        : Math.abs(raw - value.min) <= Math.abs(raw - value.max)
          ? "min"
          : "max";
    dragRef.current = { mode, origin: raw, start: value };
    setDragging(mode);
    apply(mode, raw, raw, value);
  }

  function onPointerMove(event: ReactPointerEvent<HTMLDivElement>) {
    if (!dragRef.current || disabled) return;
    apply(dragRef.current.mode, read(event), dragRef.current.origin, dragRef.current.start);
  }

  function nudge(which: "min" | "max", delta: number) {
    const size = snap || markers ? Math.max(step, 1) : 1;
    if (which === "min") onChange?.({ min: Math.min(quantize(value.min + delta * size), value.max), max: value.max });
    else onChange?.({ min: value.min, max: Math.max(quantize(value.max + delta * size), value.min) });
  }

  function onKey(which: "min" | "max", event: React.KeyboardEvent<HTMLDivElement>) {
    if (disabled) return;
    if (event.key === "ArrowRight" || event.key === "ArrowUp") {
      event.preventDefault();
      nudge(which, 1);
    } else if (event.key === "ArrowLeft" || event.key === "ArrowDown") {
      event.preventDefault();
      nudge(which, -1);
    }
  }

  function thumb(which: "min" | "max") {
    const point = value[which];
    const percent = toPercent(point);
    const active = dragging === which || dragging === "range";
    return (
      <div
        key={which}
        role="slider"
        tabIndex={disabled ? -1 : 0}
        className="ui-standards-qslider__thumb"
        style={vertical ? { bottom: `${percent}%` } : { left: `${percent}%` }}
        aria-label={which === "min" ? "Minimum" : "Maximum"}
        aria-valuemin={min}
        aria-valuemax={max}
        aria-valuenow={point}
        aria-disabled={disabled || undefined}
        aria-orientation={vertical ? "vertical" : "horizontal"}
        data-testid={testId ? `${testId}-${which}` : undefined}
        onKeyDown={(event) => onKey(which, event)}
        onPointerDown={(event) => {
          event.stopPropagation();
          if (disabled) return;
          trackRef.current?.setPointerCapture(event.pointerId);
          dragRef.current = { mode: which, origin: point, start: value };
          setDragging(which);
        }}
      >
        {showLabel && (labelAlways || active) ? <span className="ui-standards-qslider__bubble">{point}</span> : null}
      </div>
    );
  }

  return (
    <div
      className={
        "ui-standards-qslider ui-standards-qrange" +
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
        onPointerUp={() => {
          dragRef.current = null;
          setDragging(null);
        }}
        onPointerCancel={() => {
          dragRef.current = null;
          setDragging(null);
        }}
      >
        <div
          className="ui-standards-qslider__selection"
          style={
            vertical
              ? { height: `${toPercent(value.max) - toPercent(value.min)}%`, bottom: `${toPercent(value.min)}%` }
              : { left: `${toPercent(value.min)}%`, width: `${toPercent(value.max) - toPercent(value.min)}%` }
          }
        />
        {marks.map((mark) => {
          const markPercent = toPercent(mark);
          return (
            <span
              key={mark}
              className="ui-standards-qslider__marker"
              style={vertical ? { bottom: `${markPercent}%` } : { left: `${markPercent}%` }}
            />
          );
        })}
        {thumb("min")}
        {thumb("max")}
      </div>
    </div>
  );
}

function RangeColor({
  sample,
  testId,
  value,
  ...rest
}: {
  sample: InputSample;
  testId: string;
  value: SliderRangeValue;
  step?: number;
  snap?: boolean;
  showLabel?: boolean;
  labelAlways?: boolean;
  markers?: boolean;
  dragRange?: boolean;
  vertical?: boolean;
  disabled?: boolean;
  onDark?: boolean;
}) {
  const [current, setCurrent] = useState(value);
  return (
    <UiStandardsQrange
      color={sliderAccent(sample)}
      ink={sample.fg}
      value={current}
      onChange={setCurrent}
      testId={`${testId}-${sample.id}`}
      {...rest}
    />
  );
}

function RangeRow({
  title,
  testId,
  onDark = false,
  ...rest
}: {
  title: string;
  testId: string;
  onDark?: boolean;
  step?: number;
  snap?: boolean;
  showLabel?: boolean;
  labelAlways?: boolean;
  markers?: boolean;
  dragRange?: boolean;
  disabled?: boolean;
}) {
  const row = (
    <div className="ui-standards-q-example__row">
      {INPUT_SAMPLES.map((sample) => (
        <RangeColor
          key={sample.id}
          sample={sample}
          testId={testId}
          value={{ min: 20, max: 70 }}
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

export function UiStandardsRangeGallery(): ReactNode {
  return (
    <div className="flex flex-col gap-4" data-testid="ui-standard-range-gallery">
      <RangeRow title="Standard" testId="ui-standard-range" />
      <RangeRow title="Label" testId="ui-standard-range-label" showLabel />
      <RangeRow title="Label always" testId="ui-standard-range-label-always" showLabel labelAlways />
      <RangeRow title="Markers" testId="ui-standard-range-markers" markers step={10} snap />
      <RangeRow title="Drag range" testId="ui-standard-range-drag" dragRange showLabel labelAlways />
      <RangeRow title="Disabled" testId="ui-standard-range-disabled" disabled />
    </div>
  );
}

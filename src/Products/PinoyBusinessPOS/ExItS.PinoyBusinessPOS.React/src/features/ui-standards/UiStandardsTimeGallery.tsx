import { useEffect, useRef, useState, type CSSProperties, type PointerEvent as ReactPointerEvent, type ReactNode, type WheelEvent } from "react";
import { Clock } from "lucide-react";
import { INPUT_SAMPLES, UiStandardsQinput, type InputSample } from "@/features/ui-standards/UiStandardsInputGallery";
import { SoftGlassRollCaret } from "@/features/ui-standards/soft-glass-roll-caret";
import {
  SOFT_GLASS_DATETIME_FORMAT_CHECKED_LABEL,
  SOFT_GLASS_DATETIME_FORMAT_UNCHECKED_LABEL,
} from "@/features/ui-standards/soft-glass-datetime-global-default";
import {
  SOFT_GLASS_SCALE_MS,
  softGlassPrefersReducedMotion,
} from "@/features/ui-standards/soft-glass-scale-presence";
import { softGlassRollTouchHandlers } from "@/features/ui-standards/soft-glass-roll-touch";
import { radioAccent } from "@/features/ui-standards/UiStandardsRadioGallery";
import { DROPDOWN_STANDARD_COLOR, UiStandardsQdropdown } from "@/features/ui-standards/UiStandardsDropdownGallery";
import { Switch } from "@/components/ui/switch";

const SOFT_GLASS_TIME_PRIMARY = "var(--exits-primary)";
const SOFT_GLASS_TIME_INK = "#ffffff";

/**
 * Quasar-style time picker samples for the Form Components pilot.
 * Showcase only — not a product time picker.
 * @see https://quasar.dev/vue-components/time
 */
export const TIME_PICKER_SAMPLE = "09:30";

export function nowTime(now = new Date()): string {
  return formatTime(now.getHours(), now.getMinutes());
}

export type TimeRangeValue = { from: string; to: string };

export const TIME_RANGE_SAMPLE: TimeRangeValue = { from: "09:30", to: "17:00" };

export type TimePickerValue = string | TimeRangeValue;

export type TimeAppearance = "standard" | "raised" | "gradient" | "flat" | "soft-glass";
export type TimeVariety = "clock" | "roll";

const DEFAULT_TIME_INTENTS = ["Standard", "Primary", "Secondary"] as const;
const MINUTE_MARKS = [0, 5, 10, 15, 20, 25, 30, 35, 40, 45, 50, 55] as const;

export function isTimeRangeValue(value: TimePickerValue): value is TimeRangeValue {
  return typeof value !== "string";
}

export function parseTime(value: string): { hour: number; minute: number } {
  const [hour, minute] = value.split(":").map(Number);
  return {
    hour: Math.min(23, Math.max(0, hour || 0)),
    minute: Math.min(59, Math.max(0, minute || 0)),
  };
}

export function formatTime(hour: number, minute: number): string {
  return `${String(hour).padStart(2, "0")}:${String(minute).padStart(2, "0")}`;
}

export function to12Hour(hour: number): { hour: number; period: "AM" | "PM" } {
  return { hour: hour % 12 || 12, period: hour >= 12 ? "PM" : "AM" };
}

export function to24Hour(hour12: number, period: "AM" | "PM"): number {
  if (period === "AM") return hour12 === 12 ? 0 : hour12;
  return hour12 === 12 ? 12 : hour12 + 12;
}

export function minuteFromClockDegrees(degrees: number): number {
  const normalized = ((degrees % 360) + 360) % 360;
  return Math.round(normalized / 6) % 60;
}

export function hourFromClockDegrees(degrees: number, inner: boolean): number {
  const normalized = ((degrees % 360) + 360) % 360;
  const index = Math.round(normalized / 30) % 12;
  return inner ? index + 12 : index;
}

function readClockPoint(event: { clientX: number; clientY: number }, element: HTMLElement) {
  const rect = element.getBoundingClientRect();
  const dx = event.clientX - (rect.left + rect.width / 2);
  const dy = event.clientY - (rect.top + rect.height / 2);
  return {
    degrees: (Math.atan2(dx, -dy) * 180) / Math.PI,
    distance: Math.hypot(dx, dy),
    radius: rect.width / 2,
  };
}

export function displayTime(value: string, hour24: boolean): string {
  const { hour, minute } = parseTime(value);
  const mm = String(minute).padStart(2, "0");
  if (hour24) return `${String(hour).padStart(2, "0")}:${mm}`;
  const shown = to12Hour(hour);
  return `${shown.hour}:${mm} ${shown.period}`;
}

function shiftTime(value: string, part: "hour" | "minute" | "period", direction: -1 | 1, hour24: boolean): string {
  const { hour, minute } = parseTime(value);
  if (part === "minute") return formatTime(hour, (minute + direction + 60) % 60);
  if (part === "period") return formatTime((hour + 12) % 24, minute);
  if (hour24) return formatTime((hour + direction + 24) % 24, minute);
  const shown = to12Hour(hour);
  const next = shown.hour + direction;
  const wrapped = next < 1 ? 12 : next > 12 ? 1 : next;
  return formatTime(to24Hour(wrapped, shown.period), minute);
}

function TimeRoll({
  label,
  iso,
  hour24,
  disabled,
  commit = false,
  testId,
  onChange,
  onCommit,
}: {
  label?: string;
  iso: string;
  hour24: boolean;
  disabled: boolean;
  commit?: boolean;
  testId?: string;
  onChange: (iso: string) => void;
  onCommit?: () => void;
}) {
  const { hour, minute } = parseTime(iso);
  const shown = to12Hour(hour);
  const [quick, setQuick] = useState<null | "hour" | "minute">(null);
  const [quickOpen, setQuickOpen] = useState(false);
  const hourLabel = hour24 ? String(hour).padStart(2, "0") : String(shown.hour);
  const prevHour = hour24
    ? String(parseTime(shiftTime(iso, "hour", -1, true)).hour).padStart(2, "0")
    : String(to12Hour(parseTime(shiftTime(iso, "hour", -1, false)).hour).hour);
  const nextHour = hour24
    ? String(parseTime(shiftTime(iso, "hour", 1, true)).hour).padStart(2, "0")
    : String(to12Hour(parseTime(shiftTime(iso, "hour", 1, false)).hour).hour);
  const hourChoices = hour24
    ? Array.from({ length: 24 }, (_, value) => ({
        label: String(value).padStart(2, "0"),
        value,
        selected: value === hour,
      }))
    : Array.from({ length: 12 }, (_, index) => {
        const value = index + 1;
        return {
          label: String(value),
          value,
          selected: value === shown.hour,
        };
      });

  function nudge(part: "hour" | "minute" | "period", direction: -1 | 1) {
    if (!disabled) onChange(shiftTime(iso, part, direction, hour24));
  }

  function onWheel(part: "hour" | "minute" | "period", event: WheelEvent<HTMLDivElement>) {
    event.preventDefault();
    nudge(part, event.deltaY > 0 ? 1 : -1);
  }

  useEffect(() => {
    if (quickOpen || quick == null) return;
    if (softGlassPrefersReducedMotion()) {
      setQuick(null);
      return;
    }
    const timeout = window.setTimeout(() => setQuick(null), SOFT_GLASS_SCALE_MS);
    return () => window.clearTimeout(timeout);
  }, [quickOpen, quick]);

  function openQuick(next: "hour" | "minute") {
    setQuick(next);
    setQuickOpen(true);
  }

  function chooseHour(nextHourValue: number) {
    if (disabled) return;
    const next = hour24 ? formatTime(nextHourValue, minute) : formatTime(to24Hour(nextHourValue, shown.period), minute);
    onChange(next);
    setQuickOpen(false);
    onCommit?.();
  }

  function chooseMinute(nextMinute: number) {
    if (disabled) return;
    onChange(formatTime(hour, nextMinute));
    setQuickOpen(false);
    onCommit?.();
  }

  const quickState = quickOpen ? "open" : "closed";

  return (
    <div className="ui-standards-qdate__roll-group">
      {label ? <div className="ui-standards-qdate__roll-label">{label}</div> : null}
      {quick === "hour" ? (
        <div
          className="ui-standards-qdate__quick-panel"
          data-transition="scale"
          data-state={quickState}
          data-testid={testId ? `${testId}-quick-hour` : undefined}
        >
          <div
            className={"ui-standards-qdate__choices" + (hour24 ? " ui-standards-qdate__choices--hours-24" : "")}
            role="listbox"
            aria-label="Hour"
          >
            {hourChoices.map((choice) => (
              <button
                key={choice.label}
                type="button"
                className={"ui-standards-qdate__choice" + (choice.selected ? " ui-standards-qdate__choice--selected" : "")}
                aria-pressed={choice.selected}
                disabled={disabled}
                data-testid={testId ? `${testId}-pick-hour-${choice.label}` : undefined}
                onClick={() => chooseHour(choice.value)}
              >
                {choice.label}
              </button>
            ))}
          </div>
        </div>
      ) : null}
      {quick === "minute" ? (
        <div
          className="ui-standards-qdate__quick-panel"
          data-transition="scale"
          data-state={quickState}
          data-testid={testId ? `${testId}-quick-minute` : undefined}
        >
          <div className="ui-standards-qdate__choices ui-standards-qdate__choices--minutes" role="listbox" aria-label="Minute">
            {Array.from({ length: 60 }, (_, value) => {
              const label = String(value).padStart(2, "0");
              const selected = value === minute;
              return (
                <button
                  key={label}
                  type="button"
                  className={"ui-standards-qdate__choice" + (selected ? " ui-standards-qdate__choice--selected" : "")}
                  aria-pressed={selected}
                  disabled={disabled}
                  data-testid={testId ? `${testId}-pick-minute-${label}` : undefined}
                  onClick={() => chooseMinute(value)}
                >
                  {label}
                </button>
              );
            })}
          </div>
        </div>
      ) : null}
      {quick ? null : (
        <div className="ui-standards-qdate__roll" data-testid={testId}>
          <RollColumn
            caption="Hour"
            prev={prevHour}
            current={hourLabel}
            next={nextHour}
            disabled={disabled}
            choose
            testId={testId ? `${testId}-hour` : undefined}
            onPrev={() => nudge("hour", -1)}
            onNext={() => nudge("hour", 1)}
            onChoose={() => openQuick("hour")}
            onWheel={(event) => onWheel("hour", event)}
          />
          <RollColumn
            caption="Minute"
            prev={String((minute + 59) % 60).padStart(2, "0")}
            current={String(minute).padStart(2, "0")}
            next={String((minute + 1) % 60).padStart(2, "0")}
            disabled={disabled}
            choose
            testId={testId ? `${testId}-minute` : undefined}
            onPrev={() => nudge("minute", -1)}
            onNext={() => nudge("minute", 1)}
            onChoose={() => openQuick("minute")}
            onWheel={(event) => onWheel("minute", event)}
          />
          {hour24 ? null : (
            <RollColumn
              caption="Period"
              prev={shown.period === "AM" ? "PM" : "AM"}
              current={shown.period}
              next={shown.period === "AM" ? "PM" : "AM"}
              disabled={disabled}
              commit={commit}
              testId={testId ? `${testId}-period` : undefined}
              onPrev={() => nudge("period", 1)}
              onNext={() => nudge("period", 1)}
              onCommit={onCommit}
              onWheel={(event) => onWheel("period", event)}
            />
          )}
        </div>
      )}
    </div>
  );
}

function RollColumn({
  caption,
  prev,
  current,
  next,
  disabled,
  commit = false,
  choose = false,
  testId,
  onPrev,
  onNext,
  onCommit,
  onChoose,
  onWheel,
}: {
  caption: string;
  prev: string;
  current: string;
  next: string;
  disabled: boolean;
  commit?: boolean;
  choose?: boolean;
  testId?: string;
  onPrev: () => void;
  onNext: () => void;
  onCommit?: () => void;
  onChoose?: () => void;
  onWheel: (event: WheelEvent<HTMLDivElement>) => void;
}) {
  const middleClick = choose ? onChoose : commit ? onCommit : undefined;
  const startYRef = useRef<number | null>(null);
  const touch = softGlassRollTouchHandlers(onPrev, onNext, disabled, startYRef);
  return (
    <div className="ui-standards-qdate__roll-col" onWheel={onWheel} {...touch}>
      <span className="ui-standards-qdate__roll-caption">{caption}</span>
      <button type="button" className="ui-standards-qdate__roll-btn" aria-label={`Previous ${caption}`} disabled={disabled} onClick={onPrev}>
        <SoftGlassRollCaret direction="up" />
      </button>
      <div className="ui-standards-qdate__roll-window">
        <span className="ui-standards-qdate__roll-item">{prev}</span>
        {middleClick ? (
          <button
            type="button"
            className="ui-standards-qdate__roll-item ui-standards-qdate__roll-item--current"
            aria-label={`Choose ${caption}`}
            disabled={disabled}
            data-testid={testId}
            onClick={middleClick}
          >
            {current}
          </button>
        ) : (
          <span className="ui-standards-qdate__roll-item ui-standards-qdate__roll-item--current" data-testid={testId}>
            {current}
          </span>
        )}
        <span className="ui-standards-qdate__roll-item">{next}</span>
      </div>
      <button type="button" className="ui-standards-qdate__roll-btn" aria-label={`Next ${caption}`} disabled={disabled} onClick={onNext}>
        <SoftGlassRollCaret direction="down" />
      </button>
    </div>
  );
}

function ClockTicks({
  face,
  hour,
  minute,
  hour24,
  disabled,
  testId,
  onHour,
  onMinute,
  onHourPreview,
  onMinutePreview,
}: {
  face: "hour" | "minute";
  hour: number;
  minute: number;
  hour24: boolean;
  disabled: boolean;
  testId?: string;
  onHour: (hour: number) => void;
  onMinute: (minute: number) => void;
  onHourPreview?: (hour: number) => void;
  onMinutePreview?: (minute: number) => void;
}) {
  const ticks =
    face === "minute"
      ? MINUTE_MARKS.map((mark, index) => ({
          key: `m-${mark}`,
          label: String(mark).padStart(2, "0"),
          angle: index * 30,
          radius: "6.2rem",
          pressed: minute === mark,
          test: `${testId}-minute-${String(mark).padStart(2, "0")}`,
          choose: () => onMinute(mark),
        }))
      : hour24
        ? Array.from({ length: 24 }, (_, value) => ({
            key: `h-${value}`,
            label: String(value).padStart(2, "0"),
            angle: (value % 12) * 30,
            radius: value < 12 ? "6.2rem" : "4.15rem",
            pressed: hour === value,
            test: `${testId}-hour-${String(value).padStart(2, "0")}`,
            choose: () => onHour(value),
          }))
        : Array.from({ length: 12 }, (_, index) => {
            const label = index === 0 ? 12 : index;
            return {
              key: `h-${label}`,
              label: String(label),
              angle: index * 30,
              radius: "6.2rem",
              pressed: to12Hour(hour).hour === label,
              test: `${testId}-hour-${label}`,
              choose: () => onHour(to24Hour(label, to12Hour(hour).period)),
            };
          });
  const clockRef = useRef<HTMLDivElement>(null);
  const dragging = useRef(false);
  const handAngle = face === "minute" ? minute * 6 : (hour % 12) * 30;
  const handLength = face === "hour" && hour24 && hour >= 12 ? "3.55rem" : "5.5rem";
  const looseMinute = face === "minute" && minute % 5 !== 0;

  function readChoice(event: ReactPointerEvent) {
    const el = clockRef.current;
    if (!el) return null;
    const point = readClockPoint(event, el);
    if (face === "minute") return { minute: minuteFromClockDegrees(point.degrees) };
    const inner = hour24 && point.radius > 0 && point.distance < point.radius * 0.66;
    const raw = hourFromClockDegrees(point.degrees, inner);
    if (hour24) return { hour: raw };
    const label = raw === 0 ? 12 : raw;
    return { hour: to24Hour(label, to12Hour(hour).period) };
  }

  function onPointerDown(event: ReactPointerEvent<HTMLDivElement>) {
    if (disabled || (event.target as HTMLElement).closest("button")) return;
    dragging.current = true;
    event.currentTarget.setPointerCapture(event.pointerId);
    const next = readChoice(event);
    if (next?.minute != null) onMinutePreview?.(next.minute);
    if (next?.hour != null) onHourPreview?.(next.hour);
  }

  function onPointerMove(event: ReactPointerEvent<HTMLDivElement>) {
    if (!dragging.current) return;
    const next = readChoice(event);
    if (next?.minute != null) onMinutePreview?.(next.minute);
    if (next?.hour != null) onHourPreview?.(next.hour);
  }

  function onPointerUp(event: ReactPointerEvent<HTMLDivElement>) {
    if (!dragging.current) return;
    dragging.current = false;
    const next = readChoice(event);
    if (next?.minute != null) onMinute(next.minute);
    if (next?.hour != null) onHour(next.hour);
  }

  return (
    <div
      ref={clockRef}
      className="ui-standards-qtime__clock"
      data-testid={testId ? `${testId}-clock` : undefined}
      onPointerDown={onPointerDown}
      onPointerMove={onPointerMove}
      onPointerUp={onPointerUp}
    >
      <div
        className="ui-standards-qtime__hand"
        style={{ "--qtime-hand-angle": `${handAngle}deg`, "--qtime-hand-length": handLength } as CSSProperties}
        data-testid={testId ? `${testId}-hand` : undefined}
      >
        <span className="ui-standards-qtime__hand-line" />
        <span className={looseMinute ? "ui-standards-qtime__hand-knob ui-standards-qtime__hand-knob--label" : "ui-standards-qtime__hand-knob"}>
          {looseMinute ? String(minute).padStart(2, "0") : null}
        </span>
      </div>
      <span className="ui-standards-qtime__hand-center" />
      {ticks.map((tick) => (
        <button
          key={tick.key}
          type="button"
          className="ui-standards-qtime__tick"
          style={{ "--qtime-angle": `${tick.angle}deg`, "--qtime-radius": tick.radius } as CSSProperties}
          aria-pressed={tick.pressed}
          disabled={disabled}
          data-testid={testId ? tick.test : undefined}
          onClick={tick.choose}
        >
          {tick.label}
        </button>
      ))}
    </div>
  );
}

export function UiStandardsQtime({
  color,
  ink = "#ffffff",
  value,
  range = false,
  now = TIME_PICKER_SAMPLE,
  showNow = false,
  nowPlacement = "footer",
  onNow,
  hour24 = false,
  onHour24Change,
  hour24TestId,
  formatPlacement = "body",
  disabled = false,
  appearance = "standard",
  variety = "clock",
  onDark = false,
  onChange,
  onPicked,
  showSet = false,
  onSet,
  showValue = true,
  /** Body-only rolls for Soft Glass date+time combined card (shared header / footer). */
  embedded = false,
  testId,
}: {
  color: string;
  ink?: string;
  value: TimePickerValue;
  range?: boolean;
  now?: string;
  showNow?: boolean;
  /** Footer is the sample row. Header matches the Today glass button. */
  nowPlacement?: "footer" | "header";
  onNow?: () => void;
  hour24?: boolean;
  onHour24Change?: (hour24: boolean) => void;
  hour24TestId?: string;
  /** Body keeps the 24-hour label. Header puts the 12/24 switch at the top. */
  formatPlacement?: "body" | "header";
  disabled?: boolean;
  appearance?: TimeAppearance;
  variety?: TimeVariety;
  onDark?: boolean;
  onChange?: (value: TimePickerValue) => void;
  onPicked?: () => void;
  /** Soft Glass Roll date-time: Set under the roll carets. */
  showSet?: boolean;
  onSet?: () => void;
  showValue?: boolean;
  embedded?: boolean;
  testId?: string;
}) {
  const rangeValue = isTimeRangeValue(value) ? value : { from: value, to: value };
  const single = isTimeRangeValue(value) ? value.from : value;
  const focus = range ? rangeValue.to : single;
  const [face, setFace] = useState<"hour" | "minute">("hour");
  const [pendingHour, setPendingHour] = useState(parseTime(focus).hour);
  const [anchor, setAnchor] = useState<string | null>(null);
  const [dragHour, setDragHour] = useState<number | null>(null);
  const [dragMinute, setDragMinute] = useState<number | null>(null);
  const [localHour24, setLocalHour24] = useState(hour24);
  const format24 = onHour24Change ? hour24 : localHour24;
  const output = range ? `${rangeValue.from} – ${rangeValue.to}` : single;
  const viewHour = dragHour ?? pendingHour;
  const viewMinute = dragMinute ?? parseTime(focus).minute;
  const dragging = dragHour != null || dragMinute != null;
  const dragText = displayTime(formatTime(viewHour, viewMinute), format24);

  useEffect(() => {
    setLocalHour24(hour24);
  }, [hour24]);

  useEffect(() => {
    setPendingHour(parseTime(focus).hour);
  }, [focus]);

  function commit(iso: string, asSingle = false) {
    if (disabled) return;
    setDragHour(null);
    setDragMinute(null);
    if (!range || asSingle) {
      setAnchor(null);
      onChange?.(range ? { from: iso, to: iso } : iso);
      onPicked?.();
      setFace("hour");
      return;
    }
    if (!anchor) {
      setAnchor(iso);
      onChange?.({ from: iso, to: iso });
      setFace("hour");
      return;
    }
    const from = anchor < iso ? anchor : iso;
    const to = anchor < iso ? iso : anchor;
    setAnchor(null);
    onChange?.({ from, to });
    onPicked?.();
    setFace("hour");
  }

  function chooseHour(hour: number) {
    if (disabled) return;
    setDragHour(null);
    setDragMinute(null);
    setPendingHour(hour);
    setFace("minute");
  }

  function changeHour24(next: boolean) {
    setDragHour(null);
    setDragMinute(null);
    if (onHour24Change) onHour24Change(next);
    else setLocalHour24(next);
  }

  const rootClass = [
    "ui-standards-qdate",
    "ui-standards-qtime",
    appearance !== "standard" ? `ui-standards-qdate--${appearance}` : "",
    variety === "roll" ? "ui-standards-qdate--roll" : "",
    showNow && !embedded ? "ui-standards-qtime--now" : "",
    disabled ? "ui-standards-qdate--disabled" : "",
    onDark ? "ui-standards-qdate--on-dark" : "",
    embedded ? "ui-standards-qdate--embedded" : "",
  ]
    .filter(Boolean)
    .join(" ");
  const formatSwitch = (
    <Switch
      checked={format24}
      disabled={disabled}
      onCheckedChange={changeHour24}
      aria-label="Hour format"
      checkedLabel={formatPlacement === "header" ? SOFT_GLASS_DATETIME_FORMAT_CHECKED_LABEL : "ON"}
      uncheckedLabel={formatPlacement === "header" ? SOFT_GLASS_DATETIME_FORMAT_UNCHECKED_LABEL : "OFF"}
      data-testid={hour24TestId}
    />
  );
  const nowButton = showNow ? (
    <button
      type="button"
      className={"ui-standards-qdate__today" + (nowPlacement === "header" ? " ui-standards-qdate__today--header" : "")}
      disabled={disabled}
      data-testid={testId ? `${testId}-now` : undefined}
      onClick={() => {
        commit(now, true);
        onNow?.();
      }}
    >
      Now
    </button>
  ) : null;
  const timeLabel = range
    ? `${displayTime(rangeValue.from, format24)} – ${dragging ? dragText : displayTime(rangeValue.to, format24)}`
    : dragging
      ? dragText
      : displayTime(single, format24);

  return (
    <div
      className={rootClass}
      style={{ "--qdate-color": color, "--qdate-ink": ink } as CSSProperties}
      data-testid={testId}
      data-format={format24 ? "24" : "12"}
    >
      {embedded ? null : (
        <div className="ui-standards-qdate__header">
          {formatPlacement === "header" ? (
            <div className="ui-standards-qtime__format ui-standards-qtime__format--header">{formatSwitch}</div>
          ) : null}
          <span className="ui-standards-qdate__selected">{timeLabel}</span>
          {nowPlacement === "header" ? nowButton : null}
        </div>
      )}
      <div className="ui-standards-qdate__body">
        {embedded || formatPlacement === "header" ? null : (
          <label className="ui-standards-qtime__format">
            {formatSwitch}
            24-hour
          </label>
        )}
        {variety === "roll" ? (
          range ? (
            <div className="ui-standards-qdate__roll-stack">
              <TimeRoll
                label="From"
                iso={rangeValue.from}
                hour24={format24}
                disabled={disabled}
                testId={testId ? `${testId}-from` : undefined}
                onChange={(iso) => onChange?.({ from: iso, to: iso > rangeValue.to ? iso : rangeValue.to })}
              />
              <TimeRoll
                label="To"
                iso={rangeValue.to}
                hour24={format24}
                disabled={disabled}
                commit
                testId={testId ? `${testId}-to` : undefined}
                onChange={(iso) => onChange?.({ from: iso < rangeValue.from ? iso : rangeValue.from, to: iso })}
                onCommit={() => commit(rangeValue.to, true)}
              />
            </div>
          ) : (
            <TimeRoll
              iso={single}
              hour24={format24}
              disabled={disabled}
              commit
              testId={testId}
              onChange={(iso) => onChange?.(iso)}
              onCommit={() => onPicked?.()}
            />
          )
        ) : (
          <>
            <div className="ui-standards-qtime__faces">
              <button type="button" aria-pressed={face === "hour"} disabled={disabled} data-testid={testId ? `${testId}-face-hour` : undefined} onClick={() => setFace("hour")}>
                Hour
              </button>
              <button type="button" aria-pressed={face === "minute"} disabled={disabled} data-testid={testId ? `${testId}-face-minute` : undefined} onClick={() => setFace("minute")}>
                Minute
              </button>
            </div>
            <ClockTicks
              face={face}
              hour={viewHour}
              minute={viewMinute}
              hour24={format24}
              disabled={disabled}
              testId={testId}
              onHour={chooseHour}
              onMinute={(minute) => commit(formatTime(viewHour, minute))}
              onHourPreview={setDragHour}
              onMinutePreview={setDragMinute}
            />
            {format24 ? null : (
              <div className="ui-standards-qtime__period">
                {(["AM", "PM"] as const).map((period) => (
                  <button
                    key={period}
                    type="button"
                    aria-pressed={to12Hour(pendingHour).period === period}
                    disabled={disabled}
                    data-testid={testId ? `${testId}-period-${period.toLowerCase()}` : undefined}
                    onClick={() => setPendingHour(to24Hour(to12Hour(pendingHour).hour, period))}
                  >
                    {period}
                  </button>
                ))}
              </div>
            )}
          </>
        )}
        {embedded ? null : showSet ? (
          <button
            type="button"
            className="ui-standards-qdate__set"
            disabled={disabled}
            data-testid={testId ? `${testId}-set` : undefined}
            onClick={() => {
              if (range) onChange?.(rangeValue);
              else onChange?.(single);
              onSet?.();
            }}
          >
            Set
          </button>
        ) : null}
        {embedded || nowPlacement === "header" ? null : nowButton}
        {embedded || !showValue ? null : (
          <div className="ui-standards-qdate__output" data-testid={testId ? `${testId}-value` : undefined}>
            <span className="ui-standards-qdate__output-label">Value</span>
            <span className="ui-standards-qdate__output-text">{output}</span>
          </div>
        )}
      </div>
    </div>
  );
}

function TimeColor({
  sample,
  testId,
  range = false,
  showNow = false,
  hour24 = false,
  disabled = false,
  appearance = "standard",
  variety = "clock",
  onDark = false,
}: {
  sample: InputSample;
  testId: string;
  range?: boolean;
  showNow?: boolean;
  hour24?: boolean;
  disabled?: boolean;
  appearance?: TimeAppearance;
  variety?: TimeVariety;
  onDark?: boolean;
}) {
  const [value, setValue] = useState<TimePickerValue>(range ? TIME_RANGE_SAMPLE : TIME_PICKER_SAMPLE);
  return (
    <UiStandardsQtime
      color={radioAccent(sample)}
      ink={sample.fg}
      value={value}
      range={range}
      showNow={showNow}
      hour24={hour24}
      disabled={disabled}
      appearance={appearance}
      variety={variety}
      onDark={onDark}
      onChange={setValue}
      testId={`${testId}-${sample.id}`}
    />
  );
}

function TimeRow({
  title,
  testId,
  ...rest
}: {
  title: string;
  testId: string;
  range?: boolean;
  showNow?: boolean;
  hour24?: boolean;
  disabled?: boolean;
  appearance?: TimeAppearance;
  variety?: TimeVariety;
}) {
  const [intents, setIntents] = useState<string[]>([...DEFAULT_TIME_INTENTS]);
  const samples = INPUT_SAMPLES.filter((sample) => intents.includes(sample.label));
  return (
    <div className="ui-standards-q-example" data-testid={testId}>
      <div className="ui-standards-q-example__label">{title}</div>
      <div className="ui-standards-qdate-intents">
        <UiStandardsQdropdown
          label="Intent"
          color={DROPDOWN_STANDARD_COLOR}
          value=""
          values={intents}
          options={INPUT_SAMPLES.map((sample) => sample.label)}
          multiple
          standard
          onChangeValues={setIntents}
          testId={`${testId}-intents`}
        />
      </div>
      <div className="ui-standards-q-example__row">
        {samples.map((sample) => (
          <TimeColor key={sample.id} sample={sample} testId={testId} {...rest} />
        ))}
      </div>
    </div>
  );
}

/**
 * Soft Glass time UI sample — hour / minute / AM·PM roll field + popover as glass.
 * Personal Soft Glass only; Organization Date glass unchanged.
 */
function SoftGlassTimeUiGlassSample(): ReactNode {
  const [value, setValue] = useState("21:00");
  const display = displayTime(value, false);
  return (
    <div
      className="ui-standards-soft-glass-ui-sample"
      data-shell="soft-glass"
      data-testid="ui-standard-time-soft-glass-ui-sample"
    >
      <div className="ui-standards-soft-glass-ui-sample__title">Soft Glass · Time UI</div>
      <p className="ui-standards-soft-glass-ui-sample__blurb">
        Column time picker (hour, minute, AM/PM) in Soft Glass. Same shell on mobile.
      </p>
      <div className="ui-standards-soft-glass-ui-sample__stack">
        <div className="ui-standards-date-preview-card ui-standards-date-preview-card--soft-glass ui-standards-soft-glass-ui-sample__field">
          <UiStandardsQinput
            label="Time"
            value={display}
            readOnly
            placeholder="hh:mm am"
            accent={SOFT_GLASS_TIME_PRIMARY}
            fill={SOFT_GLASS_TIME_PRIMARY}
            ink={SOFT_GLASS_TIME_INK}
            appearance="outlined"
            suffix={<Clock className="ui-standards-qinput__picker-icon" aria-hidden />}
            testId="ui-standard-time-soft-glass-ui-input"
          />
        </div>
        <UiStandardsQtime
          color={SOFT_GLASS_TIME_PRIMARY}
          ink={SOFT_GLASS_TIME_INK}
          value={value}
          appearance="soft-glass"
          variety="roll"
          showValue={false}
          formatPlacement="header"
          onChange={(next) => {
            if (typeof next === "string") setValue(next);
          }}
          testId="ui-standard-time-soft-glass-ui-roll"
        />
      </div>
    </div>
  );
}

/** Soft Glass Roll time samples — sits below the Time playground (not in the Time samples gallery). */
export function UiStandardsSoftGlassRollTimeSamples(): ReactNode {
  return (
    <div className="flex flex-col gap-4" data-testid="ui-standard-time-soft-glass-roll-samples">
      <SoftGlassTimeUiGlassSample />
      <TimeRow title="Soft Glass Roll" testId="ui-standard-time-soft-glass-roll" appearance="soft-glass" variety="roll" />
    </div>
  );
}

export function UiStandardsTimeGallery(): ReactNode {
  return (
    <div className="flex flex-col gap-4" data-testid="ui-standard-time-gallery">
      <TimeRow title="Standard" testId="ui-standard-time" />
      <TimeRow title="Soft Glass" testId="ui-standard-time-soft-glass" appearance="soft-glass" />
      <TimeRow title="Raised" testId="ui-standard-time-raised" appearance="raised" />
      <TimeRow title="Gradient" testId="ui-standard-time-gradient" appearance="gradient" />
      <TimeRow title="Flat" testId="ui-standard-time-flat" appearance="flat" />
      <TimeRow title="Roll" testId="ui-standard-time-roll" variety="roll" />
      <TimeRow title="24-hour" testId="ui-standard-time-24" hour24 />
      <TimeRow title="Now" testId="ui-standard-time-now" showNow />
      <TimeRow title="Range" testId="ui-standard-time-range" range />
      <TimeRow title="Disabled" testId="ui-standard-time-disabled" disabled />
    </div>
  );
}

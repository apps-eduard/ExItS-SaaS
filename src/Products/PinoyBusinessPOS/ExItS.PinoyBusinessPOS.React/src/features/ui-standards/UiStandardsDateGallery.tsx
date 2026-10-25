import { useEffect, useRef, useState, type CSSProperties, type ReactNode, type WheelEvent } from "react";
import { createPortal } from "react-dom";
import { CalendarClock, CalendarDays, ChevronLeft, ChevronRight } from "lucide-react";
import {
  INPUT_SAMPLES,
  UiStandardsQinput,
  type InputSample,
} from "@/features/ui-standards/UiStandardsInputGallery";
import { radioAccent } from "@/features/ui-standards/UiStandardsRadioGallery";
import { DROPDOWN_STANDARD_COLOR, UiStandardsQdropdown } from "@/features/ui-standards/UiStandardsDropdownGallery";
import {
  TIME_PICKER_SAMPLE,
  UiStandardsQtime,
  displayTime,
  nowTime,
} from "@/features/ui-standards/UiStandardsTimeGallery";
import {
  SOFT_GLASS_DATETIME_DEFAULT_HOUR24,
  SOFT_GLASS_DATETIME_FORMAT_CHECKED_LABEL,
  SOFT_GLASS_DATETIME_FORMAT_UNCHECKED_LABEL,
  SOFT_GLASS_DATETIME_PLACEHOLDER,
  SOFT_GLASS_DATETIME_PLACEHOLDER_24,
} from "@/features/ui-standards/soft-glass-datetime-global-default";
import { SoftGlassRollCaret } from "@/features/ui-standards/soft-glass-roll-caret";
import {
  SOFT_GLASS_SCALE_MS,
  softGlassPrefersReducedMotion,
  useScalePresence,
} from "@/features/ui-standards/soft-glass-scale-presence";
import { softGlassRollTouchHandlers } from "@/features/ui-standards/soft-glass-roll-touch";
import { Switch } from "@/components/ui/switch";

/** Preferences → Primary color (same as playgroundIntentColor for primary). */
const SOFT_GLASS_ROLL_PRIMARY = "var(--exits-primary)";
const SOFT_GLASS_ROLL_INK = "#ffffff";

/**
 * Quasar-style date picker samples for the locked Date picker standard.
 * Showcase only — not a product date picker.
 * @see https://quasar.dev/vue-components/date
 */
export const DATE_PICKER_SAMPLE = "2026-10-14";

export function todayIsoDate(now = new Date()): string {
  return toIsoDate(now);
}

export type DateRangeValue = { from: string; to: string };

export const DATE_RANGE_SAMPLE: DateRangeValue = { from: "2026-10-14", to: "2026-10-20" };

export type DatePickerValue = string | DateRangeValue;

export function isDateRangeValue(value: DatePickerValue): value is DateRangeValue {
  return typeof value !== "string";
}

const WEEKDAYS = ["S", "M", "T", "W", "T", "F", "S"] as const;
const WEEKDAY_NAMES = ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"] as const;
const MONTHS = [
  "January",
  "February",
  "March",
  "April",
  "May",
  "June",
  "July",
  "August",
  "September",
  "October",
  "November",
  "December",
] as const;
const MONTHS_SHORT = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"] as const;
const DEFAULT_DATE_INTENTS = ["Standard", "Primary", "Secondary"] as const;
const ROLL_YEAR_MIN = 1900;
const ROLL_YEAR_MAX = 2100;

export type DateAppearance = "standard" | "raised" | "gradient" | "flat" | "soft-glass";
export type DateVariety = "calendar" | "roll";

export function parseIsoDate(iso: string): Date {
  const [year, month, day] = iso.split("-").map(Number);
  return new Date(year || 1970, (month || 1) - 1, day || 1);
}

export function toIsoDate(date: Date): string {
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${date.getFullYear()}-${month}-${day}`;
}

/** Soft Glass Roll trigger: pick-only display + placeholder (lock: soft-glass-datetime-global-default). */
export const SOFT_GLASS_DATE_INPUT_PLACEHOLDER = "mm/dd/yyyy" as const;
export const SOFT_GLASS_DATETIME_INPUT_PLACEHOLDER = SOFT_GLASS_DATETIME_PLACEHOLDER;
export const SOFT_GLASS_DATETIME_INPUT_PLACEHOLDER_24 = SOFT_GLASS_DATETIME_PLACEHOLDER_24;

export function softGlassDateTimeInputPlaceholder(hour24: boolean): string {
  return hour24 ? SOFT_GLASS_DATETIME_INPUT_PLACEHOLDER_24 : SOFT_GLASS_DATETIME_INPUT_PLACEHOLDER;
}

export function isoDateToDisplay(iso: string): string {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(iso)) return "";
  const [year, month, day] = iso.split("-");
  return `${month}/${day}/${year}`;
}

function softGlassDateTimeDisplay(dateIso: string, time: string, hour24: boolean): string {
  return [dateIso ? isoDateToDisplay(dateIso) : "", time ? displayTime(time, hour24) : ""].filter(Boolean).join(" ");
}

/** Soft Glass date header line — full month, e.g. "Wed, October 14". */
export function softGlassComboDateHeading(dateIso: string): string {
  if (!dateIso) return "";
  const date = parseIsoDate(dateIso);
  return `${WEEKDAY_NAMES[date.getDay()]}, ${MONTHS[date.getMonth()]} ${date.getDate()}`;
}

/** Soft Glass date+time combo — date heading plus optional time. */
export function softGlassComboHeaderLabel(dateIso: string, time: string, hour24: boolean): string {
  const datePart = softGlassComboDateHeading(dateIso);
  const timePart = time ? displayTime(time, hour24) : "";
  return [datePart, timePart].filter(Boolean).join(" · ");
}

function shortDate(iso: string): string {
  const date = parseIsoDate(iso);
  return `${MONTHS_SHORT[date.getMonth()]} ${date.getDate()}`;
}

function headerDate(iso: string): string {
  const date = parseIsoDate(iso);
  return `${WEEKDAY_NAMES[date.getDay()]}, ${MONTHS[date.getMonth()]} ${date.getDate()}`;
}

function monthCells(view: Date): Date[] {
  const first = new Date(view.getFullYear(), view.getMonth(), 1);
  const start = new Date(view.getFullYear(), view.getMonth(), 1 - first.getDay());
  return Array.from({ length: 42 }, (_, index) => new Date(start.getFullYear(), start.getMonth(), start.getDate() + index));
}

function daysInMonth(year: number, monthIndex: number): number {
  return new Date(year, monthIndex + 1, 0).getDate();
}

function rollIso(year: number, monthIndex: number, day: number, wrapDay: boolean): string {
  const nextYear = Math.min(ROLL_YEAR_MAX, Math.max(ROLL_YEAR_MIN, year));
  const nextMonth = ((monthIndex % 12) + 12) % 12;
  const maxDay = daysInMonth(nextYear, nextMonth);
  const nextDay = wrapDay ? ((((day - 1) % maxDay) + maxDay) % maxDay) + 1 : Math.min(maxDay, Math.max(1, day));
  return toIsoDate(new Date(nextYear, nextMonth, nextDay));
}

function DateRoll({
  label,
  iso,
  disabled,
  testId,
  onChange,
  onPicked,
  onQuickOpenChange,
}: {
  label?: string;
  iso: string;
  disabled: boolean;
  testId?: string;
  onChange: (iso: string) => void;
  onPicked?: () => void;
  /** Soft Glass date+time combo: true while month/day/year quick grid is open. */
  onQuickOpenChange?: (open: boolean) => void;
}) {
  const date = parseIsoDate(iso);
  const year = date.getFullYear();
  const month = date.getMonth();
  const day = date.getDate();
  const maxDay = daysInMonth(year, month);
  const [quick, setQuick] = useState<null | "month" | "day" | "year">(null);
  const [quickOpen, setQuickOpen] = useState(false);
  const [browseYear, setBrowseYear] = useState(year);
  const [browseMonth, setBrowseMonth] = useState(month);

  useEffect(() => {
    onQuickOpenChange?.(quick != null);
    return () => onQuickOpenChange?.(false);
    // eslint-disable-next-line react-hooks/exhaustive-deps -- notify parent when quick panel toggles
  }, [quick]);

  useEffect(() => {
    if (quickOpen || quick == null) return;
    if (softGlassPrefersReducedMotion()) {
      setQuick(null);
      return;
    }
    const timeout = window.setTimeout(() => setQuick(null), SOFT_GLASS_SCALE_MS);
    return () => window.clearTimeout(timeout);
  }, [quickOpen, quick]);

  const columns = [
    {
      id: "month" as const,
      label: "Month",
      previous: MONTHS_SHORT[(month + 11) % 12],
      current: MONTHS[month],
      next: MONTHS_SHORT[(month + 1) % 12],
      onPrevious: () => onChange(rollIso(year, month - 1, day, false)),
      onNext: () => onChange(rollIso(year, month + 1, day, false)),
    },
    {
      id: "day" as const,
      label: "Date",
      previous: String(day === 1 ? maxDay : day - 1),
      current: String(day),
      next: String(day === maxDay ? 1 : day + 1),
      onPrevious: () => onChange(rollIso(year, month, day - 1, true)),
      onNext: () => onChange(rollIso(year, month, day + 1, true)),
    },
    {
      id: "year" as const,
      label: "Year",
      previous: year > ROLL_YEAR_MIN ? String(year - 1) : "",
      current: String(year),
      next: year < ROLL_YEAR_MAX ? String(year + 1) : "",
      onPrevious: () => onChange(rollIso(year - 1, month, day, false)),
      onNext: () => onChange(rollIso(year + 1, month, day, false)),
    },
  ];
  const yearPageStart = browseYear - (browseYear % 20);
  const dayGrid = monthCells(new Date(browseYear, browseMonth, 1));

  function openQuick(next: "month" | "day" | "year") {
    setBrowseYear(year);
    setBrowseMonth(month);
    setQuick(next);
    setQuickOpen(true);
  }

  function chooseQuick(nextIso: string) {
    onChange(nextIso);
    setQuickOpen(false);
    onPicked?.();
  }

  const quickState = quickOpen ? "open" : "closed";

  return (
    <div className="ui-standards-qdate__roll-group">
      {label ? <div className="ui-standards-qdate__roll-label">{label}</div> : null}
      {quick === "month" ? (
        <div
          className="ui-standards-qdate__quick-panel"
          data-transition="scale"
          data-state={quickState}
          data-testid={testId ? `${testId}-quick-month` : undefined}
        >
          <div className="ui-standards-qdate__choices" role="listbox" aria-label="Month">
            {MONTHS_SHORT.map((name, index) => (
              <button
                key={name}
                type="button"
                className={"ui-standards-qdate__choice" + (index === month ? " ui-standards-qdate__choice--selected" : "")}
                aria-pressed={index === month}
                disabled={disabled}
                data-testid={testId ? `${testId}-pick-month-${index + 1}` : undefined}
                onClick={() => chooseQuick(rollIso(year, index, day, false))}
              >
                {name}
              </button>
            ))}
          </div>
        </div>
      ) : null}
      {quick === "year" ? (
        <div
          className="ui-standards-qdate__quick-panel"
          data-transition="scale"
          data-state={quickState}
          data-testid={testId ? `${testId}-quick-year` : undefined}
        >
          <div className="ui-standards-qdate__nav">
            <button
              type="button"
              className="ui-standards-qdate__nav-btn"
              aria-label="Previous years"
              disabled={disabled}
              onClick={() => setBrowseYear((current) => current - 20)}
            >
              <ChevronLeft aria-hidden />
            </button>
            <span className="ui-standards-qdate__month" data-testid={testId ? `${testId}-pick-year-range` : undefined}>
              {yearPageStart} – {yearPageStart + 19}
            </span>
            <button
              type="button"
              className="ui-standards-qdate__nav-btn"
              aria-label="Next years"
              disabled={disabled}
              onClick={() => setBrowseYear((current) => current + 20)}
            >
              <ChevronRight aria-hidden />
            </button>
          </div>
          <div className="ui-standards-qdate__choices" role="listbox" aria-label="Year">
            {Array.from({ length: 20 }, (_, index) => yearPageStart + index).map((pageYear) => (
              <button
                key={pageYear}
                type="button"
                className={"ui-standards-qdate__choice" + (pageYear === year ? " ui-standards-qdate__choice--selected" : "")}
                aria-pressed={pageYear === year}
                disabled={disabled || pageYear < ROLL_YEAR_MIN || pageYear > ROLL_YEAR_MAX}
                data-testid={testId ? `${testId}-pick-year-${pageYear}` : undefined}
                onClick={() => chooseQuick(rollIso(pageYear, month, day, false))}
              >
                {pageYear}
              </button>
            ))}
          </div>
        </div>
      ) : null}
      {quick === "day" ? (
        <div
          className="ui-standards-qdate__quick-panel"
          data-transition="scale"
          data-state={quickState}
          data-testid={testId ? `${testId}-quick-day` : undefined}
        >
          <div className="ui-standards-qdate__nav">
            <button
              type="button"
              className="ui-standards-qdate__nav-btn"
              aria-label="Previous month"
              disabled={disabled}
              onClick={() => {
                const next = new Date(browseYear, browseMonth - 1, 1);
                setBrowseYear(next.getFullYear());
                setBrowseMonth(next.getMonth());
              }}
            >
              <ChevronLeft aria-hidden />
            </button>
            <span className="ui-standards-qdate__month">
              {MONTHS[browseMonth]} {browseYear}
            </span>
            <button
              type="button"
              className="ui-standards-qdate__nav-btn"
              aria-label="Next month"
              disabled={disabled}
              onClick={() => {
                const next = new Date(browseYear, browseMonth + 1, 1);
                setBrowseYear(next.getFullYear());
                setBrowseMonth(next.getMonth());
              }}
            >
              <ChevronRight aria-hidden />
            </button>
          </div>
          <div className="ui-standards-qdate__weekdays" aria-hidden>
            {WEEKDAYS.map((name, index) => (
              <span key={`${name}-${index}`}>{name}</span>
            ))}
          </div>
          <div className="ui-standards-qdate__grid" role="grid">
            {dayGrid.map((cell) => {
              const cellIso = toIsoDate(cell);
              const outside = cell.getMonth() !== browseMonth;
              const selected = cellIso === iso;
              return (
                <button
                  key={cellIso}
                  type="button"
                  role="gridcell"
                  className={
                    "ui-standards-qdate__day" +
                    (outside ? " ui-standards-qdate__day--outside" : "") +
                    (selected ? " ui-standards-qdate__day--selected" : "")
                  }
                  aria-pressed={selected}
                  disabled={disabled}
                  data-testid={testId && !outside ? `${testId}-pick-day-${cell.getDate()}` : undefined}
                  onClick={() => chooseQuick(cellIso)}
                >
                  {cell.getDate()}
                </button>
              );
            })}
          </div>
        </div>
      ) : null}
      {quick ? null : (
        <div className="ui-standards-qdate__roll">
          {columns.map((column) => (
            <DateRollColumn
              key={column.id}
              caption={column.label}
              previous={column.previous}
              current={column.current}
              next={column.next}
              disabled={disabled}
              testId={testId ? `${testId}-roll-${column.id}` : undefined}
              onPrevious={column.onPrevious}
              onNext={column.onNext}
              onChoose={() => openQuick(column.id)}
            />
          ))}
        </div>
      )}
    </div>
  );
}

function DateRollColumn({
  caption,
  previous,
  current,
  next,
  disabled,
  testId,
  onPrevious,
  onNext,
  onChoose,
}: {
  caption: string;
  previous: string;
  current: string;
  next: string;
  disabled: boolean;
  testId?: string;
  onPrevious: () => void;
  onNext: () => void;
  onChoose: () => void;
}) {
  const startYRef = useRef<number | null>(null);
  const touch = softGlassRollTouchHandlers(onPrevious, onNext, disabled, startYRef);

  function onWheel(event: WheelEvent<HTMLDivElement>) {
    if (disabled) return;
    event.preventDefault();
    if (event.deltaY > 0) onNext();
    else if (event.deltaY < 0) onPrevious();
  }

  return (
    <div className="ui-standards-qdate__roll-col" onWheel={onWheel} {...touch}>
      <span className="ui-standards-qdate__roll-caption">{caption}</span>
      <button
        type="button"
        className="ui-standards-qdate__roll-btn"
        aria-label={`Previous ${caption}`}
        disabled={disabled}
        data-testid={testId ? `${testId}-prev` : undefined}
        onClick={onPrevious}
      >
        <SoftGlassRollCaret direction="up" />
      </button>
      <div className="ui-standards-qdate__roll-window">
        <span className="ui-standards-qdate__roll-item">{previous}</span>
        <button
          type="button"
          className="ui-standards-qdate__roll-item ui-standards-qdate__roll-item--current"
          aria-label={`Choose ${caption}`}
          disabled={disabled}
          data-testid={testId}
          onClick={onChoose}
        >
          {current}
        </button>
        <span className="ui-standards-qdate__roll-item">{next}</span>
      </div>
      <button
        type="button"
        className="ui-standards-qdate__roll-btn"
        aria-label={`Next ${caption}`}
        disabled={disabled}
        data-testid={testId ? `${testId}-next` : undefined}
        onClick={onNext}
      >
        <SoftGlassRollCaret direction="down" />
      </button>
    </div>
  );
}

export function UiStandardsQdate({
  color,
  ink = "#ffffff",
  value,
  range = false,
  today = DATE_PICKER_SAMPLE,
  showToday = false,
  todayPlacement = "footer",
  showClear = false,
  disabled = false,
  appearance = "standard",
  variety = "calendar",
  onDark = false,
  onChange,
  onPicked,
  onToday,
  onClear,
  showSet = false,
  onSet,
  showValue = true,
  /** Body-only rolls for Soft Glass date+time combined card (shared header / footer). */
  embedded = false,
  /** Soft Glass date+time combo: fires when month/day/year quick grid opens or closes. */
  onQuickOpenChange,
  testId,
}: {
  color: string;
  ink?: string;
  value: DatePickerValue;
  range?: boolean;
  today?: string;
  showToday?: boolean;
  /** Footer is the sample row. Header sits top-right, vertically centered, on the Soft Glass Roll popup. */
  todayPlacement?: "footer" | "header";
  /** Soft Glass calendar UI sample: Clear action in the footer row with Today. */
  showClear?: boolean;
  disabled?: boolean;
  appearance?: DateAppearance;
  variety?: DateVariety;
  onDark?: boolean;
  onChange?: (value: DatePickerValue) => void;
  onPicked?: () => void;
  onToday?: () => void;
  onClear?: () => void;
  /** Soft Glass Roll date-time: Set under the roll carets. */
  showSet?: boolean;
  onSet?: () => void;
  showValue?: boolean;
  embedded?: boolean;
  onQuickOpenChange?: (open: boolean) => void;
  testId?: string;
}) {
  const rangeValue = isDateRangeValue(value) ? value : { from: value, to: value };
  const single = isDateRangeValue(value) ? value.from : value;
  const focusIso = range ? rangeValue.from : single;
  const focus = parseIsoDate(focusIso);
  const [view, setView] = useState(() => new Date(focus.getFullYear(), focus.getMonth(), 1));
  const [panel, setPanel] = useState<"calendar" | "months" | "years">("calendar");
  const [anchor, setAnchor] = useState<string | null>(null);
  const style = { "--qdate-color": color, "--qdate-ink": ink } as CSSProperties;
  const cells = monthCells(view);
  const yearPageStart = view.getFullYear() - (view.getFullYear() % 20);
  const headerLabel = range
    ? rangeValue.from === rangeValue.to
      ? headerDate(rangeValue.from)
      : `${shortDate(rangeValue.from)} – ${shortDate(rangeValue.to)}`
    : headerDate(single);
  const output = range ? `${rangeValue.from} – ${rangeValue.to}` : single;
  const todayButton = showToday ? (
    <button
      type="button"
      className={
        "ui-standards-qdate__today" + (todayPlacement === "header" ? " ui-standards-qdate__today--header" : "")
      }
      disabled={disabled}
      data-testid={testId ? `${testId}-today` : undefined}
      onClick={() => {
        choose(parseIsoDate(today), true);
        onToday?.();
      }}
    >
      Today
    </button>
  ) : null;
  const clearButton = showClear ? (
    <button
      type="button"
      className="ui-standards-qdate__clear"
      disabled={disabled}
      data-testid={testId ? `${testId}-clear` : undefined}
      onClick={() => {
        if (disabled) return;
        setAnchor(null);
        onClear?.();
      }}
    >
      Clear
    </button>
  ) : null;
  const footerActions =
    !embedded && (showClear || (showToday && todayPlacement === "footer")) ? (
      <div
        className={
          "ui-standards-qdate__actions" +
          (showClear && showToday && todayPlacement === "footer" ? " ui-standards-qdate__actions--split" : "")
        }
      >
        {clearButton}
        {todayPlacement === "footer" ? todayButton : null}
      </div>
    ) : null;

  useEffect(() => {
    const next = parseIsoDate(range ? rangeValue.from : single);
    setView(new Date(next.getFullYear(), next.getMonth(), 1));
    setPanel("calendar");
    setAnchor(null);
  }, [range]);

  function shiftView(direction: -1 | 1) {
    if (panel === "years") {
      setView(new Date(view.getFullYear() + direction * 20, view.getMonth(), 1));
      return;
    }
    if (panel === "months") {
      setView(new Date(view.getFullYear() + direction, view.getMonth(), 1));
      return;
    }
    setView(new Date(view.getFullYear(), view.getMonth() + direction, 1));
  }

  function choose(day: Date, asSingle = false) {
    if (disabled) return;
    const iso = toIsoDate(day);
    setView(new Date(day.getFullYear(), day.getMonth(), 1));
    if (!range || asSingle) {
      setAnchor(null);
      onChange?.(range ? { from: iso, to: iso } : iso);
      onPicked?.();
      return;
    }
    if (!anchor) {
      setAnchor(iso);
      onChange?.({ from: iso, to: iso });
      return;
    }
    const from = anchor < iso ? anchor : iso;
    const to = anchor < iso ? iso : anchor;
    setAnchor(null);
    onChange?.({ from, to });
    onPicked?.();
  }

  return (
    <div
      className={
        "ui-standards-qdate" +
        (disabled ? " ui-standards-qdate--disabled" : "") +
        ((showToday || showClear) && !embedded ? " ui-standards-qdate--today" : "") +
        (appearance !== "standard" ? ` ui-standards-qdate--${appearance}` : "") +
        (variety === "roll" ? " ui-standards-qdate--roll" : "") +
        (onDark ? " ui-standards-qdate--on-dark" : "") +
        (embedded ? " ui-standards-qdate--embedded" : "")
      }
      style={style}
      data-testid={testId}
    >
      {embedded ? null : (
        <div className="ui-standards-qdate__header">
          <button
            type="button"
            className="ui-standards-qdate__year"
            disabled={disabled}
            data-testid={testId ? `${testId}-header-year` : undefined}
            onClick={() => {
              if (variety === "calendar") setPanel("years");
            }}
          >
            {focus.getFullYear()}
          </button>
          <span className="ui-standards-qdate__selected">{headerLabel}</span>
          {todayPlacement === "header" ? todayButton : null}
        </div>
      )}
      <div className="ui-standards-qdate__body">
        {variety === "roll" ? (
          range ? (
            <div className="ui-standards-qdate__roll-stack">
              <DateRoll
                label="From"
                iso={rangeValue.from}
                disabled={disabled}
                testId={testId ? `${testId}-from` : undefined}
                onChange={(iso) => onChange?.({ from: iso, to: iso > rangeValue.to ? iso : rangeValue.to })}
                onPicked={onPicked}
                onQuickOpenChange={onQuickOpenChange}
              />
              <DateRoll
                label="To"
                iso={rangeValue.to}
                disabled={disabled}
                testId={testId ? `${testId}-to` : undefined}
                onChange={(iso) => onChange?.({ from: iso < rangeValue.from ? iso : rangeValue.from, to: iso })}
                onPicked={onPicked}
                onQuickOpenChange={onQuickOpenChange}
              />
            </div>
          ) : (
            <DateRoll
              iso={single}
              disabled={disabled}
              testId={testId}
              onChange={(iso) => {
                setAnchor(null);
                onChange?.(iso);
              }}
              onPicked={onPicked}
              onQuickOpenChange={onQuickOpenChange}
            />
          )
        ) : (
          <>
        <div className="ui-standards-qdate__nav">
          <button
            type="button"
            className="ui-standards-qdate__nav-btn"
            aria-label={panel === "calendar" ? "Previous month" : panel === "months" ? "Previous year" : "Previous years"}
            disabled={disabled}
            data-testid={testId ? `${testId}-prev` : undefined}
            onClick={() => shiftView(-1)}
          >
            <ChevronLeft aria-hidden />
          </button>
          {panel === "calendar" ? (
            <div className="ui-standards-qdate__titles">
              <button
                type="button"
                className="ui-standards-qdate__jump"
                disabled={disabled}
                data-testid={testId ? `${testId}-month` : undefined}
                onClick={() => setPanel("months")}
              >
                {MONTHS[view.getMonth()]}
              </button>
              <button
                type="button"
                className="ui-standards-qdate__jump"
                disabled={disabled}
                data-testid={testId ? `${testId}-year` : undefined}
                onClick={() => setPanel("years")}
              >
                {view.getFullYear()}
              </button>
            </div>
          ) : panel === "months" ? (
            <button
              type="button"
              className="ui-standards-qdate__jump"
              disabled={disabled}
              data-testid={testId ? `${testId}-year` : undefined}
              onClick={() => setPanel("years")}
            >
              {view.getFullYear()}
            </button>
          ) : (
            <span className="ui-standards-qdate__month" data-testid={testId ? `${testId}-year-range` : undefined}>
              {yearPageStart} – {yearPageStart + 19}
            </span>
          )}
          <button
            type="button"
            className="ui-standards-qdate__nav-btn"
            aria-label={panel === "calendar" ? "Next month" : panel === "months" ? "Next year" : "Next years"}
            disabled={disabled}
            data-testid={testId ? `${testId}-next` : undefined}
            onClick={() => shiftView(1)}
          >
            <ChevronRight aria-hidden />
          </button>
        </div>
        {panel === "months" ? (
          <div className="ui-standards-qdate__choices" role="listbox" aria-label="Month">
            {MONTHS_SHORT.map((label, index) => (
              <button
                key={label}
                type="button"
                className={"ui-standards-qdate__choice" + (index === view.getMonth() ? " ui-standards-qdate__choice--selected" : "")}
                aria-pressed={index === view.getMonth()}
                disabled={disabled}
                data-testid={testId ? `${testId}-month-${index + 1}` : undefined}
                onClick={() => {
                  setView(new Date(view.getFullYear(), index, 1));
                  setPanel("calendar");
                }}
              >
                {label}
              </button>
            ))}
          </div>
        ) : null}
        {panel === "years" ? (
          <div className="ui-standards-qdate__choices" role="listbox" aria-label="Year">
            {Array.from({ length: 20 }, (_, index) => yearPageStart + index).map((year) => (
              <button
                key={year}
                type="button"
                className={"ui-standards-qdate__choice" + (year === view.getFullYear() ? " ui-standards-qdate__choice--selected" : "")}
                aria-pressed={year === view.getFullYear()}
                disabled={disabled}
                data-testid={testId ? `${testId}-year-${year}` : undefined}
                onClick={() => {
                  setView(new Date(year, view.getMonth(), 1));
                  setPanel("months");
                }}
              >
                {year}
              </button>
            ))}
          </div>
        ) : null}
        {panel === "calendar" ? (
          <>
            <div className="ui-standards-qdate__weekdays" aria-hidden>
              {WEEKDAYS.map((label, index) => (
                <span key={`${label}-${index}`}>{label}</span>
              ))}
            </div>
            <div className="ui-standards-qdate__grid" role="grid">
              {cells.map((day) => {
                const iso = toIsoDate(day);
                const outside = day.getMonth() !== view.getMonth();
                const isEndpoint = range && (iso === rangeValue.from || iso === rangeValue.to);
                const isInside = range && iso > rangeValue.from && iso < rangeValue.to;
                const isSelected = range ? isEndpoint : iso === single;
                const isToday = iso === today;
                return (
                  <button
                    key={iso}
                    type="button"
                    role="gridcell"
                    className={
                      "ui-standards-qdate__day" +
                      (outside ? " ui-standards-qdate__day--outside" : "") +
                      (isInside ? " ui-standards-qdate__day--in-range" : "") +
                      (isSelected ? " ui-standards-qdate__day--selected" : "") +
                      (isToday ? " ui-standards-qdate__day--today" : "")
                    }
                    aria-pressed={isSelected}
                    aria-current={isToday ? "date" : undefined}
                    disabled={disabled}
                    data-date={iso}
                    data-testid={testId && !outside ? `${testId}-day-${day.getDate()}` : undefined}
                    onClick={() => choose(day)}
                  >
                    {day.getDate()}
                  </button>
                );
              })}
            </div>
          </>
        ) : null}
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
        {footerActions}
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

function DateColor({
  sample,
  testId,
  range = false,
  showToday = false,
  disabled = false,
  appearance = "standard",
  variety = "calendar",
  onDark = false,
}: {
  sample: InputSample;
  testId: string;
  range?: boolean;
  showToday?: boolean;
  disabled?: boolean;
  appearance?: DateAppearance;
  variety?: DateVariety;
  onDark?: boolean;
}) {
  const [value, setValue] = useState<DatePickerValue>(range ? DATE_RANGE_SAMPLE : DATE_PICKER_SAMPLE);
  return (
    <UiStandardsQdate
      color={radioAccent(sample)}
      ink={sample.fg}
      value={value}
      range={range}
      showToday={showToday}
      disabled={disabled}
      appearance={appearance}
      variety={variety}
      onDark={onDark}
      onChange={setValue}
      testId={`${testId}-${sample.id}`}
    />
  );
}

function DateRow({
  title,
  testId,
  onDark = false,
  ...rest
}: {
  title: string;
  testId: string;
  onDark?: boolean;
  range?: boolean;
  showToday?: boolean;
  disabled?: boolean;
  appearance?: DateAppearance;
  variety?: DateVariety;
}) {
  const [intents, setIntents] = useState<string[]>([...DEFAULT_DATE_INTENTS]);
  const samples = INPUT_SAMPLES.filter((sample) => intents.includes(sample.label));
  const row = (
    <div className="ui-standards-q-example__row">
      {samples.map((sample) => (
        <DateColor key={sample.id} sample={sample} testId={testId} onDark={onDark} {...rest} />
      ))}
    </div>
  );
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
      {onDark ? <div className="ui-standards-qdate-dark">{row}</div> : row}
    </div>
  );
}

/** Soft Glass Roll · Primary, global default. Lock: soft-glass-datetime-global-default.ts */
function SoftGlassRollCard(): ReactNode {
  const [dateValue, setDateValue] = useState("");
  const [dateTimeDate, setDateTimeDate] = useState("");
  const [dateTimeTime, setDateTimeTime] = useState("");
  const [inlineDate, setInlineDate] = useState(DATE_PICKER_SAMPLE);
  const [inlineDateTimeDate, setInlineDateTimeDate] = useState(DATE_PICKER_SAMPLE);
  const [inlineDateTimeTime, setInlineDateTimeTime] = useState(TIME_PICKER_SAMPLE);
  const [dateOpen, setDateOpen] = useState(false);
  const [dateTimeOpen, setDateTimeOpen] = useState(false);
  const dateMounted = useScalePresence(dateOpen);
  const dateTimeMounted = useScalePresence(dateTimeOpen);
  const [dateTimeHour24, setDateTimeHour24] = useState<boolean>(SOFT_GLASS_DATETIME_DEFAULT_HOUR24);
  const [dateTimeQuickOpen, setDateTimeQuickOpen] = useState(false);
  const dateText = dateValue ? isoDateToDisplay(dateValue) : "";
  const dateTimeText = softGlassDateTimeDisplay(dateTimeDate, dateTimeTime, dateTimeHour24);
  const liveDateTimeDate = dateTimeDate || todayIsoDate();
  const liveDateTimeTime = dateTimeTime || nowTime();
  const dateTimeHeaderYear = liveDateTimeDate.slice(0, 4);
  const dateTimeHeaderDate = softGlassComboDateHeading(liveDateTimeDate);
  const comboStyle = { "--qdate-color": SOFT_GLASS_ROLL_PRIMARY, "--qdate-ink": SOFT_GLASS_ROLL_INK } as CSSProperties;

  function openDatePop() {
    setDateTimeOpen(false);
    setDateOpen(true);
  }

  function openDateTimePop() {
    setDateOpen(false);
    setDateTimeQuickOpen(false);
    setDateTimeOpen(true);
  }

  function closeDatePop() {
    setDateOpen(false);
  }

  function closeDateTimePop() {
    setDateTimeQuickOpen(false);
    setDateTimeOpen(false);
  }

  function onComboNow() {
    setDateTimeDate(todayIsoDate());
    setDateTimeTime(nowTime());
    setDateTimeOpen(false);
  }

  function onComboSet() {
    setDateTimeDate(dateTimeDate || todayIsoDate());
    setDateTimeTime(dateTimeTime || nowTime());
    setDateTimeOpen(false);
  }

  return (
    <div
      className="ui-standards-soft-glass-roll-card"
      data-shell="soft-glass"
      data-testid="ui-standard-date-soft-glass-roll-card"
    >
      <div className="ui-standards-soft-glass-roll-card__title">Soft Glass Roll · Primary, global default</div>
      <div className="ui-standards-soft-glass-roll-card__inputs">
        <div
          className="ui-standards-soft-glass-roll-card__field"
          data-open={dateOpen ? "true" : undefined}
          data-testid="ui-standard-date-soft-glass-roll-date-field"
        >
          <div className="exits-sg-datetime">
            <div className="exits-sg-datetime__trigger" onClick={openDatePop}>
              <UiStandardsQinput
                label="Date"
                value={dateText}
                readOnly
                placeholder={SOFT_GLASS_DATE_INPUT_PLACEHOLDER}
                accent={SOFT_GLASS_ROLL_PRIMARY}
                fill={SOFT_GLASS_ROLL_PRIMARY}
                ink={SOFT_GLASS_ROLL_INK}
                appearance="outlined"
                clearable
                clearMark="reset"
                onClear={() => {
                  setDateValue("");
                  setDateOpen(false);
                }}
                suffix={<CalendarDays className="ui-standards-qinput__picker-icon" aria-hidden />}
                testId="ui-standard-date-soft-glass-roll-date-input"
              />
            </div>
          </div>
          {(() => {
            if (!dateMounted) return null;
            const datePop = (
              <div
                className="ui-standards-soft-glass-roll-card__pop exits-sg-datetime__panel--in-portal"
                data-exits-sg-datetime-panel=""
                data-transition="scale"
                data-state={dateOpen ? "open" : "closed"}
                data-testid="ui-standard-date-soft-glass-roll-date-pop"
              >
                <UiStandardsQdate
                  color={SOFT_GLASS_ROLL_PRIMARY}
                  ink={SOFT_GLASS_ROLL_INK}
                  value={dateValue || todayIsoDate()}
                  today={todayIsoDate()}
                  showToday
                  todayPlacement="header"
                  appearance="soft-glass"
                  variety="roll"
                  onChange={(next) => {
                    if (typeof next === "string") {
                      setDateValue(next);
                      setDateOpen(false);
                    }
                  }}
                  testId="ui-standard-date-soft-glass-roll"
                />
              </div>
            );
            if (typeof document === "undefined") return datePop;
            return createPortal(
              <div
                className="exits-sg-datetime__portal"
                data-state={dateOpen ? "open" : "closed"}
                data-testid="ui-standard-date-soft-glass-roll-date-portal"
              >
                <button
                  type="button"
                  className="exits-sg-datetime__scrim"
                  aria-label="Close"
                  data-testid="ui-standard-date-soft-glass-roll-date-scrim"
                  onClick={closeDatePop}
                />
                <div className="exits-sg-datetime__portal-stage" data-panels="date">
                  {datePop}
                </div>
              </div>,
              document.body,
            );
          })()}
        </div>
        <div
          className="ui-standards-soft-glass-roll-card__field ui-standards-soft-glass-roll-card__field--datetime"
          data-open={dateTimeOpen ? "true" : undefined}
          data-testid="ui-standard-date-soft-glass-roll-datetime-field"
        >
          <div className="exits-sg-datetime">
            <div className="exits-sg-datetime__trigger" onClick={openDateTimePop}>
              <UiStandardsQinput
                label="Date and time"
                value={dateTimeText}
                readOnly
                placeholder={softGlassDateTimeInputPlaceholder(dateTimeHour24)}
                accent={SOFT_GLASS_ROLL_PRIMARY}
                fill={SOFT_GLASS_ROLL_PRIMARY}
                ink={SOFT_GLASS_ROLL_INK}
                appearance="outlined"
                clearable
                clearMark="reset"
                onClear={() => {
                  setDateTimeDate("");
                  setDateTimeTime("");
                  setDateTimeOpen(false);
                }}
                suffix={<CalendarClock className="ui-standards-qinput__picker-icon" aria-hidden />}
                testId="ui-standard-date-soft-glass-roll-datetime-input"
              />
            </div>
          </div>
          {(() => {
            if (!dateTimeMounted) return null;
            const dateTimePop = (
              <div
                className="exits-sg-datetime__combo ui-standards-qdate ui-standards-qdate--soft-glass exits-sg-datetime__panel--in-portal"
                style={comboStyle}
                data-exits-sg-datetime-panel=""
                data-transition="scale"
                data-state={dateTimeOpen ? "open" : "closed"}
                data-testid="ui-standard-date-soft-glass-roll-datetime-pop"
              >
                <div className="ui-standards-qdate__header exits-sg-datetime__combo-header">
                  <button
                    type="button"
                    className="ui-standards-qdate__year"
                    tabIndex={-1}
                    data-testid="ui-standard-date-soft-glass-roll-dt-header-year"
                  >
                    {dateTimeHeaderYear}
                  </button>
                  <span className="ui-standards-qdate__selected">{dateTimeHeaderDate}</span>
                  <div className="exits-sg-datetime__combo-actions">
                    <div className="ui-standards-qtime__format ui-standards-qtime__format--header">
                      <Switch
                        checked={dateTimeHour24}
                        onCheckedChange={setDateTimeHour24}
                        aria-label="Hour format"
                        checkedLabel={SOFT_GLASS_DATETIME_FORMAT_CHECKED_LABEL}
                        uncheckedLabel={SOFT_GLASS_DATETIME_FORMAT_UNCHECKED_LABEL}
                        data-testid="ui-standard-date-soft-glass-roll-dt-time-24h"
                      />
                    </div>
                  </div>
                </div>
                <div
                  className="exits-sg-datetime__combo-body"
                  data-date-quick={dateTimeQuickOpen ? "true" : undefined}
                >
                  <UiStandardsQdate
                    color={SOFT_GLASS_ROLL_PRIMARY}
                    ink={SOFT_GLASS_ROLL_INK}
                    value={liveDateTimeDate}
                    appearance="soft-glass"
                    variety="roll"
                    embedded
                    showValue={false}
                    onQuickOpenChange={setDateTimeQuickOpen}
                    onChange={(next) => {
                      if (typeof next === "string") setDateTimeDate(next);
                    }}
                    testId="ui-standard-date-soft-glass-roll-dt-date"
                  />
                  {dateTimeQuickOpen ? null : (
                    <UiStandardsQtime
                      color={SOFT_GLASS_ROLL_PRIMARY}
                      ink={SOFT_GLASS_ROLL_INK}
                      value={liveDateTimeTime}
                      hour24={dateTimeHour24}
                      onHour24Change={setDateTimeHour24}
                      appearance="soft-glass"
                      variety="roll"
                      embedded
                      showValue={false}
                      onChange={(next) => {
                        if (typeof next === "string") setDateTimeTime(next);
                      }}
                      testId="ui-standard-date-soft-glass-roll-dt-time"
                    />
                  )}
                </div>
                <div className="exits-sg-datetime__combo-footer">
                  <button
                    type="button"
                    className="exits-sg-datetime__combo-cancel"
                    data-testid="ui-standard-date-soft-glass-roll-dt-cancel"
                    onClick={closeDateTimePop}
                  >
                    Cancel
                  </button>
                  <button
                    type="button"
                    className="exits-sg-datetime__combo-now"
                    data-testid="ui-standard-date-soft-glass-roll-dt-now"
                    onClick={onComboNow}
                  >
                    Now
                  </button>
                  <button
                    type="button"
                    className="ui-standards-qdate__set exits-sg-datetime__combo-set"
                    data-testid="ui-standard-date-soft-glass-roll-dt-set"
                    onClick={onComboSet}
                  >
                    Set
                  </button>
                </div>
              </div>
            );
            if (typeof document === "undefined") return dateTimePop;
            return createPortal(
              <div
                className="exits-sg-datetime__portal"
                data-state={dateTimeOpen ? "open" : "closed"}
                data-testid="ui-standard-date-soft-glass-roll-datetime-portal"
              >
                <button
                  type="button"
                  className="exits-sg-datetime__scrim"
                  aria-label="Close"
                  data-testid="ui-standard-date-soft-glass-roll-datetime-scrim"
                  onClick={closeDateTimePop}
                />
                <div className="exits-sg-datetime__portal-stage" data-panels="combo">
                  {dateTimePop}
                </div>
              </div>,
              document.body,
            );
          })()}
        </div>
      </div>
      <div className="ui-standards-soft-glass-roll-card__grid">
        <div className="ui-standards-soft-glass-roll-card__sample" data-testid="ui-standard-date-soft-glass-roll-calendar">
          <div className="ui-standards-soft-glass-roll-card__label">Roll calendar glass</div>
          <UiStandardsQdate
            color={SOFT_GLASS_ROLL_PRIMARY}
            ink={SOFT_GLASS_ROLL_INK}
            value={inlineDate}
            appearance="soft-glass"
            variety="roll"
            onChange={(next) => {
              if (typeof next === "string") setInlineDate(next);
            }}
            testId="ui-standard-date-soft-glass-roll-inline"
          />
        </div>
        <div
          className="ui-standards-soft-glass-roll-card__sample ui-standards-soft-glass-roll-card__sample--datetime"
          data-testid="ui-standard-date-soft-glass-roll-datetime"
        >
          <div className="ui-standards-soft-glass-roll-card__label">Roll calendar glass with roll calendar time</div>
          <div className="ui-standards-soft-glass-roll-card__datetime">
            <UiStandardsQdate
              color={SOFT_GLASS_ROLL_PRIMARY}
              ink={SOFT_GLASS_ROLL_INK}
              value={inlineDateTimeDate}
              appearance="soft-glass"
              variety="roll"
              showValue={false}
              onChange={(next) => {
                if (typeof next === "string") setInlineDateTimeDate(next);
              }}
              testId="ui-standard-date-soft-glass-roll-inline-dt-date"
            />
            <UiStandardsQtime
              color={SOFT_GLASS_ROLL_PRIMARY}
              ink={SOFT_GLASS_ROLL_INK}
              value={inlineDateTimeTime}
              appearance="soft-glass"
              variety="roll"
              showValue={false}
              onChange={(next) => {
                if (typeof next === "string") setInlineDateTimeTime(next);
              }}
              testId="ui-standard-date-soft-glass-roll-inline-dt-time"
            />
          </div>
          <div className="ui-standards-datetime-values ui-standards-soft-glass-roll-card__values">
            <div className="ui-standards-datetime-value" data-testid="ui-standard-date-soft-glass-roll-inline-dt-date-value">
              <span className="ui-standards-qdate__output-label">Value</span>
              <span className="ui-standards-qdate__output-text">{inlineDateTimeDate}</span>
            </div>
            <div className="ui-standards-datetime-value" data-testid="ui-standard-date-soft-glass-roll-inline-dt-time-value">
              <span className="ui-standards-qdate__output-label">Value</span>
              <span className="ui-standards-qdate__output-text">{inlineDateTimeTime}</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

/**
 * Soft Glass date / time UI samples — calendar + field chrome as glass
 * (desktop + mobile). Personal Soft Glass only; Organization Date glass unchanged.
 */
function SoftGlassDateUiGlassSample(): ReactNode {
  const sampleDay = "2026-10-25";
  const [value, setValue] = useState(sampleDay);
  const display = value ? isoDateToDisplay(value) : "";
  return (
    <div
      className="ui-standards-soft-glass-ui-sample"
      data-shell="soft-glass"
      data-testid="ui-standard-date-soft-glass-ui-sample"
    >
      <div className="ui-standards-soft-glass-ui-sample__title">Soft Glass · Date UI</div>
      <p className="ui-standards-soft-glass-ui-sample__blurb">
        Calendar picker field and popover in Soft Glass — Clear / Today footer. Mobile keeps the same glass shell.
      </p>
      <div className="ui-standards-soft-glass-ui-sample__stack">
        <div className="ui-standards-date-preview-card ui-standards-date-preview-card--soft-glass ui-standards-soft-glass-ui-sample__field">
          <UiStandardsQinput
            label="Date"
            value={display}
            readOnly
            placeholder={SOFT_GLASS_DATE_INPUT_PLACEHOLDER}
            accent={SOFT_GLASS_ROLL_PRIMARY}
            fill={SOFT_GLASS_ROLL_PRIMARY}
            ink={SOFT_GLASS_ROLL_INK}
            appearance="outlined"
            clearable={Boolean(value)}
            clearMark="reset"
            onClear={() => setValue("")}
            suffix={<CalendarDays className="ui-standards-qinput__picker-icon" aria-hidden />}
            testId="ui-standard-date-soft-glass-ui-input"
          />
        </div>
        <UiStandardsQdate
          color={SOFT_GLASS_ROLL_PRIMARY}
          ink={SOFT_GLASS_ROLL_INK}
          value={value || sampleDay}
          today={todayIsoDate()}
          showToday
          showClear
          todayPlacement="footer"
          appearance="soft-glass"
          variety="calendar"
          showValue={false}
          onChange={(next) => {
            if (typeof next === "string") setValue(next);
          }}
          onClear={() => setValue("")}
          testId="ui-standard-date-soft-glass-ui-calendar"
        />
      </div>
    </div>
  );
}

/** Soft Glass Roll samples — sits below the Date playground (not in the Date samples gallery). */
export function UiStandardsSoftGlassRollSamples(): ReactNode {
  return (
    <div className="flex flex-col gap-4" data-testid="ui-standard-date-soft-glass-roll-samples">
      <SoftGlassDateUiGlassSample />
      <SoftGlassRollCard />
      <DateRow title="Soft Glass Roll" testId="ui-standard-date-soft-glass-roll-row" appearance="soft-glass" variety="roll" />
    </div>
  );
}

export function UiStandardsDateGallery(): ReactNode {
  return (
    <div className="flex flex-col gap-4" data-testid="ui-standard-date-gallery">
      <DateRow title="Standard" testId="ui-standard-date" />
      <DateRow title="Soft Glass" testId="ui-standard-date-soft-glass" appearance="soft-glass" />
      <DateRow title="Raised" testId="ui-standard-date-raised" appearance="raised" />
      <DateRow title="Gradient" testId="ui-standard-date-gradient" appearance="gradient" />
      <DateRow title="Flat" testId="ui-standard-date-flat" appearance="flat" />
      <DateRow title="Roll" testId="ui-standard-date-roll" variety="roll" />
      <DateRow title="Today" testId="ui-standard-date-today" showToday />
      <DateRow title="Range" testId="ui-standard-date-range" range />
      <DateRow title="Disabled" testId="ui-standard-date-disabled" disabled />
    </div>
  );
}

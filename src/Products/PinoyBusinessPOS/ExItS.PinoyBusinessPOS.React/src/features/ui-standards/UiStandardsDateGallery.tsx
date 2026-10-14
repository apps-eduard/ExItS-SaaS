import { useEffect, useState, type CSSProperties, type ReactNode } from "react";
import { ChevronDown, ChevronLeft, ChevronRight, ChevronUp } from "lucide-react";
import { INPUT_SAMPLES, type InputSample } from "@/features/ui-standards/UiStandardsInputGallery";
import { radioAccent } from "@/features/ui-standards/UiStandardsRadioGallery";
import { DROPDOWN_STANDARD_COLOR, UiStandardsQdropdown } from "@/features/ui-standards/UiStandardsDropdownGallery";

/**
 * Quasar-style date picker samples for the Form Components pilot.
 * Showcase only — not a product date picker.
 * @see https://quasar.dev/vue-components/date
 */
export const DATE_PICKER_SAMPLE = "2026-10-14";

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

export type DateAppearance = "standard" | "raised" | "gradient" | "flat";
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

function shortDate(iso: string): string {
  const date = parseIsoDate(iso);
  return `${MONTHS_SHORT[date.getMonth()]} ${date.getDate()}`;
}

function headerDate(iso: string): string {
  const date = parseIsoDate(iso);
  return `${WEEKDAY_NAMES[date.getDay()]}, ${MONTHS_SHORT[date.getMonth()]} ${date.getDate()}`;
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
}: {
  label?: string;
  iso: string;
  disabled: boolean;
  testId?: string;
  onChange: (iso: string) => void;
}) {
  const date = parseIsoDate(iso);
  const year = date.getFullYear();
  const month = date.getMonth();
  const day = date.getDate();
  const maxDay = daysInMonth(year, month);
  const columns = [
    {
      id: "month",
      label: "Month",
      previous: MONTHS_SHORT[(month + 11) % 12],
      current: MONTHS[month],
      next: MONTHS_SHORT[(month + 1) % 12],
      onPrevious: () => onChange(rollIso(year, month - 1, day, false)),
      onNext: () => onChange(rollIso(year, month + 1, day, false)),
    },
    {
      id: "day",
      label: "Date",
      previous: String(day === 1 ? maxDay : day - 1),
      current: String(day),
      next: String(day === maxDay ? 1 : day + 1),
      onPrevious: () => onChange(rollIso(year, month, day - 1, true)),
      onNext: () => onChange(rollIso(year, month, day + 1, true)),
    },
    {
      id: "year",
      label: "Year",
      previous: year > ROLL_YEAR_MIN ? String(year - 1) : "",
      current: String(year),
      next: year < ROLL_YEAR_MAX ? String(year + 1) : "",
      onPrevious: () => onChange(rollIso(year - 1, month, day, false)),
      onNext: () => onChange(rollIso(year + 1, month, day, false)),
    },
  ] as const;

  return (
    <div className="ui-standards-qdate__roll-group">
      {label ? <div className="ui-standards-qdate__roll-label">{label}</div> : null}
      <div className="ui-standards-qdate__roll">
        {columns.map((column) => (
          <div
            key={column.id}
            className="ui-standards-qdate__roll-col"
            onWheel={(event) => {
              if (disabled) return;
              event.preventDefault();
              if (event.deltaY > 0) column.onNext();
              else if (event.deltaY < 0) column.onPrevious();
            }}
          >
            <span className="ui-standards-qdate__roll-caption">{column.label}</span>
            <button
              type="button"
              className="ui-standards-qdate__roll-btn"
              aria-label={`Previous ${column.label}`}
              disabled={disabled}
              data-testid={testId ? `${testId}-roll-${column.id}-prev` : undefined}
              onClick={column.onPrevious}
            >
              <ChevronUp aria-hidden />
            </button>
            <div className="ui-standards-qdate__roll-window">
              <span className="ui-standards-qdate__roll-item">{column.previous}</span>
              <span
                className="ui-standards-qdate__roll-item ui-standards-qdate__roll-item--current"
                data-testid={testId ? `${testId}-roll-${column.id}` : undefined}
              >
                {column.current}
              </span>
              <span className="ui-standards-qdate__roll-item">{column.next}</span>
            </div>
            <button
              type="button"
              className="ui-standards-qdate__roll-btn"
              aria-label={`Next ${column.label}`}
              disabled={disabled}
              data-testid={testId ? `${testId}-roll-${column.id}-next` : undefined}
              onClick={column.onNext}
            >
              <ChevronDown aria-hidden />
            </button>
          </div>
        ))}
      </div>
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
  disabled = false,
  appearance = "standard",
  variety = "calendar",
  onDark = false,
  onChange,
  testId,
}: {
  color: string;
  ink?: string;
  value: DatePickerValue;
  range?: boolean;
  today?: string;
  showToday?: boolean;
  disabled?: boolean;
  appearance?: DateAppearance;
  variety?: DateVariety;
  onDark?: boolean;
  onChange?: (value: DatePickerValue) => void;
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
  }

  return (
    <div
      className={
        "ui-standards-qdate" +
        (disabled ? " ui-standards-qdate--disabled" : "") +
        (showToday ? " ui-standards-qdate--today" : "") +
        (appearance !== "standard" ? ` ui-standards-qdate--${appearance}` : "") +
        (variety === "roll" ? " ui-standards-qdate--roll" : "") +
        (onDark ? " ui-standards-qdate--on-dark" : "")
      }
      style={style}
      data-testid={testId}
    >
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
      </div>
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
              />
              <DateRoll
                label="To"
                iso={rangeValue.to}
                disabled={disabled}
                testId={testId ? `${testId}-to` : undefined}
                onChange={(iso) => onChange?.({ from: iso < rangeValue.from ? iso : rangeValue.from, to: iso })}
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
        {showToday ? (
          <button
            type="button"
            className="ui-standards-qdate__today"
            disabled={disabled}
            data-testid={testId ? `${testId}-today` : undefined}
            onClick={() => choose(parseIsoDate(today), true)}
          >
            Today
          </button>
        ) : null}
        <div className="ui-standards-qdate__output" data-testid={testId ? `${testId}-value` : undefined}>
          <span className="ui-standards-qdate__output-label">Value</span>
          <span className="ui-standards-qdate__output-text">{output}</span>
        </div>
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

export function UiStandardsDateGallery(): ReactNode {
  return (
    <div className="flex flex-col gap-4" data-testid="ui-standard-date-gallery">
      <DateRow title="Standard" testId="ui-standard-date" />
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

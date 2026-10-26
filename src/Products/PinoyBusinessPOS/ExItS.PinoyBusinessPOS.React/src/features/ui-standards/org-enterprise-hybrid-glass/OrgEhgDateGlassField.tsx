import { useEffect, useMemo, useRef, useState, type ReactNode, type WheelEvent } from "react";
import { createPortal } from "react-dom";
import {
  ArrowRight,
  CalendarClock,
  CalendarDays,
  ChevronDown,
  ChevronLeft,
  ChevronRight,
  Clock3,
} from "lucide-react";
import {
  ORG_EHG_DATE_GLASS_DEFAULT_FOCUS,
  ORG_EHG_DATE_GLASS_DEFAULT_INTENSITY,
  ORG_EHG_DATE_GLASS_DEFAULT_KIND,
  ORG_EHG_DATE_GLASS_DEFAULT_SIZE,
  ORG_EHG_DATE_GLASS_DEFAULT_SURFACE,
  ORG_EHG_DATE_GLASS_DEFAULT_VARIETY,
} from "@/features/ui-standards/org-enterprise-hybrid-glass/org-ehg-date-glass-global-default";
import { softGlassRollTouchHandlers } from "@/features/ui-standards/soft-glass-roll-touch";
import { useScalePresence } from "@/features/ui-standards/soft-glass-scale-presence";

export type OrgEhgDateGlassIntensity = "solid" | "soft" | "medium" | "strong" | "ultra";
export type OrgEhgDateGlassSize = "sm" | "md" | "lg";
export type OrgEhgDateGlassState =
  | "default"
  | "hover"
  | "focused"
  | "error"
  | "success"
  | "disabled";
export type OrgEhgDateGlassKind = "date" | "datetime" | "time" | "range" | "month";
export type OrgEhgDateGlassFocusAnimation = "none" | "thick-below" | "glow";
export type OrgEhgDateGlassPopover = "none" | "date" | "datetime" | "time" | "range";
/** How the picker opens: anchored popover under the field, or centered dialog + scrim. */
export type OrgEhgDateGlassSurface = "popover" | "dialog";
/**
 * Date glass varieties (playground Variety dropdown).
 * - quasar — Quasar calendar (Clear/Today/Set) + 3-value glass HH/MM/AM·PM roll (default)
 * - android — Material header + calendar + clock dial
 * - sidebar — Calendar + quick select / time list
 */
export type OrgEhgDateGlassVariety = "android" | "sidebar" | "quasar";

export {
  ORG_EHG_DATE_GLASS_COMMAND,
  ORG_EHG_DATE_GLASS_DEFAULT_FOCUS,
  ORG_EHG_DATE_GLASS_DEFAULT_INTENSITY,
  ORG_EHG_DATE_GLASS_DEFAULT_KIND,
  ORG_EHG_DATE_GLASS_DEFAULT_SIZE,
  ORG_EHG_DATE_GLASS_DEFAULT_SURFACE,
  ORG_EHG_DATE_GLASS_DEFAULT_VARIETY,
  ORG_EHG_DATE_GLASS_TITLE,
} from "@/features/ui-standards/org-enterprise-hybrid-glass/org-ehg-date-glass-global-default";

export type OrgEhgDateGlassFieldProps = {
  intensity?: OrgEhgDateGlassIntensity;
  size?: OrgEhgDateGlassSize;
  state?: OrgEhgDateGlassState;
  kind?: OrgEhgDateGlassKind;
  variety?: OrgEhgDateGlassVariety;
  label?: string;
  value?: string;
  hint?: string;
  placeholder?: string;
  open?: boolean;
  showPopover?: OrgEhgDateGlassPopover;
  /** `popover` anchors under the field; `dialog` centers over a scrim. Same Soft Glass scale animation. */
  surface?: OrgEhgDateGlassSurface;
  focusAnimation?: OrgEhgDateGlassFocusAnimation;
  onOpenChange?: (open: boolean) => void;
  /** Commits a picked value from the popover (playground / interactive demos). */
  onChange?: (value: string) => void;
  testId?: string;
  className?: string;
};

const WEEKDAYS = ["S", "M", "T", "W", "T", "F", "S"] as const;
const QUASAR_WEEKDAYS = ["Su", "Mo", "Tu", "We", "Th", "Fr", "Sa"] as const;
const MONTHS_SHORT = [
  "Jan",
  "Feb",
  "Mar",
  "Apr",
  "May",
  "Jun",
  "Jul",
  "Aug",
  "Sep",
  "Oct",
  "Nov",
  "Dec",
] as const;
const MONTHS_LONG = [
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
const WEEKDAY_SHORT = ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"] as const;

const QUICK_SELECT = [
  "Today",
  "Tomorrow",
  "Next 7 Days",
  "This Month",
  "Next Month",
] as const;

const TIME_OPTIONS = [
  "9:00 AM",
  "9:30 AM",
  "10:00 AM",
  "10:30 AM",
  "11:00 AM",
  "11:30 AM",
  "12:00 PM",
  "12:30 PM",
] as const;

const QUASAR_HOURS = [
  "01",
  "02",
  "03",
  "04",
  "05",
  "06",
  "07",
  "08",
  "09",
  "10",
  "11",
  "12",
] as const;
const QUASAR_MINUTES = Array.from({ length: 60 }, (_, i) => String(i).padStart(2, "0"));
const QUASAR_PERIODS = ["AM", "PM"] as const;

const DEFAULT_VALUES: Record<OrgEhgDateGlassKind, string> = {
  date: "02/20/2025",
  datetime: "02/20/2025  10:30 AM",
  time: "9:00 PM",
  range: "Feb 20, 2025 → Feb 27, 2025",
  month: "Feb 2025",
};

const DEFAULT_PLACEHOLDERS: Record<OrgEhgDateGlassKind, string> = {
  date: "Select date",
  datetime: "Select date & time",
  time: "Select time",
  range: "Select range",
  month: "Select month",
};

const QUASAR_YEAR_MIN = 1900;
const QUASAR_YEAR_MAX = 2100;
/** Year picker page: 3 columns × 6 rows. */
const QUASAR_YEAR_PAGE_SIZE = 18;

type DraftDate = { year: number; month: number; day: number | null };
type DraftTime = { hour: string; minute: string; period: "AM" | "PM" };

function daysInMonth(year: number, month: number): number {
  return new Date(year, month + 1, 0).getDate();
}

function monthCells(year: number, month: number): Array<number | null> {
  const first = new Date(year, month, 1).getDay();
  const total = daysInMonth(year, month);
  const cells: Array<number | null> = Array.from({ length: first }, () => null);
  for (let day = 1; day <= total; day += 1) cells.push(day);
  while (cells.length % 7 !== 0) cells.push(null);
  return cells;
}

function shiftMonth(year: number, month: number, delta: number): { year: number; month: number } {
  const date = new Date(year, month + delta, 1);
  return { year: date.getFullYear(), month: date.getMonth() };
}

function formatDisplayDate(year: number, month: number, day: number): string {
  return `${MONTHS_SHORT[month]} ${day}, ${year}`;
}

function formatQuasarDate(year: number, month: number, day: number): string {
  return `${String(month + 1).padStart(2, "0")}/${String(day).padStart(2, "0")}/${year}`;
}

function formatAndroidHeaderDate(year: number, month: number, day: number): string {
  const weekday = WEEKDAY_SHORT[new Date(year, month, day).getDay()];
  return `${weekday}, ${MONTHS_SHORT[month]} ${day}`;
}

function formatTime(hour: string, minute: string, period: "AM" | "PM"): string {
  return `${Number(hour)}:${minute} ${period}`;
}

function formatDatetime(dateLabel: string, timeLabel: string): string {
  return `${dateLabel}  ${timeLabel}`;
}

function formatRangeLabel(start: DraftDate, end: DraftDate): string {
  if (start.day == null || end.day == null) return "";
  return `${formatDisplayDate(start.year, start.month, start.day)} → ${formatDisplayDate(end.year, end.month, end.day)}`;
}

function dateStamp(year: number, month: number, day: number): number {
  return year * 10_000 + month * 100 + day;
}

function parseNamedOrNumericDay(token: string, fallbackYear: number): DraftDate | null {
  const trimmed = token.trim();
  const quasarMatch = trimmed.match(/^(\d{1,2})\/(\d{1,2})\/(\d{4})$/);
  if (quasarMatch) {
    return {
      year: Number(quasarMatch[3]),
      month: Number(quasarMatch[1]) - 1,
      day: Number(quasarMatch[2]),
    };
  }
  const namedMatch = trimmed.match(
    /^(Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)\s+(\d{1,2})(?:,\s*(\d{4}))?$/i,
  );
  if (!namedMatch) return null;
  const month = MONTHS_SHORT.findIndex(
    (name) => name.toLowerCase() === namedMatch[1].toLowerCase(),
  );
  return {
    year: namedMatch[3] ? Number(namedMatch[3]) : fallbackYear,
    month: month >= 0 ? month : 0,
    day: Number(namedMatch[2]),
  };
}

function parseInitialRange(value: string | undefined): { start: DraftDate; end: DraftDate | null } {
  const now = new Date();
  const fallbackStart: DraftDate = { year: 2025, month: 1, day: 20 };
  const fallbackEnd: DraftDate = { year: 2025, month: 1, day: 27 };
  if (!value) return { start: fallbackStart, end: fallbackEnd };
  const parts = value.split(/\s*→\s*/);
  if (parts.length < 2) {
    const single = parseNamedOrNumericDay(value, now.getFullYear());
    return single ? { start: single, end: null } : { start: fallbackStart, end: fallbackEnd };
  }
  const start =
    parseNamedOrNumericDay(parts[0], now.getFullYear()) ?? fallbackStart;
  const end = parseNamedOrNumericDay(parts[1], start.year) ?? fallbackEnd;
  return { start, end };
}

function parseInitialDraft(
  value: string | undefined,
  variant: "date" | "datetime" | "time",
): {
  date: DraftDate;
  time: DraftTime;
} {
  const now = new Date();
  const hour24 = now.getHours();
  const nowTime: DraftTime = {
    hour: String(hour24 % 12 || 12).padStart(2, "0"),
    minute: String(now.getMinutes()).padStart(2, "0"),
    period: hour24 >= 12 ? "PM" : "AM",
  };
  /** Empty open → today / now (same idea as Soft Glass Roll OPEN_SHOWS_CURRENT). */
  const fallbackDate: DraftDate = {
    year: now.getFullYear(),
    month: now.getMonth(),
    day: now.getDate(),
  };
  const fallbackTime: DraftTime =
    variant === "datetime" || variant === "time" ? nowTime : { hour: "09", minute: "00", period: "PM" };

  if (!value) {
    return { date: fallbackDate, time: fallbackTime };
  }

  const timeOnlyMatch = value.match(/^(\d{1,2}):(\d{2})\s*(AM|PM)$/i);
  if (timeOnlyMatch && (variant === "time" || !value.includes("/"))) {
    return {
      date: fallbackDate,
      time: {
        hour: String(Number(timeOnlyMatch[1])).padStart(2, "0"),
        minute: timeOnlyMatch[2],
        period: timeOnlyMatch[3].toUpperCase() as "AM" | "PM",
      },
    };
  }

  const quasarMatch = value.match(
    /^(\d{1,2})\/(\d{1,2})\/(\d{4})(?:\s{1,}(\d{1,2}):(\d{2})\s*(AM|PM))?/i,
  );
  if (quasarMatch) {
    return {
      date: {
        year: Number(quasarMatch[3]),
        month: Number(quasarMatch[1]) - 1,
        day: Number(quasarMatch[2]),
      },
      time: {
        hour: String(Number(quasarMatch[4] ?? fallbackTime.hour)).padStart(2, "0"),
        minute: quasarMatch[5] ?? fallbackTime.minute,
        period: ((quasarMatch[6] ?? fallbackTime.period).toUpperCase() as "AM" | "PM"),
      },
    };
  }

  const namedMatch = value.match(
    /^(Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)\s+(\d{1,2}),\s+(\d{4})(?:\s{1,}(\d{1,2}):(\d{2})\s*(AM|PM))?/i,
  );
  if (namedMatch) {
    const month = MONTHS_SHORT.findIndex(
      (name) => name.toLowerCase() === namedMatch[1].toLowerCase(),
    );
    return {
      date: {
        year: Number(namedMatch[3]),
        month: month >= 0 ? month : now.getMonth(),
        day: Number(namedMatch[2]),
      },
      time: {
        hour: String(Number(namedMatch[4] ?? fallbackTime.hour)).padStart(2, "0"),
        minute: namedMatch[5] ?? fallbackTime.minute,
        period: ((namedMatch[6] ?? fallbackTime.period).toUpperCase() as "AM" | "PM"),
      },
    };
  }

  return { date: fallbackDate, time: fallbackTime };
}

function CalendarPane({
  year,
  month,
  selectedDay,
  rangeStart,
  rangeEnd,
  onSelectDay,
  onShiftMonth,
}: {
  year: number;
  month: number;
  selectedDay: number | null;
  rangeStart?: DraftDate | null;
  rangeEnd?: DraftDate | null;
  onSelectDay: (day: number) => void;
  onShiftMonth: (delta: number) => void;
}) {
  const cells = useMemo(() => monthCells(year, month), [year, month]);

  return (
    <div className="org-ehg-date-glass-popover__calendar">
      <div className="org-ehg-date-glass-popover__cal-head">
        <button
          type="button"
          className="org-ehg-date-glass-popover__nav"
          aria-label="Previous month"
          onClick={() => onShiftMonth(-1)}
        >
          <ChevronLeft className="size-4" strokeWidth={2.25} aria-hidden />
        </button>
        <strong className="org-ehg-date-glass-popover__month">
          {MONTHS_LONG[month]} {year}
        </strong>
        <button
          type="button"
          className="org-ehg-date-glass-popover__nav"
          aria-label="Next month"
          onClick={() => onShiftMonth(1)}
        >
          <ChevronRight className="size-4" strokeWidth={2.25} aria-hidden />
        </button>
      </div>
      <div className="org-ehg-date-glass-popover__weekdays" aria-hidden>
        {WEEKDAYS.map((day, index) => (
          <span key={`${day}-${index}`}>{day}</span>
        ))}
      </div>
      <div className="org-ehg-date-glass-popover__grid" role="grid" aria-label={`${MONTHS_LONG[month]} ${year}`}>
        {cells.map((day, index) =>
          day == null ? (
            <span
              key={`empty-${index}`}
              className="org-ehg-date-glass-popover__day org-ehg-date-glass-popover__day--empty"
            />
          ) : (
            <button
              key={day}
              type="button"
              className={
                "org-ehg-date-glass-popover__day" +
                (() => {
                  const extra = dayRangeClass(year, month, day, selectedDay, rangeStart, rangeEnd);
                  return extra ? ` ${extra}` : "";
                })()
              }
              aria-pressed={
                rangeStart?.day != null
                  ? dayRangeClass(year, month, day, selectedDay, rangeStart, rangeEnd).includes(
                      "selected",
                    )
                  : day === selectedDay
              }
              onClick={() => onSelectDay(day)}
            >
              {day}
            </button>
          ),
        )}
      </div>
    </div>
  );
}

function QuickSelectPane({
  selected,
  onSelect,
}: {
  selected: string;
  onSelect: (item: string) => void;
}) {
  return (
    <div className="org-ehg-date-glass-popover__side">
      <p className="org-ehg-date-glass-popover__side-title">Quick Select</p>
      <ul className="org-ehg-date-glass-popover__quick" role="list">
        {QUICK_SELECT.map((item) => (
          <li key={item}>
            <button
              type="button"
              className={
                "org-ehg-date-glass-popover__quick-item" +
                (item === selected ? " org-ehg-date-glass-popover__quick-item--active" : "")
              }
              aria-pressed={item === selected}
              onClick={() => onSelect(item)}
            >
              {item}
            </button>
          </li>
        ))}
      </ul>
    </div>
  );
}

function TimePane({
  selected,
  onSelect,
}: {
  selected: string;
  onSelect: (time: string) => void;
}) {
  return (
    <div className="org-ehg-date-glass-popover__side org-ehg-date-glass-popover__side--times">
      <p className="org-ehg-date-glass-popover__side-title">Time</p>
      <ul className="org-ehg-date-glass-popover__times" role="list">
        {TIME_OPTIONS.map((time) => (
          <li key={time}>
            <button
              type="button"
              className={
                "org-ehg-date-glass-popover__time" +
                (time === selected ? " org-ehg-date-glass-popover__time--selected" : "")
              }
              aria-pressed={time === selected}
              onClick={() => onSelect(time)}
            >
              {time}
            </button>
          </li>
        ))}
      </ul>
    </div>
  );
}

type QuasarCalView = "calendar" | "months" | "years";

function dayRangeClass(
  year: number,
  month: number,
  day: number,
  selectedDay: number | null,
  rangeStart: DraftDate | null | undefined,
  rangeEnd: DraftDate | null | undefined,
): string {
  if (rangeStart?.day != null) {
    const stamp = dateStamp(year, month, day);
    const startStamp = dateStamp(rangeStart.year, rangeStart.month, rangeStart.day);
    const endStamp =
      rangeEnd?.day != null ? dateStamp(rangeEnd.year, rangeEnd.month, rangeEnd.day) : null;
    const classes: string[] = [];
    if (stamp === startStamp) classes.push("org-ehg-date-glass-popover__day--selected", "org-ehg-date-glass-popover__day--range-start");
    if (endStamp != null && stamp === endStamp) {
      classes.push("org-ehg-date-glass-popover__day--selected", "org-ehg-date-glass-popover__day--range-end");
    }
    if (endStamp != null && stamp > startStamp && stamp < endStamp) {
      classes.push("org-ehg-date-glass-popover__day--in-range");
    }
    return classes.join(" ");
  }
  return day === selectedDay ? "org-ehg-date-glass-popover__day--selected" : "";
}

function QuasarCalendarPane({
  year,
  month,
  selectedDay,
  rangeStart,
  rangeEnd,
  onSelectDay,
  onSelectMonth,
  onSelectYear,
  onShiftMonth,
}: {
  year: number;
  month: number;
  selectedDay: number | null;
  rangeStart?: DraftDate | null;
  rangeEnd?: DraftDate | null;
  onSelectDay: (day: number) => void;
  onSelectMonth: (month: number) => void;
  onSelectYear: (year: number) => void;
  onShiftMonth: (delta: number) => void;
}) {
  const [view, setView] = useState<QuasarCalView>("calendar");
  const [yearPage, setYearPage] = useState(
    year - ((year - QUASAR_YEAR_MIN) % QUASAR_YEAR_PAGE_SIZE),
  );
  const cells = useMemo(() => monthCells(year, month), [year, month]);
  const years = useMemo(
    () =>
      Array.from({ length: QUASAR_YEAR_PAGE_SIZE }, (_, i) => yearPage + i).filter(
        (y) => y >= QUASAR_YEAR_MIN && y <= QUASAR_YEAR_MAX,
      ),
    [yearPage],
  );

  function setCalView(next: QuasarCalView) {
    setView(next);
  }

  useEffect(() => {
    setYearPage(year - ((year - QUASAR_YEAR_MIN) % QUASAR_YEAR_PAGE_SIZE));
  }, [year]);

  const yearPageEnd = Math.min(QUASAR_YEAR_MAX, yearPage + QUASAR_YEAR_PAGE_SIZE - 1);

  return (
    <div className="org-ehg-date-glass-popover__quasar-cal" data-cal-view={view}>
      <div className="org-ehg-date-glass-popover__quasar-cal-head org-ehg-date-glass-popover__quasar-cal-head--carets">
        <span className="org-ehg-date-glass-popover__quasar-stepper">
          <button
            type="button"
            className="org-ehg-date-glass-popover__quasar-caret"
            aria-label="Previous month"
            onClick={() => onShiftMonth(-1)}
          >
            <ChevronLeft className="size-3.5" strokeWidth={2} aria-hidden />
          </button>
          <button
            type="button"
            className={
              "org-ehg-date-glass-popover__quasar-month" +
              (view === "months" ? " org-ehg-date-glass-popover__quasar-month--active" : "")
            }
            aria-label="Select month"
            aria-pressed={view === "months"}
            onClick={() => setCalView(view === "months" ? "calendar" : "months")}
          >
            {MONTHS_LONG[month]}
          </button>
          <button
            type="button"
            className="org-ehg-date-glass-popover__quasar-caret"
            aria-label="Next month"
            onClick={() => onShiftMonth(1)}
          >
            <ChevronRight className="size-3.5" strokeWidth={2} aria-hidden />
          </button>
        </span>
        <span className="org-ehg-date-glass-popover__quasar-stepper">
          <button
            type="button"
            className="org-ehg-date-glass-popover__quasar-caret"
            aria-label={view === "years" ? "Previous years" : "Previous year"}
            disabled={
              view === "years" ? yearPage <= QUASAR_YEAR_MIN : year <= QUASAR_YEAR_MIN
            }
            onClick={() => {
              if (view === "years") {
                setYearPage((page) => Math.max(QUASAR_YEAR_MIN, page - QUASAR_YEAR_PAGE_SIZE));
                return;
              }
              onSelectYear(Math.max(QUASAR_YEAR_MIN, year - 1));
            }}
          >
            <ChevronLeft className="size-3.5" strokeWidth={2} aria-hidden />
          </button>
          <button
            type="button"
            className={
              "org-ehg-date-glass-popover__quasar-year" +
              (view === "years" ? " org-ehg-date-glass-popover__quasar-year--active" : "")
            }
            aria-label="Select year"
            aria-pressed={view === "years"}
            onClick={() => setCalView(view === "years" ? "calendar" : "years")}
          >
            {view === "years" ? `${yearPage} – ${yearPageEnd}` : year}
          </button>
          <button
            type="button"
            className="org-ehg-date-glass-popover__quasar-caret"
            aria-label={view === "years" ? "Next years" : "Next year"}
            disabled={
              view === "years"
                ? yearPage + QUASAR_YEAR_PAGE_SIZE > QUASAR_YEAR_MAX
                : year >= QUASAR_YEAR_MAX
            }
            onClick={() => {
              if (view === "years") {
                setYearPage((page) =>
                  Math.min(QUASAR_YEAR_MAX - QUASAR_YEAR_PAGE_SIZE + 1, page + QUASAR_YEAR_PAGE_SIZE),
                );
                return;
              }
              onSelectYear(Math.min(QUASAR_YEAR_MAX, year + 1));
            }}
          >
            <ChevronRight className="size-3.5" strokeWidth={2} aria-hidden />
          </button>
        </span>
      </div>

      <div className="org-ehg-date-glass-popover__quasar-cal-body">
        {view === "months" ? (
          <div
            className="org-ehg-date-glass-popover__quasar-choices"
            role="listbox"
            aria-label="Month"
          >
            {MONTHS_SHORT.map((name, index) => (
              <button
                key={name}
                type="button"
                className={
                  "org-ehg-date-glass-popover__quasar-choice" +
                  (index === month ? " org-ehg-date-glass-popover__quasar-choice--selected" : "")
                }
                aria-pressed={index === month}
                onClick={() => {
                  onSelectMonth(index);
                  setCalView("calendar");
                }}
              >
                {name}
              </button>
            ))}
          </div>
        ) : view === "years" ? (
          <div
            className="org-ehg-date-glass-popover__quasar-choices org-ehg-date-glass-popover__quasar-choices--years"
            role="listbox"
            aria-label="Year"
          >
            {years.map((item) => (
              <button
                key={item}
                type="button"
                className={
                  "org-ehg-date-glass-popover__quasar-choice" +
                  (item === year ? " org-ehg-date-glass-popover__quasar-choice--selected" : "")
                }
                aria-pressed={item === year}
                onClick={() => {
                  onSelectYear(item);
                  setCalView("calendar");
                }}
              >
                {item}
              </button>
            ))}
          </div>
        ) : (
          <>
            <div
              className="org-ehg-date-glass-popover__weekdays org-ehg-date-glass-popover__weekdays--quasar"
              aria-hidden
            >
              {QUASAR_WEEKDAYS.map((day) => (
                <span key={day}>{day}</span>
              ))}
            </div>
            <div
              className="org-ehg-date-glass-popover__grid"
              role="grid"
              aria-label={`${MONTHS_LONG[month]} ${year}`}
            >
              {cells.map((day, index) =>
                day == null ? (
                  <span
                    key={`empty-${index}`}
                    className="org-ehg-date-glass-popover__day org-ehg-date-glass-popover__day--empty"
                  />
                ) : (
                  <button
                    key={day}
                    type="button"
                    className={
                      "org-ehg-date-glass-popover__day org-ehg-date-glass-popover__day--quasar" +
                      (() => {
                        const extra = dayRangeClass(
                          year,
                          month,
                          day,
                          selectedDay,
                          rangeStart,
                          rangeEnd,
                        );
                        return extra ? ` ${extra}` : "";
                      })()
                    }
                    aria-pressed={
                      rangeStart?.day != null
                        ? dayRangeClass(
                            year,
                            month,
                            day,
                            selectedDay,
                            rangeStart,
                            rangeEnd,
                          ).includes("selected")
                        : day === selectedDay
                    }
                    onClick={() => onSelectDay(day)}
                  >
                    {day}
                  </button>
                ),
              )}
            </div>
          </>
        )}
      </div>
    </div>
  );
}

function stepQuasarList(list: readonly string[], current: string, delta: number): string {
  const index = list.indexOf(current);
  const start = index >= 0 ? index : 0;
  return list[(start + delta + list.length) % list.length]!;
}

function clampQuasarHour(raw: string, fallback: string): string {
  const digits = raw.replace(/\D/g, "").slice(0, 2);
  if (!digits) return fallback;
  const n = Number(digits);
  if (!Number.isFinite(n) || n < 1) return "01";
  if (n > 12) return "12";
  return String(n).padStart(2, "0");
}

function clampQuasarMinute(raw: string, fallback: string): string {
  const digits = raw.replace(/\D/g, "").slice(0, 2);
  if (!digits) return fallback;
  const n = Number(digits);
  if (!Number.isFinite(n) || n < 0) return "00";
  if (n > 59) return "59";
  return String(n).padStart(2, "0");
}

function QuasarTimeDigitInput({
  value,
  ariaLabel,
  onCommit,
  clamp,
}: {
  value: string;
  ariaLabel: string;
  onCommit: (next: string) => void;
  clamp: (raw: string, fallback: string) => string;
}) {
  const [draft, setDraft] = useState(value);
  const [editing, setEditing] = useState(false);
  const valueRef = useRef(value);

  useEffect(() => {
    valueRef.current = value;
    setDraft(value);
  }, [value]);

  function commit(raw: string) {
    const next = clamp(raw, valueRef.current);
    setDraft(next);
    if (next !== valueRef.current) onCommit(next);
  }

  return (
    <input
      type="text"
      inputMode="numeric"
      pattern="[0-9]*"
      maxLength={2}
      className="org-ehg-date-glass-popover__quasar-time-value org-ehg-date-glass-popover__quasar-time-value--input org-ehg-date-glass-popover__quasar-time-value--current"
      aria-label={ariaLabel}
      value={editing ? draft : value}
      onFocus={(event) => {
        setEditing(true);
        setDraft(valueRef.current);
        event.currentTarget.select();
      }}
      onBlur={() => {
        commit(draft);
        setEditing(false);
      }}
      onChange={(event) => {
        const digits = event.target.value.replace(/\D/g, "").slice(0, 2);
        setDraft(digits);
        if (digits.length === 2) {
          const next = clamp(digits, valueRef.current);
          setDraft(next);
          if (next !== valueRef.current) onCommit(next);
        }
      }}
      onKeyDown={(event) => {
        if (event.key === "Enter") {
          event.preventDefault();
          event.currentTarget.blur();
        } else if (event.key === "Escape") {
          event.preventDefault();
          setDraft(valueRef.current);
          event.currentTarget.blur();
        }
      }}
      onMouseDown={(event) => event.stopPropagation()}
      onClick={(event) => event.stopPropagation()}
    />
  );
}

function QuasarTimeGlassRoll({
  label,
  caption,
  list,
  value,
  typeable,
  clamp,
  onChange,
}: {
  label: string;
  caption: string;
  list: readonly string[];
  value: string;
  typeable?: boolean;
  clamp?: (raw: string, fallback: string) => string;
  onChange: (next: string) => void;
}) {
  const prev = stepQuasarList(list, value, -1);
  const next = stepQuasarList(list, value, 1);
  const startYRef = useRef<number | null>(null);
  const goPrev = () => onChange(prev);
  const goNext = () => onChange(next);
  const touch = softGlassRollTouchHandlers(goPrev, goNext, false, startYRef);

  function onWheel(event: WheelEvent<HTMLDivElement>) {
    if (Math.abs(event.deltaY) < 2) return;
    event.preventDefault();
    if (event.deltaY > 0) goNext();
    else goPrev();
  }

  return (
    <div
      className="org-ehg-date-glass-popover__quasar-time-step"
      role="group"
      aria-label={label}
      onWheel={onWheel}
      {...touch}
    >
      <span className="org-ehg-date-glass-popover__quasar-time-caption">{caption}</span>
      <div className="org-ehg-date-glass-popover__quasar-time-window">
        <button
          type="button"
          className="org-ehg-date-glass-popover__quasar-time-neighbor"
          aria-label={`Select ${prev}`}
          onClick={goPrev}
        >
          {prev}
        </button>
        <div className="org-ehg-date-glass-popover__quasar-time-band">
          {typeable && clamp ? (
            <QuasarTimeDigitInput value={value} ariaLabel={label} onCommit={onChange} clamp={clamp} />
          ) : (
            <span
              className="org-ehg-date-glass-popover__quasar-time-value org-ehg-date-glass-popover__quasar-time-value--current org-ehg-date-glass-popover__quasar-time-value--period"
              aria-live="polite"
            >
              {value}
            </span>
          )}
        </div>
        <button
          type="button"
          className="org-ehg-date-glass-popover__quasar-time-neighbor"
          aria-label={`Select ${next}`}
          onClick={goNext}
        >
          {next}
        </button>
      </div>
    </div>
  );
}

function QuasarTimeColumns({
  hour,
  minute,
  period,
  onHour,
  onMinute,
  onPeriod,
}: {
  hour: string;
  minute: string;
  period: "AM" | "PM";
  onHour: (hour: string) => void;
  onMinute: (minute: string) => void;
  onPeriod: (period: "AM" | "PM") => void;
}) {
  return (
    <div className="org-ehg-date-glass-popover__quasar-time" aria-label="Time">
      <div className="org-ehg-date-glass-popover__quasar-time-steppers">
        <QuasarTimeGlassRoll
          label="Hour"
          caption="Hours"
          list={QUASAR_HOURS}
          value={hour}
          typeable
          clamp={clampQuasarHour}
          onChange={onHour}
        />
        <QuasarTimeGlassRoll
          label="Minute"
          caption="Minutes"
          list={QUASAR_MINUTES}
          value={minute}
          typeable
          clamp={clampQuasarMinute}
          onChange={onMinute}
        />
        <QuasarTimeGlassRoll
          label="AM or PM"
          caption="Period"
          list={QUASAR_PERIODS}
          value={period}
          onChange={(next) => onPeriod(next as "AM" | "PM")}
        />
      </div>
    </div>
  );
}

/** Android Material-style clock dial sample. */
function AndroidClockDial({ hour, minute }: { hour: string; minute: string }) {
  const hourNum = Number(hour) % 12;
  const minuteNum = Number(minute);
  const hourAngle = ((hourNum + minuteNum / 60) / 12) * Math.PI * 2 - Math.PI / 2;
  const minuteAngle = (minuteNum / 60) * Math.PI * 2 - Math.PI / 2;
  const hourX = 60 + Math.cos(hourAngle) * 28;
  const hourY = 60 + Math.sin(hourAngle) * 28;
  const minuteX = 60 + Math.cos(minuteAngle) * 40;
  const minuteY = 60 + Math.sin(minuteAngle) * 40;

  return (
    <div className="org-ehg-date-glass-popover__clock" aria-hidden>
      <svg viewBox="0 0 120 120" className="org-ehg-date-glass-popover__clock-svg">
        <circle className="org-ehg-date-glass-popover__clock-face" cx="60" cy="60" r="54" />
        <circle className="org-ehg-date-glass-popover__clock-center" cx="60" cy="60" r="3.5" />
        <line
          className="org-ehg-date-glass-popover__clock-hand org-ehg-date-glass-popover__clock-hand--hour"
          x1="60"
          y1="60"
          x2={hourX}
          y2={hourY}
        />
        <line
          className="org-ehg-date-glass-popover__clock-hand org-ehg-date-glass-popover__clock-hand--minute"
          x1="60"
          y1="60"
          x2={minuteX}
          y2={minuteY}
        />
        {[12, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11].map((n) => {
          const angle = ((n % 12) / 12) * Math.PI * 2 - Math.PI / 2;
          const x = 60 + Math.cos(angle) * 40;
          const y = 60 + Math.sin(angle) * 40;
          return (
            <text
              key={n}
              className="org-ehg-date-glass-popover__clock-num"
              x={x}
              y={y}
              textAnchor="middle"
              dominantBaseline="central"
            >
              {n}
            </text>
          );
        })}
      </svg>
    </div>
  );
}

function AndroidHeader({
  variant,
  dateLabel,
  timeLabel,
  meridiem,
  onMeridiem,
}: {
  variant: "date" | "datetime";
  dateLabel: string;
  timeLabel: string;
  meridiem: "AM" | "PM";
  onMeridiem: (next: "AM" | "PM") => void;
}) {
  return (
    <header className="org-ehg-date-glass-popover__android-header">
      <p className="org-ehg-date-glass-popover__android-eyebrow">
        {variant === "datetime" ? "Select date & time" : "Select date"}
      </p>
      {variant === "datetime" ? (
        <div className="org-ehg-date-glass-popover__android-value-row">
          <strong className="org-ehg-date-glass-popover__android-date">{dateLabel}</strong>
          <span className="org-ehg-date-glass-popover__android-time-block">
            <strong className="org-ehg-date-glass-popover__android-time">{timeLabel}</strong>
            <span className="org-ehg-date-glass-popover__android-ampm" role="group" aria-label="AM/PM">
              <button
                type="button"
                className={
                  "org-ehg-date-glass-popover__ampm" +
                  (meridiem === "AM" ? " org-ehg-date-glass-popover__ampm--active" : "")
                }
                aria-pressed={meridiem === "AM"}
                onClick={() => onMeridiem("AM")}
              >
                AM
              </button>
              <button
                type="button"
                className={
                  "org-ehg-date-glass-popover__ampm" +
                  (meridiem === "PM" ? " org-ehg-date-glass-popover__ampm--active" : "")
                }
                aria-pressed={meridiem === "PM"}
                onClick={() => onMeridiem("PM")}
              >
                PM
              </button>
            </span>
          </span>
        </div>
      ) : (
        <strong className="org-ehg-date-glass-popover__android-date org-ehg-date-glass-popover__android-date--solo">
          {dateLabel}
        </strong>
      )}
    </header>
  );
}

export function OrgEhgDateGlassPopover({
  variant = "datetime",
  variety = ORG_EHG_DATE_GLASS_DEFAULT_VARIETY,
  intensity = ORG_EHG_DATE_GLASS_DEFAULT_INTENSITY,
  value,
  onChange,
  onClose,
  open = true,
  surface = ORG_EHG_DATE_GLASS_DEFAULT_SURFACE,
  className,
  testId,
}: {
  variant?: "date" | "datetime" | "time" | "range";
  variety?: OrgEhgDateGlassVariety;
  /** Same Soft→Ultra glass recipe as the field — drives popover + HH/MM glass. */
  intensity?: OrgEhgDateGlassIntensity;
  value?: string;
  onChange?: (next: string) => void;
  onClose?: () => void;
  /** Drives dropdown-like open / close animation (`data-state`). */
  open?: boolean;
  surface?: OrgEhgDateGlassSurface;
  className?: string;
  testId?: string;
}) {
  const isAndroid = variety === "android";
  const isQuasar = variety === "quasar";
  const isTimeOnly = variant === "time";
  const isRange = variant === "range";
  const initial = useMemo(
    () =>
      isRange
        ? {
            date: parseInitialRange(value).start,
            time: { hour: "10", minute: "30", period: "AM" as const },
          }
        : parseInitialDraft(
            value,
            variant === "time" ? "time" : variant === "datetime" ? "datetime" : "date",
          ),
    [value, variant, isRange],
  );
  const initialRange = useMemo(() => parseInitialRange(value), [value]);
  const [date, setDate] = useState<DraftDate>(initial.date);
  const [rangeStart, setRangeStart] = useState<DraftDate | null>(initialRange.start);
  const [rangeEnd, setRangeEnd] = useState<DraftDate | null>(initialRange.end);
  const [time, setTime] = useState<DraftTime>(initial.time);
  const [quick, setQuick] = useState<string>(QUICK_SELECT[0]);
  const [listTime, setListTime] = useState(
    formatTime(initial.time.hour, initial.time.minute, initial.time.period),
  );
  // Keep footer + time chrome while month/year grids are open so the popover does not shrink.
  const showQuasarTime = isTimeOnly || variant === "datetime";

  useEffect(() => {
    if (isRange) {
      const next = parseInitialRange(value);
      setRangeStart(next.start);
      setRangeEnd(next.end);
      setDate(next.start);
      return;
    }
    const next = parseInitialDraft(
      value,
      variant === "time" ? "time" : variant === "datetime" ? "datetime" : "date",
    );
    setDate(next.date);
    setTime(next.time);
    setListTime(formatTime(next.time.hour, next.time.minute, next.time.period));
  }, [value, variant, isRange]);

  function shiftView(delta: number) {
    setDate((prev) => {
      const next = shiftMonth(prev.year, prev.month, delta);
      const maxDay = daysInMonth(next.year, next.month);
      return {
        ...next,
        day: prev.day == null ? null : Math.min(prev.day, maxDay),
      };
    });
  }

  function selectMonth(month: number) {
    setDate((prev) => {
      const maxDay = daysInMonth(prev.year, month);
      return {
        ...prev,
        month,
        day: prev.day == null ? null : Math.min(prev.day, maxDay),
      };
    });
  }

  function selectYear(year: number) {
    setDate((prev) => {
      const maxDay = daysInMonth(year, prev.month);
      return {
        ...prev,
        year,
        day: prev.day == null ? null : Math.min(prev.day, maxDay),
      };
    });
  }

  function commitTime(nextTime = time) {
    onChange?.(formatTime(nextTime.hour, nextTime.minute, nextTime.period));
  }

  function commitRange(start = rangeStart, end = rangeEnd) {
    if (start?.day == null || end?.day == null) {
      onChange?.("");
      return;
    }
    const a = dateStamp(start.year, start.month, start.day);
    const b = dateStamp(end.year, end.month, end.day);
    const ordered = a <= b ? { start, end } : { start: end, end: start };
    onChange?.(formatRangeLabel(ordered.start, ordered.end));
  }

  function commitNamed(day = date.day) {
    if (isTimeOnly) {
      commitTime();
      return;
    }
    if (isRange) {
      commitRange();
      return;
    }
    if (day == null) {
      onChange?.("");
      return;
    }
    const dateLabel = formatDisplayDate(date.year, date.month, day);
    if (variant === "datetime") {
      onChange?.(formatDatetime(dateLabel, formatTime(time.hour, time.minute, time.period)));
    } else {
      onChange?.(dateLabel);
    }
  }

  function commitQuasar(day = date.day, nextTime = time) {
    if (isTimeOnly) {
      commitTime(nextTime);
      return;
    }
    if (isRange) {
      commitRange();
      return;
    }
    if (day == null) {
      onChange?.("");
      return;
    }
    const dateLabel = formatQuasarDate(date.year, date.month, day);
    if (variant === "datetime") {
      onChange?.(
        formatDatetime(dateLabel, formatTime(nextTime.hour, nextTime.minute, nextTime.period)),
      );
    } else {
      onChange?.(dateLabel);
    }
  }

  function selectDay(day: number) {
    if (isRange) {
      const picked: DraftDate = { year: date.year, month: date.month, day };
      if (rangeStart?.day == null || (rangeStart.day != null && rangeEnd?.day != null)) {
        setRangeStart(picked);
        setRangeEnd(null);
        return;
      }
      const startDay = rangeStart.day;
      if (startDay == null || picked.day == null) return;
      const startStamp = dateStamp(rangeStart.year, rangeStart.month, startDay);
      const pickStamp = dateStamp(picked.year, picked.month, picked.day);
      if (pickStamp < startStamp) {
        setRangeEnd(rangeStart);
        setRangeStart(picked);
      } else {
        setRangeEnd(picked);
      }
      return;
    }
    setDate((prev) => ({ ...prev, day }));
  }

  function selectQuasarTime(patch: Partial<DraftTime>) {
    setTime((prev) => ({ ...prev, ...patch }));
  }

  function quasarClear() {
    if (isTimeOnly) {
      onChange?.("");
      onClose?.();
      return;
    }
    if (isRange) {
      setRangeStart(null);
      setRangeEnd(null);
      onChange?.("");
      onClose?.();
      return;
    }
    setDate((prev) => ({ ...prev, day: null }));
    onChange?.("");
    onClose?.();
  }

  function currentDraftTime(now = new Date()): DraftTime {
    const hour24 = now.getHours();
    const period: "AM" | "PM" = hour24 >= 12 ? "PM" : "AM";
    const hour12 = hour24 % 12 || 12;
    return {
      hour: String(hour12).padStart(2, "0"),
      minute: String(now.getMinutes()).padStart(2, "0"),
      period,
    };
  }

  function quasarToday() {
    const now = new Date();
    const nextTime = currentDraftTime(now);
    const picked: DraftDate = {
      year: now.getFullYear(),
      month: now.getMonth(),
      day: now.getDate(),
    };

    if (isTimeOnly) {
      setTime(nextTime);
      commitTime(nextTime);
      onClose?.();
      return;
    }

    if (isRange) {
      setDate(picked);
      setRangeStart(picked);
      setRangeEnd(picked);
      onChange?.(formatRangeLabel(picked, picked));
      onClose?.();
      return;
    }

    setDate(picked);
    const dateLabel = formatQuasarDate(picked.year, picked.month, picked.day!);
    if (variant === "datetime") {
      setTime(nextTime);
      onChange?.(
        formatDatetime(dateLabel, formatTime(nextTime.hour, nextTime.minute, nextTime.period)),
      );
    } else {
      onChange?.(dateLabel);
    }
    onClose?.();
  }

  function quasarSet() {
    commitQuasar();
    onClose?.();
  }

  const headerDate = isRange
    ? rangeStart?.day != null && rangeEnd?.day != null
      ? formatRangeLabel(rangeStart, rangeEnd)
      : rangeStart?.day != null
        ? `${formatDisplayDate(rangeStart.year, rangeStart.month, rangeStart.day)} → …`
        : "Select range"
    : date.day == null
      ? `${MONTHS_SHORT[date.month]} ${date.year}`
      : formatAndroidHeaderDate(date.year, date.month, date.day);
  const headerTime = `${Number(time.hour)}:${time.minute}`;
  const summary = isTimeOnly
    ? formatTime(time.hour, time.minute, time.period)
    : isRange
      ? rangeStart?.day != null && rangeEnd?.day != null
        ? formatRangeLabel(rangeStart, rangeEnd)
        : "Select start and end"
      : date.day == null
        ? "No date selected"
        : variant === "datetime"
          ? `${formatDisplayDate(date.year, date.month, date.day)} · ${formatTime(time.hour, time.minute, time.period)}`
          : formatDisplayDate(date.year, date.month, date.day);

  const ariaLabel = isTimeOnly
    ? isAndroid
      ? "Android time picker"
      : isQuasar
        ? "Quasar time picker"
        : "Time picker"
    : isRange
      ? isAndroid
        ? "Android date range picker"
        : isQuasar
          ? "Quasar date range picker"
          : "Date range picker"
      : variant === "datetime"
        ? isAndroid
          ? "Android date and time picker"
          : isQuasar
            ? "Quasar date and time picker"
            : "Date and time picker"
        : isAndroid
          ? "Android date picker"
          : isQuasar
            ? "Quasar date picker"
            : "Date picker";

  return (
    <div
      className={[
        "org-ehg-date-glass-popover",
        `org-ehg-date-glass-popover--${variety}`,
        `org-ehg-date-glass-popover--${intensity}`,
        `org-ehg-date-glass-popover--surface-${surface}`,
        variant === "datetime" ? "org-ehg-date-glass-popover--datetime" : "",
        isTimeOnly ? "org-ehg-date-glass-popover--time" : "",
        isRange ? "org-ehg-date-glass-popover--range" : "",
        className ?? "",
      ]
        .filter(Boolean)
        .join(" ")}
      data-testid={testId}
      data-variety={variety}
      data-intensity={intensity}
      data-surface={surface}
      data-state={open ? "open" : "closed"}
      data-transition="scale"
      role="dialog"
      aria-modal={surface === "dialog" ? true : undefined}
      aria-label={ariaLabel}
      aria-hidden={open ? undefined : true}
      onMouseDown={(event) => event.stopPropagation()}
    >
      {isAndroid && !isTimeOnly ? (
        <AndroidHeader
          variant={variant === "datetime" ? "datetime" : "date"}
          dateLabel={headerDate}
          timeLabel={headerTime}
          meridiem={time.period}
          onMeridiem={(period) => setTime((prev) => ({ ...prev, period }))}
        />
      ) : null}

      {isAndroid && isTimeOnly ? (
        <AndroidHeader
          variant="datetime"
          dateLabel="Select time"
          timeLabel={headerTime}
          meridiem={time.period}
          onMeridiem={(period) => setTime((prev) => ({ ...prev, period }))}
        />
      ) : null}

      {isQuasar ? (
        <>
          <div
            className={[
              "org-ehg-date-glass-popover__body",
              "org-ehg-date-glass-popover__body--quasar",
              variant === "datetime" ? "org-ehg-date-glass-popover__body--quasar-datetime" : "",
              isTimeOnly ? "org-ehg-date-glass-popover__body--quasar-time" : "",
            ]
              .filter(Boolean)
              .join(" ")}
          >
            {isTimeOnly ? null : (
              <QuasarCalendarPane
                year={date.year}
                month={date.month}
                selectedDay={isRange ? null : date.day}
                rangeStart={isRange ? rangeStart : null}
                rangeEnd={isRange ? rangeEnd : null}
                onSelectDay={selectDay}
                onSelectMonth={selectMonth}
                onSelectYear={selectYear}
                onShiftMonth={shiftView}
              />
            )}
            {showQuasarTime ? (
              <QuasarTimeColumns
                hour={time.hour}
                minute={time.minute}
                period={time.period}
                onHour={(hour) => selectQuasarTime({ hour })}
                onMinute={(minute) => selectQuasarTime({ minute })}
                onPeriod={(period) => selectQuasarTime({ period })}
              />
            ) : null}
          </div>
          <footer
            className="org-ehg-date-glass-popover__quasar-footer"
            onMouseDown={(event) => event.stopPropagation()}
            onClick={(event) => event.stopPropagation()}
          >
            <button
              type="button"
              className="org-ehg-date-glass-popover__quasar-footer-btn org-ehg-date-glass-popover__quasar-footer-btn--clear"
              data-testid={testId ? `${testId}-clear` : undefined}
              onMouseDown={(event) => event.preventDefault()}
              onClick={(event) => {
                event.stopPropagation();
                quasarClear();
              }}
            >
              Clear
            </button>
            <button
              type="button"
              className="org-ehg-date-glass-popover__quasar-footer-btn org-ehg-date-glass-popover__quasar-footer-btn--today"
              data-testid={testId ? `${testId}-today` : undefined}
              onMouseDown={(event) => event.preventDefault()}
              onClick={(event) => {
                event.stopPropagation();
                quasarToday();
              }}
            >
              {isTimeOnly || variant === "datetime" ? "Now" : "Today"}
            </button>
            <button
              type="button"
              className="org-ehg-date-glass-popover__quasar-footer-btn org-ehg-date-glass-popover__quasar-footer-btn--set"
              data-testid={testId ? `${testId}-set` : undefined}
              onMouseDown={(event) => event.preventDefault()}
              onClick={(event) => {
                event.stopPropagation();
                quasarSet();
              }}
            >
              Set
            </button>
          </footer>
        </>
      ) : isAndroid ? (
        <div
          className={[
            "org-ehg-date-glass-popover__body",
            "org-ehg-date-glass-popover__body--android",
            variant === "datetime" || isTimeOnly
              ? "org-ehg-date-glass-popover__body--android-datetime"
              : "",
            isTimeOnly ? "org-ehg-date-glass-popover__body--android-time" : "",
          ]
            .filter(Boolean)
            .join(" ")}
        >
          {isTimeOnly ? null : (
            <CalendarPane
              year={date.year}
              month={date.month}
              selectedDay={isRange ? null : date.day}
              rangeStart={isRange ? rangeStart : null}
              rangeEnd={isRange ? rangeEnd : null}
              onSelectDay={selectDay}
              onShiftMonth={shiftView}
            />
          )}
          {variant === "datetime" || isTimeOnly ? (
            <AndroidClockDial hour={time.hour} minute={time.minute} />
          ) : null}
        </div>
      ) : (
        <div
          className={
            "org-ehg-date-glass-popover__body" +
            (isTimeOnly ? " org-ehg-date-glass-popover__body--sidebar-time" : "") +
            (isRange ? " org-ehg-date-glass-popover__body--sidebar-range" : "")
          }
        >
          {isTimeOnly ? null : (
            <CalendarPane
              year={date.year}
              month={date.month}
              selectedDay={isRange ? null : date.day}
              rangeStart={isRange ? rangeStart : null}
              rangeEnd={isRange ? rangeEnd : null}
              onSelectDay={selectDay}
              onShiftMonth={shiftView}
            />
          )}
          {variant === "datetime" || isTimeOnly ? (
            <TimePane
              selected={listTime}
              onSelect={(next) => {
                setListTime(next);
                const match = next.match(/^(\d{1,2}):(\d{2})\s*(AM|PM)$/i);
                if (match) {
                  setTime({
                    hour: String(Number(match[1])).padStart(2, "0"),
                    minute: match[2],
                    period: match[3].toUpperCase() as "AM" | "PM",
                  });
                }
              }}
            />
          ) : isRange ? null : (
            <QuickSelectPane selected={quick} onSelect={setQuick} />
          )}
        </div>
      )}

      {isQuasar ? null : (
        <footer
          className={
            "org-ehg-date-glass-popover__footer" +
            (isAndroid ? " org-ehg-date-glass-popover__footer--android" : "")
          }
        >
          {isAndroid ? null : (
            <span className="org-ehg-date-glass-popover__summary">{summary}</span>
          )}
          <div className="org-ehg-date-glass-popover__actions">
            <button
              type="button"
              className="org-ehg-date-glass-popover__btn org-ehg-date-glass-popover__btn--ghost"
              onClick={() => onClose?.()}
            >
              Cancel
            </button>
            <button
              type="button"
              className="org-ehg-date-glass-popover__btn org-ehg-date-glass-popover__btn--primary"
              onClick={() => {
                if (isTimeOnly) {
                  if (variety === "sidebar") {
                    const match = listTime.match(/^(\d{1,2}):(\d{2})\s*(AM|PM)$/i);
                    const nextTime = match
                      ? {
                          hour: String(Number(match[1])).padStart(2, "0"),
                          minute: match[2],
                          period: match[3].toUpperCase() as "AM" | "PM",
                        }
                      : time;
                    onChange?.(formatTime(nextTime.hour, nextTime.minute, nextTime.period));
                  } else {
                    commitTime();
                  }
                } else if (isRange) {
                  commitRange();
                } else if (variety === "sidebar" && variant === "datetime") {
                  const match = listTime.match(/^(\d{1,2}):(\d{2})\s*(AM|PM)$/i);
                  const nextTime = match
                    ? {
                        hour: String(Number(match[1])).padStart(2, "0"),
                        minute: match[2],
                        period: match[3].toUpperCase() as "AM" | "PM",
                      }
                    : time;
                  if (date.day == null) {
                    onChange?.("");
                  } else {
                    onChange?.(
                      formatDatetime(
                        formatDisplayDate(date.year, date.month, date.day),
                        formatTime(nextTime.hour, nextTime.minute, nextTime.period),
                      ),
                    );
                  }
                } else {
                  commitNamed();
                }
                onClose?.();
              }}
            >
              {isAndroid ? "OK" : "Apply"}
            </button>
          </div>
        </footer>
      )}
    </div>
  );
}

/**
 * Organization Enterprise Hybrid Glass date / date-time field (UI Standards / DEV only).
 * Default: Quasar date-and-time glass (popover surface).
 */
export function OrgEhgDateGlassField({
  intensity = ORG_EHG_DATE_GLASS_DEFAULT_INTENSITY,
  size = ORG_EHG_DATE_GLASS_DEFAULT_SIZE,
  state = "default",
  kind = ORG_EHG_DATE_GLASS_DEFAULT_KIND,
  variety = ORG_EHG_DATE_GLASS_DEFAULT_VARIETY,
  label,
  value,
  hint,
  placeholder,
  open: openProp,
  showPopover = "none",
  surface = ORG_EHG_DATE_GLASS_DEFAULT_SURFACE,
  focusAnimation = ORG_EHG_DATE_GLASS_DEFAULT_FOCUS,
  onOpenChange,
  onChange,
  testId,
  className,
}: OrgEhgDateGlassFieldProps) {
  const [uncontrolledOpen, setUncontrolledOpen] = useState(false);
  const [uncontrolledValue, setUncontrolledValue] = useState(value ?? DEFAULT_VALUES[kind]);
  const open = openProp ?? uncontrolledOpen;
  const disabled = state === "disabled";
  const displayValue = value ?? uncontrolledValue;
  const displayPlaceholder = placeholder ?? DEFAULT_PLACEHOLDERS[kind];
  const showValue = Boolean(displayValue);
  const popoverVariant =
    showPopover !== "none"
      ? showPopover
      : kind === "datetime"
        ? "datetime"
        : kind === "time"
          ? "time"
          : kind === "range"
            ? "range"
            : kind === "date"
              ? "date"
              : "none";
  const Mark = kind === "time" ? Clock3 : kind === "datetime" ? CalendarClock : CalendarDays;
  const canShowPopover = popoverVariant !== "none";
  const popoverMounted = useScalePresence(open && canShowPopover);

  useEffect(() => {
    if (value !== undefined) setUncontrolledValue(value);
  }, [value]);

  function setOpen(next: boolean) {
    if (disabled) return;
    onOpenChange?.(next);
    if (openProp === undefined) setUncontrolledOpen(next);
  }

  function commitValue(next: string) {
    onChange?.(next);
    if (value === undefined) setUncontrolledValue(next);
  }

  const rootClass = [
    "org-ehg-date-glass-field",
    `org-ehg-date-glass-field--${intensity}`,
    `org-ehg-date-glass-field--size-${size}`,
    `org-ehg-date-glass-field--kind-${kind}`,
    `org-ehg-date-glass-field--${variety}`,
    state !== "default" ? `org-ehg-date-glass-field--state-${state}` : "",
    open ? "org-ehg-date-glass-field--open" : "",
    focusAnimation !== "none" ? `org-ehg-date-glass-field--focus-${focusAnimation}` : "",
    className ?? "",
  ]
    .filter(Boolean)
    .join(" ");

  let valueNode: ReactNode;
  if (!showValue) {
    valueNode = (
      <span className="org-ehg-date-glass-field__placeholder">{displayPlaceholder}</span>
    );
  } else if (kind === "range") {
    const [start, end] = displayValue.split(/\s*→\s*/);
    valueNode = (
      <span className="org-ehg-date-glass-field__range">
        <span>{start || displayValue}</span>
        <ArrowRight className="org-ehg-date-glass-field__range-arrow" aria-hidden strokeWidth={2.25} />
        <span>{end || ""}</span>
      </span>
    );
  } else if (kind === "datetime") {
    const match =
      displayValue.match(/^(.*?)\s{2,}(.+)$/) ??
      displayValue.match(/^(.*?)\s+(\d{1,2}:\d{2}\s*[AP]M)$/i);
    valueNode = match ? (
      <span className="org-ehg-date-glass-field__datetime">
        <span>{match[1]}</span>
        <span className="org-ehg-date-glass-field__time-chip">
          <Clock3 className="size-3.5" aria-hidden strokeWidth={2.25} />
          {match[2]}
        </span>
      </span>
    ) : (
      <span>{displayValue}</span>
    );
  } else {
    valueNode = <span>{displayValue}</span>;
  }

  const picker =
    popoverMounted && canShowPopover ? (
      <OrgEhgDateGlassPopover
        variant={popoverVariant}
        variety={variety}
        intensity={intensity}
        value={displayValue}
        open={open}
        surface={surface}
        onChange={commitValue}
        onClose={() => setOpen(false)}
        testId={testId ? `${testId}-popover` : undefined}
      />
    ) : null;

  const dialogLayer =
    surface === "dialog" && picker && typeof document !== "undefined"
      ? createPortal(
          <div
            className="org-ehg-date-glass-dialog"
            data-state={open ? "open" : "closed"}
            data-testid={testId ? `${testId}-dialog` : undefined}
          >
            <button
              type="button"
              className="org-ehg-date-glass-dialog__scrim"
              aria-label="Close"
              data-testid={testId ? `${testId}-scrim` : undefined}
              onClick={() => setOpen(false)}
            />
            <div className="org-ehg-date-glass-dialog__stage">{picker}</div>
          </div>,
          document.body,
        )
      : null;

  return (
    <div
      className={rootClass}
      data-testid={testId}
      data-variety={variety}
      data-surface={surface}
    >
      {label ? <label className="org-ehg-date-glass-field__label">{label}</label> : null}
      <div className="org-ehg-date-glass-field__anchor">
        <button
          type="button"
          className="org-ehg-date-glass-field__control"
          aria-haspopup="dialog"
          aria-expanded={open}
          aria-invalid={state === "error" || undefined}
          disabled={disabled}
          data-testid={testId ? `${testId}-trigger` : undefined}
          onClick={() => setOpen(!open)}
        >
          {focusAnimation === "thick-below" ? (
            <span className="org-ehg-date-glass-field__focus-bar" aria-hidden />
          ) : null}
          <Mark className="org-ehg-date-glass-field__mark" aria-hidden strokeWidth={2.25} />
          <span className="org-ehg-date-glass-field__value">{valueNode}</span>
          {kind === "month" ? (
            <ChevronDown className="org-ehg-date-glass-field__caret" aria-hidden strokeWidth={2.25} />
          ) : null}
        </button>
        {surface === "popover" ? picker : null}
      </div>
      {hint ? <p className="org-ehg-date-glass-field__hint">{hint}</p> : null}
      {dialogLayer}
    </div>
  );
}

import { useId, useLayoutEffect, useState, type CSSProperties } from "react";
import { Button } from "../button/Button";
import {
  CALENDAR_MAX_YEAR,
  CALENDAR_MIN_YEAR,
  addDays,
  addMonths,
  buildMonthGrid,
  endOfWeek,
  dayAriaLabel,
  daysInMonth,
  formatCalendarDate,
  isDateUnavailable,
  isInsideRange,
  monthLabel,
  monthNames,
  orderRange,
  parseCalendarDate,
  shiftMonth,
  startOfWeek,
  weekdayLabels,
  yearPage,
  type CalendarDate,
  type CalendarView,
  type DateConstraints,
  type WeekStartsOn,
} from "./calendar";
import { cx } from "./cx";
import { ChevronGlyph } from "./glyphs";
import styles from "./CalendarMonth.module.scss";

type CalendarMonthProps = {
  view: CalendarView;
  direction: -1 | 0 | 1;
  onViewChange: (view: CalendarView, direction: -1 | 1) => void;
  mode: "single" | "range";
  selectedDate: CalendarDate | null;
  rangeStart: CalendarDate | null;
  rangeEnd: CalendarDate | null;
  preview: boolean;
  constraints: DateConstraints;
  weekStartsOn: WeekStartsOn;
  locale: string;
  today: CalendarDate;
  focusedDate: CalendarDate;
  onFocusedDate: (date: CalendarDate) => void;
  onSelectDate: (date: CalendarDate) => void;
  onHoverDate?: (date: CalendarDate | null) => void;
};

type Panel = "days" | "months" | "years";

function rowEdge(index: number) {
  if (index % 7 === 0) return "start";
  if (index % 7 === 6) return "end";
  return undefined;
}

export function CalendarMonth({
  view,
  direction,
  onViewChange,
  mode,
  selectedDate,
  rangeStart,
  rangeEnd,
  preview,
  constraints,
  weekStartsOn,
  locale,
  today,
  focusedDate,
  onFocusedDate,
  onSelectDate,
  onHoverDate,
}: CalendarMonthProps) {
  const [panel, setPanel] = useState<Panel>("days");
  const [yearAnchor, setYearAnchor] = useState(view.year);
  const [focusNonce, setFocusNonce] = useState(0);
  const labelId = useId();
  const helpId = useId();
  const baseId = useId();
  const narrow = weekdayLabels(locale, weekStartsOn, "narrow");
  const long = weekdayLabels(locale, weekStartsOn, "long");
  const cells = buildMonthGrid(view.year, view.month, weekStartsOn);
  const years = yearPage(yearAnchor);
  const months = monthNames(locale, "short");
  const monthLong = monthNames(locale, "long");
  const heading = monthLabel(view.year, view.month, locale);
  const atFirstMonth = view.year === CALENDAR_MIN_YEAR && view.month === 1;
  const atLastMonth = view.year === CALENDAR_MAX_YEAR && view.month === 12;
  const paneStyle = { "--month-shift": direction < 0 ? "-8px" : direction > 0 ? "8px" : "0px" } as CSSProperties;

  useLayoutEffect(() => {
    if (focusNonce === 0 || panel !== "days") return;
    document.getElementById(`${baseId}-${focusedDate}`)?.focus();
  }, [baseId, focusNonce, focusedDate, panel, view.year, view.month]);

  function moveFocus(next: CalendarDate) {
    const parts = parseCalendarDate(next);
    if (!parts || parts.year < CALENDAR_MIN_YEAR || parts.year > CALENDAR_MAX_YEAR) return;
    if (parts.year !== view.year || parts.month !== view.month) {
      onViewChange({ year: parts.year, month: parts.month }, next < focusedDate ? -1 : 1);
    }
    onFocusedDate(next);
    setFocusNonce((value) => value + 1);
  }

  function goMonth(amount: number) {
    const next = shiftMonth(view.year, view.month, amount);
    if (next.year < CALENDAR_MIN_YEAR || next.year > CALENDAR_MAX_YEAR) return;
    const currentDay = parseCalendarDate(focusedDate)?.day ?? 1;
    const day = Math.min(currentDay, daysInMonth(next.year, next.month));
    onFocusedDate(formatCalendarDate({ year: next.year, month: next.month, day }));
    onViewChange(next, amount > 0 ? 1 : -1);
  }

  function selectMonth(month: number) {
    onViewChange({ year: view.year, month }, month >= view.month ? 1 : -1);
    const currentDay = parseCalendarDate(focusedDate)?.day ?? 1;
    const day = Math.min(currentDay, daysInMonth(view.year, month));
    onFocusedDate(formatCalendarDate({ year: view.year, month, day }));
    setPanel("days");
    setFocusNonce((nonce) => nonce + 1);
  }

  function selectYear(year: number) {
    onViewChange({ year, month: view.month }, year >= view.year ? 1 : -1);
    const currentDay = parseCalendarDate(focusedDate)?.day ?? 1;
    const day = Math.min(currentDay, daysInMonth(year, view.month));
    onFocusedDate(formatCalendarDate({ year, month: view.month, day }));
    setYearAnchor(year);
    setPanel("months");
  }

  const ordered = rangeStart && rangeEnd ? orderRange(rangeStart, rangeEnd) : null;

  return (
    <div className={styles.calendar}>
      {panel === "days" ? (
        <div className={styles.header}>
          <Button variant="ghost" iconOnly aria-label="Previous month" disabled={atFirstMonth} onClick={() => goMonth(-1)} icon={<ChevronGlyph direction="previous" />} />
          <button id={labelId} type="button" className={styles.period} aria-label={`Choose month, ${heading}`} onClick={() => setPanel("months")}>
            <span className={styles.periodLabel}>{heading}</span>
          </button>
          <Button variant="ghost" iconOnly aria-label="Next month" disabled={atLastMonth} onClick={() => goMonth(1)} icon={<ChevronGlyph direction="next" />} />
        </div>
      ) : null}
      {panel === "months" ? (
        <div className={styles.header}>
          <Button
            variant="ghost"
            iconOnly
            aria-label="Previous year"
            disabled={view.year <= CALENDAR_MIN_YEAR}
            onClick={() => onViewChange({ year: view.year - 1, month: view.month }, -1)}
            icon={<ChevronGlyph direction="previous" />}
          />
          <button type="button" className={styles.period} aria-label={`Choose year, ${view.year}`} onClick={() => { setYearAnchor(view.year); setPanel("years"); }}>
            <span className={styles.periodLabel}>{view.year}</span>
          </button>
          <Button
            variant="ghost"
            iconOnly
            aria-label="Next year"
            disabled={view.year >= CALENDAR_MAX_YEAR}
            onClick={() => onViewChange({ year: view.year + 1, month: view.month }, 1)}
            icon={<ChevronGlyph direction="next" />}
          />
        </div>
      ) : null}
      {panel === "years" ? (
        <div className={styles.header}>
          <Button variant="ghost" iconOnly aria-label="Previous years" disabled={years[0] <= CALENDAR_MIN_YEAR} onClick={() => setYearAnchor(years[0] - 1)} icon={<ChevronGlyph direction="previous" />} />
          <p className={cx(styles.period, styles.periodStatic)}>
            <span className={styles.periodLabel}>
              {years[0]} – {years[years.length - 1]}
            </span>
          </p>
          <Button
            variant="ghost"
            iconOnly
            aria-label="Next years"
            disabled={years[years.length - 1] >= CALENDAR_MAX_YEAR}
            onClick={() => setYearAnchor(years[years.length - 1] + 1)}
            icon={<ChevronGlyph direction="next" />}
          />
        </div>
      ) : null}

      {panel === "days" ? (
        <div key={`${view.year}-${view.month}`} className={styles.monthPane} style={paneStyle}>
          <p id={helpId} className={styles.srOnly}>
            Arrow keys move by day. Page up and page down move by month. Shift plus page up and page down move by year. Home and end move to the ends of the week.
          </p>
          <div
            className={styles.days}
            role="grid"
            aria-labelledby={labelId}
            aria-describedby={helpId}
            onKeyDown={(event) => {
            if (event.key === "ArrowRight") moveFocus(addDays(focusedDate, 1));
            else if (event.key === "ArrowLeft") moveFocus(addDays(focusedDate, -1));
            else if (event.key === "ArrowDown") moveFocus(addDays(focusedDate, 7));
            else if (event.key === "ArrowUp") moveFocus(addDays(focusedDate, -7));
            else if (event.key === "Home") moveFocus(startOfWeek(focusedDate, weekStartsOn));
            else if (event.key === "End") moveFocus(endOfWeek(focusedDate, weekStartsOn));
            else if (event.key === "PageUp") moveFocus(addMonths(focusedDate, event.shiftKey ? -12 : -1));
            else if (event.key === "PageDown") moveFocus(addMonths(focusedDate, event.shiftKey ? 12 : 1));
            else return;
            event.preventDefault();
          }}
          onPointerLeave={() => onHoverDate?.(null)}
        >
          <div className={styles.weekdays} role="row">
            {narrow.map((label, index) => (
              <div key={`${long[index]}-${index}`} className={styles.weekday} role="columnheader" aria-label={long[index]}>
                <span aria-hidden="true">{label}</span>
              </div>
            ))}
          </div>
          {Array.from({ length: 6 }, (_, row) => (
            <div key={cells[row * 7]?.date ?? row} className={styles.grid} role="row">
              {cells.slice(row * 7, row * 7 + 7).map((cell, column) => {
                const index = row * 7 + column;
                const parts = parseCalendarDate(cell.date);
                const unavailable = isDateUnavailable(cell.date, constraints);
                const inRange = Boolean(ordered && isInsideRange(cell.date, ordered.start, ordered.end));
                const edge = !ordered
                  ? rangeStart === cell.date
                    ? "single"
                    : null
                  : ordered.start === ordered.end && cell.date === ordered.start
                    ? "single"
                    : cell.date === ordered.start
                      ? "start"
                      : cell.date === ordered.end
                        ? "end"
                        : null;
                const selected = mode === "single" && selectedDate === cell.date;
                return (
                  <button
                    key={cell.date}
                    id={`${baseId}-${cell.date}`}
                    type="button"
                    role="gridcell"
                    className={styles.day}
                    tabIndex={cell.date === focusedDate ? 0 : -1}
                    data-date={cell.date}
                    data-outside={cell.inMonth ? undefined : "true"}
                    data-today={cell.date === today ? "true" : undefined}
                    data-selected={selected ? "true" : undefined}
                    data-in-range={inRange ? "true" : undefined}
                    data-preview={preview && inRange ? "true" : undefined}
                    data-range={edge ?? undefined}
                    data-row-edge={rowEdge(index)}
                    data-autofocus={cell.date === focusedDate ? "true" : undefined}
                    aria-selected={selected || edge === "start" || edge === "end" || edge === "single" ? true : undefined}
                    aria-disabled={unavailable || undefined}
                    aria-current={cell.date === today ? "date" : undefined}
                    aria-label={dayAriaLabel(cell.date, locale, today)}
                    onClick={() => {
                      if (unavailable) return;
                      onFocusedDate(cell.date);
                      onSelectDate(cell.date);
                    }}
                    onPointerEnter={() => {
                      if (!unavailable) onHoverDate?.(cell.date);
                    }}
                  >
                    {parts?.day}
                  </button>
                );
              })}
            </div>
          ))}
        </div>
        </div>
      ) : null}

      {panel === "months" ? (
        <div key={`months-${view.year}`} className={cx(styles.monthPane, styles.choiceGrid)} role="grid" aria-label={`Months in ${view.year}`}>
          {months.map((label, index) => {
            const month = index + 1;
            const selected = month === view.month;
            const current = view.year === parseCalendarDate(today)?.year && month === parseCalendarDate(today)?.month;
            return (
              <button
                key={label}
                type="button"
                role="gridcell"
                className={styles.choice}
                data-selected={selected ? "true" : undefined}
                data-current={current ? "true" : undefined}
                aria-selected={selected}
                aria-label={monthLong[index]}
                onClick={() => selectMonth(month)}
              >
                {label}
              </button>
            );
          })}
        </div>
      ) : null}

      {panel === "years" ? (
        <div className={cx(styles.monthPane, styles.choiceGrid)} role="grid" aria-label="Choose a year">
          {years.map((year) => (
            <button
              key={year}
              type="button"
              role="gridcell"
              className={styles.choice}
              data-selected={year === view.year ? "true" : undefined}
              data-current={year === parseCalendarDate(today)?.year ? "true" : undefined}
              aria-selected={year === view.year}
              aria-label={String(year)}
              onClick={() => selectYear(year)}
            >
              {year}
            </button>
          ))}
        </div>
      ) : null}
    </div>
  );
}

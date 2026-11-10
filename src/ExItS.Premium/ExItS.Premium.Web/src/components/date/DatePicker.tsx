import { useCallback, useEffect, useId, useState } from "react";
import { createPortal } from "react-dom";
import { Button } from "../button/Button";
import { BottomSheet } from "../sheet/BottomSheet";
import { CalendarMonth } from "./CalendarMonth";
import {
  formatDisplayDate,
  isDateUnavailable,
  localeWeekStart,
  todayCalendarDate,
  viewOf,
  type CalendarDate,
  type CalendarView,
  type DateConstraints,
  type WeekStartsOn,
} from "./calendar";
import { DateField } from "./DateField";
import { trapTab } from "./focus";
import surface from "./surface.module.scss";
import { useCompactViewport } from "./useCompactViewport";
import { useControllable } from "./useControllable";
import { useDateSurface } from "./useDateSurface";

export type DatePickerProps = {
  label: string;
  value?: CalendarDate | null;
  defaultValue?: CalendarDate | null;
  onChange?: (value: CalendarDate | null) => void;
  min?: CalendarDate;
  max?: CalendarDate;
  disabledDates?: readonly CalendarDate[];
  isDateDisabled?: (date: CalendarDate) => boolean;
  weekStartsOn?: WeekStartsOn;
  locale?: string;
  placeholder?: string;
  disabled?: boolean;
  name?: string;
  id?: string;
  hint?: string;
  error?: string;
  success?: string;
  clearable?: boolean;
  /**
   * Renders the calendar in place. No field, popover, or bottom sheet.
   * Date selection reports through `onChange` and leaves the calendar open.
   */
  embedded?: boolean;
  /** Calendar date highlighted as today. Defaults to the local calendar day. */
  today?: CalendarDate;
};

export function DatePicker({
  label,
  value,
  defaultValue = null,
  onChange,
  min,
  max,
  disabledDates,
  isDateDisabled,
  weekStartsOn,
  locale,
  placeholder = "Select a date",
  disabled = false,
  name,
  id,
  hint,
  error,
  success,
  clearable = true,
  embedded = false,
  today: todayProp,
}: DatePickerProps) {
  const autoId = useId();
  const fieldId = id ?? autoId;
  const panelId = useId();
  const resolvedLocale = locale ?? (typeof navigator !== "undefined" ? navigator.language : "en");
  const resolvedWeek = weekStartsOn ?? localeWeekStart(resolvedLocale);
  const today = todayProp ?? todayCalendarDate();
  const [current, setCurrent] = useControllable(value, defaultValue, onChange);
  const [open, setOpen] = useState(false);
  const [view, setView] = useState<CalendarView>(() => viewOf(current ?? today));
  const [focused, setFocused] = useState<CalendarDate>(current ?? today);
  const [direction, setDirection] = useState<-1 | 0 | 1>(0);
  const compact = useCompactViewport();
  const { anchorRef, triggerRef, popoverRef } = useDateSurface(embedded ? false : open, compact);
  const externalDate = embedded && current ? current : "";
  const [trackedDate, setTrackedDate] = useState(externalDate);
  if (trackedDate !== externalDate) {
    setTrackedDate(externalDate);
    if (current) {
      setView(viewOf(current));
      setFocused(current);
      setDirection(0);
    }
  }
  const constraints: DateConstraints = { min, max, disabledDates, isDateDisabled };
  const text = current ? formatDisplayDate(current, resolvedLocale) : null;

  const close = useCallback(() => {
    setOpen(false);
    triggerRef.current?.focus();
  }, [triggerRef]);

  useEffect(() => {
    if (!open || compact) return;
    function onPointerDown(event: PointerEvent) {
      const target = event.target;
      if (!(target instanceof Node)) return;
      if (popoverRef.current?.contains(target) || anchorRef.current?.contains(target)) return;
      close();
    }
    function onKeyDown(event: KeyboardEvent) {
      if (event.key !== "Escape") return;
      close();
    }
    document.addEventListener("pointerdown", onPointerDown);
    document.addEventListener("keydown", onKeyDown);
    return () => {
      document.removeEventListener("pointerdown", onPointerDown);
      document.removeEventListener("keydown", onKeyDown);
    };
  }, [anchorRef, close, compact, open, popoverRef]);

  function openPanel() {
    const anchor = current ?? today;
    setView(viewOf(anchor));
    setFocused(anchor);
    setDirection(0);
    setOpen(true);
  }

  function commit(next: CalendarDate | null) {
    setCurrent(next);
    close();
  }

  function selectDate(date: CalendarDate) {
    if (isDateUnavailable(date, constraints)) return;
    setFocused(date);
    if (embedded) {
      setCurrent(date);
      return;
    }
    commit(date);
  }

  function selectToday() {
    setView(viewOf(today));
    setFocused(today);
    setDirection(0);
    if (isDateUnavailable(today, constraints)) return;
    commit(today);
  }

  if (embedded) {
    return (
      <div className={surface.embedded} data-component="date-picker" data-embedded="true" role="group" aria-label={label}>
        <CalendarMonth
          view={view}
          direction={direction}
          onViewChange={(next, nextDirection) => {
            setDirection(nextDirection);
            setView(next);
          }}
          mode="single"
          selectedDate={current}
          rangeStart={null}
          rangeEnd={null}
          preview={false}
          constraints={constraints}
          weekStartsOn={resolvedWeek}
          locale={resolvedLocale}
          today={today}
          focusedDate={focused}
          onFocusedDate={setFocused}
          onSelectDate={selectDate}
        />
      </div>
    );
  }

  const panel = (
    <>
      <CalendarMonth
        view={view}
        direction={direction}
        onViewChange={(next, nextDirection) => {
          setDirection(nextDirection);
          setView(next);
        }}
        mode="single"
        selectedDate={current}
        rangeStart={null}
        rangeEnd={null}
        preview={false}
        constraints={constraints}
        weekStartsOn={resolvedWeek}
        locale={resolvedLocale}
        today={today}
        focusedDate={focused}
        onFocusedDate={setFocused}
        onSelectDate={selectDate}
      />
      <div className={compact ? surface.sheetActions : surface.actions}>
        <Button variant="ghost" onClick={selectToday} disabled={isDateUnavailable(today, constraints)}>
          Today
        </Button>
        <Button variant="ghost" onClick={() => commit(null)} disabled={!current}>
          Clear
        </Button>
      </div>
    </>
  );

  return (
    <DateField
      id={fieldId}
      label={label}
      text={text}
      placeholder={placeholder}
      open={open}
      controls={panelId}
      disabled={disabled}
      hint={hint}
      error={error}
      success={success}
      name={name}
      hiddenValue={current ?? ""}
      clearable={clearable}
      clearLabel={`Clear ${label}`}
      viewport={compact ? "compact" : "regular"}
      triggerRef={triggerRef}
      anchorRef={anchorRef}
      onToggle={() => (open ? close() : openPanel())}
      onClear={() => commit(null)}
    >
      {compact ? (
        <BottomSheet open={open} title="Choose date" onClose={close}>
          {open ? (
            <div id={panelId} className={surface.sheetBody} data-surface="sheet">
              {panel}
            </div>
          ) : null}
        </BottomSheet>
      ) : null}
      {open && !compact
        ? createPortal(
            <div
              ref={popoverRef}
              id={panelId}
              className={surface.popover}
              popover="manual"
              role="dialog"
              aria-modal="true"
              aria-label="Choose date"
              data-surface="popover"
              onKeyDown={(event) => {
                if (event.key === "Escape") {
                  event.preventDefault();
                  close();
                  return;
                }
                trapTab(event, event.currentTarget);
              }}
            >
              {panel}
            </div>,
            document.body,
          )
        : null}
    </DateField>
  );
}

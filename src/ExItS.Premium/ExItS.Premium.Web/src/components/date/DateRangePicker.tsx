import { useCallback, useEffect, useId, useState } from "react";
import { createPortal } from "react-dom";
import { Button } from "../button/Button";
import { BottomSheet } from "../sheet/BottomSheet";
import { CalendarMonth } from "./CalendarMonth";
import {
  RANGE_PRESET_IDS,
  RANGE_PRESET_LABELS,
  formatDisplayDate,
  formatRangeLabel,
  isDateUnavailable,
  localeWeekStart,
  matchingPreset,
  orderRange,
  rangeIsValid,
  resolvePreset,
  todayCalendarDate,
  viewOf,
  type CalendarDate,
  type CalendarView,
  type DateConstraints,
  type DateRange,
  type RangePresetId,
  type WeekStartsOn,
} from "./calendar";
import { cx } from "./cx";
import { DateField } from "./DateField";
import { trapTab } from "./focus";
import styles from "./DateRangePicker.module.scss";
import surface from "./surface.module.scss";
import { useCompactViewport } from "./useCompactViewport";
import { useControllable } from "./useControllable";
import { useDateSurface } from "./useDateSurface";

export type { DateRange };

export type DateRangePickerProps = {
  label: string;
  value?: DateRange | null;
  defaultValue?: DateRange | null;
  onChange?: (value: DateRange | null) => void;
  min?: CalendarDate;
  max?: CalendarDate;
  disabledDates?: readonly CalendarDate[];
  isDateDisabled?: (date: CalendarDate) => boolean;
  weekStartsOn?: WeekStartsOn;
  locale?: string;
  presets?: readonly RangePresetId[];
  placeholder?: string;
  disabled?: boolean;
  name?: string;
  id?: string;
  hint?: string;
  error?: string;
  success?: string;
  clearable?: boolean;
};

type Draft =
  | { kind: "empty" }
  | { kind: "partial"; start: CalendarDate }
  | { kind: "complete"; start: CalendarDate; end: CalendarDate };

function draftFromValue(value: DateRange | null): Draft {
  if (!value) return { kind: "empty" };
  return { kind: "complete", start: value.start, end: value.end };
}

export function DateRangePicker({
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
  presets = RANGE_PRESET_IDS,
  placeholder = "Select dates",
  disabled = false,
  name,
  id,
  hint,
  error,
  success,
  clearable = true,
}: DateRangePickerProps) {
  const autoId = useId();
  const fieldId = id ?? autoId;
  const panelId = useId();
  const resolvedLocale = locale ?? (typeof navigator !== "undefined" ? navigator.language : "en");
  const resolvedWeek = weekStartsOn ?? localeWeekStart(resolvedLocale);
  const today = todayCalendarDate();
  const [current, setCurrent] = useControllable(value, defaultValue, onChange);
  const [open, setOpen] = useState(false);
  const [draft, setDraft] = useState<Draft>(() => draftFromValue(current));
  const [hover, setHover] = useState<CalendarDate | null>(null);
  const [view, setView] = useState<CalendarView>(() => viewOf(current?.start ?? today));
  const [focused, setFocused] = useState<CalendarDate>(current?.start ?? today);
  const [direction, setDirection] = useState<-1 | 0 | 1>(0);
  const compact = useCompactViewport();
  const { anchorRef, triggerRef, popoverRef } = useDateSurface(open, compact);
  const constraints: DateConstraints = { min, max, disabledDates, isDateDisabled };
  const text = current ? formatRangeLabel(current, resolvedLocale) : null;
  const previewEnd = draft.kind === "partial" ? hover : null;
  const rangeStart = draft.kind === "empty" ? null : draft.start;
  const rangeEnd = draft.kind === "complete" ? draft.end : previewEnd;
  const canApply = draft.kind === "empty" || (draft.kind === "complete" && rangeIsValid(draft.start, draft.end, constraints));
  const activePreset = draft.kind === "complete" ? matchingPreset({ start: draft.start, end: draft.end }, today, resolvedWeek, constraints) : "custom";
  const status =
    draft.kind === "complete"
      ? formatRangeLabel({ start: draft.start, end: draft.end }, resolvedLocale)
      : draft.kind === "partial"
        ? `Start ${formatDisplayDate(draft.start, resolvedLocale)}. Select an end date.`
        : "No dates selected";

  const close = useCallback(() => {
    setOpen(false);
    setHover(null);
    triggerRef.current?.focus();
  }, [setHover, triggerRef]);

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
    setDraft(draftFromValue(current));
    setHover(null);
    const anchor = current?.start ?? today;
    setView(viewOf(anchor));
    setFocused(anchor);
    setDirection(0);
    setOpen(true);
  }

  function selectDate(date: CalendarDate) {
    if (isDateUnavailable(date, constraints)) return;
    if (draft.kind !== "partial") {
      setDraft({ kind: "partial", start: date });
      setHover(null);
      return;
    }
    const ordered = orderRange(draft.start, date);
    if (!rangeIsValid(ordered.start, ordered.end, constraints)) {
      setDraft({ kind: "partial", start: date });
      setHover(null);
      return;
    }
    setDraft({ kind: "complete", start: ordered.start, end: ordered.end });
    setHover(null);
  }

  function choosePreset(id: RangePresetId) {
    if (id === "custom") {
      setDraft({ kind: "empty" });
      setHover(null);
      return;
    }
    const resolved = resolvePreset(id, today, resolvedWeek, constraints);
    if (!resolved) return;
    setDraft({ kind: "complete", start: resolved.start, end: resolved.end });
    setHover(null);
    setView(viewOf(resolved.start));
    setFocused(resolved.start);
    setDirection(0);
  }

  function apply() {
    if (draft.kind === "partial") return;
    if (draft.kind === "empty") setCurrent(null);
    else setCurrent({ start: draft.start, end: draft.end });
    close();
  }

  function clearDraft() {
    setDraft({ kind: "empty" });
    setHover(null);
  }

  const panel = (
    <>
      <div className={styles.layout}>
        <div className={styles.presets} role="group" aria-label="Date range presets">
          {presets.map((id) => {
            const available = id === "custom" || resolvePreset(id, today, resolvedWeek, constraints) !== null;
            return (
              <div key={id} className={styles.preset}>
                <Button variant="ghost" aria-pressed={activePreset === id} disabled={!available} onClick={() => choosePreset(id)}>
                  {RANGE_PRESET_LABELS[id]}
                </Button>
              </div>
            );
          })}
        </div>
        <div className={styles.main}>
          <p className={cx("type-caption", styles.status)} role="status">
            {status}
          </p>
          <CalendarMonth
            view={view}
            direction={direction}
            onViewChange={(next, nextDirection) => {
              setDirection(nextDirection);
              setView(next);
            }}
            mode="range"
            selectedDate={null}
            rangeStart={rangeStart}
            rangeEnd={rangeEnd}
            preview={draft.kind === "partial" && Boolean(previewEnd)}
            constraints={constraints}
            weekStartsOn={resolvedWeek}
            locale={resolvedLocale}
            today={today}
            focusedDate={focused}
            onFocusedDate={setFocused}
            onSelectDate={selectDate}
            onHoverDate={setHover}
          />
        </div>
      </div>
      <div className={compact ? surface.sheetActions : surface.actions}>
        <Button variant="ghost" onClick={clearDraft}>
          Clear
        </Button>
        <div className={surface.actionsEnd}>
          <Button variant="secondary" onClick={close}>
            Cancel
          </Button>
          <Button variant="primary" onClick={apply} disabled={!canApply}>
            Apply
          </Button>
        </div>
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
      hiddenValue={current ? `${current.start}/${current.end}` : ""}
      clearable={clearable}
      clearLabel={`Clear ${label}`}
      viewport={compact ? "compact" : "regular"}
      triggerRef={triggerRef}
      anchorRef={anchorRef}
      onToggle={() => (open ? close() : openPanel())}
      onClear={() => {
        setCurrent(null);
        close();
      }}
    >
      {compact ? (
        <BottomSheet open={open} title="Choose dates" onClose={close}>
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
              className={cx(surface.popover, surface.popoverRange)}
              popover="manual"
              role="dialog"
              aria-modal="true"
              aria-label="Choose dates"
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

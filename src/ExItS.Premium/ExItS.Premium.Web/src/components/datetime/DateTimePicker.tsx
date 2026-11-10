import { useId, useRef, useState } from "react";
import { ExIcon } from "../../icons/ExIcon";
import { Button } from "../button/Button";
import { Field } from "../field/Field";
import { SegmentedControl } from "../segmented/SegmentedControl";
import { BottomSheet } from "../sheet/BottomSheet";
import { AnchoredPopover } from "../time/AnchoredPopover";
import { TimeInput, type TimeEntrySnapshot } from "../time/TimeInput";
import { TimeWheel } from "../time/TimeWheel";
import { formatTimeDisplay, type HourCycle, type MinuteStep } from "../time/time";
import { useControllable } from "../time/useControllable";
import { useViewport } from "../time/useViewport";
import chrome from "../time/TimePicker.module.scss";
import { DatePicker } from "../date/DatePicker";
import { todayCalendarDate } from "../date/calendar";
import { calendarDateMessage, formatCalendarDisplay, parseCalendarDate } from "./date";
import {
  formatDateTimeDisplay,
  validateDateTime,
  zonePreviewSuffix,
  zonedNow,
  type DateTimeValidation,
  type DateTimeValue,
} from "./datetime";
import { currentWallClock, type AmbiguityPolicy } from "./timezone";
import styles from "./DateTimePicker.module.scss";

type Draft = { date: string | null; time: string | null };

export type DateTimePickerProps = {
  id?: string;
  name?: string;
  label: string;
  hint?: string;
  error?: string;
  /** Controlled local date and time. `null` is empty. Omit `value` for uncontrolled use. */
  value?: DateTimeValue | null;
  defaultValue?: DateTimeValue | null;
  /** Called when Apply commits or Clear removes the value. Draft edits do not call this. */
  onChange?: (value: DateTimeValue | null) => void;
  min?: DateTimeValue;
  max?: DateTimeValue;
  hourCycle?: HourCycle;
  minuteStep?: MinuteStep;
  disabledTimes?: readonly string[];
  isTimeDisabled?: (time: string) => boolean;
  /** Optional IANA zone. Wall-clock values are not rewritten when the instant is missing or ambiguous. */
  timeZone?: string;
  /** Default `reject` leaves ambiguous civil times unresolved. */
  ambiguity?: AmbiguityPolicy;
  disabled?: boolean;
  readOnly?: boolean;
  required?: boolean;
  clearable?: boolean;
  locale?: string;
  placeholder?: string;
  viewport?: "auto" | "desktop" | "mobile";
  /** Clock for Today and Now. Defaults to the current instant. */
  now?: () => Date;
};

function emptyDraft(): Draft {
  return { date: null, time: null };
}

function toDraft(value: DateTimeValue | null): Draft {
  return value ? { date: value.date, time: value.time } : emptyDraft();
}

export function DateTimePicker({
  id,
  name,
  label,
  hint,
  error,
  value,
  defaultValue = null,
  onChange,
  min,
  max,
  hourCycle = "h12",
  minuteStep = 1,
  disabledTimes,
  isTimeDisabled,
  timeZone,
  ambiguity = "reject",
  disabled = false,
  readOnly = false,
  required = false,
  clearable = true,
  locale,
  placeholder = "Choose a date and time",
  viewport = "auto",
  now = () => new Date(),
}: DateTimePickerProps) {
  const autoId = useId();
  const fieldId = id ?? autoId;
  const triggerRef = useRef<HTMLButtonElement>(null);
  const surface = useViewport(viewport);
  const [committed, setCommitted] = useControllable({ value, defaultValue, onChange });
  const [open, setOpen] = useState(false);
  const [draft, setDraft] = useState<Draft>(toDraft(committed));
  const [step, setStep] = useState<"date" | "time">("date");
  const [touched, setTouched] = useState(false);
  const [shortcutError, setShortcutError] = useState<string | null>(null);
  const [entryError, setEntryError] = useState<string | null>(null);
  const timeEntryRef = useRef<TimeEntrySnapshot>({ value: committed?.time ?? null, error: null, pending: false });
  const locked = disabled || readOnly;
  const validationInput = { min, max, minuteStep, disabledTimes, isTimeDisabled, timeZone, ambiguity, required, locale, hourCycle };

  function inspect(next: Draft): DateTimeValidation {
    if (!next.date && !next.time) return validateDateTime({ value: null, ...validationInput });
    if (next.date && !parseCalendarDate(next.date)) {
      return { valid: false, code: "invalid-date", message: calendarDateMessage(next.date) };
    }
    if (!next.date) return { valid: false, code: "invalid-date", message: "Choose a date." };
    if (!next.time) return { valid: false, code: "invalid-time", message: "Choose a time." };
    return validateDateTime({ value: { date: next.date, time: next.time }, ...validationInput });
  }

  const draftValidation = inspect(draft);
  const committedValidation = inspect(toDraft(committed));
  const fieldError = error ?? (touched && !open && !committedValidation.valid ? committedValidation.message : undefined);
  const timeMin = draft.date && min && draft.date === min.date ? min.time : undefined;
  const timeMax = draft.date && max && draft.date === max.date ? max.time : undefined;
  const dateLabel = draft.date ? (formatCalendarDisplay(draft.date, locale) ?? "Invalid date") : "No date";
  const timeLabel = draft.time ? (formatTimeDisplay(draft.time, hourCycle, locale) ?? "Invalid time") : "No time";
  const preview = !draft.date && !draft.time ? "No date and time selected" : `${dateLabel}, ${timeLabel}${draftValidation.valid ? zonePreviewSuffix(draftValidation, timeZone) : ""}`;
  const shown = committed ? (formatDateTimeDisplay(committed, hourCycle, locale) ?? `${committed.date} ${committed.time}`) : placeholder;

  function openEditor() {
    if (locked) return;
    setDraft(toDraft(committed));
    setStep("date");
    setShortcutError(null);
    setEntryError(null);
    timeEntryRef.current = { value: committed?.time ?? null, error: null, pending: false };
    setOpen(true);
  }

  function cancel() {
    setDraft(toDraft(committed));
    setShortcutError(null);
    setOpen(false);
  }

  function apply() {
    const entry = timeEntryRef.current;
    if (entry.pending) {
      setEntryError(entry.error);
      setTouched(true);
      return;
    }
    const next = { date: draft.date, time: entry.value ?? draft.time };
    const result = inspect(next);
    if (!result.valid) {
      setTouched(true);
      return;
    }
    setDraft(next);
    setCommitted(next.date && next.time ? { date: next.date, time: next.time } : null);
    setTouched(true);
    setOpen(false);
  }

  function clear(event?: { stopPropagation: () => void }) {
    event?.stopPropagation();
    if (locked) return;
    setDraft(emptyDraft());
    setCommitted(null);
    setShortcutError(null);
    setOpen(false);
  }

  function applyToday() {
    const wall = currentWallClock(now(), timeZone);
    if ("error" in wall) {
      setShortcutError(wall.error === "unsupported" ? "This browser cannot resolve named time zones." : `"${timeZone}" is not a valid IANA time zone.`);
      return;
    }
    setShortcutError(null);
    setDraft((current) => ({ ...current, date: wall.date }));
  }

  function applyNow() {
    const result = zonedNow(now(), minuteStep, timeZone);
    if ("error" in result) {
      setShortcutError(result.error === "unsupported" ? "This browser cannot resolve named time zones." : `"${timeZone}" is not a valid IANA time zone.`);
      return;
    }
    setShortcutError(null);
    setDraft({ date: result.value.date, time: result.value.time });
  }

  const zoneClock = currentWallClock(now(), timeZone);
  const calendarToday = "error" in zoneClock ? todayCalendarDate() : zoneClock.date;
  const dateControl = (
    <DatePicker
      embedded
      id={`${fieldId}-date`}
      label="Date"
      value={draft.date}
      min={min && parseCalendarDate(min.date) ? min.date : undefined}
      max={max && parseCalendarDate(max.date) ? max.date : undefined}
      locale={locale}
      today={calendarToday}
      onChange={(date) => setDraft((current) => ({ ...current, date }))}
    />
  );

  const timeControl = (
    <div className={styles.time}>
      {surface === "mobile" ? (
        <TimeWheel
          value={draft.time}
          onChange={(time) => setDraft((current) => ({ ...current, time }))}
          hourCycle={hourCycle}
          minuteStep={minuteStep}
          min={timeMin}
          max={timeMax}
          disabledTimes={disabledTimes}
          isTimeDisabled={isTimeDisabled}
          locale={locale}
        />
      ) : null}
      <div className={chrome.typed} role="group" aria-label="Type a time">
        {surface === "mobile" ? <p className={chrome.typedLabel}>Type a time</p> : null}
        <TimeInput
          idPrefix={`${fieldId}-time`}
          value={draft.time}
          onChange={(time) => setDraft((current) => ({ ...current, time }))}
          hourCycle={hourCycle}
          minuteStep={minuteStep}
          locale={locale}
          onInvalidChange={setEntryError}
          onSnapshot={(snapshot) => {
            timeEntryRef.current = snapshot;
          }}
          hourLabel={surface === "mobile" ? "Hour typed" : "Hour"}
          minuteLabel={surface === "mobile" ? "Minute typed" : "Minute"}
        />
      </div>
    </div>
  );

  const editor = (
    <div className={styles.editor}>
      <p className={chrome.preview} aria-live="polite">
        {preview}
      </p>
      {surface === "mobile" ? (
        <>
          <SegmentedControl
            label="Step"
            name={`${fieldId}-step`}
            value={step}
            onChange={(next) => setStep(next)}
            options={[
              { value: "date", label: "Date" },
              { value: "time", label: "Time" },
            ]}
          />
          {step === "date" ? dateControl : timeControl}
        </>
      ) : (
        <div className={styles.split}>
          {dateControl}
          {timeControl}
        </div>
      )}
      {!draftValidation.valid && draftValidation.message ? (
        <p className={chrome.error} role="alert">
          {draftValidation.message}
        </p>
      ) : entryError ? (
        <p className={chrome.error} role="alert">
          {entryError}
        </p>
      ) : null}
      {shortcutError ? (
        <p className={chrome.error} role="alert">
          {shortcutError}
        </p>
      ) : null}
      <div className={chrome.actions}>
        <Button variant="secondary" className={chrome.action} onClick={applyToday}>
          Today
        </Button>
        <Button variant="secondary" className={chrome.action} onClick={applyNow}>
          Now
        </Button>
        {clearable ? (
          <Button variant="ghost" className={chrome.action} onClick={() => setDraft(emptyDraft())}>
            Clear
          </Button>
        ) : null}
        <Button variant="secondary" className={chrome.action} onClick={cancel}>
          Cancel
        </Button>
        <Button variant="primary" className={chrome.action} onClick={apply}>
          Apply
        </Button>
      </div>
    </div>
  );

  return (
    <Field id={fieldId} label={label} hint={fieldError ? undefined : hint} error={fieldError}>
      <div className={chrome.control} data-component="datetime-picker">
        <button
          ref={triggerRef}
          id={fieldId}
          type="button"
          className={chrome.trigger}
          data-empty={committed ? undefined : "true"}
          data-state={fieldError ? "error" : undefined}
          disabled={disabled}
          aria-readonly={readOnly || undefined}
          aria-label={`${label}, ${shown}`}
          aria-haspopup="dialog"
          aria-expanded={open}
          aria-invalid={fieldError ? true : undefined}
          aria-describedby={fieldError || hint ? `${fieldId}-message` : undefined}
          onClick={() => {
            if (open) cancel();
            else openEditor();
          }}
        >
          <span>{shown}</span>
          <ExIcon name="expand_more" />
        </button>
        {clearable && committed && !locked ? (
          <button type="button" className={chrome.clear} aria-label={`Clear ${label}`} onClick={clear}>
            <ExIcon name="close" />
          </button>
        ) : null}
        {name && committed ? <input type="hidden" name={name} value={`${committed.date}T${committed.time}`} /> : null}
      </div>
      {surface === "mobile" ? (
        <BottomSheet open={open} title={label} description="Choose a date and a time, then apply." onClose={cancel}>
          {editor}
        </BottomSheet>
      ) : (
        <AnchoredPopover open={open} anchorRef={triggerRef} label={label} onCancel={cancel} wide placementKey={`${draft.date ?? ""}|${draft.time ?? ""}`}>
          {editor}
        </AnchoredPopover>
      )}
    </Field>
  );
}

import { useId, useRef, useState } from "react";
import { ExIcon } from "../../icons/ExIcon";
import { ClockGlyph } from "../date/glyphs";
import { Button } from "../button/Button";
import { Field } from "../field/Field";
import { BottomSheet } from "../sheet/BottomSheet";
import { AnchoredPopover } from "./AnchoredPopover";
import { TimeInput, type TimeEntrySnapshot } from "./TimeInput";
import { TimeColumns } from "./TimeColumns";
import { TimeWheel } from "./TimeWheel";
import { floorMinute, formatTimeDisplay, formatTimeValue, validateTime, type HourCycle, type MinuteStep } from "./time";
import { useControllable } from "./useControllable";
import { useViewport } from "./useViewport";
import styles from "./TimePicker.module.scss";

export type TimePickerProps = {
  id?: string;
  name?: string;
  label: string;
  hint?: string;
  error?: string;
  /** Controlled `HH:mm` value. `null` is empty. Omit `value` for uncontrolled use. */
  value?: string | null;
  defaultValue?: string | null;
  /** Called when Apply commits a value or Clear removes it. Draft edits do not call this. */
  onChange?: (value: string | null) => void;
  hourCycle?: HourCycle;
  minuteStep?: MinuteStep;
  min?: string;
  max?: string;
  disabledTimes?: readonly string[];
  isTimeDisabled?: (time: string) => boolean;
  disabled?: boolean;
  readOnly?: boolean;
  required?: boolean;
  clearable?: boolean;
  locale?: string;
  placeholder?: string;
  /** `auto` uses a bottom sheet at 767px and below. */
  viewport?: "auto" | "desktop" | "mobile";
  /** Clock for Now. Defaults to the current instant. */
  now?: () => Date;
};

export function TimePicker({
  id,
  name,
  label,
  hint,
  error,
  value,
  defaultValue = null,
  onChange,
  hourCycle = "h12",
  minuteStep = 1,
  min,
  max,
  disabledTimes,
  isTimeDisabled,
  disabled = false,
  readOnly = false,
  required = false,
  clearable = true,
  locale,
  placeholder = "Choose a time",
  viewport = "auto",
  now,
}: TimePickerProps) {
  const autoId = useId();
  const fieldId = id ?? autoId;
  const triggerRef = useRef<HTMLButtonElement>(null);
  const surface = useViewport(viewport);
  const [committed, setCommitted] = useControllable({ value, defaultValue, onChange });
  const [open, setOpen] = useState(false);
  const [draft, setDraft] = useState<string | null>(committed);
  const [live, setLive] = useState<string | null>(null);
  const [touched, setTouched] = useState(false);
  const [entryError, setEntryError] = useState<string | null>(null);
  const entryRef = useRef<TimeEntrySnapshot>({ value: committed, error: null, pending: false });
  const validationInput = { minuteStep, min, max, disabledTimes, isTimeDisabled, required, locale };
  const draftValidation = validateTime({ value: draft, ...validationInput });
  const committedValidation = validateTime({ value: committed, ...validationInput });
  const fieldError = error ?? (touched && !open && !committedValidation.valid ? committedValidation.message : undefined);
  const locked = disabled || readOnly;

  function openEditor() {
    if (locked) return;
    setLive(null);
    setDraft(committed);
    setEntryError(null);
    entryRef.current = { value: committed, error: null, pending: false };
    setOpen(true);
  }

  function cancel() {
    setLive(null);
    setDraft(committed);
    setEntryError(null);
    setOpen(false);
  }

  function apply() {
    const entry = entryRef.current;
    if (entry.pending) {
      setEntryError(entry.error);
      setTouched(true);
      return;
    }
    const candidate = live ?? entry.value ?? draft;
    const result = validateTime({ value: candidate, ...validationInput });
    if (!result.valid) {
      setEntryError(result.message ?? null);
      setTouched(true);
      return;
    }
    setLive(null);
    setDraft(candidate);
    setCommitted(candidate);
    setEntryError(null);
    setTouched(true);
    setOpen(false);
  }

  function choose(next: string | null) {
    setLive(null);
    setDraft(next);
    entryRef.current = { value: next, error: null, pending: false };
  }

  function preview(next: string) {
    setLive((current) => (current === next ? current : next));
  }

  function applyNow() {
    const clock = (now ?? (() => new Date()))();
    choose(formatTimeValue({ hour: clock.getHours(), minute: floorMinute(clock.getMinutes(), minuteStep) }));
  }

  function clear(event: { stopPropagation: () => void }) {
    event.stopPropagation();
    if (locked) return;
    setDraft(null);
    setCommitted(null);
    setOpen(false);
  }

  const active = open ? (live ?? draft) : committed;
  const activeLabel = active ? (formatTimeDisplay(active, hourCycle, locale) ?? active) : null;
  const shown = activeLabel ?? placeholder;
  const editor = (
    <div className={styles.editor}>
      <p className={styles.preview} aria-live="polite">
        {activeLabel ?? "No time selected"}
      </p>
      {surface === "mobile" ? (
        <>
          <TimeWheel
            value={draft}
            onChange={(next) => {
              setLive(null);
              setDraft(next);
            }}
            onPreview={preview}
            hourCycle={hourCycle}
            minuteStep={minuteStep}
            min={min}
            max={max}
            disabledTimes={disabledTimes}
            isTimeDisabled={isTimeDisabled}
            locale={locale}
          />
          <div className={styles.typed} role="group" aria-label="Type a time">
            <p className={styles.typedLabel}>Type a time</p>
            <TimeInput
              value={draft}
              onChange={(next) => {
                setLive(null);
                setDraft(next);
              }}
              hourCycle={hourCycle}
              minuteStep={minuteStep}
              locale={locale}
              hideMessage
              onInvalidChange={setEntryError}
              onSnapshot={(snapshot) => {
                entryRef.current = snapshot;
                setEntryError(null);
              }}
              hourLabel="Hour typed"
              minuteLabel="Minute typed"
            />
          </div>
        </>
      ) : (
        <TimeColumns
          value={draft}
          onChange={choose}
          onPreview={preview}
          hourCycle={hourCycle}
          minuteStep={minuteStep}
          min={min}
          max={max}
          disabledTimes={disabledTimes}
          isTimeDisabled={isTimeDisabled}
          locale={locale}
        />
      )}
      {entryRef.current.pending && entryError ? (
        <p className={styles.error} role="alert">
          {entryError}
        </p>
      ) : !draftValidation.valid && draftValidation.message ? (
        <p className={styles.error} role="alert">
          {draftValidation.message}
        </p>
      ) : entryError ? (
        <p className={styles.error} role="alert">
          {entryError}
        </p>
      ) : null}
      <div className={surface === "desktop" ? `${styles.actions} ${styles.actionsGrid}` : styles.actions}>
        {clearable ? (
          <Button variant="secondary" appearance="outline" className={styles.action} onClick={() => choose(null)}>
            Clear
          </Button>
        ) : null}
        <Button variant="secondary" appearance="outline" className={styles.action} onClick={cancel}>
          Cancel
        </Button>
        {surface === "desktop" ? (
          <Button variant="primary" appearance="soft" className={styles.action} onClick={applyNow}>
            Now
          </Button>
        ) : null}
        <Button variant="primary" appearance="solid" className={styles.action} onClick={apply}>
          Apply
        </Button>
      </div>
    </div>
  );

  return (
    <Field id={fieldId} label={label} hint={fieldError ? undefined : hint} error={fieldError}>
      <div className={styles.control} data-component="time-picker">
        <span className={styles.icon}>
          <ClockGlyph />
        </span>
        <button
          ref={triggerRef}
          id={fieldId}
          type="button"
          className={styles.trigger}
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
          <span className={activeLabel ? styles.value : `${styles.value} ${styles.placeholder}`}>{shown}</span>
        </button>
        {clearable && committed && !locked ? (
          <button type="button" className={styles.clear} aria-label={`Clear ${label}`} onClick={clear}>
            <ExIcon name="close" />
          </button>
        ) : null}
        {name ? <input type="hidden" name={name} value={committed ?? ""} /> : null}
      </div>
      {surface === "mobile" ? (
        <BottomSheet open={open} title={label} description="Scroll the wheel or type a time. Apply saves it." onClose={cancel}>
          {editor}
        </BottomSheet>
      ) : (
        <AnchoredPopover open={open} anchorRef={triggerRef} label={label} onCancel={cancel} placementKey={draft ?? ""}>
          {editor}
        </AnchoredPopover>
      )}
    </Field>
  );
}

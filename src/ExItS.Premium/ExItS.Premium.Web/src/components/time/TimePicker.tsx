import { useId, useRef, useState } from "react";
import { ExIcon } from "../../icons/ExIcon";
import { Button } from "../button/Button";
import { Field } from "../field/Field";
import { BottomSheet } from "../sheet/BottomSheet";
import { AnchoredPopover } from "./AnchoredPopover";
import { TimeInput, type TimeEntrySnapshot } from "./TimeInput";
import { TimeWheel } from "./TimeWheel";
import { formatTimeDisplay, validateTime, type HourCycle, type MinuteStep } from "./time";
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
}: TimePickerProps) {
  const autoId = useId();
  const fieldId = id ?? autoId;
  const triggerRef = useRef<HTMLButtonElement>(null);
  const surface = useViewport(viewport);
  const [committed, setCommitted] = useControllable({ value, defaultValue, onChange });
  const [open, setOpen] = useState(false);
  const [draft, setDraft] = useState<string | null>(committed);
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
    setDraft(committed);
    setEntryError(null);
    entryRef.current = { value: committed, error: null, pending: false };
    setOpen(true);
  }

  function cancel() {
    setDraft(committed);
    setOpen(false);
  }

  function apply() {
    const entry = entryRef.current;
    if (entry.pending) {
      setEntryError(entry.error);
      setTouched(true);
      return;
    }
    const candidate = entry.value ?? draft;
    const result = validateTime({ value: candidate, ...validationInput });
    if (!result.valid) {
      setEntryError(result.message ?? null);
      setTouched(true);
      return;
    }
    setDraft(candidate);
    setCommitted(candidate);
    setTouched(true);
    setOpen(false);
  }

  function clear(event: { stopPropagation: () => void }) {
    event.stopPropagation();
    if (locked) return;
    setDraft(null);
    setCommitted(null);
    setOpen(false);
  }

  const shown = committed ? (formatTimeDisplay(committed, hourCycle, locale) ?? committed) : placeholder;
  const editor = (
    <div className={styles.editor}>
      <p className={styles.preview} aria-live="polite">
        {draft ? (formatTimeDisplay(draft, hourCycle, locale) ?? draft) : "No time selected"}
      </p>
      {surface === "mobile" ? (
        <TimeWheel
          value={draft}
          onChange={setDraft}
          hourCycle={hourCycle}
          minuteStep={minuteStep}
          min={min}
          max={max}
          disabledTimes={disabledTimes}
          isTimeDisabled={isTimeDisabled}
          locale={locale}
        />
      ) : null}
      <div className={styles.typed} role="group" aria-label="Type a time">
        {surface === "mobile" ? <p className={styles.typedLabel}>Type a time</p> : null}
        <TimeInput
          value={draft}
          onChange={setDraft}
          hourCycle={hourCycle}
          minuteStep={minuteStep}
          locale={locale}
          autoFocus={surface === "desktop"}
          onInvalidChange={setEntryError}
          onSnapshot={(snapshot) => {
            entryRef.current = snapshot;
          }}
          hourLabel={surface === "mobile" ? "Hour typed" : "Hour"}
          minuteLabel={surface === "mobile" ? "Minute typed" : "Minute"}
        />
      </div>
      {!draftValidation.valid && draftValidation.message ? (
        <p className={styles.error} role="alert">
          {draftValidation.message}
        </p>
      ) : entryError ? (
        <p className={styles.error} role="alert">
          {entryError}
        </p>
      ) : null}
      <div className={styles.actions}>
        {clearable ? (
          <Button variant="ghost" className={styles.action} onClick={() => setDraft(null)}>
            Clear
          </Button>
        ) : null}
        <Button variant="secondary" className={styles.action} onClick={cancel}>
          Cancel
        </Button>
        <Button variant="primary" className={styles.action} onClick={apply}>
          Apply
        </Button>
      </div>
    </div>
  );

  return (
    <Field id={fieldId} label={label} hint={fieldError ? undefined : hint} error={fieldError}>
      <div className={styles.control} data-component="time-picker">
        <button
          ref={triggerRef}
          id={fieldId}
          type="button"
          className={styles.trigger}
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

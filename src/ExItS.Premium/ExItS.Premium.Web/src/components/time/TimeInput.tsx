import { useEffect, useId, useRef, useState, type ClipboardEvent, type KeyboardEvent } from "react";
import {
  dayPeriodLabel,
  formatTimeValue,
  fromDayPeriod,
  parseTimeEntry,
  parseTimeValue,
  toDayPeriod,
  type DayPeriod,
  type HourCycle,
  type MinuteStep,
} from "./time";
import styles from "./TimeInput.module.scss";

export type TimeEntrySnapshot = {
  /** Parsed `HH:mm` when the typed fields are complete. `null` when they are empty or not yet valid. */
  value: string | null;
  /** The fields contain text that is not a valid time yet. Apply must not commit the previous draft. */
  pending: boolean;
  error: string | null;
};

export type TimeInputProps = {
  value: string | null;
  onChange: (value: string) => void;
  hourCycle?: HourCycle;
  minuteStep?: MinuteStep;
  disabled?: boolean;
  readOnly?: boolean;
  locale?: string;
  hourLabel?: string;
  minuteLabel?: string;
  idPrefix?: string;
  autoFocus?: boolean;
  onInvalidChange?: (message: string | null) => void;
  /** Latest typed entry, including incomplete text. Used so Apply does not wait for blur. */
  onSnapshot?: (snapshot: TimeEntrySnapshot) => void;
};

function readTimeEntry(
  nextHour: string,
  nextMinute: string,
  nextPeriod: DayPeriod | null,
  hourCycle: HourCycle,
  minuteStep: MinuteStep,
): TimeEntrySnapshot {
  if (!nextHour.trim() && !nextMinute.trim()) return { value: null, error: null, pending: false };
  if (!nextHour.trim() || !nextMinute.trim()) return { value: null, error: "Enter an hour and minute.", pending: true };
  const combined = `${nextHour}:${nextMinute}${hourCycle === "h12" && nextPeriod ? ` ${nextPeriod}` : ""}`;
  const parsed = parseTimeEntry(combined, hourCycle);
  if (!parsed) {
    return {
      value: null,
      pending: true,
      error: hourCycle === "h12" ? "Enter a time with AM or PM, such as 9:30 AM." : "Enter a time from 00:00 to 23:59.",
    };
  }
  if (parsed.minute % minuteStep !== 0) {
    return { value: null, pending: true, error: `Minutes must be in steps of ${minuteStep}.` };
  }
  return { value: formatTimeValue(parsed), error: null, pending: false };
}

export function TimeInput({
  value,
  onChange,
  hourCycle = "h24",
  minuteStep = 1,
  disabled = false,
  readOnly = false,
  locale,
  hourLabel = "Hour",
  minuteLabel = "Minute",
  idPrefix,
  autoFocus = false,
  onInvalidChange,
  onSnapshot,
}: TimeInputProps) {
  const autoId = useId();
  const prefix = idPrefix ?? autoId;
  const parts = parseTimeValue(value);
  const period = parts ? toDayPeriod(parts.hour).period : null;
  const track = `${value ?? ""}|${hourCycle}`;
  const initialParts = parseTimeValue(value);
  const [tracked, setTracked] = useState(track);
  const [hourText, setHourText] = useState(
    initialParts ? (hourCycle === "h24" ? String(initialParts.hour).padStart(2, "0") : String(toDayPeriod(initialParts.hour).hour12)) : "",
  );
  const [minuteText, setMinuteText] = useState(initialParts ? String(initialParts.minute).padStart(2, "0") : "");
  const [error, setError] = useState<string | null>(null);

  if (tracked !== track) {
    setTracked(track);
    const parsed = parseTimeValue(value);
    setError(null);
    if (!parsed) {
      setHourText("");
      setMinuteText("");
    } else {
      setHourText(hourCycle === "h24" ? String(parsed.hour).padStart(2, "0") : String(toDayPeriod(parsed.hour).hour12));
      setMinuteText(String(parsed.minute).padStart(2, "0"));
    }
  }

  const onSnapshotRef = useRef(onSnapshot);
  useEffect(() => {
    onSnapshotRef.current = onSnapshot;
  }, [onSnapshot]);

  function publish(nextHour: string, nextMinute: string, nextPeriod: DayPeriod | null) {
    const snapshot = readTimeEntry(nextHour, nextMinute, nextPeriod, hourCycle, minuteStep);
    onSnapshotRef.current?.(snapshot);
    setError(snapshot.error);
  }

  useEffect(() => {
    onSnapshotRef.current?.(readTimeEntry(hourText, minuteText, period, hourCycle, minuteStep));
  }, [hourText, minuteText, period, hourCycle, minuteStep]);

  useEffect(() => {
    onInvalidChange?.(error);
  }, [error, onInvalidChange]);

  function emit(hour24: number, minute: number) {
    if (minute < 0 || minute > 59 || minute % minuteStep !== 0) {
      setError(minute % minuteStep !== 0 ? `Minutes must be in steps of ${minuteStep}.` : "Minutes must be between 0 and 59.");
      return;
    }
    setError(null);
    onChange(formatTimeValue({ hour: hour24, minute }));
  }

  function commitTexts(nextHour: string, nextMinute: string, nextPeriod: DayPeriod | null) {
    if (!nextHour.trim() || !nextMinute.trim()) {
      setError("Enter an hour and minute.");
      return;
    }
    const combined = `${nextHour}:${nextMinute}${hourCycle === "h12" && nextPeriod ? ` ${nextPeriod}` : ""}`;
    const parsed = parseTimeEntry(combined, hourCycle);
    if (!parsed) {
      setError(hourCycle === "h12" ? "Enter a time with AM or PM, such as 9:30 AM." : "Enter a time from 00:00 to 23:59.");
      return;
    }
    emit(parsed.hour, parsed.minute);
  }

  function step(part: "hour" | "minute", direction: -1 | 1) {
    if (disabled || readOnly) return;
    const current = parts ?? { hour: 0, minute: 0 };
    if (part === "hour") {
      const hour = (current.hour + direction + 24) % 24;
      emit(hour, current.minute);
      return;
    }
    const total = current.hour * 60 + current.minute + direction * minuteStep;
    const wrapped = ((total % 1440) + 1440) % 1440;
    emit(Math.floor(wrapped / 60), wrapped % 60);
  }

  function onHourKeyDown(event: KeyboardEvent<HTMLInputElement>) {
    if (event.key === "ArrowUp" || event.key === "ArrowDown") {
      event.preventDefault();
      step("hour", event.key === "ArrowUp" ? 1 : -1);
    } else if (event.key === "Enter") {
      event.preventDefault();
      commitTexts(hourText, minuteText, period);
    }
  }

  function onMinuteKeyDown(event: KeyboardEvent<HTMLInputElement>) {
    if (event.key === "ArrowUp" || event.key === "ArrowDown") {
      event.preventDefault();
      step("minute", event.key === "ArrowUp" ? 1 : -1);
    } else if (event.key === "Enter") {
      event.preventDefault();
      commitTexts(hourText, minuteText, period);
    }
  }

  function onPaste(event: ClipboardEvent<HTMLInputElement>) {
    const parsed = parseTimeEntry(event.clipboardData.getData("text"), hourCycle);
    if (!parsed) return;
    event.preventDefault();
    emit(parsed.hour, parsed.minute);
  }

  function selectPeriod(next: DayPeriod) {
    if (disabled || readOnly) return;
    const hour12 = parts ? toDayPeriod(parts.hour).hour12 : Number(hourText) || 12;
    const minute = parts?.minute ?? (minuteText === "" ? 0 : Number(minuteText));
    if (!Number.isInteger(hour12) || hour12 < 1 || hour12 > 12 || !Number.isInteger(minute)) {
      setError("Enter the hour and minute before choosing AM or PM.");
      return;
    }
    emit(fromDayPeriod(hour12, next), minute);
  }

  const locked = disabled || readOnly;

  return (
    <div className={styles.entry}>
      <div className={styles.row}>
        <label className={styles.part} htmlFor={`${prefix}-hour`}>
          <span>{hourLabel}</span>
          <input
            id={`${prefix}-hour`}
            className={styles.input}
            inputMode="numeric"
            autoComplete="off"
            spellCheck={false}
            maxLength={2}
            disabled={disabled}
            readOnly={readOnly}
            aria-invalid={error ? true : undefined}
            data-autofocus={autoFocus ? "" : undefined}
            value={hourText}
            onChange={(event) => {
              const next = event.target.value.replace(/\D/g, "").slice(0, 2);
              setHourText(next);
              publish(next, minuteText, period);
            }}
            onBlur={(event) => {
              const next = event.relatedTarget;
              if (next instanceof Node && event.currentTarget.closest(`.${styles.entry}`)?.contains(next)) return;
              if (hourText || minuteText) commitTexts(hourText, minuteText, period);
            }}
            onKeyDown={onHourKeyDown}
            onPaste={onPaste}
            onFocus={(event) => {
              if (typeof event.currentTarget.scrollIntoView === "function") {
                event.currentTarget.scrollIntoView({ block: "center" });
              }
            }}
          />
        </label>
        <span className={styles.colon} aria-hidden="true">
          :
        </span>
        <label className={styles.part} htmlFor={`${prefix}-minute`}>
          <span>{minuteLabel}</span>
          <input
            id={`${prefix}-minute`}
            className={styles.input}
            inputMode="numeric"
            autoComplete="off"
            spellCheck={false}
            maxLength={2}
            disabled={disabled}
            readOnly={readOnly}
            aria-invalid={error ? true : undefined}
            value={minuteText}
            onChange={(event) => {
              const next = event.target.value.replace(/\D/g, "").slice(0, 2);
              setMinuteText(next);
              publish(hourText, next, period);
            }}
            onBlur={(event) => {
              const next = event.relatedTarget;
              if (next instanceof Node && event.currentTarget.closest(`.${styles.entry}`)?.contains(next)) return;
              if (hourText || minuteText) commitTexts(hourText, minuteText, period);
            }}
            onKeyDown={onMinuteKeyDown}
            onPaste={onPaste}
            onFocus={(event) => {
              if (typeof event.currentTarget.scrollIntoView === "function") {
                event.currentTarget.scrollIntoView({ block: "center" });
              }
            }}
          />
        </label>
        {hourCycle === "h12" ? (
          <div className={styles.period} role="radiogroup" aria-label="AM or PM">
            {(["AM", "PM"] as const).map((item) => {
              const selected = period === item;
              return (
                <button
                  key={item}
                  type="button"
                  role="radio"
                  aria-checked={selected}
                  disabled={locked}
                  className={styles.periodButton}
                  onClick={() => selectPeriod(item)}
                >
                  {dayPeriodLabel(item, locale)}
                </button>
              );
            })}
          </div>
        ) : null}
      </div>
      {error ? (
        <p className={styles.error} role="alert">
          {error}
        </p>
      ) : (
        <p className={styles.hint}>Arrow keys step the focused value. Paste a time such as 9:30 AM or 21:30.</p>
      )}
    </div>
  );
}

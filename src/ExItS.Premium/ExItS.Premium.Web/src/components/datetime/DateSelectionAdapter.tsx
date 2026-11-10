import { useId, type ComponentType } from "react";
import { Field } from "../field/Field";
import styles from "./DateSelectionAdapter.module.scss";

/**
 * Temporary date-selection contract for DateTimePicker.
 *
 * INTEGRATION POINT — Agent 2 DatePicker
 * --------------------------------------
 * Do not import Agent 2's branch. The Integration Agent replaces the temporary
 * native date field by either:
 * 1. Passing Agent 2's DatePicker as `dateAdapter` on `DateTimePicker`, or
 * 2. Changing `DATE_TIME_DATE_ADAPTER` in `DateTimePicker.tsx` only.
 *
 * The replacement must honor this prop contract:
 * - `value` / `onChange`: local calendar `YYYY-MM-DD` or `null`
 * - never a UTC timestamp, `Date` instance, or `YYYY-MM-DDT00:00:00.000Z`
 * - `min` / `max`: inclusive calendar dates, date portion only
 * - `disabled`, `readOnly`, `locale`, `id`, `label`
 *
 * `NativeDateAdapter` below is the stand-in used until that connection.
 */
export type DateSelectionAdapterProps = {
  id?: string;
  label?: string;
  value: string | null;
  onChange: (value: string | null) => void;
  min?: string;
  max?: string;
  disabled?: boolean;
  readOnly?: boolean;
  locale?: string;
};

export function NativeDateAdapter({
  id,
  label = "Date",
  value,
  onChange,
  min,
  max,
  disabled = false,
  readOnly = false,
  locale,
}: DateSelectionAdapterProps) {
  const autoId = useId();
  const fieldId = id ?? autoId;
  return (
    <Field id={fieldId} label={label}>
      <input
        id={fieldId}
        className={styles.input}
        type="date"
        lang={locale}
        data-adapter="native-date"
        value={value ?? ""}
        min={min}
        max={max}
        disabled={disabled}
        readOnly={readOnly}
        aria-readonly={readOnly || undefined}
        onChange={(event) => {
          if (readOnly) return;
          const next = event.target.value;
          onChange(next === "" ? null : next);
        }}
      />
    </Field>
  );
}

export type DateSelectionAdapter = ComponentType<DateSelectionAdapterProps>;

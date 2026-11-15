import { useRef } from "react";
import { TimeWheel } from "./TimeWheel";
import { formatTimeValue, fromDayPeriod, parseTimeEntry, parseTimeValue, toDayPeriod, type HourCycle, type MinuteStep } from "./time";
import styles from "./TimePicker.module.scss";

type TimeColumnsProps = {
  value: string | null;
  onChange: (value: string) => void;
  hourCycle?: HourCycle;
  minuteStep?: MinuteStep;
  min?: string;
  max?: string;
  disabledTimes?: readonly string[];
  isTimeDisabled?: (time: string) => boolean;
  locale?: string;
  onPreview?: (value: string) => void;
};

export function TimeColumns({
  value,
  onChange,
  hourCycle = "h12",
  minuteStep = 1,
  min,
  max,
  disabledTimes,
  isTimeDisabled,
  locale,
  onPreview,
}: TimeColumnsProps) {
  const digitBuffer = useRef<{ column: string; text: string }>({ column: "", text: "" });

  function typeDigit(column: "Hour" | "Minute", digit: string) {
    const buffer = digitBuffer.current;
    if (buffer.column !== column) buffer.text = "";
    buffer.column = column;
    buffer.text = `${buffer.text}${digit}`.slice(-2);
    const parts = parseTimeValue(value) ?? { hour: 0, minute: 0 };
    if (column === "Hour") {
      const hourNumber = Number(buffer.text);
      const finished = buffer.text.length === 2 || (hourCycle === "h24" ? hourNumber > 2 : hourNumber > 1);
      if (!finished) return;
      if (hourCycle === "h12") {
        if (hourNumber < 1 || hourNumber > 12) return;
        onChange(formatTimeValue({ hour: fromDayPeriod(hourNumber, toDayPeriod(parts.hour).period), minute: parts.minute }));
      } else if (hourNumber <= 23) {
        onChange(formatTimeValue({ hour: hourNumber, minute: parts.minute }));
      }
      buffer.text = "";
      return;
    }
    const minute = Number(buffer.text);
    const finished = buffer.text.length === 2 || minute > 5;
    if (!finished || minute > 59) return;
    onChange(formatTimeValue({ hour: parts.hour, minute }));
    buffer.text = "";
  }

  return (
    <div
      className={styles.columns}
      onPaste={(event) => {
        const parsed = parseTimeEntry(event.clipboardData.getData("text"), hourCycle);
        if (!parsed) return;
        event.preventDefault();
        onChange(formatTimeValue(parsed));
      }}
      onKeyDown={(event) => {
        if (!/^\d$/.test(event.key)) return;
        const list = (event.target as HTMLElement).closest("[role='listbox']");
        const labelledBy = list?.getAttribute("aria-labelledby");
        const name = labelledBy ? document.getElementById(labelledBy)?.textContent : list?.getAttribute("aria-label");
        if (name !== "Hour" && name !== "Minute") return;
        event.preventDefault();
        typeDigit(name, event.key);
      }}
    >
      <TimeWheel
        size="compact"
        value={value}
        onChange={onChange}
        onPreview={onPreview}
        hourCycle={hourCycle}
        minuteStep={minuteStep}
        min={min}
        max={max}
        disabledTimes={disabledTimes}
        isTimeDisabled={isTimeDisabled}
        locale={locale}
      />
    </div>
  );
}

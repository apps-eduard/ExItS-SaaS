import { useEffect, useId, useLayoutEffect, useRef } from "react";
import {
  dayPeriodLabel,
  firstAllowedMinute,
  formatHourLabel,
  formatMinuteLabel,
  formatTimeValue,
  fromDayPeriod,
  indexFromScroll,
  isAllowedTime,
  minuteOptions,
  nearestEnabledIndex,
  parseTimeValue,
  toDayPeriod,
  type DayPeriod,
  type HourCycle,
  type MinuteStep,
  type TimeConstraints,
} from "./time";
import styles from "./TimeWheel.module.scss";

export const WHEEL_ITEM_PX = 44;

type WheelOption = {
  value: string;
  label: string;
  disabled: boolean;
};

type TimeWheelProps = {
  value: string | null;
  onChange: (value: string) => void;
  hourCycle?: HourCycle;
  minuteStep?: MinuteStep;
  min?: string;
  max?: string;
  disabledTimes?: readonly string[];
  isTimeDisabled?: (time: string) => boolean;
  disabled?: boolean;
  readOnly?: boolean;
  locale?: string;
};

function itemSize(scroller: HTMLElement): number {
  const item = scroller.querySelector<HTMLElement>("[data-wheel-item]");
  const measured = item?.getBoundingClientRect().height ?? 0;
  return measured > 0 ? measured : WHEEL_ITEM_PX;
}

function WheelColumn({
  label,
  options,
  selected,
  onSelect,
  disabled = false,
  readOnly = false,
  autoFocus = false,
}: {
  label: string;
  options: WheelOption[];
  selected: string | null;
  onSelect: (value: string) => void;
  disabled?: boolean;
  readOnly?: boolean;
  autoFocus?: boolean;
}) {
  const scrollerRef = useRef<HTMLDivElement>(null);
  const syncing = useRef(false);
  const onSelectRef = useRef(onSelect);
  const listId = useId();
  const selectedIndex = options.findIndex((option) => option.value === selected);
  const optionKey = options.map((option) => `${option.value}:${option.disabled ? 1 : 0}`).join("|");
  const optionsRef = useRef(options);

  useEffect(() => {
    onSelectRef.current = onSelect;
    optionsRef.current = options;
  }, [onSelect, options]);

  useLayoutEffect(() => {
    const scroller = scrollerRef.current;
    if (!scroller || selectedIndex < 0) return;
    const top = selectedIndex * itemSize(scroller);
    if (Math.abs(scroller.scrollTop - top) <= 1) return;
    syncing.current = true;
    scroller.scrollTop = top;
    const frame = window.requestAnimationFrame(() => {
      syncing.current = false;
    });
    return () => {
      window.cancelAnimationFrame(frame);
      syncing.current = false;
    };
  }, [optionKey, selectedIndex]);

  useEffect(() => {
    const scroller = scrollerRef.current;
    if (!scroller) return;
    let timer = 0;
    const settle = () => {
      if (syncing.current || disabled || readOnly) return;
      const current = optionsRef.current;
      const size = itemSize(scroller);
      const landed = indexFromScroll(scroller.scrollTop, size, current.length);
      const enabled = nearestEnabledIndex(
        landed,
        current.map((option) => option.disabled),
        0,
      );
      if (enabled == null) return;
      const top = enabled * size;
      if (Math.abs(scroller.scrollTop - top) > 1) {
        syncing.current = true;
        scroller.scrollTop = top;
        window.requestAnimationFrame(() => {
          syncing.current = false;
        });
      }
      const next = current[enabled];
      if (next && next.value !== selected) onSelectRef.current(next.value);
    };
    const onScroll = () => {
      if (syncing.current) return;
      window.clearTimeout(timer);
      timer = window.setTimeout(settle, 80);
    };
    scroller.addEventListener("scroll", onScroll, { passive: true });
    scroller.addEventListener("scrollend", settle);
    return () => {
      window.clearTimeout(timer);
      scroller.removeEventListener("scroll", onScroll);
      scroller.removeEventListener("scrollend", settle);
    };
  }, [disabled, readOnly, selected]);

  function move(direction: -1 | 1) {
    if (disabled || readOnly) return;
    const flags = options.map((option) => option.disabled);
    const origin = selectedIndex < 0 ? (direction > 0 ? -1 : options.length) : selectedIndex;
    const index = nearestEnabledIndex(origin + direction, flags, direction);
    if (index == null) return;
    onSelect(options[index].value);
  }

  return (
    <div className={styles.column}>
      <div className={styles.columnLabel} id={`${listId}-label`}>
        {label}
      </div>
      <div className={styles.viewport}>
        <div className={styles.band} aria-hidden="true" />
        <div
          ref={scrollerRef}
          className={styles.scroller}
          role="listbox"
          data-autofocus={autoFocus ? "" : undefined}
          aria-labelledby={`${listId}-label`}
          aria-activedescendant={selectedIndex >= 0 ? `${listId}-${selectedIndex}` : undefined}
          aria-disabled={disabled || undefined}
          aria-readonly={readOnly || undefined}
          tabIndex={disabled ? -1 : 0}
          onKeyDown={(event) => {
            if (event.key === "ArrowDown") {
              event.preventDefault();
              move(1);
            } else if (event.key === "ArrowUp") {
              event.preventDefault();
              move(-1);
            } else if (event.key === "Home") {
              event.preventDefault();
              const index = nearestEnabledIndex(0, options.map((option) => option.disabled), 1);
              if (index != null) onSelect(options[index].value);
            } else if (event.key === "End") {
              event.preventDefault();
              const index = nearestEnabledIndex(options.length - 1, options.map((option) => option.disabled), -1);
              if (index != null) onSelect(options[index].value);
            }
          }}
        >
          {options.map((option, index) => {
            const isSelected = option.value === selected;
            return (
              <button
                key={option.value}
                id={`${listId}-${index}`}
                type="button"
                role="option"
                data-wheel-item=""
                className={styles.option}
                aria-selected={isSelected}
                disabled={disabled || option.disabled}
                tabIndex={-1}
                onClick={() => {
                  if (readOnly || option.disabled) return;
                  onSelect(option.value);
                }}
              >
                {option.label}
              </button>
            );
          })}
        </div>
      </div>
    </div>
  );
}

export function TimeWheel({
  value,
  onChange,
  hourCycle = "h24",
  minuteStep = 1,
  min,
  max,
  disabledTimes,
  isTimeDisabled,
  disabled = false,
  readOnly = false,
  locale,
}: TimeWheelProps) {
  const parts = parseTimeValue(value);
  const constraints: TimeConstraints = { minuteStep, min, max, disabledTimes, isTimeDisabled };
  const period = parts ? toDayPeriod(parts.hour).period : null;

  function commit(hour24: number, minute: number) {
    if (disabled || readOnly) return;
    let nextMinute = minute;
    const candidate = formatTimeValue({ hour: hour24, minute: nextMinute });
    if (!isAllowedTime(candidate, constraints)) {
      const allowed = firstAllowedMinute(hour24, constraints);
      if (allowed == null) return;
      nextMinute = allowed;
    }
    onChange(formatTimeValue({ hour: hour24, minute: nextMinute }));
  }

  const hours: WheelOption[] =
    hourCycle === "h24"
      ? Array.from({ length: 24 }, (_, hour) => ({
          value: String(hour),
          label: formatHourLabel(hour, "h24", locale),
          disabled: firstAllowedMinute(hour, constraints) == null,
        }))
      : Array.from({ length: 12 }, (_, index) => {
          const hour12 = index + 1;
          const hour24 = fromDayPeriod(hour12, period ?? "AM");
          return {
            value: String(hour12),
            label: formatHourLabel(hour12, "h12", locale),
            disabled: firstAllowedMinute(hour24, constraints) == null,
          };
        });

  const minutes: WheelOption[] = minuteOptions(minuteStep).map((minute) => ({
    value: String(minute),
    label: formatMinuteLabel(minute, locale),
    disabled: parts ? !isAllowedTime(formatTimeValue({ hour: parts.hour, minute }), constraints) : false,
  }));

  const periods: WheelOption[] = (["AM", "PM"] as const).map((item) => {
    const hour12 = parts ? toDayPeriod(parts.hour).hour12 : 12;
    const minute = parts?.minute ?? 0;
    return {
      value: item,
      label: dayPeriodLabel(item, locale),
      disabled: parts ? !isAllowedTime(formatTimeValue({ hour: fromDayPeriod(hour12, item), minute }), constraints) : false,
    };
  });

  return (
    <div className={styles.wheels} data-columns={hourCycle === "h12" ? "3" : "2"} aria-label="Time wheel">
      <WheelColumn
        autoFocus
        label="Hour"
        options={hours}
        selected={parts ? (hourCycle === "h24" ? String(parts.hour) : String(toDayPeriod(parts.hour).hour12)) : null}
        disabled={disabled}
        readOnly={readOnly}
        onSelect={(next) => {
          const hourNumber = Number(next);
          const minute = parts?.minute ?? 0;
          const hour24 = hourCycle === "h24" ? hourNumber : fromDayPeriod(hourNumber, period ?? "AM");
          commit(hour24, minute);
        }}
      />
      <WheelColumn
        label="Minute"
        options={minutes}
        selected={parts ? String(parts.minute) : null}
        disabled={disabled}
        readOnly={readOnly}
        onSelect={(next) => commit(parts?.hour ?? (hourCycle === "h12" ? 0 : 0), Number(next))}
      />
      {hourCycle === "h12" ? (
        <WheelColumn
          label="AM or PM"
          options={periods}
          selected={period}
          disabled={disabled}
          readOnly={readOnly}
          onSelect={(next) => {
            const hour12 = parts ? toDayPeriod(parts.hour).hour12 : 12;
            commit(fromDayPeriod(hour12, next as DayPeriod), parts?.minute ?? 0);
          }}
        />
      ) : null}
    </div>
  );
}

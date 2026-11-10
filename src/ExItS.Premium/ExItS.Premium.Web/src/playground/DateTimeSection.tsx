import { useState } from "react";
import { DatePicker, DateRangePicker, type CalendarDate, type DateRange } from "../components/date";
import { weekdayIndex } from "../components/date/calendar";
import styles from "./Playground.module.scss";

/**
 * Shared Date & Time playground section.
 * TimePicker and DateTimePicker belong here when they are ready.
 */
function isWeekend(date: CalendarDate) {
  const weekday = weekdayIndex(date);
  return weekday === 0 || weekday === 6;
}

export function DateTimeSection() {
  const [date, setDate] = useState<CalendarDate | null>("2026-11-10");
  const [range, setRange] = useState<DateRange | null>({ start: "2026-11-02", end: "2026-11-08" });
  const [bounded, setBounded] = useState<CalendarDate | null>("2026-11-12");

  return (
    <>
      <header className={styles.intro}>
        <h1 className="type-page">Date & Time Pickers</h1>
        <p className="type-secondary">
          Date and date range fields use the live appearance. On a phone the calendar opens in a bottom sheet. On a wider screen it opens in a popover. Time and date-time pickers will join this section.
        </p>
      </header>
      <section className={styles.stageBlock}>
        <h2 className="type-section">Default selection</h2>
        <div className={styles.stage}>
          <DatePicker label="Date" value={date} onChange={setDate} locale="en-US" hint="Choose a day. Today jumps to the current date." />
          <DateRangePicker
            label="Date range"
            value={range}
            onChange={setRange}
            locale="en-US"
            weekStartsOn={0}
            hint="Apply commits the range. Cancel restores it. Clear empties the draft."
          />
        </div>
      </section>
      <section className={styles.stageBlock}>
        <h2 className="type-section">Minimum, maximum, and disabled dates</h2>
        <div className={styles.stage}>
          <DatePicker
            label="Bounded date"
            value={bounded}
            onChange={setBounded}
            min="2026-11-01"
            max="2026-11-30"
            disabledDates={["2026-11-15"]}
            locale="en-US"
            hint="November 2026 only. The 15th is disabled."
          />
          <DateRangePicker label="Weekdays" locale="en-US" weekStartsOn={1} isDateDisabled={isWeekend} hint="A range cannot include a weekend." />
        </div>
      </section>
      <section className={styles.stageBlock}>
        <h2 className="type-section">Month and year navigation</h2>
        <div className={styles.stage}>
          <DatePicker label="Jump in time" locale="en-US" hint="Open the month title, then the year, to move directly." />
        </div>
      </section>
      <section className={styles.stageBlock}>
        <h2 className="type-section">Light and dark</h2>
        <div className={styles.pair}>
          <div className={styles.themeSample} data-theme="light" data-palette="teal" data-radius="soft" data-density="standard">
            <p className="type-label">Light</p>
            <DatePicker label="Light date" defaultValue="2026-11-10" locale="en-US" />
          </div>
          <div className={styles.themeSample} data-theme="dark" data-palette="teal" data-radius="soft" data-density="standard">
            <p className="type-label">Dark</p>
            <DatePicker label="Dark date" defaultValue="2026-11-10" locale="en-US" />
          </div>
        </div>
      </section>
    </>
  );
}

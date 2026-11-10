import { useState } from "react";
import { DatePicker, DateRangePicker, type CalendarDate, type DateRange } from "../components/date";
import { weekdayIndex } from "../components/date/calendar";
import { DateTimePicker } from "../components/datetime/DateTimePicker";
import type { DateTimeValue } from "../components/datetime/datetime";
import { TimePicker } from "../components/time/TimePicker";
import styles from "./Playground.module.scss";

function isWeekend(date: CalendarDate) {
  const weekday = weekdayIndex(date);
  return weekday === 0 || weekday === 6;
}

export function DateTimeSection() {
  const [date, setDate] = useState<CalendarDate | null>("2026-11-10");
  const [bounded, setBounded] = useState<CalendarDate | null>("2026-11-12");
  const [start, setStart] = useState<string | null>("09:30");
  const [close, setClose] = useState<string | null>(null);
  const [appointment, setAppointment] = useState<DateTimeValue | null>(null);
  const [visit, setVisit] = useState<DateTimeValue | null>(null);
  const [range, setRange] = useState<DateRange | null>({ start: "2026-11-02", end: "2026-11-08" });

  return (
    <>
      <header className={styles.intro}>
        <h1 className="type-page">Date & Time Pickers</h1>
        <p className="type-secondary">
          Date, time, date and time, and date range use the live appearance. On a phone the editor opens in a bottom sheet. On a wider screen it opens in a popover.
        </p>
      </header>

      <section className={styles.stageBlock}>
        <h2 className="type-section">Date Picker</h2>
        <div className={styles.stage}>
          <div className={styles.pickerField}>
            <DatePicker label="Date" value={date} onChange={setDate} locale="en-US" hint="Choose a day. Today jumps to the current date. Clear removes it." />
          </div>
          <div className={styles.pickerField}>
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
          </div>
          <div className={styles.pickerField}>
            <DatePicker label="Jump in time" locale="en-US" hint="Open the month title, then the year, to move directly." />
          </div>
        </div>
      </section>

      <section className={styles.stageBlock}>
        <h2 className="type-section">Time Picker</h2>
        <div className={styles.stage}>
          <div className={styles.pickerField}>
            <TimePicker
              label="Start time"
              value={start}
              onChange={setStart}
              locale="en-US"
              hourCycle="h12"
              minuteStep={5}
              min="06:00"
              max="22:00"
              hint="12-hour time. On a phone, scroll the wheel or type the time. Apply saves it."
            />
          </div>
          <div className={styles.pickerField}>
            <TimePicker
              label="Close time"
              value={close}
              onChange={setClose}
              locale="en-US"
              hourCycle="h24"
              minuteStep={15}
              disabledTimes={["12:00", "12:15"]}
              hint="24-hour time in steps of 15. 12:00 and 12:15 are unavailable."
            />
          </div>
        </div>
      </section>

      <section className={styles.stageBlock}>
        <h2 className="type-section">DateTime Picker</h2>
        <div className={styles.stage}>
          <div className={styles.pickerField}>
            <DateTimePicker
              label="Appointment"
              value={appointment}
              onChange={setAppointment}
              locale="en-US"
              hourCycle="h12"
              minuteStep={5}
              timeZone="America/New_York"
              min={{ date: "2024-01-01", time: "08:00" }}
              max={{ date: "2026-12-31", time: "20:00" }}
              hint="Desktop popover. Today sets the date. Now sets the date and time. Apply commits both."
            />
          </div>
          <div className={styles.pickerField}>
            <DateTimePicker
              label="Visit"
              value={visit}
              onChange={setVisit}
              locale="en-US"
              hourCycle="h24"
              minuteStep={15}
              viewport="mobile"
              hint="This example always uses the bottom sheet, with date and time on separate steps."
            />
          </div>
        </div>
      </section>

      <section className={styles.stageBlock}>
        <h2 className="type-section">Date Range Picker</h2>
        <div className={styles.stage}>
          <div className={styles.pickerField}>
            <DateRangePicker
              label="Date range"
              value={range}
              onChange={setRange}
              locale="en-US"
              weekStartsOn={0}
              hint="Choose a preset or the start and end dates. Apply commits the range. Cancel restores it."
            />
          </div>
          <div className={styles.pickerField}>
            <DateRangePicker label="Weekdays" locale="en-US" weekStartsOn={1} isDateDisabled={isWeekend} hint="A range cannot include a weekend." />
          </div>
        </div>
      </section>

      <section className={styles.stageBlock}>
        <h2 className="type-section">Light and dark</h2>
        <div className={styles.pair}>
          <div className={styles.themeSample} data-theme="light" data-palette="teal" data-radius="soft" data-density="standard">
            <p className="type-label">Light</p>
            <DatePicker label="Light date" defaultValue="2026-11-10" locale="en-US" />
            <TimePicker label="Light time" locale="en-US" hourCycle="h12" defaultValue="09:30" />
          </div>
          <div className={styles.themeSample} data-theme="dark" data-palette="teal" data-radius="soft" data-density="standard">
            <p className="type-label">Dark</p>
            <DatePicker label="Dark date" defaultValue="2026-11-10" locale="en-US" />
            <TimePicker label="Dark time" locale="en-US" hourCycle="h24" defaultValue="18:45" />
          </div>
        </div>
      </section>
    </>
  );
}

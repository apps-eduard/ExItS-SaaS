import { useState } from "react";
import { Button } from "../../components/button/Button";
import { Checkbox } from "../../components/checkbox/Checkbox";
import { DatePicker } from "../../components/date/DatePicker";
import { DateRangePicker } from "../../components/date/DateRangePicker";
import type { CalendarDate, DateRange } from "../../components/date/calendar";
import { DateTimePicker } from "../../components/datetime/DateTimePicker";
import type { DateTimeValue } from "../../components/datetime/datetime";
import { Input } from "../../components/input/Input";
import { Radio } from "../../components/radio/Radio";
import { SegmentedControl } from "../../components/segmented/SegmentedControl";
import { Select } from "../../components/select/Select";
import { Switch } from "../../components/switch/Switch";
import { TimePicker } from "../../components/time/TimePicker";
import styles from "../Playground.module.scss";
import review from "./review.module.scss";

const CATEGORIES = ["Groceries", "Rent", "Transfers"] as const;
const WATCH = ["Groceries", "Rent", "Transfers"] as const;

type Method = "cash" | "card" | "transfer";
type Reminder = "morning" | "evening" | "none";
type Span = "week" | "month" | "year";

export function SelectionInputReview() {
  const [description, setDescription] = useState("");
  const [amount, setAmount] = useState("");
  const [category, setCategory] = useState("");
  const [date, setDate] = useState<CalendarDate | null>(null);
  const [method, setMethod] = useState<Method>("cash");
  const [receipt, setReceipt] = useState(false);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [notice, setNotice] = useState("Nothing saved yet.");

  const [alerts, setAlerts] = useState(true);
  const [reminder, setReminder] = useState<Reminder>("evening");
  const [span, setSpan] = useState<Span>("month");
  const [watch, setWatch] = useState<string[]>(["Groceries"]);

  const [picked, setPicked] = useState<string[]>(["Groceries"]);
  const [sampleSpan, setSampleSpan] = useState<Span>("month");
  const [range, setRange] = useState<DateRange | null>(null);
  const [time, setTime] = useState<string | null>(null);
  const [recorded, setRecorded] = useState<DateTimeValue | null>(null);

  function togglePicked(name: string, checked: boolean) {
    setPicked((current) => (checked ? [...current, name] : current.filter((item) => item !== name)));
  }

  function toggleAll(checked: boolean) {
    setPicked(checked ? [...CATEGORIES] : []);
  }

  function clearTransaction() {
    setDescription("");
    setAmount("");
    setCategory("");
    setDate(null);
    setMethod("cash");
    setReceipt(false);
    setErrors({});
    setNotice("Cleared the draft.");
  }

  function saveTransaction() {
    const next: Record<string, string> = {};
    if (description.trim().length === 0) next.description = "Enter a description.";
    const parsed = Number(amount);
    if (amount.trim().length === 0 || !Number.isFinite(parsed) || parsed <= 0) next.amount = "Enter an amount greater than 0.";
    if (category.length === 0) next.category = "Choose a category.";
    if (!date) next.date = "Choose a date.";
    setErrors(next);
    if (Object.keys(next).length > 0) {
      setNotice("Fix the highlighted fields.");
      return;
    }
    setNotice(`Saved ${description.trim()} for ₱${parsed.toFixed(2)} on ${date}.`);
  }

  const allPicked = picked.length === CATEGORIES.length;
  const somePicked = picked.length > 0 && !allPicked;
  const watchLabel = watch.length === 0 ? "none" : watch.join(", ");

  return (
    <>
      <header className={styles.intro}>
        <h1 className="type-page">Selection & Input</h1>
        <p className="type-secondary">
          Text fields, selects, checkboxes, radios, switches, segmented controls, and date and time pickers use the components already in this playground. This page does not add a second input system.
        </p>
      </header>

      <section className={`${styles.stageBlock} ${review.block}`}>
        <h2 className="type-section">Interactive preview</h2>
        <p className="type-secondary">Personal samples. Nothing is sent to a server.</p>
        <h3 className="type-card">Add transaction</h3>
        <div className={review.form}>
          <Input label="Description" value={description} error={errors.description} onChange={(event) => setDescription(event.target.value)} />
          <Input label="Amount" inputMode="decimal" value={amount} error={errors.amount} onChange={(event) => setAmount(event.target.value)} />
          <Select label="Category" value={category} error={errors.category} onChange={(event) => setCategory(event.target.value)}>
            <option value="">Choose</option>
            <option value="groceries">Groceries</option>
            <option value="rent">Rent</option>
            <option value="transfer">Transfer</option>
          </Select>
          <DatePicker label="Date" value={date} onChange={setDate} error={errors.date} />
          <div className={review.choiceStack} role="radiogroup" aria-label="Payment method">
            <Radio name="payment-method" label="Cash" checked={method === "cash"} onChange={() => setMethod("cash")} />
            <Radio name="payment-method" label="Card" checked={method === "card"} onChange={() => setMethod("card")} />
            <Radio name="payment-method" label="Transfer" checked={method === "transfer"} onChange={() => setMethod("transfer")} />
          </div>
          <Checkbox label="Keep the receipt with this transaction" checked={receipt} onChange={(event) => setReceipt(event.target.checked)} />
          <div className={review.actions}>
            <Button variant="secondary" onClick={clearTransaction}>
              Clear
            </Button>
            <Button onClick={saveTransaction}>Save transaction</Button>
          </div>
        </div>
        <p className="type-secondary" role="status">
          {notice}
        </p>

        <h3 className="type-card">Personal preferences</h3>
        <div className={review.form}>
          <Switch label="Payment alerts" checked={alerts} onChange={(event) => setAlerts(event.target.checked)} />
          <div className={review.choiceStack} role="radiogroup" aria-label="Reminder">
            <Radio name="reminder" label="Morning" checked={reminder === "morning"} onChange={() => setReminder("morning")} />
            <Radio name="reminder" label="Evening" checked={reminder === "evening"} onChange={() => setReminder("evening")} />
            <Radio name="reminder" label="None" checked={reminder === "none"} onChange={() => setReminder("none")} />
          </div>
          <SegmentedControl
            label="Summary"
            name="summary-span"
            value={span}
            onChange={setSpan}
            options={[
              { value: "week", label: "Week" },
              { value: "month", label: "Month" },
              { value: "year", label: "Year", disabled: true },
            ]}
          />
          <Select
            label="Watch categories"
            multiple
            value={watch}
            onChange={(event) => setWatch([...event.target.selectedOptions].map((option) => option.value))}
          >
            {WATCH.map((item) => (
              <option key={item} value={item}>
                {item}
              </option>
            ))}
          </Select>
          <p className={`type-secondary ${review.note}`}>
            {alerts ? "Alerts on" : "Alerts off"}. Reminder: {reminder}. Summary: {span}. Watching {watchLabel}.
          </p>
        </div>
      </section>

      <section className={`${styles.stageBlock} ${review.block}`}>
        <h2 className="type-section">Text input</h2>
        <p className="type-secondary">Standard, floating, outlined, and soft. Focus draw and border reveal are the existing focus treatments.</p>
        <div className={styles.stage}>
          <Input label="Standard" placeholder="Empty" />
          <Input label="Filled" defaultValue="Neighborhood market" />
          <Input label="Disabled" defaultValue="Unavailable" disabled />
          <Input label="Error" defaultValue="" error="Enter a description." />
          <Input label="Floating" appearance="floating" defaultValue="Rent" />
          <Input label="Outlined" appearance="outlined" defaultValue="Card" />
          <Input label="Soft" appearance="soft" defaultValue="Cash" />
          <Input label="Focus draw" defaultValue="Market" focusMotion="focus-draw" />
          <Input label="Border reveal" defaultValue="Transfer" focusMotion="border-reveal" />
        </div>
      </section>

      <section className={`${styles.stageBlock} ${review.block}`}>
        <h2 className="type-section">Select</h2>
        <p className="type-secondary">Single, multiple, and searchable. Radius and density follow the live appearance controls. A narrow screen opens the same menu in the existing sheet.</p>
        <div className={styles.stage}>
          <Select label="Single category" defaultValue="groceries">
            <option value="groceries">Groceries</option>
            <option value="rent">Rent</option>
          </Select>
          <Select label="Several categories" multiple defaultValue={["Groceries"]}>
            <option value="Groceries">Groceries</option>
            <option value="Rent">Rent</option>
          </Select>
          <Select label="Search category" searchable defaultValue="">
            <option value="">Choose</option>
            <option value="groceries">Groceries</option>
            <option value="rent">Rent</option>
            <option value="transfer">Transfer</option>
          </Select>
          <Select label="Disabled category" value="rent" disabled onChange={() => undefined}>
            <option value="rent">Rent</option>
          </Select>
        </div>
      </section>

      <section className={`${styles.stageBlock} ${review.block}`}>
        <h2 className="type-section">Checkbox, radio, and switch</h2>
        <div className={styles.stage}>
          <div className={review.choiceStack}>
            <Checkbox label="All categories" checked={allPicked} indeterminate={somePicked} onChange={(event) => toggleAll(event.target.checked)} />
            {CATEGORIES.map((item) => (
              <Checkbox key={item} label={item} checked={picked.includes(item)} onChange={(event) => togglePicked(item, event.target.checked)} />
            ))}
            <Checkbox label="Disabled choice" disabled />
          </div>
          <div className={review.choiceStack} role="radiogroup" aria-label="Sample method">
            <Radio name="sample-method" label="Cash" defaultChecked />
            <Radio name="sample-method" label="Card" />
            <Radio name="sample-method" label="Unavailable" disabled />
          </div>
          <div className={review.choiceStack}>
            <Switch label="Alerts" defaultChecked />
            <Switch label="Sample alerts off" />
            <Switch label="Alerts unavailable" disabled />
          </div>
        </div>
      </section>

      <section className={`${styles.stageBlock} ${review.block}`}>
        <h2 className="type-section">Segmented control</h2>
        <p className="type-secondary">Three options. Year stays disabled. The indicator follows the active option.</p>
        <SegmentedControl
          label="Range"
          name="range-span"
          value={sampleSpan}
          onChange={setSampleSpan}
          options={[
            { value: "week", label: "Week" },
            { value: "month", label: "Month" },
            { value: "year", label: "Year", disabled: true },
          ]}
        />
      </section>

      <section className={`${styles.stageBlock} ${review.block}`}>
        <h2 className="type-section">Date and time</h2>
        <p className="type-secondary">The existing pickers. A narrow screen uses the existing sheet. Desktop stays a popover.</p>
        <div className={styles.stage}>
          <DatePicker label="Payment date" />
          <DateRangePicker label="Statement period" value={range} onChange={setRange} />
          <TimePicker label="Reminder time" value={time} onChange={setTime} />
          <DateTimePicker label="Recorded at" value={recorded} onChange={setRecorded} />
        </div>
        <p className={`type-secondary ${review.note}`}>
          Period {range ? `${range.start} to ${range.end}` : "not set"}. Time {time ?? "not set"}. Recorded {recorded ? `${recorded.date} ${recorded.time}` : "not set"}.
        </p>
      </section>

      <section className={`${styles.stageBlock} ${review.block}`}>
        <h2 className="type-section">Mobile and desktop</h2>
        <ul className={review.notes}>
          <li>Fields, choices, and the segmented control stay the same controls at every width. They wrap inside the page.</li>
          <li>Under 768px, searchable and plain Select open in the existing bottom sheet. Date, time, and date-time do the same.</li>
          <li>At 390px the existing mobile hit areas stay at least 44px. This page does not add a separate size preview.</li>
        </ul>
      </section>

      <section className={`${styles.stageBlock} ${review.block}`}>
        <h2 className="type-section">Accessibility and behavior</h2>
        <ul className={review.notes}>
          <li>Each field keeps its visible label. An error is tied to that field.</li>
          <li>Radio groups name the choice. A switch exposes on and off. A segmented control is a radio group.</li>
          <li>Disabled controls stay out of the choice. Focus uses the shared focus ring.</li>
          <li>Save transaction checks the draft once. Clear removes it. Preferences update the sentence beside them.</li>
          <li>Reduced motion removes the segmented indicator travel and the picker motion that already honors it.</li>
        </ul>
      </section>

      <section className={`${styles.stageBlock} ${review.block}`}>
        <h2 className="type-section">Review status</h2>
        <p className="type-secondary">Locked. Text fields, selects, choices, and the existing pickers stay as reviewed. The Components category stays unlocked.</p>
      </section>
    </>
  );
}

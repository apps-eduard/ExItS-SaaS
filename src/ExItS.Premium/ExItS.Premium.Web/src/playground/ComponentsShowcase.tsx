import { useState, type ReactNode } from "react";
import { Badge } from "../components/badge/Badge";
import { Button } from "../components/button/Button";
import { Card } from "../components/card/Card";
import { Checkbox } from "../components/checkbox/Checkbox";
import { Input } from "../components/input/Input";
import { Radio } from "../components/radio/Radio";
import { SearchField } from "../components/search/SearchField";
import { SegmentedControl } from "../components/segmented/SegmentedControl";
import { Select } from "../components/select/Select";
import { Switch } from "../components/switch/Switch";
import type { SectionId } from "./nav";
import { AdvancedShowcase } from "./AdvancedShowcase";
import { DateTimeSection } from "./DateTimeSection";
import { InputPlayground } from "./input/InputPlayground";
import { SelectPlayground } from "./select/SelectPlayground";
import styles from "./Playground.module.scss";

function Intro({ title, children }: { title: string; children: ReactNode }) {
  return (
    <header className={styles.intro}>
      <h1 className="type-page">{title}</h1>
      <p className="type-secondary">{children}</p>
    </header>
  );
}

function Stage({ label, children }: { label?: string; children: ReactNode }) {
  return (
    <section className={styles.stageBlock}>
      {label ? <h2 className="type-section">{label}</h2> : null}
      <div className={styles.stage}>{children}</div>
    </section>
  );
}

export function ComponentsShowcase({ id }: { id: SectionId }) {
  if (id === "button") return <ButtonSection />;
  if (id === "button-group") return <ButtonGroupStub />;
  if (id === "button-dropdown") return <ButtonDropdownStub />;
  if (id === "inputs") return <InputsSection />;
  if (id === "search") return <SearchSection />;
  if (id === "selects") return <SelectPlayground />;
  if (id === "date-time") return <DateTimeSection />;
  if (id === "cards") return <CardsSection />;
  if (id === "badges") return <BadgesSection />;
  if (id === "toggles") return <TogglesSection />;
  if (id === "checkboxes") return <CheckboxesSection />;
  if (id === "radio") return <RadioSection />;
  if (id === "segmented") return <SegmentedSection />;
  if (id === "advanced") return <AdvancedShowcase />;
  if (id === "component-states") return <StatesSection />;
  return null;
}

function ButtonSection() {
  return (
    <>
      <Intro title="Button">
        One solid card for the nine semantic roles from Colors. Appearances (soft, outline, raise) stay on the API. Ghost is chrome-only. Disabled and loading sit in States.
      </Intro>
      <Stage label="Semantic">
        <Button variant="primary">Primary</Button>
        <Button variant="secondary">Secondary</Button>
        <Button variant="accent">Accent</Button>
        <Button variant="success">Success</Button>
        <Button variant="info">Info</Button>
        <Button variant="warning">Warning</Button>
        <Button variant="help">Help</Button>
        <Button variant="danger">Danger</Button>
        <Button variant="contrast">Contrast</Button>
      </Stage>
      <Stage label="States">
        <Button disabled>Disabled</Button>
        <Button variant="secondary" disabled>Secondary</Button>
        <Button loading>Saving</Button>
      </Stage>
    </>
  );
}

function ButtonGroupStub() {
  return (
    <>
      <Intro title="Button group">
        Attached or segmented actions that share height and borders. This page is a playground shell — the ButtonGroup component is not built yet.
      </Intro>
      <Stage label="Coming next">
        <p className="type-secondary">Use adjacent primary/secondary actions here once ButtonGroup ships. Until then, compose single Buttons.</p>
      </Stage>
    </>
  );
}

function ButtonDropdownStub() {
  return (
    <>
      <Intro title="Button dropdown">
        A primary action with a caret menu (or split button). This page is a playground shell — the menu button API is not built yet.
      </Intro>
      <Stage label="Coming next">
        <p className="type-secondary">Reserve this section for Save ▾ / overflow menus. Keyboard and mobile menu behavior will land with the component.</p>
      </Stage>
    </>
  );
}

function InputsSection() {
  return <InputPlayground />;
}

function SearchSection() {
  const [value, setValue] = useState("Market");
  const [empty, setEmpty] = useState("");
  return (
    <>
      <Intro title="Search">
        A labeled search field with a clear action when it has text. Success is not a search state. Error is a message under the field.
      </Intro>
      <Stage>
        <SearchField label="Search" value={value} onChange={setValue} placeholder="Name or note" />
        <SearchField label="Empty" value={empty} onChange={setEmpty} placeholder="Search activity" />
        <SearchField label="Disabled" value="Locked" onChange={() => undefined} disabled />
        <SearchField label="Error" value="?" onChange={() => undefined} error="Enter at least two letters." />
      </Stage>
    </>
  );
}

function CardsSection() {
  return (
    <>
      <Intro title="Cards">
        A card is a quiet surface. It does not have hover, loading, or disabled states unless it becomes an action later.
      </Intro>
      <div className={styles.pair}>
        <Card eyebrow="Personal" title="Available balance">
          <p className="type-secondary">Level 1 resting surface.</p>
        </Card>
        <Card elevation={2} title="Raised">
          <p className="type-secondary">Level 2 raised surface.</p>
        </Card>
      </div>
    </>
  );
}

function BadgesSection() {
  return (
    <>
      <Intro title="Badges">
        Status badges are soft tints with contrast-safe ink. Warning is a solid fill because its hue is too light for small text on a pale background.
      </Intro>
      <Stage>
        <Badge>Neutral</Badge>
        <Badge tone="primary">Primary</Badge>
        <Badge tone="success">Paid</Badge>
        <Badge tone="warning">Due</Badge>
        <Badge tone="danger">Failed</Badge>
        <Badge tone="info">Info</Badge>
      </Stage>
    </>
  );
}

function TogglesSection() {
  const [on, setOn] = useState(true);
  return (
    <>
      <Intro title="Toggles">
        A native checkbox presented as a switch. Off, on, and disabled are the states. There is no loading state.
      </Intro>
      <Stage>
        <Switch label="Off" />
        <Switch label="Reminders" checked={on} onChange={(event) => setOn(event.target.checked)} />
        <Switch label="Disabled" disabled />
        <Switch label="Disabled on" checked disabled />
      </Stage>
    </>
  );
}

function CheckboxesSection() {
  const [checked, setChecked] = useState(true);
  return (
    <>
      <Intro title="Checkboxes">
        Native checkboxes, including the indeterminate state. Error is expressed by nearby text in a form, not by a fake checkbox color.
      </Intro>
      <Stage>
        <Checkbox label="Unchecked" />
        <Checkbox label="Checked" checked={checked} onChange={(event) => setChecked(event.target.checked)} />
        <Checkbox label="Indeterminate" indeterminate />
        <Checkbox label="Disabled" disabled />
        <Checkbox label="Disabled checked" checked disabled />
      </Stage>
    </>
  );
}

function RadioSection() {
  const [value, setValue] = useState("soft");
  return (
    <>
      <Intro title="Radio">
        One native group. Arrow keys move the selection. A disabled option stays in the group and cannot be chosen.
      </Intro>
      <Stage>
        <Radio name="shape-demo" label="Standard" value="standard" checked={value === "standard"} onChange={() => setValue("standard")} />
        <Radio name="shape-demo" label="Soft" value="soft" checked={value === "soft"} onChange={() => setValue("soft")} />
        <Radio name="shape-demo" label="Pill" value="pill" checked={value === "pill"} onChange={() => setValue("pill")} />
        <Radio name="shape-disabled" label="Disabled" disabled />
      </Stage>
    </>
  );
}

function SegmentedSection() {
  const [value, setValue] = useState<"day" | "week" | "month">("week");
  return (
    <>
      <Intro title="Segmented controls">
        A radio group with a thumb that moves on transform. The whole control can be disabled, or a single option can.
      </Intro>
      <Stage>
        <SegmentedControl
          label="Range"
          name="range-demo"
          value={value}
          onChange={setValue}
          options={[
            { value: "day", label: "Day" },
            { value: "week", label: "Week" },
            { value: "month", label: "Month" },
          ]}
        />
        <SegmentedControl
          label="Disabled"
          name="range-disabled"
          value="week"
          disabled
          onChange={() => undefined}
          options={[
            { value: "day", label: "Day" },
            { value: "week", label: "Week" },
          ]}
        />
        <SegmentedControl
          label="Option disabled"
          name="range-partial"
          value="day"
          onChange={() => undefined}
          options={[
            { value: "day", label: "Day" },
            { value: "week", label: "Week", disabled: true },
          ]}
        />
      </Stage>
    </>
  );
}

function StateRow({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div className={styles.matrixRow}>
      <h2 className="type-label">{label}</h2>
      <div className={styles.matrixItems}>{children}</div>
    </div>
  );
}

function StatesSection() {
  const [on, setOn] = useState(true);
  const [query, setQuery] = useState("Market");
  return (
    <>
      <Intro title="Component states">
        Applicable states on one surface. Unsupported states are omitted rather than imitated.
      </Intro>
      <div className={styles.matrix}>
        <StateRow label="Button">
          <Button>Default</Button>
          <Button disabled>Disabled</Button>
          <Button loading>Loading</Button>
        </StateRow>
        <StateRow label="Input">
          <Input label="Default" placeholder="Name" />
          <Input label="Error" defaultValue="a" error="Too short." />
        </StateRow>
        <StateRow label="Search">
          <SearchField label="Search" value={query} onChange={setQuery} />
        </StateRow>
        <StateRow label="Select">
          <Select label="Category" defaultValue="rent">
            <option value="rent">Rent</option>
            <option value="transport">Transport</option>
          </Select>
        </StateRow>
        <StateRow label="Badge">
          <Badge tone="success">Paid</Badge>
          <Badge tone="warning">Due</Badge>
          <Badge tone="danger">Failed</Badge>
        </StateRow>
        <StateRow label="Toggle">
          <Switch label={on ? "On" : "Off"} checked={on} onChange={(event) => setOn(event.target.checked)} />
          <Switch label="Disabled" disabled />
        </StateRow>
      </div>
    </>
  );
}

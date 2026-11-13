import { useState } from "react";
import { INPUT_FOCUS_MOTION_OPTIONS, type InputFocusMotion } from "../../components/input/focus-motion";
import { Select } from "../../components/select/Select";
import type { SelectMenuMotion } from "../../components/select/select-menu-motion";
import { Switch } from "../../components/switch/Switch";
import { CursorCopyCommand } from "../CursorCopyCommand";
import { PlaygroundShell } from "../shell/PlaygroundShell";
import { selectCursorCommand } from "./select-cursor-command";
import styles from "./SelectPlayground.module.scss";

type SelectAppearance = "outlined" | "soft" | "stacked";

const OPTIONS = (
  <>
    <option value="groceries">Groceries</option>
    <option value="rent">Rent</option>
    <option value="utilities">Utilities</option>
    <option value="transfer">Transfer</option>
  </>
);

const FOCUS_LABELS: Record<InputFocusMotion, string> = {
  none: "None",
  "border-reveal": "Border Reveal",
  "soft-glow": "Soft Glow",
  "running-accent": "Running Accent",
  "focus-draw": "Focus Draw",
};

export function SelectPlayground() {
  const [appearance, setAppearance] = useState<SelectAppearance>("stacked");
  const [focusMotion, setFocusMotion] = useState<InputFocusMotion>("none");
  const [menuMotion, setMenuMotion] = useState<SelectMenuMotion>("scale");
  const [searchable, setSearchable] = useState(false);
  const [multiple, setMultiple] = useState(false);
  const [value, setValue] = useState("groceries");
  const [many, setMany] = useState<string[]>(["groceries"]);
  const cursorCommand = selectCursorCommand({
    appearance,
    focusMotion,
    menuMotion,
    searchable,
    multiple,
  });

  return (
    <PlaygroundShell
      title="Selects"
      description="Select reuses Input appearance and focus motion. Soft keeps the label inside the tint, Outlined floats it on the border, and Stacked keeps it above. Dropdown transitions stay independent of the focus treatment."
      panes="equal"
      propertiesColumns={2}
      preview={
        <div className={styles.previewStack}>
          <div className={styles.previewField}>
            <Select
              label="Category"
              appearance={appearance}
              focusMotion={focusMotion}
              menuMotion={menuMotion}
              searchable={searchable}
              multiple={multiple}
              value={multiple ? many : value}
              onChange={(event) => {
                if (multiple) {
                  setMany([...event.target.selectedOptions].map((option) => option.value));
                  return;
                }
                setValue(event.target.value);
              }}
            >
              <option value="">Choose</option>
              {OPTIONS}
            </Select>
          </div>
          <CursorCopyCommand standard="Select" command={cursorCommand} />
        </div>
      }
      properties={
        <>
          <Select label="Appearance" value={appearance} onChange={(event) => setAppearance(event.target.value as SelectAppearance)}>
            <option value="outlined">Outlined</option>
            <option value="soft">Soft</option>
            <option value="stacked">Stacked</option>
          </Select>
          <Select label="Focus Animation" value={focusMotion} onChange={(event) => setFocusMotion(event.target.value as InputFocusMotion)}>
            <option value="none">{FOCUS_LABELS.none}</option>
            {INPUT_FOCUS_MOTION_OPTIONS.map((option) => (
              <option key={option.id} value={option.id}>
                {FOCUS_LABELS[option.id]}
              </option>
            ))}
            <option value="running-accent">{FOCUS_LABELS["running-accent"]}</option>
          </Select>
          <div className={styles.spanFull}>
            <Select label="Dropdown Transition" value={menuMotion} onChange={(event) => setMenuMotion(event.target.value as SelectMenuMotion)}>
              <option value="flip-up">Flip Up</option>
              <option value="flip-down">Flip Down</option>
              <option value="scale">Scale</option>
              <option value="jump-up">Jump Up</option>
            </Select>
          </div>
          <div className={`${styles.toggles} ${styles.spanFull}`}>
            <Switch label="Searchable" checked={searchable} onChange={(event) => setSearchable(event.target.checked)} />
            <Switch label="Multiple" checked={multiple} onChange={(event) => setMultiple(event.target.checked)} />
          </div>
        </>
      }
      examples={
        <>
          <article className={styles.example}>
            <p className={styles.exampleLabel}>Default</p>
            <Select label="Category" defaultValue="groceries">
              {OPTIONS}
            </Select>
          </article>
          <article className={styles.example}>
            <p className={styles.exampleLabel}>Focused</p>
            <Select label="Focused category" defaultValue="rent" focusMotion="focus-draw">
              {OPTIONS}
            </Select>
          </article>
          <article className={styles.example}>
            <p className={styles.exampleLabel}>Error</p>
            <Select label="Category" defaultValue="" error="Choose a category.">
              <option value="">Choose</option>
              {OPTIONS}
            </Select>
          </article>
          <article className={styles.example}>
            <p className={styles.exampleLabel}>Disabled</p>
            <Select label="Category" value="rent" disabled onChange={() => undefined}>
              <option value="rent">Rent</option>
            </Select>
          </article>
          <article className={styles.example}>
            <p className={styles.exampleLabel}>Success</p>
            <Select label="Category" defaultValue="rent" success="Category saved.">
              {OPTIONS}
            </Select>
          </article>
          <article className={styles.example}>
            <p className={styles.exampleLabel}>Searchable</p>
            <Select label="Status" searchable defaultValue="">
              <option value="">Choose</option>
              <option value="paid">Paid</option>
              <option value="due">Due</option>
              <option value="partial">Partial</option>
              <option value="void">Void</option>
            </Select>
          </article>
        </>
      }
    />
  );
}

import { useState } from "react";
import { Select } from "../../components/select/Select";
import type { SelectMenuMotion } from "../../components/select/select-menu-motion";
import { Switch } from "../../components/switch/Switch";
import { PlaygroundShell } from "../shell/PlaygroundShell";
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

export function SelectPlayground() {
  const [appearance, setAppearance] = useState<SelectAppearance>("stacked");
  const [motion, setMotion] = useState<SelectMenuMotion>("flip-down");
  const [searchable, setSearchable] = useState(false);
  const [multiple, setMultiple] = useState(false);
  const [value, setValue] = useState("groceries");
  const [many, setMany] = useState<string[]>(["groceries"]);

  return (
    <PlaygroundShell
      title="Selects"
      description="One live field that matches Input height, type, radius, and border. Stacked keeps the label above, Outlined floats it on the border, and Soft keeps it inside the tint. The menu matches the trigger, follows density, and opens in a bottom sheet below 768px."
      preview={
        <div className={styles.previewField}>
          <Select
            label="Category"
            appearance={appearance}
            menuMotion={motion}
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
      }
      properties={
        <>
          <Select label="Appearance" value={appearance} onChange={(event) => setAppearance(event.target.value as SelectAppearance)}>
            <option value="outlined">Outlined</option>
            <option value="soft">Soft</option>
            <option value="stacked">Stacked</option>
          </Select>
          <Select label="Transition" value={motion} onChange={(event) => setMotion(event.target.value as SelectMenuMotion)}>
            <option value="flip-up">Flip Up</option>
            <option value="flip-down">Flip Down</option>
            <option value="scale">Scale</option>
            <option value="jump-up">Jump Up</option>
          </Select>
          <div className={styles.toggles}>
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
            <Select label="Focused category" defaultValue="rent">
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

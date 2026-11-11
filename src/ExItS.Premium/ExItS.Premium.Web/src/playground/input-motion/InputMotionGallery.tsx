import { useRef, useState } from "react";
import { Button } from "../../components/button/Button";
import { Input } from "../../components/input/Input";
import { INPUT_APPEARANCES, type InputAppearance } from "../../components/input/input-appearance";
import { INPUT_FOCUS_MOTION_OPTIONS, type InputFocusMotion } from "../../components/input/focus-motion";
import { SearchField } from "../../components/search/SearchField";
import { SegmentedControl } from "../../components/segmented/SegmentedControl";
import { Select } from "../../components/select/Select";
import { Switch } from "../../components/switch/Switch";
import { AppearanceControls } from "../AppearanceControls";
import styles from "./InputMotionGallery.module.scss";

type FieldKind = "text" | "search";
type FieldStatus = "none" | "error" | "success";

const APPEARANCE_LABELS: Record<InputAppearance, string> = {
  stacked: "Stacked",
  outlined: "Outlined",
  soft: "Soft",
  filled: "Filled",
};

const MOTION_LABELS: Record<InputFocusMotion, string> = {
  none: "None",
  "border-reveal": "Border reveal",
  "soft-glow": "Soft glow",
  "running-accent": "Running accent",
  "focus-draw": "Focus draw",
};

const ERROR_MESSAGE = "Use at least two letters.";
const SUCCESS_MESSAGE = "Name is available.";

export function InputMotionGallery({ embedded = false }: { embedded?: boolean }) {
  const previewRef = useRef<HTMLDivElement>(null);
  const [appearance, setAppearance] = useState<InputAppearance>("stacked");
  const [motion, setMotion] = useState<InputFocusMotion>("border-reveal");
  const [kind, setKind] = useState<FieldKind>("text");
  const [status, setStatus] = useState<FieldStatus>("none");
  const [disabled, setDisabled] = useState(false);
  const [readOnly, setReadOnly] = useState(false);
  const [required, setRequired] = useState(false);
  const [label, setLabel] = useState("Name");
  const [text, setText] = useState("");
  const [query, setQuery] = useState("Market");
  const [lockedQuery, setLockedQuery] = useState("Market");

  const previewLabel = label.trim() || "Name";
  const error = status === "error" ? ERROR_MESSAGE : undefined;
  const success = status === "success" && kind === "text" ? SUCCESS_MESSAGE : undefined;
  const detail =
    motion === "none"
      ? "The inside stays the surface color. Only the preview follows these controls."
      : (INPUT_FOCUS_MOTION_OPTIONS.find((option) => option.id === motion)?.detail ?? "");

  function replay() {
    const node = previewRef.current?.querySelector("input");
    if (!(node instanceof HTMLInputElement) || node.disabled) return;
    node.blur();
    node.focus();
  }

  function reset() {
    setAppearance("stacked");
    setMotion("border-reveal");
    setKind("text");
    setStatus("none");
    setDisabled(false);
    setReadOnly(false);
    setRequired(false);
    setLabel("Name");
    setText("");
    setQuery("Market");
  }

  function chooseKind(next: FieldKind) {
    setKind(next);
    if (next === "search" && status === "success") setStatus("none");
  }

  const recipe = [
    APPEARANCE_LABELS[appearance],
    MOTION_LABELS[motion],
    kind,
    status,
    disabled ? "disabled" : "",
    readOnly && kind === "text" ? "read-only" : "",
    required && kind === "text" ? "required" : "",
  ]
    .filter(Boolean)
    .join(" · ");

  const previewKey = [appearance, motion, kind, status, disabled, readOnly, required].join(":");

  const playground = (
    <section className={styles.card} aria-labelledby="input-playground-title">
      <div className={styles.cardCopy}>
        <h2 id="input-playground-title" className="type-section">
          Input playground
        </h2>
        <p className="type-secondary">{detail}</p>
      </div>
      <div className={styles.controls}>
        <Select label="Appearance" value={appearance} onChange={(event) => setAppearance(event.target.value as InputAppearance)}>
          <option value="outlined">Outlined</option>
          <option value="soft">Soft</option>
          <option value="filled">Filled</option>
          <option value="stacked">Stacked</option>
        </Select>
        <Select label="Transition" value={motion} onChange={(event) => setMotion(event.target.value as InputFocusMotion)}>
          <option value="none">None</option>
          <option value="border-reveal">Border Reveal</option>
          <option value="soft-glow">Soft Focus Glow</option>
          <option value="focus-draw">ExItS Focus Draw</option>
        </Select>
        <SegmentedControl
          label="Field"
          name="input-playground-kind"
          value={kind}
          options={[
            { value: "text", label: "Text" },
            { value: "search", label: "Search" },
          ]}
          onChange={chooseKind}
        />
        <SegmentedControl
          label="Status"
          name="input-playground-status"
          value={kind === "search" && status === "success" ? "none" : status}
          options={[
            { value: "none", label: "None" },
            { value: "error", label: "Error" },
            { value: "success", label: "Success", disabled: kind === "search" },
          ]}
          onChange={setStatus}
        />
        <Input label="Label" value={label} onChange={(event) => setLabel(event.target.value)} autoComplete="off" />
      </div>
      <div className={styles.toggles}>
        <Switch label="Disabled" checked={disabled} onChange={(event) => setDisabled(event.target.checked)} />
        <Switch label="Read-only" checked={readOnly} disabled={kind === "search"} onChange={(event) => setReadOnly(event.target.checked)} />
        <Switch label="Required" checked={required} disabled={kind === "search"} onChange={(event) => setRequired(event.target.checked)} />
      </div>
      <div className={styles.actions}>
        <Button variant="secondary" onClick={reset}>
          Default global
        </Button>
        {motion === "border-reveal" || motion === "focus-draw" ? (
          <Button variant="secondary" onClick={replay}>
            Replay
          </Button>
        ) : null}
      </div>
      <div className={styles.stage}>
        <div className={styles.preview} ref={previewRef} role="region" aria-label="Preview">
          <p className="type-caption">Preview</p>
          {kind === "search" ? (
            <SearchField
              key={previewKey}
              label={previewLabel}
              value={query}
              onChange={setQuery}
              placeholder="Name or note"
              disabled={disabled}
              error={error}
              appearance={appearance}
              focusMotion={motion}
            />
          ) : (
            <Input
              key={previewKey}
              label={previewLabel}
              appearance={appearance}
              focusMotion={motion}
              value={readOnly ? "Published" : text}
              onChange={(event) => setText(event.target.value)}
              placeholder="Account name"
              autoComplete="off"
              disabled={disabled}
              readOnly={readOnly}
              required={required}
              error={error}
              success={success}
              hint={status === "none" && !readOnly ? "Shown on the payment." : undefined}
            />
          )}
        </div>
        <div className={styles.locked} role="region" aria-label="Default global">
          <p className="type-caption">Default global</p>
          <Input label="Name" placeholder="Account name" hint="Shown on the payment." autoComplete="off" />
          <SearchField label="Search" value={lockedQuery} onChange={setLockedQuery} placeholder="Name or note" />
        </div>
      </div>
      <p className={styles.command}>{recipe}</p>
      <section className={styles.compare} aria-label="Appearance comparison">
        <h3 className="type-section">Appearances</h3>
        <div className={styles.compareGrid}>
          {INPUT_APPEARANCES.map((item) => (
            <Input key={item} appearance={item} label={APPEARANCE_LABELS[item]} defaultValue="Market" placeholder="Account name" autoComplete="off" />
          ))}
        </div>
      </section>
    </section>
  );

  if (embedded) {
    return (
      <div className={styles.embedded}>
        <p className={styles.note}>
          One preview follows the controls. Stacked is the shipped default, and Default global does not follow the controls. Outlined, soft, and filled float the same label. Disabled fields do not animate. Error and success keep their message.
        </p>
        {playground}
      </div>
    );
  }

  return (
    <div className={styles.page}>
      <div className={styles.layout}>
        <header className={styles.intro}>
          <a className={styles.back} href="/design-system#inputs">
            Design system
          </a>
          <h1 className="type-page">Input playground</h1>
          <p className="type-secondary">
            Change appearance and focus motion on one preview. Stacked stays the shipped default. Theme, primary, corner shape, and density stay in Appearance.
          </p>
        </header>
        <div className={styles.workspace}>
          {playground}
          <aside className={styles.panel} aria-label="Appearance">
            <h2 className="type-section">Appearance</h2>
            <AppearanceControls name="input-motion" />
          </aside>
        </div>
      </div>
    </div>
  );
}

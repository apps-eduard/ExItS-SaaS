import { useRef, useState } from "react";
import { Button } from "../../components/button/Button";
import { Input } from "../../components/input/Input";
import { INPUT_APPEARANCES, type InputAppearance } from "../../components/input/input-appearance";
import { INPUT_FOCUS_MOTION_OPTIONS, type InputFocusMotion } from "../../components/input/focus-motion";
import { SearchField } from "../../components/search/SearchField";
import { Select } from "../../components/select/Select";
import { Switch } from "../../components/switch/Switch";
import { CursorCopyCommand } from "../CursorCopyCommand";
import { PlaygroundShell } from "../shell/PlaygroundShell";
import { inputCursorCommand } from "./input-cursor-command";
import styles from "./InputPlayground.module.scss";

type FieldKind = "text" | "search";
type FieldStatus = "none" | "error" | "success";

const APPEARANCE_LABELS: Record<InputAppearance, string> = {
  outlined: "Outlined",
  soft: "Soft",
  standard: "Standard",
  floating: "Floating label",
};

const FOCUS_LABELS: Record<InputFocusMotion, string> = {
  none: "None",
  "border-reveal": "Border Reveal",
  "soft-glow": "Soft Glow",
  "running-accent": "Running Accent",
  "focus-draw": "Focus Draw",
};

const ERROR_MESSAGE = "Use at least two letters.";
const SUCCESS_MESSAGE = "Name is available.";

export function InputPlayground() {
  const previewRef = useRef<HTMLDivElement>(null);
  const [appearance, setAppearance] = useState<InputAppearance>("standard");
  const [focusMotion, setFocusMotion] = useState<InputFocusMotion>("border-reveal");
  const [kind, setKind] = useState<FieldKind>("text");
  const [status, setStatus] = useState<FieldStatus>("none");
  const [disabled, setDisabled] = useState(false);
  const [readOnly, setReadOnly] = useState(false);
  const [required, setRequired] = useState(false);
  const [label, setLabel] = useState("Name");
  const [text, setText] = useState("");
  const [query, setQuery] = useState("Market");

  const previewLabel = label.trim() || "Name";
  const error = status === "error" ? ERROR_MESSAGE : undefined;
  const success = status === "success" && kind === "text" ? SUCCESS_MESSAGE : undefined;
  const previewKey = [appearance, focusMotion, kind, status, disabled, readOnly, required].join(":");
  const cursorCommand = inputCursorCommand({
    appearance,
    focusMotion,
    kind,
    status,
    disabled,
    readOnly,
    required,
  });

  function replay() {
    const node = previewRef.current?.querySelector("input");
    if (!(node instanceof HTMLInputElement) || node.disabled) return;
    node.blur();
    node.focus();
  }

  function reset() {
    setAppearance("standard");
    setFocusMotion("border-reveal");
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

  return (
    <PlaygroundShell
      title="Inputs"
      description="One preview follows appearance, focus motion, and field state. Soft keeps the label inside the tint, Outlined floats it on the border, Floating label rises from the placeholder, and Standard stays the shipped field."
      panes="equal"
      propertiesColumns={2}
      preview={
        <div className={styles.previewStack}>
          <div className={styles.previewField} ref={previewRef}>
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
                focusMotion={focusMotion}
              />
            ) : (
              <Input
                key={previewKey}
                label={previewLabel}
                appearance={appearance}
                focusMotion={focusMotion}
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
          <CursorCopyCommand standard="Input" command={cursorCommand} />
        </div>
      }
      properties={
        <>
          <Select label="Appearance" value={appearance} onChange={(event) => setAppearance(event.target.value as InputAppearance)}>
            {INPUT_APPEARANCES.map((item) => (
              <option key={item} value={item}>
                {APPEARANCE_LABELS[item]}
              </option>
            ))}
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
          <Select label="Field" value={kind} onChange={(event) => chooseKind(event.target.value as FieldKind)}>
            <option value="text">Text</option>
            <option value="search">Search</option>
          </Select>
          <Select
            label="Status"
            value={kind === "search" && status === "success" ? "none" : status}
            onChange={(event) => setStatus(event.target.value as FieldStatus)}
          >
            <option value="none">None</option>
            <option value="error">Error</option>
            <option value="success" disabled={kind === "search"}>
              Success
            </option>
          </Select>
          <div className={styles.spanFull}>
            <Input label="Label" value={label} onChange={(event) => setLabel(event.target.value)} autoComplete="off" />
          </div>
          <div className={`${styles.toggles} ${styles.spanFull}`}>
            <Switch label="Disabled" checked={disabled} onChange={(event) => setDisabled(event.target.checked)} />
            <Switch label="Read-only" checked={readOnly} disabled={kind === "search"} onChange={(event) => setReadOnly(event.target.checked)} />
            <Switch label="Required" checked={required} disabled={kind === "search"} onChange={(event) => setRequired(event.target.checked)} />
          </div>
          <div className={`${styles.actions} ${styles.spanFull}`}>
            <Button variant="secondary" onClick={reset}>
              Default global
            </Button>
            {focusMotion === "border-reveal" || focusMotion === "focus-draw" ? (
              <Button variant="secondary" onClick={replay}>
                Replay
              </Button>
            ) : null}
          </div>
        </>
      }
      examples={
        <>
          {INPUT_APPEARANCES.map((item) => (
            <article key={item} className={styles.example}>
              <p className={styles.exampleLabel}>{APPEARANCE_LABELS[item]}</p>
              <Input appearance={item} label={APPEARANCE_LABELS[item]} defaultValue="Market" placeholder="Account name" autoComplete="off" />
            </article>
          ))}
          <article className={styles.example}>
            <p className={styles.exampleLabel}>Error</p>
            <Input label="Name" defaultValue="?" error={ERROR_MESSAGE} autoComplete="off" />
          </article>
          <article className={styles.example}>
            <p className={styles.exampleLabel}>Disabled</p>
            <Input label="Name" value="Locked" disabled onChange={() => undefined} autoComplete="off" />
          </article>
          <article className={styles.example}>
            <p className={styles.exampleLabel}>Search</p>
            <SearchField label="Search" value="Market" onChange={() => undefined} placeholder="Name or note" />
          </article>
        </>
      }
    />
  );
}

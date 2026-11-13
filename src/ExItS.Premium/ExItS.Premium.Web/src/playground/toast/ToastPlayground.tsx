import { useState } from "react";
import { Button } from "../../components/button/Button";
import { Input } from "../../components/input/Input";
import { Select } from "../../components/select/Select";
import { Switch } from "../../components/switch/Switch";
import {
  TOAST_DEFAULT_POSITION,
  TOAST_POSITIONS,
  TOAST_TONES,
  useToast,
  type ToastPosition,
  type ToastTone,
} from "../../components/toast/ToastProvider";
import { CursorCopyCommand } from "../CursorCopyCommand";
import { PlaygroundShell } from "../shell/PlaygroundShell";
import { toastCursorCommand } from "./toast-cursor-command";
import styles from "./ToastPlayground.module.scss";

const TONE_LABELS: Record<ToastTone, string> = {
  info: "Info",
  success: "Success",
  warning: "Warning",
  danger: "Danger",
};

const POSITION_LABELS: Record<ToastPosition, string> = {
  "top-start": "Top start",
  "top-center": "Top center",
  "top-end": "Top end",
  "bottom-start": "Bottom start",
  "bottom-center": "Bottom center",
  "bottom-end": "Bottom end",
};

const SAMPLE_TITLES: Record<ToastTone, string> = {
  info: "Reminder set",
  success: "Saved",
  warning: "Payment due soon",
  danger: "Could not save",
};

const SAMPLE_DESCRIPTIONS: Record<ToastTone, string> = {
  info: "You will get a nudge before the due date.",
  success: "The change is local to this preview.",
  warning: "Review the balance before the due date.",
  danger: "Try the action again.",
};

export function ToastPlayground() {
  const toast = useToast();
  const [tone, setTone] = useState<ToastTone>("success");
  const [position, setPosition] = useState<ToastPosition>(TOAST_DEFAULT_POSITION);
  const [title, setTitle] = useState(SAMPLE_TITLES.success);
  const [description, setDescription] = useState(SAMPLE_DESCRIPTIONS.success);
  const [withDescription, setWithDescription] = useState(true);

  const previewTitle = title.trim() || SAMPLE_TITLES[tone];
  const previewDescription = description.trim() || SAMPLE_DESCRIPTIONS[tone];
  const cursorCommand = toastCursorCommand({ tone, position, description: withDescription });

  function reset() {
    setTone("success");
    choosePosition(TOAST_DEFAULT_POSITION);
    setTitle(SAMPLE_TITLES.success);
    setDescription(SAMPLE_DESCRIPTIONS.success);
    setWithDescription(true);
  }

  function chooseTone(next: ToastTone) {
    setTone(next);
    setTitle(SAMPLE_TITLES[next]);
    setDescription(SAMPLE_DESCRIPTIONS[next]);
  }

  function choosePosition(next: ToastPosition) {
    setPosition(next);
    toast.setPosition(next);
  }

  function showToast(nextTone: ToastTone = tone, includeDescription = withDescription) {
    toast.push({
      tone: nextTone,
      title: nextTone === tone ? previewTitle : SAMPLE_TITLES[nextTone],
      description: includeDescription
        ? nextTone === tone
          ? previewDescription
          : SAMPLE_DESCRIPTIONS[nextTone]
        : undefined,
    });
  }

  return (
    <PlaygroundShell
      title="Toasts"
      description="Transient notifications anchored by Position (six corners and centers). Info, Success, Warning, and Danger share elevation and dismiss. Hover pauses auto-dismiss. Show Toast fires the live stack — Cursor copy captures the recipe."
      panes="equal"
      propertiesColumns={2}
      preview={
        <div className={styles.previewStack}>
          <div className={styles.previewField}>
            <Button
              variant={
                tone === "danger"
                  ? "danger"
                  : tone === "success"
                    ? "success"
                    : tone === "warning"
                      ? "warning"
                      : "primary"
              }
              onClick={() => showToast()}
            >
              Show toast
            </Button>
          </div>
          <CursorCopyCommand standard="Toast" command={cursorCommand} />
        </div>
      }
      properties={
        <>
          <Select
            label="Tone"
            value={tone}
            onChange={(event) => chooseTone(event.target.value as ToastTone)}
          >
            {TOAST_TONES.map((value) => (
              <option key={value} value={value}>
                {TONE_LABELS[value]}
              </option>
            ))}
          </Select>
          <Select
            label="Position"
            value={position}
            onChange={(event) => choosePosition(event.target.value as ToastPosition)}
          >
            {TOAST_POSITIONS.map((value) => (
              <option key={value} value={value}>
                {POSITION_LABELS[value]}
              </option>
            ))}
          </Select>
          <div className={styles.spanFull}>
            <Input label="Title" value={title} onChange={(event) => setTitle(event.target.value)} />
          </div>
          <div className={styles.spanFull}>
            <Input
              label="Description"
              value={description}
              onChange={(event) => setDescription(event.target.value)}
              disabled={!withDescription}
            />
          </div>
          <Switch
            label="Description"
            checked={withDescription}
            onChange={(event) => setWithDescription(event.target.checked)}
          />
          <div className={`${styles.actions} ${styles.spanFull}`}>
            <Button variant="secondary" onClick={reset}>
              Default global
            </Button>
          </div>
        </>
      }
      examples={
        <div className={styles.examplesStack}>
          <div className={styles.example}>
            <p className={styles.exampleLabel}>Tones</p>
            <div className={styles.exampleRow}>
              {TOAST_TONES.map((value) => (
                <Button
                  key={value}
                  variant={value === "danger" ? "danger" : value === "success" ? "success" : "secondary"}
                  onClick={() => {
                    chooseTone(value);
                    showToast(value, true);
                  }}
                >
                  {TONE_LABELS[value]}
                </Button>
              ))}
            </div>
          </div>
          <div className={styles.example}>
            <p className={styles.exampleLabel}>Positions</p>
            <div className={styles.exampleRow}>
              {TOAST_POSITIONS.map((value) => (
                <Button
                  key={value}
                  variant="ghost"
                  onClick={() => {
                    choosePosition(value);
                    showToast(tone, withDescription);
                  }}
                >
                  {POSITION_LABELS[value]}
                </Button>
              ))}
            </div>
          </div>
          <div className={styles.example}>
            <p className={styles.exampleLabel}>Title only</p>
            <div className={styles.exampleRow}>
              {TOAST_TONES.map((value) => (
                <Button
                  key={value}
                  variant="ghost"
                  onClick={() => {
                    chooseTone(value);
                    setWithDescription(false);
                    showToast(value, false);
                  }}
                >
                  {TONE_LABELS[value]}
                </Button>
              ))}
            </div>
          </div>
        </div>
      }
    />
  );
}

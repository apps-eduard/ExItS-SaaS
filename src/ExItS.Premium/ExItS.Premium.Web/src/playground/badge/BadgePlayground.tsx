import { useState } from "react";
import { BADGE_TONES, Badge, type BadgeTone } from "../../components/badge/Badge";
import { Button } from "../../components/button/Button";
import { Input } from "../../components/input/Input";
import { Select } from "../../components/select/Select";
import { CursorCopyCommand } from "../CursorCopyCommand";
import { PlaygroundShell } from "../shell/PlaygroundShell";
import { badgeCursorCommand } from "./badge-cursor-command";
import styles from "./BadgePlayground.module.scss";

const TONE_LABELS: Record<BadgeTone, string> = {
  neutral: "Neutral",
  primary: "Primary",
  success: "Success",
  warning: "Warning",
  danger: "Danger",
  info: "Info",
};

const SAMPLE_LABELS: Record<BadgeTone, string> = {
  neutral: "Neutral",
  primary: "Primary",
  success: "Paid",
  warning: "Due",
  danger: "Failed",
  info: "Info",
};

export function BadgePlayground() {
  const [tone, setTone] = useState<BadgeTone>("success");
  const [label, setLabel] = useState("Paid");

  const previewLabel = label.trim() || SAMPLE_LABELS[tone];
  const cursorCommand = badgeCursorCommand({ tone });

  function reset() {
    setTone("success");
    setLabel("Paid");
  }

  function chooseTone(next: BadgeTone) {
    setTone(next);
    setLabel(SAMPLE_LABELS[next]);
  }

  return (
    <PlaygroundShell
      title="Badges"
      description="Status badges use soft tints with contrast-safe ink. Warning stays a solid fill because its hue is too light for small text on a pale background."
      panes="equal"
      propertiesColumns={2}
      preview={
        <div className={styles.previewStack}>
          <div className={styles.previewField}>
            <Badge tone={tone}>{previewLabel}</Badge>
          </div>
          <CursorCopyCommand standard="Badge" command={cursorCommand} />
        </div>
      }
      properties={
        <>
          <Select label="Tone" value={tone} onChange={(event) => chooseTone(event.target.value as BadgeTone)}>
            {BADGE_TONES.map((item) => (
              <option key={item} value={item}>
                {TONE_LABELS[item]}
              </option>
            ))}
          </Select>
          <Input label="Label" value={label} onChange={(event) => setLabel(event.target.value)} autoComplete="off" />
          <div className={`${styles.actions} ${styles.spanFull}`}>
            <Button variant="secondary" onClick={reset}>
              Default global
            </Button>
          </div>
        </>
      }
      examples={
        <div className={styles.examplesStack}>
          <article className={styles.example}>
            <p className={styles.exampleLabel}>Semantic tones</p>
            <div className={styles.exampleRow}>
              {BADGE_TONES.map((item) => (
                <Badge key={item} tone={item}>
                  {SAMPLE_LABELS[item]}
                </Badge>
              ))}
            </div>
          </article>
        </div>
      }
    />
  );
}

import { useState } from "react";
import {
  BADGE_APPEARANCES,
  BADGE_TONES,
  Badge,
  type BadgeAppearance,
  type BadgeTone,
} from "../../components/badge/Badge";
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

const APPEARANCE_LABELS: Record<BadgeAppearance, string> = {
  soft: "Soft",
  emphasis: "Emphasis",
  solid: "Solid",
  outline: "Outline",
  ghost: "Ghost",
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
  const [appearance, setAppearance] = useState<BadgeAppearance>("soft");
  const [label, setLabel] = useState("Paid");

  const previewLabel = label.trim() || SAMPLE_LABELS[tone];
  const cursorCommand = badgeCursorCommand({ tone, appearance });

  function reset() {
    setTone("success");
    setAppearance("soft");
    setLabel("Paid");
  }

  function chooseTone(next: BadgeTone) {
    setTone(next);
    setLabel(SAMPLE_LABELS[next]);
  }

  return (
    <PlaygroundShell
      title="Badges"
      description="Status badges follow tone and appearance. Soft is the default tint. Emphasis is a stronger wash with bolder ink. Solid fills the chip, outline keeps a border, and ghost is ink only. Prefer solid for warning when soft tint contrast is too low."
      panes="equal"
      propertiesColumns={2}
      preview={
        <div className={styles.previewStack}>
          <div className={styles.previewField}>
            <Badge tone={tone} appearance={appearance}>
              {previewLabel}
            </Badge>
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
          <Select
            label="Appearance"
            value={appearance}
            onChange={(event) => setAppearance(event.target.value as BadgeAppearance)}
          >
            {BADGE_APPEARANCES.map((item) => (
              <option key={item} value={item}>
                {APPEARANCE_LABELS[item]}
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
          {BADGE_APPEARANCES.map((item) => (
            <article key={item} className={styles.example}>
              <p className={styles.exampleLabel}>
                {item === "soft" ? "Semantic · Soft" : APPEARANCE_LABELS[item]}
              </p>
              <div className={styles.exampleRow}>
                {BADGE_TONES.map((toneItem) => (
                  <Badge key={toneItem} tone={toneItem} appearance={item}>
                    {SAMPLE_LABELS[toneItem]}
                  </Badge>
                ))}
              </div>
            </article>
          ))}
        </div>
      }
    />
  );
}

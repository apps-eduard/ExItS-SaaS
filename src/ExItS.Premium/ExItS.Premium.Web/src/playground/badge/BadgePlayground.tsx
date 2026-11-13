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
import { Switch } from "../../components/switch/Switch";
import { CursorCopyCommand } from "../CursorCopyCommand";
import { PlaygroundShell } from "../shell/PlaygroundShell";
import { badgeCursorCommand } from "./badge-cursor-command";
import styles from "./BadgePlayground.module.scss";

const TONE_LABELS: Record<BadgeTone, string> = {
  neutral: "Neutral",
  primary: "Primary",
  secondary: "Secondary",
  accent: "Accent",
  success: "Success",
  info: "Info",
  warning: "Warning",
  help: "Help",
  danger: "Danger",
  contrast: "Contrast",
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
  secondary: "Secondary",
  accent: "Accent",
  success: "Paid",
  info: "Info",
  warning: "Due",
  help: "Help",
  danger: "Failed",
  contrast: "Contrast",
};

export function BadgePlayground() {
  const [tone, setTone] = useState<BadgeTone>("success");
  const [appearance, setAppearance] = useState<BadgeAppearance>("soft");
  const [label, setLabel] = useState("Paid");
  const [allCaps, setAllCaps] = useState(false);
  const [indicator, setIndicator] = useState(false);

  const previewLabel = label.trim() || SAMPLE_LABELS[tone];
  const cursorCommand = badgeCursorCommand({ tone, appearance, allCaps, indicator });

  function reset() {
    setTone("success");
    setAppearance("soft");
    setLabel("Paid");
    setAllCaps(false);
    setIndicator(false);
  }

  function chooseTone(next: BadgeTone) {
    setTone(next);
    setLabel(SAMPLE_LABELS[next]);
  }

  return (
    <PlaygroundShell
      title="Badges"
      description="Status badges follow tone and appearance. Tone includes neutral plus the full semantic set. Soft is the default tint. Emphasis is a stronger wash with bolder ink. Solid fills the chip, outline keeps a border, and ghost is ink only. All caps uppercases the label. Indicator adds a leading status pip."
      panes="equal"
      propertiesColumns={2}
      preview={
        <div className={styles.previewStack}>
          <div className={styles.previewField}>
            <Badge tone={tone} appearance={appearance} allCaps={allCaps} indicator={indicator}>
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
          <div className={`${styles.toggles} ${styles.spanFull}`}>
            <Switch label="All caps" checked={allCaps} onChange={(event) => setAllCaps(event.target.checked)} />
            <Switch label="Indicator" checked={indicator} onChange={(event) => setIndicator(event.target.checked)} />
          </div>
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
          <article className={styles.example}>
            <p className={styles.exampleLabel}>All caps</p>
            <div className={styles.exampleRow}>
              {BADGE_TONES.map((toneItem) => (
                <Badge key={toneItem} tone={toneItem} allCaps>
                  {SAMPLE_LABELS[toneItem]}
                </Badge>
              ))}
            </div>
          </article>
          <article className={styles.example}>
            <p className={styles.exampleLabel}>Indicator</p>
            <div className={styles.exampleRow}>
              {BADGE_TONES.map((toneItem) => (
                <Badge key={toneItem} tone={toneItem} indicator>
                  {SAMPLE_LABELS[toneItem]}
                </Badge>
              ))}
            </div>
          </article>
          <article className={styles.example}>
            <p className={styles.exampleLabel}>Indicator only</p>
            <div className={styles.exampleRow}>
              {BADGE_TONES.map((toneItem) => (
                <Badge key={toneItem} tone={toneItem} indicator aria-label={SAMPLE_LABELS[toneItem]} />
              ))}
            </div>
          </article>
        </div>
      }
    />
  );
}

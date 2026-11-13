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

const COUNT_SAMPLES: Array<{ tone: BadgeTone; appearance: BadgeAppearance; count: number }> = [
  { tone: "primary", appearance: "solid", count: 1 },
  { tone: "success", appearance: "solid", count: 9 },
  { tone: "info", appearance: "solid", count: 12 },
  { tone: "warning", appearance: "solid", count: 42 },
  { tone: "danger", appearance: "solid", count: 99 },
  { tone: "help", appearance: "solid", count: 128 },
  { tone: "neutral", appearance: "soft", count: 0 },
  { tone: "accent", appearance: "emphasis", count: 3 },
];

export function BadgePlayground() {
  const [tone, setTone] = useState<BadgeTone>("success");
  const [appearance, setAppearance] = useState<BadgeAppearance>("soft");
  const [label, setLabel] = useState("Paid");
  const [allCaps, setAllCaps] = useState(false);
  const [indicator, setIndicator] = useState(false);
  const [removable, setRemovable] = useState(false);
  const [removed, setRemoved] = useState(false);
  const [countSample, setCountSample] = useState(COUNT_SAMPLES[0]);
  const [indicatorSample, setIndicatorSample] = useState<BadgeTone>("success");
  const [indicatorOnlySample, setIndicatorOnlySample] = useState<BadgeTone>("danger");

  const previewLabel = label.trim() || SAMPLE_LABELS[tone];
  const cursorCommand = badgeCursorCommand({ tone, appearance, allCaps, indicator, removable });
  const countCursorCommand = badgeCursorCommand({
    tone: countSample.tone,
    appearance: countSample.appearance,
    allCaps: false,
    indicator: false,
    removable: false,
    count: true,
  });
  const indicatorCursorCommand = badgeCursorCommand({
    tone: indicatorSample,
    appearance: "soft",
    allCaps: false,
    indicator: true,
    removable: false,
  });
  const indicatorOnlyCursorCommand = badgeCursorCommand({
    tone: indicatorOnlySample,
    appearance: "soft",
    allCaps: false,
    indicator: true,
    removable: false,
    indicatorOnly: true,
  });

  function reset() {
    setTone("success");
    setAppearance("soft");
    setLabel("Paid");
    setAllCaps(false);
    setIndicator(false);
    setRemovable(false);
    setRemoved(false);
  }

  function chooseTone(next: BadgeTone) {
    setTone(next);
    setLabel(SAMPLE_LABELS[next]);
    setRemoved(false);
  }

  function chooseRemovable(next: boolean) {
    setRemovable(next);
    if (!next) setRemoved(false);
  }

  return (
    <PlaygroundShell
      title="Badges"
      description="Status badges follow tone and appearance in light and dark. Soft and emphasis washes mix against the theme surface. Solid uses tone fill plus contrast-safe foreground. Outline and ghost use theme ink. All caps uppercases the label. Indicator adds a leading pip. Remove adds a trailing dismiss control. Count and Indicator samples below include Cursor copy commands."
      panes="equal"
      propertiesColumns={2}
      preview={
        <div className={styles.previewStack}>
          <div className={styles.previewField}>
            {removed ? (
              <p className={styles.removedNote}>Removed — toggle Remove off or Default global to restore.</p>
            ) : (
              <Badge
                tone={tone}
                appearance={appearance}
                allCaps={allCaps}
                indicator={indicator}
                removable={removable}
                onRemove={() => setRemoved(true)}
              >
                {previewLabel}
              </Badge>
            )}
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
            <Switch
              label="Remove"
              checked={removable}
              onChange={(event) => chooseRemovable(event.target.checked)}
            />
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
            <p className={styles.exampleLabel}>Remove</p>
            <div className={styles.exampleRow}>
              {BADGE_TONES.map((toneItem) => (
                <Badge key={toneItem} tone={toneItem} removable onRemove={() => undefined}>
                  {SAMPLE_LABELS[toneItem]}
                </Badge>
              ))}
            </div>
          </article>
          <article className={styles.example}>
            <p className={styles.exampleLabel}>Count</p>
            <div className={styles.exampleRow}>
              {COUNT_SAMPLES.map((sample) => {
                const selected =
                  sample.tone === countSample.tone &&
                  sample.appearance === countSample.appearance &&
                  sample.count === countSample.count;
                return (
                  <button
                    key={`${sample.tone}-${sample.appearance}-${sample.count}`}
                    type="button"
                    className={styles.samplePick}
                    data-selected={selected ? "true" : "false"}
                    aria-pressed={selected}
                    aria-label={`Count sample ${sample.tone} ${sample.appearance}`}
                    onClick={() => setCountSample(sample)}
                  >
                    <Badge tone={sample.tone} appearance={sample.appearance} count={sample.count} />
                  </button>
                );
              })}
            </div>
            <CursorCopyCommand standard="Badge" command={countCursorCommand} className={styles.sampleCursor} />
          </article>
          <article className={styles.example}>
            <p className={styles.exampleLabel}>Indicator</p>
            <div className={styles.exampleRow}>
              {BADGE_TONES.map((toneItem) => (
                <button
                  key={toneItem}
                  type="button"
                  className={styles.samplePick}
                  data-selected={indicatorSample === toneItem ? "true" : "false"}
                  aria-pressed={indicatorSample === toneItem}
                  aria-label={`Indicator sample ${toneItem}`}
                  onClick={() => setIndicatorSample(toneItem)}
                >
                  <Badge tone={toneItem} indicator>
                    {SAMPLE_LABELS[toneItem]}
                  </Badge>
                </button>
              ))}
            </div>
            <CursorCopyCommand standard="Badge" command={indicatorCursorCommand} className={styles.sampleCursor} />
          </article>
          <article className={styles.example}>
            <p className={styles.exampleLabel}>Indicator only</p>
            <div className={styles.exampleRow}>
              {BADGE_TONES.map((toneItem) => (
                <button
                  key={toneItem}
                  type="button"
                  className={styles.samplePick}
                  data-selected={indicatorOnlySample === toneItem ? "true" : "false"}
                  aria-pressed={indicatorOnlySample === toneItem}
                  aria-label={`Indicator only sample ${toneItem}`}
                  onClick={() => setIndicatorOnlySample(toneItem)}
                >
                  <Badge tone={toneItem} indicator aria-label={SAMPLE_LABELS[toneItem]} />
                </button>
              ))}
            </div>
            <CursorCopyCommand
              standard="Badge"
              command={indicatorOnlyCursorCommand}
              className={styles.sampleCursor}
            />
          </article>
        </div>
      }
    />
  );
}

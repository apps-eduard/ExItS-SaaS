import { useState } from "react";
import {
  BUTTON_APPEARANCES,
  BUTTON_SEMANTIC_VARIANTS,
  Button,
  type ButtonAppearance,
  type ButtonVariant,
} from "../../components/button/Button";
import { Input } from "../../components/input/Input";
import { Select } from "../../components/select/Select";
import { Switch } from "../../components/switch/Switch";
import { AppIcon } from "../../icons/ExIcon";
import { PlaygroundShell } from "../shell/PlaygroundShell";
import styles from "./ButtonPlayground.module.scss";

const TONE_LABELS: Record<ButtonVariant, string> = {
  primary: "Primary",
  secondary: "Secondary",
  accent: "Accent",
  success: "Success",
  info: "Info",
  warning: "Warning",
  help: "Help",
  danger: "Danger",
  contrast: "Contrast",
  ghost: "Ghost",
};

const APPEARANCE_LABELS: Record<ButtonAppearance, string> = {
  solid: "Solid",
  soft: "Soft",
  outline: "Outline",
  raise: "Raise",
  "solid-raise": "Solid raise",
};

export function ButtonPlayground() {
  const [variant, setVariant] = useState<ButtonVariant>("primary");
  const [appearance, setAppearance] = useState<ButtonAppearance>("solid");
  const [label, setLabel] = useState("Save");
  const [disabled, setDisabled] = useState(false);
  const [loading, setLoading] = useState(false);
  const [iconOnly, setIconOnly] = useState(false);
  const [fullWidth, setFullWidth] = useState(false);

  const previewLabel = label.trim() || "Save";
  const previewKey = [variant, appearance, disabled, loading, iconOnly, fullWidth].join(":");

  function reset() {
    setVariant("primary");
    setAppearance("solid");
    setLabel("Save");
    setDisabled(false);
    setLoading(false);
    setIconOnly(false);
    setFullWidth(false);
  }

  return (
    <PlaygroundShell
      title="Button"
      description="One preview follows semantic tone and appearance. Soft, outline, raise, and solid raise stay on the API. Ghost is chrome-only. Disabled and loading sit in States."
      preview={
        <div className={styles.previewField}>
          <Button
            key={previewKey}
            variant={variant}
            appearance={appearance}
            disabled={disabled}
            loading={loading}
            iconOnly={iconOnly}
            fullWidth={fullWidth}
            icon={iconOnly ? <AppIcon name="add" /> : undefined}
            aria-label={iconOnly ? previewLabel : undefined}
          >
            {iconOnly ? null : previewLabel}
          </Button>
        </div>
      }
      properties={
        <>
          <Select label="Tone" value={variant} onChange={(event) => setVariant(event.target.value as ButtonVariant)}>
            {BUTTON_SEMANTIC_VARIANTS.map((item) => (
              <option key={item} value={item}>
                {TONE_LABELS[item]}
              </option>
            ))}
            <option value="ghost">{TONE_LABELS.ghost}</option>
          </Select>
          <Select
            label="Appearance"
            value={appearance}
            onChange={(event) => setAppearance(event.target.value as ButtonAppearance)}
          >
            {BUTTON_APPEARANCES.map((item) => (
              <option key={item} value={item}>
                {APPEARANCE_LABELS[item]}
              </option>
            ))}
          </Select>
          <Input label="Label" value={label} onChange={(event) => setLabel(event.target.value)} autoComplete="off" />
          <div className={styles.toggles}>
            <Switch label="Disabled" checked={disabled} onChange={(event) => setDisabled(event.target.checked)} />
            <Switch label="Loading" checked={loading} onChange={(event) => setLoading(event.target.checked)} />
            <Switch label="Icon only" checked={iconOnly} onChange={(event) => setIconOnly(event.target.checked)} />
            <Switch label="Full width" checked={fullWidth} onChange={(event) => setFullWidth(event.target.checked)} />
          </div>
          <div className={styles.actions}>
            <Button variant="secondary" onClick={reset}>
              Default global
            </Button>
          </div>
        </>
      }
      examples={
        <>
          <article className={styles.example}>
            <p className={styles.exampleLabel}>Semantic</p>
            <div className={styles.exampleRow}>
              {BUTTON_SEMANTIC_VARIANTS.map((item) => (
                <Button key={item} variant={item}>
                  {TONE_LABELS[item]}
                </Button>
              ))}
            </div>
          </article>
          {BUTTON_APPEARANCES.map((item) => (
            <article key={item} className={styles.example}>
              <p className={styles.exampleLabel}>{APPEARANCE_LABELS[item]}</p>
              <div className={styles.exampleRow}>
                <Button appearance={item} variant="primary">
                  Primary
                </Button>
                <Button appearance={item} variant="danger">
                  Danger
                </Button>
                <Button appearance={item} variant="success">
                  Success
                </Button>
              </div>
            </article>
          ))}
          <article className={styles.example}>
            <p className={styles.exampleLabel}>Disabled</p>
            <div className={styles.exampleRow}>
              <Button disabled>Disabled</Button>
              <Button variant="secondary" disabled>
                Secondary
              </Button>
            </div>
          </article>
          <article className={styles.example}>
            <p className={styles.exampleLabel}>Loading</p>
            <Button loading>Saving</Button>
          </article>
        </>
      }
    />
  );
}

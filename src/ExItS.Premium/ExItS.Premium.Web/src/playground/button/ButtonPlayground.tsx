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
import type { IconName } from "../../icons/registry";
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
  "soft-raise": "Soft raise",
  "solid-raise": "Solid raise",
};

const BUTTON_ICONS = [
  "add",
  "check",
  "close",
  "edit_note",
  "search",
  "payments",
  "info",
  "warning",
  "expand_more",
  "notifications",
] as const satisfies readonly IconName[];

type ButtonIconChoice = "none" | (typeof BUTTON_ICONS)[number];

const ICON_LABELS: Record<(typeof BUTTON_ICONS)[number], string> = {
  add: "Add",
  check: "Check",
  close: "Close",
  edit_note: "Edit",
  search: "Search",
  payments: "Payments",
  info: "Info",
  warning: "Warning",
  expand_more: "Expand",
  notifications: "Notifications",
};

export function ButtonPlayground() {
  const [variant, setVariant] = useState<ButtonVariant>("primary");
  const [appearance, setAppearance] = useState<ButtonAppearance>("solid");
  const [label, setLabel] = useState("Save");
  const [iconName, setIconName] = useState<ButtonIconChoice>("none");
  const [disabled, setDisabled] = useState(false);
  const [loading, setLoading] = useState(false);
  const [iconOnly, setIconOnly] = useState(false);
  const [fullWidth, setFullWidth] = useState(false);

  const previewLabel = label.trim() || "Save";
  const resolvedIcon: IconName = iconName === "none" ? "add" : iconName;
  const showIcon = iconOnly || iconName !== "none";
  const previewKey = [variant, appearance, iconName, disabled, loading, iconOnly, fullWidth].join(":");

  function reset() {
    setVariant("primary");
    setAppearance("solid");
    setLabel("Save");
    setIconName("none");
    setDisabled(false);
    setLoading(false);
    setIconOnly(false);
    setFullWidth(false);
  }

  function chooseIconOnly(next: boolean) {
    setIconOnly(next);
    if (next && iconName === "none") setIconName("add");
  }

  return (
    <PlaygroundShell
      title="Button"
      description="One preview follows semantic tone and appearance. Soft, outline, raise, and solid raise stay on the API. Ghost is chrome-only. Disabled and loading sit in States."
      panes="equal"
      propertiesColumns={2}
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
            icon={showIcon ? <AppIcon name={resolvedIcon} /> : undefined}
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
          <Select
            label="Icon"
            value={iconName}
            onChange={(event) => setIconName(event.target.value as ButtonIconChoice)}
          >
            <option value="none">None</option>
            {BUTTON_ICONS.map((item) => (
              <option key={item} value={item}>
                {ICON_LABELS[item]}
              </option>
            ))}
          </Select>
          <div className={`${styles.toggles} ${styles.spanFull}`}>
            <Switch label="Disabled" checked={disabled} onChange={(event) => setDisabled(event.target.checked)} />
            <Switch label="Loading" checked={loading} onChange={(event) => setLoading(event.target.checked)} />
            <Switch label="Icon only" checked={iconOnly} onChange={(event) => chooseIconOnly(event.target.checked)} />
            <Switch label="Full width" checked={fullWidth} onChange={(event) => setFullWidth(event.target.checked)} />
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
          {BUTTON_APPEARANCES.map((item) => (
            <article key={item} className={styles.example}>
              <p className={styles.exampleLabel}>
                {item === "solid" ? "Semantic · Solid" : APPEARANCE_LABELS[item]}
              </p>
              <div className={styles.exampleRow}>
                {BUTTON_SEMANTIC_VARIANTS.map((tone) => (
                  <Button key={tone} appearance={item} variant={tone}>
                    {TONE_LABELS[tone]}
                  </Button>
                ))}
              </div>
            </article>
          ))}
          <article className={styles.example}>
            <p className={styles.exampleLabel}>States</p>
            <div className={styles.exampleRow}>
              <Button disabled>Disabled</Button>
              <Button variant="secondary" disabled>
                Secondary
              </Button>
              <Button loading>Saving</Button>
            </div>
          </article>
        </div>
      }
    />
  );
}

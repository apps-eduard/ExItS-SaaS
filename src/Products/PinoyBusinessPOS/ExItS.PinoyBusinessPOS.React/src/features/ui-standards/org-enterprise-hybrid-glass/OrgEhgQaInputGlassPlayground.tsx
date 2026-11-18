import { useState } from "react";
import { Mail, Moon, RotateCcw, Search, Sun, UserRound, type LucideIcon } from "lucide-react";
import { Button } from "@/components/ui/button";
import { CardTitle } from "@/components/ui/card";
import { Switch } from "@/components/ui/switch";
import { UiStandardsCopyCommand } from "@/features/ui-standards/UiStandardsCopyCommand";
import {
  CompactSelect,
  PlaygroundTextField,
} from "@/features/ui-standards/UiStandardsPlaygrounds";
import { PlaygroundLabel, PlaygroundSection } from "@/features/ui-standards/UiStandardsSnippetBlock";
import {
  ORG_EHG_PLAYGROUND_BG_DEFAULT,
  ORG_EHG_PLAYGROUND_BG_DEFAULT_INTENSITY,
  ORG_EHG_PLAYGROUND_BG_OPTIONS,
} from "@/features/ui-standards/org-enterprise-hybrid-glass/orgEhgPlaygroundBackground";
import {
  ORG_EHG_INPUT_GLASS_DEFAULT_APPEARANCE,
  ORG_EHG_INPUT_GLASS_DEFAULT_COMMAND,
  ORG_EHG_INPUT_GLASS_DEFAULT_SHAPE,
  ORG_EHG_INPUT_GLASS_DEFAULT_SIZE,
  ORG_EHG_INPUT_GLASS_DEFAULT_TONE,
  ORG_EHG_SEARCH_GLASS_DEFAULT_COMMAND,
} from "@/features/ui-standards/org-enterprise-hybrid-glass/org-ehg-input-glass-global-default";
import type {
  OrgEhgQaGlassIconShape,
  OrgEhgQaGlassSize,
  OrgEhgQaGlassTone,
} from "@/features/ui-standards/org-enterprise-hybrid-glass/OrgEhgQuickActionGlassGallery";
import { useUiStandardsShellAppearance } from "@/features/ui-standards/UiStandardsShellAppearance";
import { usePreferences } from "@/hooks/usePreferences";
import { type DensityPreference } from "@/lib/preferences/ui-preferences";

type InputIconId = "none" | "search" | "mail" | "user";

/** Same appearances as the Input playground. */
type InputAppearance =
  | "standard"
  | "soft"
  | "soft-outline"
  | "filled"
  | "outlined"
  | "standout"
  | "stacked";

/** Same icon chip looks as Button glass. */
type IconAppearance = "none" | "filled" | "ghost" | "outline" | "soft" | "glossy" | "bare";

type IconChipAppearance = Exclude<IconAppearance, "none" | "bare">;

const ICON_APPEARANCE_CHIP_CLASS: Record<IconChipAppearance, string> = {
  filled: "",
  ghost: " org-ehg-qa-glass__icon--ghost",
  outline: " org-ehg-qa-glass__icon--outline",
  soft: " org-ehg-qa-glass__icon--soft-fill",
  glossy: " org-ehg-qa-glass__icon--glossy",
};

const ICON_APPEARANCE_COMMAND: Partial<Record<IconAppearance, string>> = {
  none: "icon-none",
  ghost: "icon-ghost",
  outline: "icon-outline",
  soft: "icon-soft-fill",
  glossy: "icon-glossy",
  bare: "icon-bare",
};

const ICON_APPEARANCES: ReadonlyArray<{ label: string; value: IconAppearance }> = [
  { label: "None", value: "none" },
  { label: "Filled", value: "filled" },
  { label: "Ghost", value: "ghost" },
  { label: "Outline", value: "outline" },
  { label: "Soft", value: "soft" },
  { label: "Glossy", value: "glossy" },
  { label: "Bare", value: "bare" },
];

const INPUT_APPEARANCES: ReadonlyArray<{ label: string; value: InputAppearance }> = [
  { label: "Standard", value: "standard" },
  { label: "Soft", value: "soft" },
  { label: "Soft outline", value: "soft-outline" },
  { label: "Filled", value: "filled" },
  { label: "Outlined", value: "outlined" },
  { label: "Standout", value: "standout" },
  { label: "Label above", value: "stacked" },
];

const DENSITIES: ReadonlyArray<{ label: string; value: DensityPreference }> = [
  { label: "Compact", value: "compact" },
  { label: "Standard", value: "balance" },
  { label: "Comfort", value: "comfort" },
];

const TONES: ReadonlyArray<{ label: string; value: OrgEhgQaGlassTone }> = [
  { label: "Amber", value: "amber" },
  { label: "Charcoal", value: "charcoal" },
  { label: "Cyan", value: "cyan" },
  { label: "Danger", value: "danger" },
  { label: "Emerald", value: "emerald" },
  { label: "Fuchsia", value: "fuchsia" },
  { label: "Indigo", value: "indigo" },
  { label: "Info", value: "info" },
  { label: "Inventory", value: "inventory" },
  { label: "Lime", value: "lime" },
  { label: "Neutral", value: "neutral" },
  { label: "Orange", value: "orange" },
  { label: "People", value: "people" },
  { label: "Primary", value: "primary" },
  { label: "Rose", value: "rose" },
  { label: "Sales / POS", value: "sales" },
  { label: "Secondary", value: "secondary" },
  { label: "Sky", value: "sky" },
  { label: "Success", value: "success" },
  { label: "Teal", value: "teal" },
  { label: "Violet", value: "violet" },
  { label: "Warning", value: "warning" },
];

const ICON_SHAPES: ReadonlyArray<{ label: string; value: OrgEhgQaGlassIconShape }> = [
  { label: "Soft square", value: "soft" },
  { label: "Rounded circle", value: "rounded" },
];

const SIZES: ReadonlyArray<{ label: string; value: OrgEhgQaGlassSize }> = [
  { label: "SM", value: "sm" },
  { label: "MD", value: "md" },
  { label: "LG", value: "lg" },
];

/**
 * Input glass card shapes — Standard sits in the middle of the ladder.
 * Square → Small (control) → Standard (md) → Soft → Pill.
 */
type GlassCardShape = "square" | "small" | "standard" | "soft" | "pill";

const CARD_SHAPES: ReadonlyArray<{ label: string; value: GlassCardShape }> = [
  { label: "Square", value: "square" },
  { label: "Small", value: "small" },
  { label: "Standard", value: "standard" },
  { label: "Soft", value: "soft" },
  { label: "Pill", value: "pill" },
];

const ICONS: ReadonlyArray<{ label: string; value: InputIconId; Icon: LucideIcon | null }> = [
  { label: "None", value: "none", Icon: null },
  { label: "Search", value: "search", Icon: Search },
  { label: "Mail", value: "mail", Icon: Mail },
  { label: "User", value: "user", Icon: UserRound },
];

/** Locked playground reset command (must match ORG_EHG_INPUT_GLASS_DEFAULT_COMMAND). */
export const ORG_EHG_INPUT_GLASS_PLAYGROUND_DEFAULT_COMMAND = ORG_EHG_INPUT_GLASS_DEFAULT_COMMAND;

/** Search creation lock (icon-bare + search mark) — not the playground seed. */
export const ORG_EHG_SEARCH_GLASS_PLAYGROUND_COMMAND = ORG_EHG_SEARCH_GLASS_DEFAULT_COMMAND;

/** Seeded from `ORG_EHG_INPUT_GLASS_DEFAULT_COMMAND`. */
const DEFAULTS = {
  tone: ORG_EHG_INPUT_GLASS_DEFAULT_TONE as OrgEhgQaGlassTone,
  appearance: ORG_EHG_INPUT_GLASS_DEFAULT_APPEARANCE as InputAppearance,
  iconId: "none" as InputIconId,
  iconShape: "soft" as OrgEhgQaGlassIconShape,
  iconAppearance: "none" as IconAppearance,
  size: ORG_EHG_INPUT_GLASS_DEFAULT_SIZE as OrgEhgQaGlassSize,
  cardShape: ORG_EHG_INPUT_GLASS_DEFAULT_SHAPE as GlassCardShape,
  density: "balance" as DensityPreference,
  label: "Input",
  placeholder: "Type here…",
  ghost: false,
  shadow: false,
  iconShadow: false,
  clearable: false,
};

function inputGlassCommand(
  tone: OrgEhgQaGlassTone,
  appearance: InputAppearance,
  iconShape: OrgEhgQaGlassIconShape,
  iconAppearance: IconAppearance,
  size: OrgEhgQaGlassSize,
  cardShape: GlassCardShape,
  density: DensityPreference,
  ghost: boolean,
  shadow: boolean,
  iconShadow: boolean,
  clearable: boolean,
): string {
  const parts = [
    "ext",
    "org-ehg",
    "input-glass",
    "qa",
    tone,
    appearance,
    size,
    iconAppearance === "none" || iconAppearance === "bare"
      ? (ICON_APPEARANCE_COMMAND[iconAppearance] as string)
      : `icon-${iconShape}`,
    `shape-${cardShape}`,
  ];
  if (density !== "balance") parts.push(density);
  if (ghost) parts.push("ghost");
  if (!shadow) parts.push("no-shadow");
  if (iconAppearance !== "none" && iconAppearance !== "bare" && !iconShadow) {
    parts.push("no-icon-shadow");
  }
  const iconLook = ICON_APPEARANCE_COMMAND[iconAppearance];
  if (iconLook && iconAppearance !== "none" && iconAppearance !== "bare") parts.push(iconLook);
  if (clearable) parts.push("clearable");
  return parts.join(" ");
}

/**
 * Input glass playground — same Enterprise Hybrid Glass recipe as Quick Action glass.
 * UI Standards / DEV only.
 */
export function OrgEhgQaInputGlassPlayground() {
  const { background, setBackground, theme, toggleTheme } = useUiStandardsShellAppearance();
  const { preferences, setDensity } = usePreferences();
  const density = preferences.density;
  const [tone, setTone] = useState(DEFAULTS.tone);
  const [appearance, setAppearance] = useState(DEFAULTS.appearance);
  const [iconId, setIconId] = useState(DEFAULTS.iconId);
  const [iconShape, setIconShape] = useState(DEFAULTS.iconShape);
  const [iconAppearance, setIconAppearance] = useState(DEFAULTS.iconAppearance);
  const [size, setSize] = useState(DEFAULTS.size);
  const [cardShape, setCardShape] = useState(DEFAULTS.cardShape);
  const [label, setLabel] = useState(DEFAULTS.label);
  const [placeholder, setPlaceholder] = useState(DEFAULTS.placeholder);
  const [value, setValue] = useState("");
  const [ghost, setGhost] = useState(DEFAULTS.ghost);
  const [shadow, setShadow] = useState(DEFAULTS.shadow);
  const [iconShadow, setIconShadow] = useState(DEFAULTS.iconShadow);
  const [clearable, setClearable] = useState(DEFAULTS.clearable);

  const Icon = ICONS.find((item) => item.value === iconId)?.Icon ?? null;
  const displayLabel = label.trim() || "Input";
  const iconNone = iconAppearance === "none";
  const iconBare = iconAppearance === "bare";
  const iconShadowAllowed =
    iconAppearance === "filled" || iconAppearance === "soft" || iconAppearance === "glossy";
  const chipNoShadowClass =
    iconShadow || !iconShadowAllowed ? "" : " org-ehg-qa-glass__icon--no-shadow";
  const chipLookClass =
    iconNone || iconBare ? "" : ICON_APPEARANCE_CHIP_CLASS[iconAppearance as IconChipAppearance];
  const hasIcon = Icon != null && !iconNone;

  function resetPreview() {
    setTone(DEFAULTS.tone);
    setAppearance(DEFAULTS.appearance);
    setIconId(DEFAULTS.iconId);
    setIconShape(DEFAULTS.iconShape);
    setIconAppearance(DEFAULTS.iconAppearance);
    setSize(DEFAULTS.size);
    setCardShape(DEFAULTS.cardShape);
    setDensity(DEFAULTS.density);
    setLabel(DEFAULTS.label);
    setPlaceholder(DEFAULTS.placeholder);
    setValue("");
    setGhost(DEFAULTS.ghost);
    setShadow(DEFAULTS.shadow);
    setIconShadow(DEFAULTS.iconShadow);
    setClearable(DEFAULTS.clearable);
    setBackground(ORG_EHG_PLAYGROUND_BG_DEFAULT);
  }

  function onIconAppearanceChange(next: IconAppearance) {
    setIconAppearance(next);
    if (next !== "filled" && next !== "soft" && next !== "glossy") setIconShadow(false);
    if (next === "none") {
      /* keep glyph selection; hide mark only */
    } else if (iconId === "none") {
      setIconId("search");
    }
  }

  const iconMark =
    !hasIcon || Icon == null ? null : iconBare ? (
      <Icon
        className="org-ehg-qa-glass__glyph org-ehg-qa-glass__glyph--bare"
        strokeWidth={2.25}
        aria-hidden
        data-testid="ui-standard-org-ehg-qa-input-pg-preview-bare"
      />
    ) : (
      <span
        className={
          "org-ehg-qa-glass__icon" +
          (iconShape === "rounded" ? " org-ehg-qa-glass__icon--rounded" : "") +
          chipNoShadowClass +
          chipLookClass
        }
        aria-hidden="true"
        data-testid="ui-standard-org-ehg-qa-input-pg-preview-chip"
        data-icon-appearance={iconAppearance}
      >
        <Icon className="org-ehg-qa-glass__glyph" strokeWidth={2.25} />
      </span>
    );

  const fieldShellClass = [
    "org-ehg-qa-glass",
    "org-ehg-qa-input-glass__field",
    `org-ehg-qa-glass--tone-${tone}`,
    `org-ehg-qa-glass--size-${size}`,
    `org-ehg-qa-input-glass--density-${density}`,
    ghost ? "org-ehg-qa-glass--ghost" : "",
    shadow ? "" : "org-ehg-qa-glass--no-shadow",
  ]
    .filter(Boolean)
    .join(" ");

  return (
    <section
      className="org-ehg org-ehg-pg-bg org-ehg-glass-playground-card flex min-w-0 flex-col"
      data-testid="ui-standard-card-org-ehg-qa-input-glass-playground"
      data-bg-option={background}
      data-org-ehg-theme={theme}
      data-preview-theme={theme}
      style={{
        ["--org-ehg-bg-intensity" as string]: String(ORG_EHG_PLAYGROUND_BG_DEFAULT_INTENSITY),
      }}
    >
      <div className="org-ehg-bg-lab__base" aria-hidden />
      <div className="org-ehg-bg-lab__decor" aria-hidden />
      <div className="org-ehg-pg-bg__content org-ehg-glass-playground-card__body">
        <div className="flex items-start justify-between gap-3">
          <CardTitle>Input glass playground</CardTitle>
          <div className="flex items-center gap-2">
            <Button
              type="button"
              intent={theme === "dark" ? "primary" : "neutral"}
              appearance={theme === "dark" ? "solid" : "outline"}
              shape="pill"
              size="icon"
              aria-label={theme === "dark" ? "Light mode" : "Dark mode"}
              aria-pressed={theme === "dark"}
              data-testid="ui-standard-org-ehg-qa-input-pg-theme"
              onClick={toggleTheme}
            >
              {theme === "dark" ? (
                <Moon className="size-4" aria-hidden />
              ) : (
                <Sun className="size-4" aria-hidden />
              )}
            </Button>
            <Button
              type="button"
              intent="neutral"
              appearance="outline"
              shape="pill"
              size="icon"
              aria-label="Reset"
              data-testid="ui-standard-org-ehg-qa-input-pg-reset"
              onClick={resetPreview}
            >
              <RotateCcw className="size-4" aria-hidden />
            </Button>
          </div>
        </div>

        <PlaygroundSection testId="ui-standard-org-ehg-qa-input-glass-playground" className="">
          <div className="grid min-w-0 gap-2 sm:grid-cols-2 lg:grid-cols-3">
            <CompactSelect
              label="Tone"
              value={tone}
              options={[...TONES]}
              onChange={setTone}
              testId="ui-standard-org-ehg-qa-input-pg-tone"
            />
            <CompactSelect
              label="Appearance"
              value={appearance}
              options={[...INPUT_APPEARANCES]}
              onChange={setAppearance}
              testId="ui-standard-org-ehg-qa-input-pg-appearance"
            />
            <CompactSelect
              label="Icon"
              value={iconId}
              options={ICONS.map((item) => ({ label: item.label, value: item.value }))}
              onChange={(next) => {
                setIconId(next);
                if (next === "none") {
                  setIconAppearance("none");
                  setIconShadow(false);
                } else if (iconAppearance === "none") {
                  setIconAppearance("filled");
                }
              }}
              testId="ui-standard-org-ehg-qa-input-pg-icon"
            />
            <CompactSelect
              label="Icon appearance"
              value={iconAppearance}
              options={[...ICON_APPEARANCES]}
              onChange={onIconAppearanceChange}
              testId="ui-standard-org-ehg-qa-input-pg-icon-appearance"
            />
            <CompactSelect
              label="Icon shape"
              value={iconShape}
              options={[...ICON_SHAPES]}
              onChange={setIconShape}
              disabled={iconNone || iconBare || iconId === "none"}
              testId="ui-standard-org-ehg-qa-input-pg-icon-shape"
            />
            <CompactSelect
              label="Size"
              value={size}
              options={[...SIZES]}
              onChange={setSize}
              testId="ui-standard-org-ehg-qa-input-pg-size"
            />
            <CompactSelect
              label="Shape"
              value={cardShape}
              options={[...CARD_SHAPES]}
              onChange={setCardShape}
              testId="ui-standard-org-ehg-qa-input-pg-card-shape"
            />
            <CompactSelect
              label="Density"
              value={density}
              options={[...DENSITIES]}
              onChange={setDensity}
              testId="ui-standard-org-ehg-qa-input-pg-density"
            />
            <PlaygroundTextField
              label="Label"
              value={label}
              onChange={setLabel}
              testId="ui-standard-org-ehg-qa-input-pg-label"
            />
            <PlaygroundTextField
              label="Placeholder"
              value={placeholder}
              onChange={setPlaceholder}
              testId="ui-standard-org-ehg-qa-input-pg-placeholder"
            />
            <CompactSelect
              label="Background"
              value={background}
              options={[...ORG_EHG_PLAYGROUND_BG_OPTIONS]}
              onChange={setBackground}
              testId="ui-standard-org-ehg-qa-input-pg-background"
            />
          </div>

          <div className="col-span-full flex flex-row flex-wrap items-center gap-6">
            <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
              <Switch
                checked={ghost}
                onCheckedChange={setGhost}
                aria-label="Ghost"
                data-testid="ui-standard-org-ehg-qa-input-pg-ghost"
              />
              Ghost
            </label>
            <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
              <Switch
                checked={shadow}
                onCheckedChange={setShadow}
                aria-label="Shadow"
                data-testid="ui-standard-org-ehg-qa-input-pg-shadow"
              />
              Shadow
            </label>
            <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
              <Switch
                checked={iconShadow}
                onCheckedChange={setIconShadow}
                aria-label="Icon shadow"
                disabled={!hasIcon || !iconShadowAllowed}
                data-testid="ui-standard-org-ehg-qa-input-pg-icon-shadow"
              />
              Icon shadow
            </label>
            <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
              <Switch
                checked={clearable}
                onCheckedChange={setClearable}
                aria-label="Clearable"
                data-testid="ui-standard-org-ehg-qa-input-pg-clearable"
              />
              Clearable
            </label>
          </div>

          <div
            className="flex min-w-0 flex-col gap-3"
            data-testid="ui-standard-org-ehg-qa-input-pg-preview-card"
          >
            <PlaygroundLabel>Preview</PlaygroundLabel>
            <div
              className="org-ehg-qa-glass-panel org-ehg-qa-glass-panel--playground"
              data-org-ehg-theme={theme}
              data-bg-option={background}
              data-testid="ui-standard-org-ehg-qa-input-pg-preview"
              data-preview-theme={theme}
            >
              <label
                className={`org-ehg-qa-input-glass org-ehg-qa-glass--tone-${tone}`}
                data-testid="ui-standard-org-ehg-qa-input-pg-preview-field"
                data-appearance={appearance}
                data-size={size}
                data-card-shape={cardShape}
                data-density={density}
                data-raised={value.trim().length > 0 ? "true" : "false"}
                data-has-icon={hasIcon ? "true" : "false"}
                data-icon-look={!hasIcon ? "none" : iconBare ? "bare" : "chip"}
              >
                <span className={fieldShellClass} data-card-shape={cardShape}>
                  {iconMark}
                  <span className="org-ehg-qa-input-glass__label">{displayLabel}</span>
                  <input
                    className="org-ehg-qa-input-glass__control"
                    value={value}
                    /* Label above: space only (same as normal qinput stacked) — no visible placeholder. */
                    placeholder={appearance === "stacked" ? " " : placeholder}
                    aria-label={displayLabel}
                    data-testid="ui-standard-org-ehg-qa-input-pg-preview-input"
                    onChange={(event) => setValue(event.target.value)}
                  />
                  {clearable && value ? (
                    <button
                      type="button"
                      className="org-ehg-qa-input-glass__clear"
                      aria-label="Clear"
                      data-testid="ui-standard-org-ehg-qa-input-pg-preview-clear"
                      onClick={() => setValue("")}
                    >
                      ×
                    </button>
                  ) : null}
                </span>
              </label>
            </div>
          </div>

          <UiStandardsCopyCommand
            standard="Org Input Glass"
            standardStatus="pilot"
            command={inputGlassCommand(
              tone,
              appearance,
              iconShape,
              iconAppearance,
              size,
              cardShape,
              density,
              ghost,
              shadow,
              iconShadow,
              clearable,
            )}
            testId="ui-standard-org-ehg-qa-input-pg-copy"
          />
        </PlaygroundSection>
      </div>
    </section>
  );
}

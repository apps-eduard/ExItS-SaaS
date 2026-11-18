import { useState } from "react";
import {
  Check,
  ChevronDown,
  ListFilter,
  Mail,
  Moon,
  RotateCcw,
  Search,
  Sun,
  UserRound,
  type LucideIcon,
} from "lucide-react";
import { Button } from "@/components/ui/button";
import { CardTitle } from "@/components/ui/card";
import { DropdownMenu, MenuItem, useDismissibleOpen } from "@/components/ui/dropdown-menu";
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
  ORG_EHG_SELECT_GLASS_DEFAULT_APPEARANCE,
  ORG_EHG_SELECT_GLASS_DEFAULT_COMMAND,
  ORG_EHG_SELECT_GLASS_DEFAULT_MENU_TRANSITION,
  ORG_EHG_SELECT_GLASS_DEFAULT_SHAPE,
  ORG_EHG_SELECT_GLASS_DEFAULT_SIZE,
  ORG_EHG_SELECT_GLASS_DEFAULT_TONE,
} from "@/features/ui-standards/org-enterprise-hybrid-glass/org-ehg-select-glass-global-default";
import type {
  OrgEhgQaGlassIconShape,
  OrgEhgQaGlassSize,
  OrgEhgQaGlassTone,
} from "@/features/ui-standards/org-enterprise-hybrid-glass/OrgEhgQuickActionGlassGallery";
import { useUiStandardsShellAppearance } from "@/features/ui-standards/UiStandardsShellAppearance";
import { usePreferences } from "@/hooks/usePreferences";
import { type DensityPreference } from "@/lib/preferences/ui-preferences";

type SelectIconId = "none" | "search" | "mail" | "user" | "list";

/** Same appearances as the Input glass playground. */
type SelectAppearance =
  | "standard"
  | "soft"
  | "soft-outline"
  | "filled"
  | "outlined"
  | "standout"
  | "stacked";

/** Same icon chip looks as Button / Input glass. */
type IconAppearance = "none" | "filled" | "ghost" | "outline" | "soft" | "glossy" | "bare";

type IconChipAppearance = Exclude<IconAppearance, "none" | "bare">;

type SelectOption = { label: string; value: string };

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

const SELECT_APPEARANCES: ReadonlyArray<{ label: string; value: SelectAppearance }> = [
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

/** Same menu transitions as the Select / Dropdown playground (default Scale). */
type SelectMenuTransition = "flip-up" | "flip-down" | "scale" | "jump-up";

const MENU_TRANSITIONS: ReadonlyArray<{ label: string; value: SelectMenuTransition }> = [
  { label: "Flip up", value: "flip-up" },
  { label: "Flip down", value: "flip-down" },
  { label: "Scale", value: "scale" },
  { label: "Jump up", value: "jump-up" },
];

/**
 * Select glass card shapes — Standard sits in the middle of the ladder.
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

const ICONS: ReadonlyArray<{ label: string; value: SelectIconId; Icon: LucideIcon | null }> = [
  { label: "None", value: "none", Icon: null },
  { label: "List", value: "list", Icon: ListFilter },
  { label: "Search", value: "search", Icon: Search },
  { label: "Mail", value: "mail", Icon: Mail },
  { label: "User", value: "user", Icon: UserRound },
];

const DEFAULT_OPTIONS: ReadonlyArray<SelectOption> = [
  { label: "Option A", value: "a" },
  { label: "Option B", value: "b" },
  { label: "Option C", value: "c" },
  { label: "Option D", value: "d" },
];

/** Locked playground reset command (must match ORG_EHG_SELECT_GLASS_DEFAULT_COMMAND). */
export const ORG_EHG_SELECT_GLASS_PLAYGROUND_DEFAULT_COMMAND = ORG_EHG_SELECT_GLASS_DEFAULT_COMMAND;

/** Seeded from `ORG_EHG_SELECT_GLASS_DEFAULT_COMMAND`. */
const DEFAULTS = {
  tone: ORG_EHG_SELECT_GLASS_DEFAULT_TONE as OrgEhgQaGlassTone,
  appearance: ORG_EHG_SELECT_GLASS_DEFAULT_APPEARANCE as SelectAppearance,
  iconId: "none" as SelectIconId,
  iconShape: "soft" as OrgEhgQaGlassIconShape,
  iconAppearance: "none" as IconAppearance,
  size: ORG_EHG_SELECT_GLASS_DEFAULT_SIZE as OrgEhgQaGlassSize,
  cardShape: ORG_EHG_SELECT_GLASS_DEFAULT_SHAPE as GlassCardShape,
  density: "balance" as DensityPreference,
  label: "Select",
  placeholder: "Choose…",
  transition: ORG_EHG_SELECT_GLASS_DEFAULT_MENU_TRANSITION as SelectMenuTransition,
  multi: false,
  ghost: false,
  shadow: false,
  iconShadow: false,
  clearable: false,
  hover: true,
};

function selectGlassCommand(
  tone: OrgEhgQaGlassTone,
  appearance: SelectAppearance,
  iconShape: OrgEhgQaGlassIconShape,
  iconAppearance: IconAppearance,
  size: OrgEhgQaGlassSize,
  cardShape: GlassCardShape,
  density: DensityPreference,
  transition: SelectMenuTransition,
  multi: boolean,
  ghost: boolean,
  shadow: boolean,
  iconShadow: boolean,
  clearable: boolean,
  hover: boolean,
): string {
  const parts = [
    "ext",
    "org-ehg",
    "select-glass",
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
  if (transition !== "scale") parts.push(transition);
  if (multi) parts.push("multi");
  if (ghost) parts.push("ghost");
  if (!shadow) parts.push("no-shadow");
  if (iconAppearance !== "none" && iconAppearance !== "bare" && !iconShadow) {
    parts.push("no-icon-shadow");
  }
  const iconLook = ICON_APPEARANCE_COMMAND[iconAppearance];
  if (iconLook && iconAppearance !== "none" && iconAppearance !== "bare") parts.push(iconLook);
  if (clearable) parts.push("clearable");
  if (!hover) parts.push("no-hover");
  return parts.join(" ");
}

/**
 * Select / dropdown glass playground — same Enterprise Hybrid Glass recipe as Input glass.
 * UI Standards / DEV only.
 */
export function OrgEhgQaSelectGlassPlayground() {
  const { background, setBackground, theme, toggleTheme } = useUiStandardsShellAppearance();
  const { preferences, setDensity } = usePreferences();
  const density = preferences.density;
  const menu = useDismissibleOpen(false);
  const [tone, setTone] = useState(DEFAULTS.tone);
  const [appearance, setAppearance] = useState(DEFAULTS.appearance);
  const [iconId, setIconId] = useState(DEFAULTS.iconId);
  const [iconShape, setIconShape] = useState(DEFAULTS.iconShape);
  const [iconAppearance, setIconAppearance] = useState(DEFAULTS.iconAppearance);
  const [size, setSize] = useState(DEFAULTS.size);
  const [cardShape, setCardShape] = useState(DEFAULTS.cardShape);
  const [label, setLabel] = useState(DEFAULTS.label);
  const [placeholder, setPlaceholder] = useState(DEFAULTS.placeholder);
  const [transition, setTransition] = useState(DEFAULTS.transition);
  const [multi, setMulti] = useState(DEFAULTS.multi);
  const [value, setValue] = useState("");
  const [values, setValues] = useState<string[]>([]);
  const [ghost, setGhost] = useState(DEFAULTS.ghost);
  const [shadow, setShadow] = useState(DEFAULTS.shadow);
  const [iconShadow, setIconShadow] = useState(DEFAULTS.iconShadow);
  const [clearable, setClearable] = useState(DEFAULTS.clearable);
  const [hover, setHover] = useState(DEFAULTS.hover);

  const Icon = ICONS.find((item) => item.value === iconId)?.Icon ?? null;
  const displayLabel = label.trim() || "Select";
  const selectedValues = multi ? values : value ? [value] : [];
  const selectedLabels = DEFAULT_OPTIONS.filter((option) => selectedValues.includes(option.value)).map(
    (option) => option.label,
  );
  const hasSelection = selectedValues.length > 0;
  const displayValue =
    multi && selectedLabels.length >= 2
      ? `${selectedLabels.length} Selected`
      : selectedLabels.join(", ");
  const raised = hasSelection || menu.open;
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
    setTransition(DEFAULTS.transition);
    setMulti(DEFAULTS.multi);
    setValue("");
    setValues([]);
    setGhost(DEFAULTS.ghost);
    setShadow(DEFAULTS.shadow);
    setIconShadow(DEFAULTS.iconShadow);
    setClearable(DEFAULTS.clearable);
    setHover(DEFAULTS.hover);
    setBackground(ORG_EHG_PLAYGROUND_BG_DEFAULT);
    menu.close();
  }

  function onIconAppearanceChange(next: IconAppearance) {
    setIconAppearance(next);
    if (next !== "filled" && next !== "soft" && next !== "glossy") setIconShadow(false);
    if (next === "none") {
      /* keep glyph selection; hide mark only */
    } else if (iconId === "none") {
      setIconId("list");
    }
  }

  const iconMark =
    !hasIcon || Icon == null ? null : iconBare ? (
      <Icon
        className="org-ehg-qa-glass__glyph org-ehg-qa-glass__glyph--bare"
        strokeWidth={2.25}
        aria-hidden
        data-testid="ui-standard-org-ehg-qa-select-pg-preview-bare"
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
        data-testid="ui-standard-org-ehg-qa-select-pg-preview-chip"
        data-icon-appearance={iconAppearance}
      >
        <Icon className="org-ehg-qa-glass__glyph" strokeWidth={2.25} />
      </span>
    );

  const fieldShellClass = [
    "org-ehg-qa-glass",
    "org-ehg-qa-input-glass__field",
    "org-ehg-qa-select-glass__field",
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
      data-testid="ui-standard-card-org-ehg-qa-select-glass-playground"
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
          <CardTitle>Select glass playground</CardTitle>
          <div className="flex items-center gap-2">
            <Button
              type="button"
              intent={theme === "dark" ? "primary" : "neutral"}
              appearance={theme === "dark" ? "solid" : "outline"}
              shape="pill"
              size="icon"
              aria-label={theme === "dark" ? "Light mode" : "Dark mode"}
              aria-pressed={theme === "dark"}
              data-testid="ui-standard-org-ehg-qa-select-pg-theme"
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
              data-testid="ui-standard-org-ehg-qa-select-pg-reset"
              onClick={resetPreview}
            >
              <RotateCcw className="size-4" aria-hidden />
            </Button>
          </div>
        </div>

        <PlaygroundSection testId="ui-standard-org-ehg-qa-select-glass-playground" className="">
          <div className="grid min-w-0 gap-2 sm:grid-cols-2 lg:grid-cols-3">
            <CompactSelect
              label="Tone"
              value={tone}
              options={[...TONES]}
              onChange={setTone}
              testId="ui-standard-org-ehg-qa-select-pg-tone"
            />
            <CompactSelect
              label="Appearance"
              value={appearance}
              options={[...SELECT_APPEARANCES]}
              onChange={setAppearance}
              testId="ui-standard-org-ehg-qa-select-pg-appearance"
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
              testId="ui-standard-org-ehg-qa-select-pg-icon"
            />
            <CompactSelect
              label="Icon appearance"
              value={iconAppearance}
              options={[...ICON_APPEARANCES]}
              onChange={onIconAppearanceChange}
              testId="ui-standard-org-ehg-qa-select-pg-icon-appearance"
            />
            <CompactSelect
              label="Icon shape"
              value={iconShape}
              options={[...ICON_SHAPES]}
              onChange={setIconShape}
              disabled={iconNone || iconBare || iconId === "none"}
              testId="ui-standard-org-ehg-qa-select-pg-icon-shape"
            />
            <CompactSelect
              label="Size"
              value={size}
              options={[...SIZES]}
              onChange={setSize}
              testId="ui-standard-org-ehg-qa-select-pg-size"
            />
            <CompactSelect
              label="Shape"
              value={cardShape}
              options={[...CARD_SHAPES]}
              onChange={setCardShape}
              testId="ui-standard-org-ehg-qa-select-pg-card-shape"
            />
            <CompactSelect
              label="Density"
              value={density}
              options={[...DENSITIES]}
              onChange={setDensity}
              testId="ui-standard-org-ehg-qa-select-pg-density"
            />
            <CompactSelect
              label="Transition"
              value={transition}
              options={[...MENU_TRANSITIONS]}
              onChange={setTransition}
              testId="ui-standard-org-ehg-qa-select-pg-transition"
            />
            <PlaygroundTextField
              label="Label"
              value={label}
              onChange={setLabel}
              testId="ui-standard-org-ehg-qa-select-pg-label"
            />
            <PlaygroundTextField
              label="Placeholder"
              value={placeholder}
              onChange={setPlaceholder}
              testId="ui-standard-org-ehg-qa-select-pg-placeholder"
            />
            <CompactSelect
              label="Background"
              value={background}
              options={[...ORG_EHG_PLAYGROUND_BG_OPTIONS]}
              onChange={setBackground}
              testId="ui-standard-org-ehg-qa-select-pg-background"
            />
          </div>

          <div className="col-span-full flex flex-row flex-wrap items-center gap-6">
            <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
              <Switch
                checked={ghost}
                onCheckedChange={setGhost}
                aria-label="Ghost"
                data-testid="ui-standard-org-ehg-qa-select-pg-ghost"
              />
              Ghost
            </label>
            <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
              <Switch
                checked={shadow}
                onCheckedChange={setShadow}
                aria-label="Shadow"
                data-testid="ui-standard-org-ehg-qa-select-pg-shadow"
              />
              Shadow
            </label>
            <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
              <Switch
                checked={iconShadow}
                onCheckedChange={setIconShadow}
                aria-label="Icon shadow"
                disabled={!hasIcon || !iconShadowAllowed}
                data-testid="ui-standard-org-ehg-qa-select-pg-icon-shadow"
              />
              Icon shadow
            </label>
            <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
              <Switch
                checked={clearable}
                onCheckedChange={setClearable}
                aria-label="Clearable"
                data-testid="ui-standard-org-ehg-qa-select-pg-clearable"
              />
              Clearable
            </label>
            <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
              <Switch
                checked={hover}
                onCheckedChange={setHover}
                aria-label="Hover"
                data-testid="ui-standard-org-ehg-qa-select-pg-hover"
              />
              Hover
            </label>
            <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
              <Switch
                checked={multi}
                onCheckedChange={(checked) => {
                  setMulti(checked);
                  if (checked) {
                    setValues(value ? [value] : []);
                  } else {
                    setValue(values[0] ?? "");
                    setValues([]);
                  }
                }}
                aria-label="Multi select"
                data-testid="ui-standard-org-ehg-qa-select-pg-multi"
              />
              Multi select
            </label>
          </div>

          <div
            className="flex min-w-0 flex-col gap-3"
            data-testid="ui-standard-org-ehg-qa-select-pg-preview-card"
          >
            <PlaygroundLabel>Preview</PlaygroundLabel>
            <div
              className="org-ehg-qa-glass-panel org-ehg-qa-glass-panel--playground"
              data-org-ehg-theme={theme}
              data-bg-option={background}
              data-testid="ui-standard-org-ehg-qa-select-pg-preview"
              data-preview-theme={theme}
            >
              <div
                className={`org-ehg-qa-input-glass org-ehg-qa-select-glass org-ehg-qa-glass--tone-${tone}`}
                data-testid="ui-standard-org-ehg-qa-select-pg-preview-field"
                data-appearance={appearance}
                data-size={size}
                data-card-shape={cardShape}
                data-density={density}
                data-hover={hover ? "on" : "off"}
                data-raised={raised ? "true" : "false"}
                data-open={menu.open ? "true" : "false"}
                data-has-icon={hasIcon ? "true" : "false"}
                data-icon-look={!hasIcon ? "none" : iconBare ? "bare" : "chip"}
              >
                <DropdownMenu
                  open={menu.open}
                  onOpenChange={menu.setOpen}
                  align="start"
                  matchTriggerWidth
                  menuGapPx={1}
                  menuTransition={transition}
                  menuClassName={
                    `org-ehg-glass-menu org-ehg-qa-select-glass-menu org-ehg-qa-glass--tone-${tone}` +
                    (theme === "dark" ? " org-ehg-glass-menu--dark" : "")
                  }
                  menuLabel={displayLabel}
                  className="org-ehg-qa-select-glass__menu-host"
                  trigger={({ id, expanded, controls, onClick, onKeyDown }) => (
                    <button
                      type="button"
                      id={id}
                      className={fieldShellClass}
                      data-card-shape={cardShape}
                      data-hover={hover ? "on" : "off"}
                      aria-haspopup="menu"
                      aria-expanded={expanded}
                      aria-controls={controls}
                      aria-label={displayLabel}
                      data-testid="ui-standard-org-ehg-qa-select-pg-preview-trigger"
                      onClick={onClick}
                      onKeyDown={onKeyDown}
                    >
                      {iconMark}
                      <span className="org-ehg-qa-input-glass__label">{displayLabel}</span>
                      <span
                        className={
                          "org-ehg-qa-input-glass__control org-ehg-qa-select-glass__value" +
                          (hasSelection ? "" : " org-ehg-qa-select-glass__value--placeholder")
                        }
                        data-testid="ui-standard-org-ehg-qa-select-pg-preview-value"
                      >
                        {hasSelection
                          ? displayValue
                          : /* Idle: floating label is the prompt — no second placeholder. */
                            appearance === "stacked" || !raised
                            ? "\u00a0"
                            : placeholder}
                      </span>
                      {clearable && hasSelection ? (
                        <span
                          role="button"
                          tabIndex={0}
                          className="org-ehg-qa-input-glass__clear"
                          aria-label="Clear"
                          data-testid="ui-standard-org-ehg-qa-select-pg-preview-clear"
                          onClick={(event) => {
                            event.preventDefault();
                            event.stopPropagation();
                            setValue("");
                            setValues([]);
                            menu.close();
                          }}
                          onKeyDown={(event) => {
                            if (event.key === "Enter" || event.key === " ") {
                              event.preventDefault();
                              event.stopPropagation();
                              setValue("");
                              setValues([]);
                              menu.close();
                            }
                          }}
                        >
                          ×
                        </span>
                      ) : null}
                      <ChevronDown
                        className="org-ehg-qa-select-glass__chevron"
                        aria-hidden
                        strokeWidth={2.25}
                        data-testid="ui-standard-org-ehg-qa-select-pg-preview-chevron"
                      />
                    </button>
                  )}
                >
                  <div
                    className="org-ehg-glass-menu__list"
                    role="group"
                    aria-multiselectable={multi || undefined}
                    data-testid="ui-standard-org-ehg-qa-select-pg-preview-menu"
                  >
                    {DEFAULT_OPTIONS.map((option) => {
                      const selectedOption = selectedValues.includes(option.value);
                      return (
                        <MenuItem
                          key={option.value}
                          className={
                            "org-ehg-glass-menu__option org-ehg-qa-select-glass-menu__option" +
                            (selectedOption ? " org-ehg-glass-menu__option--selected" : "")
                          }
                          data-selected={selectedOption ? "true" : "false"}
                          data-testid={`ui-standard-org-ehg-qa-select-pg-preview-option-${option.value}`}
                          onSelect={() => {
                            if (multi) {
                              setValues((current) =>
                                current.includes(option.value)
                                  ? current.filter((item) => item !== option.value)
                                  : [...current, option.value],
                              );
                              return;
                            }
                            setValue(option.value);
                            menu.close();
                          }}
                        >
                          <span className="org-ehg-qa-select-glass-menu__option-label">
                            {option.label}
                          </span>
                          {selectedOption ? (
                            <Check
                              className="org-ehg-qa-select-glass-menu__check"
                              strokeWidth={2.5}
                              aria-hidden
                              data-testid={`ui-standard-org-ehg-qa-select-pg-preview-option-${option.value}-check`}
                            />
                          ) : (
                            <span className="org-ehg-qa-select-glass-menu__check" aria-hidden />
                          )}
                        </MenuItem>
                      );
                    })}
                  </div>
                </DropdownMenu>
              </div>
            </div>
          </div>

          <UiStandardsCopyCommand
            standard="Org Select Glass"
            standardStatus="pilot"
            command={selectGlassCommand(
              tone,
              appearance,
              iconShape,
              iconAppearance,
              size,
              cardShape,
              density,
              transition,
              multi,
              ghost,
              shadow,
              iconShadow,
              clearable,
              hover,
            )}
            testId="ui-standard-org-ehg-qa-select-pg-copy"
          />
        </PlaygroundSection>
      </div>
    </section>
  );
}

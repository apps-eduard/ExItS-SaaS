import { useLayoutEffect, useState } from "react";
import {
  Check,
  ChevronDown,
  FilePlus,
  Moon,
  Plus,
  RotateCcw,
  ShoppingCart,
  Sparkles,
  Sun,
  UserPlus,
  type LucideIcon,
} from "lucide-react";
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
  type OrgEhgPlaygroundBgOption,
} from "@/features/ui-standards/org-enterprise-hybrid-glass/orgEhgPlaygroundBackground";
import {
  ORG_EHG_BTN_GLASS_DEFAULT_APPEARANCE,
  ORG_EHG_BTN_GLASS_DEFAULT_COMMAND,
  ORG_EHG_BTN_GLASS_DEFAULT_SHAPE,
  ORG_EHG_BTN_GLASS_DEFAULT_SIZE,
  ORG_EHG_BTN_GLASS_DEFAULT_TONE,
} from "@/features/ui-standards/org-enterprise-hybrid-glass/org-ehg-btn-glass-global-default";
import {
  ORG_EHG_SPEED_DIAL_SHOW_DEFAULT,
  ORG_EHG_SPEED_DIAL_TRANSITION_DEFAULT,
} from "@/features/ui-standards/org-enterprise-hybrid-glass/orgEhgSpeedDial";
import type {
  OrgEhgQaGlassIconShape,
  OrgEhgQaGlassSize,
  OrgEhgQaGlassTone,
} from "@/features/ui-standards/org-enterprise-hybrid-glass/OrgEhgQuickActionGlassGallery";
import { useUiStandardsShellAppearance } from "@/features/ui-standards/UiStandardsShellAppearance";
import { usePreferences } from "@/hooks/usePreferences";
import { type DensityPreference } from "@/lib/preferences/ui-preferences";

type BtnIconId = "plus" | "check" | "cart" | "sparkles" | "none";

/** Icon chip look: none (hidden), filled / ghost / outline / soft / glossy wrap, or bare glyph. */
type IconAppearance = "none" | "filled" | "ghost" | "outline" | "soft" | "glossy" | "bare";

type IconChipAppearance = Exclude<IconAppearance, "none" | "bare">;

const ICON_APPEARANCE_CHIP_CLASS: Record<IconChipAppearance, string> = {
  filled: "",
  ghost: " org-ehg-qa-glass__icon--ghost",
  outline: " org-ehg-qa-glass__icon--outline",
  soft: " org-ehg-qa-glass__icon--soft-fill",
  glossy: " org-ehg-qa-glass__icon--glossy",
};

const ICON_APPEARANCE_ONLY_CLASS: Record<IconChipAppearance, string> = {
  filled: "org-ehg-qa-glass-icon--filled",
  ghost: "org-ehg-qa-glass-icon--ghost",
  outline: "org-ehg-qa-glass-icon--outline",
  soft: "org-ehg-qa-glass-icon--soft-fill",
  glossy: "org-ehg-qa-glass-icon--glossy",
};

const ICON_APPEARANCE_COMMAND: Partial<Record<IconAppearance, string>> = {
  none: "icon-none",
  ghost: "icon-ghost",
  outline: "icon-outline",
  soft: "icon-soft-fill",
  glossy: "icon-glossy",
  bare: "icon-bare",
};

/** Same appearance set as Org EHG Button glass / product ExitsOrgEhgButton. */
type GlassAppearance =
  | "standard"
  | "soft"
  | "soft-outline"
  | "soft-glass"
  | "outline"
  | "raised"
  | "ghost"
  | "glossy"
  | "glossy-glass"
  | "glossy-raise"
  | "gradient"
  | "gradient-glass"
  | "rounded"
  | "flat"
  | "medallion"
  | "text";

const APPEARANCES: ReadonlyArray<{ label: string; value: GlassAppearance }> = [
  { label: "Flat", value: "flat" },
  { label: "Ghost", value: "ghost" },
  { label: "Glossy", value: "glossy" },
  { label: "Glossy glass", value: "glossy-glass" },
  { label: "Glossy raise", value: "glossy-raise" },
  { label: "Gradient", value: "gradient" },
  { label: "Gradient glass", value: "gradient-glass" },
  { label: "Medallion", value: "medallion" },
  { label: "Outline", value: "outline" },
  { label: "Raised", value: "raised" },
  { label: "Rounded", value: "rounded" },
  { label: "Soft", value: "soft" },
  { label: "Soft glass", value: "soft-glass" },
  { label: "Soft outline", value: "soft-outline" },
  { label: "Standard", value: "standard" },
  { label: "Text", value: "text" },
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

const ICON_APPEARANCES: ReadonlyArray<{ label: string; value: IconAppearance }> = [
  { label: "None", value: "none" },
  { label: "Filled", value: "filled" },
  { label: "Ghost", value: "ghost" },
  { label: "Outline", value: "outline" },
  { label: "Soft", value: "soft" },
  { label: "Glossy", value: "glossy" },
  { label: "Bare", value: "bare" },
];

const SIZES: ReadonlyArray<{ label: string; value: OrgEhgQaGlassSize }> = [
  { label: "SM", value: "sm" },
  { label: "MD", value: "md" },
  { label: "LG", value: "lg" },
];

/**
 * Button glass shapes — Standard sits in the middle (same ladder as Input / Select glass).
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

/** Icon + label content alignment inside the glass button. */
type ContentAlign = "start" | "center" | "end";

const CONTENT_ALIGNS: ReadonlyArray<{ label: string; value: ContentAlign }> = [
  { label: "Start", value: "start" },
  { label: "Center", value: "center" },
  { label: "End", value: "end" },
];

const ICONS: ReadonlyArray<{ label: string; value: BtnIconId; Icon: LucideIcon | null }> = [
  { label: "None", value: "none", Icon: null },
  { label: "Plus", value: "plus", Icon: Plus },
  { label: "Check", value: "check", Icon: Check },
  { label: "Cart", value: "cart", Icon: ShoppingCart },
  { label: "Sparkles", value: "sparkles", Icon: Sparkles },
];

/** Locked soft square — Icon shape dropdown removed from the playground. */
const ICON_SHAPE: OrgEhgQaGlassIconShape = "soft";

/** FAB stage placement inside the preview scene. */
type FabPosition =
  | "left-top"
  | "center-top"
  | "right-top"
  | "left-bottom"
  | "center-bottom"
  | "right-bottom"
  | "middle";

const FAB_POSITIONS: ReadonlyArray<{ label: string; value: FabPosition }> = [
  { label: "Left top", value: "left-top" },
  { label: "Center top", value: "center-top" },
  { label: "Right top", value: "right-top" },
  { label: "Left bottom", value: "left-bottom" },
  { label: "Center bottom", value: "center-bottom" },
  { label: "Right bottom", value: "right-bottom" },
  { label: "Middle", value: "middle" },
];

const FAB_POSITION_DEFAULT: FabPosition = "right-bottom";

/** Icons available as speed-dial child actions (multi-select). */
type SpeedDialIconId = "person" | "item" | "cart" | "check" | "sparkles";

const SPEED_DIAL_ICON_CATALOG: ReadonlyArray<{
  label: string;
  value: SpeedDialIconId;
  actionLabel: string;
  Icon: LucideIcon;
  tone: OrgEhgQaGlassTone;
}> = [
  { label: "Person", value: "person", actionLabel: "New person", Icon: UserPlus, tone: "info" },
  { label: "Item", value: "item", actionLabel: "New item", Icon: FilePlus, tone: "success" },
  { label: "Cart", value: "cart", actionLabel: "Cart", Icon: ShoppingCart, tone: "secondary" },
  { label: "Check", value: "check", actionLabel: "Done", Icon: Check, tone: "primary" },
  { label: "Sparkles", value: "sparkles", actionLabel: "Sparkle", Icon: Sparkles, tone: "violet" },
];

const SPEED_DIAL_ICONS_DEFAULT: SpeedDialIconId[] = ["person", "item", "cart"];

/** Locked playground reset command (must match ORG_EHG_BTN_GLASS_DEFAULT_COMMAND). */
export const ORG_EHG_BTN_GLASS_PLAYGROUND_DEFAULT_COMMAND = ORG_EHG_BTN_GLASS_DEFAULT_COMMAND;

/** Seeded from `ORG_EHG_BTN_GLASS_DEFAULT_COMMAND` (non-FAB). */
const DEFAULTS = {
  tone: ORG_EHG_BTN_GLASS_DEFAULT_TONE as OrgEhgQaGlassTone,
  appearance: ORG_EHG_BTN_GLASS_DEFAULT_APPEARANCE as GlassAppearance,
  iconId: "none" as BtnIconId,
  iconAppearance: "none" as IconAppearance,
  size: ORG_EHG_BTN_GLASS_DEFAULT_SIZE as OrgEhgQaGlassSize,
  cardShape: ORG_EHG_BTN_GLASS_DEFAULT_SHAPE as GlassCardShape,
  density: "balance" as DensityPreference,
  contentAlign: "center" as ContentAlign,
  label: "Save",
  split: false,
  shadow: false,
  iconShadow: false,
  iconOnly: false,
  fab: false,
  fabPosition: FAB_POSITION_DEFAULT,
  speedDial: false,
  speedDialIcons: SPEED_DIAL_ICONS_DEFAULT,
  speedDialLabels: true,
  hover: true,
};

/** Speed dial / FAB glass seed — `ext org-ehg btn-glass qa people flat lg icon-soft shape-pill fab pos-right-bottom speed-dial icons-person-item-cart no-shadow no-icon-shadow` */
const FAB_DEFAULTS = {
  tone: "people" as OrgEhgQaGlassTone,
  appearance: "flat" as GlassAppearance,
  iconId: "plus" as BtnIconId,
  iconAppearance: "soft" as IconAppearance,
  size: "lg" as OrgEhgQaGlassSize,
  cardShape: "pill" as GlassCardShape,
  density: "balance" as DensityPreference,
  contentAlign: "center" as ContentAlign,
  label: "Add",
  split: false,
  shadow: false,
  iconShadow: false,
  iconOnly: false,
  fab: true,
  fabPosition: "right-bottom" as FabPosition,
  speedDial: true,
  speedDialIcons: SPEED_DIAL_ICONS_DEFAULT,
  speedDialLabels: true,
  hover: true,
};

function btnGlassCommand(
  tone: OrgEhgQaGlassTone,
  appearance: GlassAppearance,
  iconAppearance: IconAppearance,
  size: OrgEhgQaGlassSize,
  cardShape: GlassCardShape,
  density: DensityPreference,
  contentAlign: ContentAlign,
  iconOnly: boolean,
  fab: boolean,
  fabPosition: FabPosition,
  speedDial: boolean,
  speedDialIcons: readonly SpeedDialIconId[],
  speedDialLabels: boolean,
  split: boolean,
  shadow: boolean,
  iconShadow: boolean,
  hover: boolean,
): string {
  const parts = [
    "ext",
    "org-ehg",
    "btn-glass",
    "qa",
    tone,
    appearance,
    size,
    iconAppearance === "none" || iconAppearance === "bare"
      ? (ICON_APPEARANCE_COMMAND[iconAppearance] as string)
      : `icon-${ICON_SHAPE}`,
    `shape-${cardShape}`,
  ];
  if (density !== "balance") parts.push(density);
  if (!fab && contentAlign !== "center") parts.push(`align-${contentAlign}`);
  if (fab) {
    /* Floating action = icon itself (not btn-glass card), staged in the scene. */
    parts.push("fab");
    parts.push(`pos-${fabPosition}`);
  } else if (iconOnly && iconAppearance !== "none") {
    parts.push("icon-only");
  }
  if (speedDial && fab) {
    parts.push("speed-dial");
    if (speedDialIcons.length > 0) {
      parts.push(`icons-${speedDialIcons.join("-")}`);
    }
    if (!speedDialLabels) parts.push("no-labels");
  }
  if (split) parts.push("split");
  if (!shadow) parts.push("no-shadow");
  if (iconAppearance !== "none" && iconAppearance !== "bare" && !iconShadow) {
    parts.push("no-icon-shadow");
  }
  const iconLook = ICON_APPEARANCE_COMMAND[iconAppearance];
  if (iconLook && iconAppearance !== "none" && iconAppearance !== "bare") parts.push(iconLook);
  if (!hover) parts.push("no-hover");
  return parts.join(" ");
}

/**
 * Button glass playground — same Enterprise Hybrid Glass recipe as Quick Action glass.
 * Pass `defaultFab` when hosting under Floating Action glass.
 * UI Standards / DEV only.
 */
export function OrgEhgQaBtnGlassPlayground({ defaultFab = false }: { defaultFab?: boolean }) {
  const seed = defaultFab ? FAB_DEFAULTS : DEFAULTS;
  const seedBackground: OrgEhgPlaygroundBgOption = defaultFab
    ? "spotlight-glow"
    : ORG_EHG_PLAYGROUND_BG_DEFAULT;
  const { background, setBackground, theme, toggleTheme } = useUiStandardsShellAppearance();
  const { preferences, setDensity } = usePreferences();
  const density = preferences.density;
  const [tone, setTone] = useState(seed.tone);
  const [appearance, setAppearance] = useState(seed.appearance);
  const [iconId, setIconId] = useState(seed.iconId);
  const [iconAppearance, setIconAppearance] = useState(seed.iconAppearance);
  const [size, setSize] = useState(seed.size);
  const [cardShape, setCardShape] = useState(seed.cardShape);
  const [contentAlign, setContentAlign] = useState(seed.contentAlign);
  const [label, setLabel] = useState(seed.label);
  const [split, setSplit] = useState(seed.split);
  const [shadow, setShadow] = useState(seed.shadow);
  const [iconShadow, setIconShadow] = useState(seed.iconShadow);
  const [iconOnly, setIconOnly] = useState(seed.iconOnly);
  const [fab, setFab] = useState(seed.fab);
  const [fabPosition, setFabPosition] = useState(seed.fabPosition);
  const [speedDial, setSpeedDial] = useState(seed.speedDial);
  const [speedDialIcons, setSpeedDialIcons] = useState<SpeedDialIconId[]>([...seed.speedDialIcons]);
  const [speedDialLabels, setSpeedDialLabels] = useState(seed.speedDialLabels);
  const [speedDialOpen, setSpeedDialOpen] = useState(seed.speedDial);
  const [hover, setHover] = useState(seed.hover);

  /* Seed Spotlight Glow on Speed dial / FAB host (Soft Ambient elsewhere). */
  useLayoutEffect(() => {
    setBackground(seedBackground);
  }, [seedBackground, setBackground]);

  const Icon = ICONS.find((item) => item.value === iconId)?.Icon ?? null;
  const displayLabel = label.trim() || "Button";
  const iconNone = iconAppearance === "none";
  const iconBare = iconAppearance === "bare";
  const iconShadowAllowed =
    iconAppearance === "filled" || iconAppearance === "soft" || iconAppearance === "glossy";
  const chipNoShadowClass =
    iconShadow || !iconShadowAllowed ? "" : " org-ehg-qa-glass__icon--no-shadow";
  const chipLookClass =
    iconNone || iconBare ? "" : ICON_APPEARANCE_CHIP_CLASS[iconAppearance as IconChipAppearance];
  const hasIcon = Icon != null && !iconNone;
  const asFab = fab && hasIcon;
  const asSpeedDial = asFab && speedDial && speedDialIcons.length > 0;
  const speedDialLabelType = speedDialLabels ? "label-tooltip" : "icon-only";
  const showIconOnly = !asFab && iconOnly && hasIcon;
  const speedDialActions = SPEED_DIAL_ICON_CATALOG.filter((item) =>
    speedDialIcons.includes(item.value),
  );
  const childSize: OrgEhgQaGlassSize = size === "lg" ? "md" : size === "md" ? "sm" : "sm";

  function resetPreview() {
    setTone(seed.tone);
    setAppearance(seed.appearance);
    setIconId(seed.iconId);
    setIconAppearance(seed.iconAppearance);
    setSize(seed.size);
    setCardShape(seed.cardShape);
    setDensity(seed.density);
    setContentAlign(seed.contentAlign);
    setLabel(seed.label);
    setSplit(seed.split);
    setShadow(seed.shadow);
    setIconShadow(seed.iconShadow);
    setIconOnly(seed.iconOnly);
    setFab(seed.fab);
    setFabPosition(seed.fabPosition);
    setSpeedDial(seed.speedDial);
    setSpeedDialIcons([...seed.speedDialIcons]);
    setSpeedDialLabels(seed.speedDialLabels);
    setSpeedDialOpen(seed.speedDial);
    setHover(seed.hover);
    setBackground(seedBackground);
  }

  function onIconAppearanceChange(next: IconAppearance) {
    setIconAppearance(next);
    if (next !== "filled" && next !== "soft" && next !== "glossy") setIconShadow(false);
    if (next === "none") {
      setIconOnly(false);
      setFab(false);
      setSpeedDial(false);
      setSpeedDialOpen(false);
      setIconId("none");
    } else if (iconId === "none") {
      setIconId("plus");
    }
  }

  function onIconOnlyChange(next: boolean) {
    setIconOnly(next);
    if (next) {
      setSplit(false);
      setFab(false);
      setSpeedDial(false);
      setSpeedDialOpen(false);
      if (iconId === "none") setIconId("plus");
    }
  }

  function onFabChange(next: boolean) {
    setFab(next);
    if (next) {
      setSplit(false);
      setIconOnly(false);
      setFabPosition(FAB_POSITION_DEFAULT);
      /* Floating action is the icon itself (not btn-glass card) — keep Icon appearance. */
      if (iconAppearance === "none") setIconAppearance("bare");
      if (iconId === "none") setIconId("plus");
    } else {
      setSpeedDial(false);
      setSpeedDialOpen(false);
    }
  }

  function onSpeedDialChange(next: boolean) {
    setSpeedDial(next);
    if (next) {
      onFabChange(true);
      if (speedDialIcons.length === 0) setSpeedDialIcons([...SPEED_DIAL_ICONS_DEFAULT]);
      setSpeedDialOpen(true);
    } else {
      setSpeedDialOpen(false);
    }
  }

  function onSplitChange(next: boolean) {
    setSplit(next);
    if (next) {
      setIconOnly(false);
      setFab(false);
      setSpeedDial(false);
      setSpeedDialOpen(false);
    }
  }

  const rowClass = [
    "org-ehg-qa-glass",
    "org-ehg-qa-btn-glass",
    `org-ehg-qa-glass--tone-${tone}`,
    `org-ehg-qa-glass--size-${size}`,
    `org-ehg-qa-btn-glass--density-${density}`,
    `org-ehg-qa-btn-glass--appearance-${appearance}`,
    `org-ehg-qa-btn-glass--align-${contentAlign}`,
    split ? "org-ehg-qa-glass--split" : "",
    shadow ? "" : "org-ehg-qa-glass--no-shadow",
  ]
    .filter(Boolean)
    .join(" ");

  /** Icon-only / Floating action surface — the icon itself, not a btn-glass card. */
  const iconOnlyClass = [
    "org-ehg-qa-glass-icon",
    `org-ehg-qa-glass-icon--tone-${tone}`,
    `org-ehg-qa-glass-icon--size-${size}`,
    `org-ehg-qa-btn-glass--density-${density}`,
    `org-ehg-qa-btn-glass--appearance-${appearance}`,
    /* Shape dropdown owns radius via data-card-shape. */
    iconBare ? "org-ehg-qa-glass-icon--bare" : "",
    iconBare ? "" : ICON_APPEARANCE_ONLY_CLASS[iconAppearance as IconChipAppearance],
    iconBare || iconShadow || !iconShadowAllowed ? "" : "org-ehg-qa-glass-icon--no-shadow",
  ]
    .filter(Boolean)
    .join(" ");

  const iconMark =
    !hasIcon || Icon == null ? null : iconBare ? (
      <Icon
        className="org-ehg-qa-glass__glyph org-ehg-qa-glass__glyph--bare"
        strokeWidth={2.25}
        aria-hidden
        data-testid="ui-standard-org-ehg-qa-btn-pg-preview-bare"
      />
    ) : (
      <span
        className={
          "org-ehg-qa-glass__icon" +
          (ICON_SHAPE === "rounded" ? " org-ehg-qa-glass__icon--rounded" : "") +
          chipNoShadowClass +
          chipLookClass
        }
        aria-hidden="true"
        data-testid="ui-standard-org-ehg-qa-btn-pg-preview-chip"
        data-icon-appearance={iconAppearance}
      >
        <Icon className="org-ehg-qa-glass__glyph" strokeWidth={2.25} />
      </span>
    );

  return (
    <section
      className="org-ehg org-ehg-pg-bg org-ehg-glass-playground-card flex min-w-0 flex-col"
      data-testid="ui-standard-card-org-ehg-qa-btn-glass-playground"
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
          <CardTitle>Button glass playground</CardTitle>
          <div className="flex items-center gap-2">
            <Button
              type="button"
              intent={theme === "dark" ? "primary" : "neutral"}
              appearance={theme === "dark" ? "solid" : "outline"}
              shape="pill"
              size="icon"
              aria-label={theme === "dark" ? "Light mode" : "Dark mode"}
              aria-pressed={theme === "dark"}
              data-testid="ui-standard-org-ehg-qa-btn-pg-theme"
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
              data-testid="ui-standard-org-ehg-qa-btn-pg-reset"
              onClick={resetPreview}
            >
              <RotateCcw className="size-4" aria-hidden />
            </Button>
          </div>
        </div>

        <PlaygroundSection testId="ui-standard-org-ehg-qa-btn-glass-playground" className="">
          <div className="grid min-w-0 gap-2 sm:grid-cols-2 lg:grid-cols-3">
            <CompactSelect
              label="Tone"
              value={tone}
              options={[...TONES]}
              onChange={setTone}
              testId="ui-standard-org-ehg-qa-btn-pg-tone"
            />
            <CompactSelect
              label="Appearance"
              value={appearance}
              options={[...APPEARANCES]}
              onChange={setAppearance}
              testId="ui-standard-org-ehg-qa-btn-pg-appearance"
            />
            <CompactSelect
              label="Icon"
              value={iconId}
              options={ICONS.map((item) => ({ label: item.label, value: item.value }))}
              onChange={(next) => {
                setIconId(next);
                if (next === "none") {
                  setIconAppearance("none");
                  setIconOnly(false);
                  setFab(false);
                  setIconShadow(false);
                } else if (iconAppearance === "none") {
                  setIconAppearance(fab ? "bare" : "filled");
                }
              }}
              testId="ui-standard-org-ehg-qa-btn-pg-icon"
            />
            <CompactSelect
              label="Icon appearance"
              value={iconAppearance}
              options={[...ICON_APPEARANCES]}
              onChange={onIconAppearanceChange}
              testId="ui-standard-org-ehg-qa-btn-pg-icon-appearance"
            />
            <CompactSelect
              label="Size"
              value={size}
              options={[...SIZES]}
              onChange={setSize}
              testId="ui-standard-org-ehg-qa-btn-pg-size"
            />
            <CompactSelect
              label="Shape"
              value={cardShape}
              options={[...CARD_SHAPES]}
              onChange={setCardShape}
              testId="ui-standard-org-ehg-qa-btn-pg-card-shape"
            />
            <CompactSelect
              label="Density"
              value={density}
              options={[...DENSITIES]}
              onChange={setDensity}
              testId="ui-standard-org-ehg-qa-btn-pg-density"
            />
            <CompactSelect
              label="Content align"
              value={contentAlign}
              options={[...CONTENT_ALIGNS]}
              onChange={setContentAlign}
              disabled={fab || showIconOnly}
              testId="ui-standard-org-ehg-qa-btn-pg-content-align"
            />
            <CompactSelect
              label="Position"
              value={fabPosition}
              options={[...FAB_POSITIONS]}
              onChange={setFabPosition}
              disabled={!fab}
              testId="ui-standard-org-ehg-qa-btn-pg-fab-position"
            />
            <CompactSelect
              label="Speed dial icons"
              value={speedDialIcons[0] ?? "person"}
              values={speedDialIcons}
              multiple
              options={SPEED_DIAL_ICON_CATALOG.map((item) => ({
                label: item.label,
                value: item.value,
              }))}
              onChangeValues={(next) => {
                setSpeedDialIcons(next);
                if (next.length === 0) {
                  setSpeedDial(false);
                  setSpeedDialOpen(false);
                }
              }}
              disabled={!speedDial}
              testId="ui-standard-org-ehg-qa-btn-pg-speed-dial-icons"
            />
            <PlaygroundTextField
              label="Label"
              value={label}
              onChange={setLabel}
              testId="ui-standard-org-ehg-qa-btn-pg-label"
            />
            <CompactSelect
              label="Background"
              value={background}
              options={[...ORG_EHG_PLAYGROUND_BG_OPTIONS]}
              onChange={setBackground}
              testId="ui-standard-org-ehg-qa-btn-pg-background"
            />
          </div>

          <div className="col-span-full flex flex-row flex-wrap items-center gap-6">
            <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
              <Switch
                checked={fab}
                onCheckedChange={onFabChange}
                aria-label="Floating action"
                disabled={iconNone}
                data-testid="ui-standard-org-ehg-qa-btn-pg-fab"
              />
              Floating action
            </label>
            <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
              <Switch
                checked={speedDial}
                onCheckedChange={onSpeedDialChange}
                aria-label="Speed dial"
                disabled={iconNone}
                data-testid="ui-standard-org-ehg-qa-btn-pg-speed-dial"
              />
              Speed dial
            </label>
            <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
              <Switch
                checked={speedDialLabels}
                onCheckedChange={setSpeedDialLabels}
                aria-label="Labels"
                disabled={!speedDial}
                data-testid="ui-standard-org-ehg-qa-btn-pg-speed-dial-labels"
              />
              Labels
            </label>
            <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
              <Switch
                checked={showIconOnly}
                onCheckedChange={onIconOnlyChange}
                aria-label="Icon only"
                disabled={fab || iconNone || iconId === "none"}
                data-testid="ui-standard-org-ehg-qa-btn-pg-icon-only"
              />
              Icon only
            </label>
            <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
              <Switch
                checked={split}
                onCheckedChange={onSplitChange}
                aria-label="Split"
                disabled={fab || iconOnly}
                data-testid="ui-standard-org-ehg-qa-btn-pg-split"
              />
              Split
            </label>
            <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
              <Switch
                checked={shadow}
                onCheckedChange={setShadow}
                aria-label="Shadow"
                disabled={showIconOnly}
                data-testid="ui-standard-org-ehg-qa-btn-pg-shadow"
              />
              Shadow
            </label>
            <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
              <Switch
                checked={iconShadow}
                onCheckedChange={setIconShadow}
                aria-label="Icon shadow"
                disabled={!hasIcon || !iconShadowAllowed}
                data-testid="ui-standard-org-ehg-qa-btn-pg-icon-shadow"
              />
              Icon shadow
            </label>
            <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
              <Switch
                checked={hover}
                onCheckedChange={setHover}
                aria-label="Hover"
                data-testid="ui-standard-org-ehg-qa-btn-pg-hover"
              />
              Hover
            </label>
          </div>

          <div className="flex min-w-0 flex-col gap-3" data-testid="ui-standard-org-ehg-qa-btn-pg-preview-card">
            <PlaygroundLabel>Preview</PlaygroundLabel>
            <div
              className={
                "org-ehg-qa-glass-panel org-ehg-qa-glass-panel--playground" +
                (asFab ? " org-ehg-qa-glass-panel--playground-fab" : "")
              }
              data-org-ehg-theme={theme}
              data-bg-option={background}
              data-testid="ui-standard-org-ehg-qa-btn-pg-preview"
              data-preview-theme={theme}
            >
              {asFab && Icon ? (
                <div
                  className={
                    "org-ehg-btn-glass-fab-scene org-ehg-btn-glass-fab-scene--playground" +
                    (asSpeedDial ? " org-ehg-btn-glass-fab-scene--speed-dial" : "")
                  }
                  data-testid="ui-standard-org-ehg-qa-btn-pg-preview-fab-scene"
                  data-fab-position={fabPosition}
                >
                  {asSpeedDial ? (
                    <div
                      className="org-ehg-btn-glass-speed-dial"
                      data-open={speedDialOpen ? "true" : "false"}
                      data-label-type={speedDialLabelType}
                      data-transition={ORG_EHG_SPEED_DIAL_TRANSITION_DEFAULT}
                      data-show={ORG_EHG_SPEED_DIAL_SHOW_DEFAULT}
                      data-testid="ui-standard-org-ehg-qa-btn-pg-preview-speed-dial"
                    >
                      <div
                        className="org-ehg-btn-glass-speed-dial__actions"
                        role="menu"
                        aria-hidden={!speedDialOpen}
                        data-testid="ui-standard-org-ehg-qa-btn-pg-preview-speed-dial-actions"
                      >
                        {speedDialActions.map((action) => {
                          const ActionIcon = action.Icon;
                          const showSideLabel = speedDialOpen && speedDialLabels;
                          return (
                            <div
                              key={action.value}
                              className="org-ehg-btn-glass-speed-dial__action-wrap"
                            >
                              {showSideLabel ? (
                                <span className="org-ehg-btn-glass-speed-dial__label" aria-hidden>
                                  {action.actionLabel}
                                  <span className="org-ehg-btn-glass-speed-dial__label-caret" />
                                </span>
                              ) : null}
                              <button
                                type="button"
                                role="menuitem"
                                className={[
                                  "org-ehg-qa-glass-icon",
                                  `org-ehg-qa-glass-icon--tone-${action.tone}`,
                                  `org-ehg-qa-glass-icon--size-${childSize}`,
                                  `org-ehg-qa-btn-glass--density-${density}`,
                                  `org-ehg-qa-btn-glass--appearance-${appearance}`,
                                  iconBare ? "org-ehg-qa-glass-icon--bare" : "",
                                  iconBare
                                    ? ""
                                    : ICON_APPEARANCE_ONLY_CLASS[
                                        iconAppearance as IconChipAppearance
                                      ],
                                ]
                                  .filter(Boolean)
                                  .join(" ")}
                                data-card-shape={cardShape}
                                data-size={childSize}
                                data-appearance={appearance}
                                aria-label={action.actionLabel}
                                tabIndex={speedDialOpen ? 0 : -1}
                                data-testid={`ui-standard-org-ehg-qa-btn-pg-preview-speed-dial-${action.value}`}
                                onClick={() => setSpeedDialOpen(false)}
                              >
                                <ActionIcon
                                  className={
                                    "org-ehg-qa-glass__glyph" +
                                    (iconBare ? " org-ehg-qa-glass__glyph--bare" : "")
                                  }
                                  aria-hidden
                                  strokeWidth={2.25}
                                />
                              </button>
                            </div>
                          );
                        })}
                      </div>
                      <button
                        type="button"
                        className={iconOnlyClass + " org-ehg-btn-glass-speed-dial__trigger"}
                        aria-label={displayLabel}
                        aria-expanded={speedDialOpen}
                        aria-haspopup="menu"
                        data-testid="ui-standard-org-ehg-qa-btn-pg-preview-btn"
                        data-size={size}
                        data-card-shape={cardShape}
                        data-density={density}
                        data-appearance={appearance}
                        data-icon-appearance={iconAppearance}
                        data-fab="true"
                        onClick={() => setSpeedDialOpen((open) => !open)}
                      >
                        <Icon
                          className={
                            "org-ehg-qa-glass__glyph" +
                            (iconBare ? " org-ehg-qa-glass__glyph--bare" : "")
                          }
                          aria-hidden
                          strokeWidth={2.25}
                          data-testid={
                            iconBare
                              ? "ui-standard-org-ehg-qa-btn-pg-preview-bare"
                              : "ui-standard-org-ehg-qa-btn-pg-preview-icon-glyph"
                          }
                        />
                      </button>
                    </div>
                  ) : (
                    <button
                      type="button"
                      className={iconOnlyClass}
                      aria-label={displayLabel}
                      data-testid="ui-standard-org-ehg-qa-btn-pg-preview-btn"
                      data-size={size}
                      data-card-shape={cardShape}
                      data-density={density}
                      data-appearance={appearance}
                      data-icon-appearance={iconAppearance}
                      data-fab="true"
                      data-fab-position={fabPosition}
                    >
                      <Icon
                        className={
                          "org-ehg-qa-glass__glyph" +
                          (iconBare ? " org-ehg-qa-glass__glyph--bare" : "")
                        }
                        aria-hidden
                        strokeWidth={2.25}
                        data-testid={
                          iconBare
                            ? "ui-standard-org-ehg-qa-btn-pg-preview-bare"
                            : "ui-standard-org-ehg-qa-btn-pg-preview-icon-glyph"
                        }
                      />
                    </button>
                  )}
                </div>
              ) : showIconOnly && Icon ? (
                <button
                  type="button"
                  className={iconOnlyClass}
                  aria-label={displayLabel}
                  data-testid="ui-standard-org-ehg-qa-btn-pg-preview-icon"
                  data-size={size}
                  data-card-shape={cardShape}
                  data-density={density}
                  data-appearance={appearance}
                >
                  <Icon className="org-ehg-qa-glass__glyph" aria-hidden strokeWidth={2.25} />
                </button>
              ) : split ? (
                <div
                  className={rowClass}
                  role="group"
                  aria-label={displayLabel}
                  data-testid="ui-standard-org-ehg-qa-btn-pg-preview-btn"
                  data-size={size}
                  data-card-shape={cardShape}
                  data-density={density}
                  data-appearance={appearance}
                  data-content-align={contentAlign}
                  data-hover={hover ? "on" : "off"}
                >
                  <button type="button" className="org-ehg-qa-glass__split-main">
                    {iconMark}
                    <span className="org-ehg-qa-glass__label">{displayLabel}</span>
                  </button>
                  <span className="org-ehg-qa-glass__split-rule" aria-hidden />
                  <button
                    type="button"
                    className="org-ehg-qa-glass__split-caret"
                    aria-label={`${displayLabel} menu`}
                  >
                    <ChevronDown className="org-ehg-qa-glass__trail" aria-hidden strokeWidth={2.25} />
                  </button>
                </div>
              ) : (
                <button
                  type="button"
                  className={rowClass}
                  data-testid="ui-standard-org-ehg-qa-btn-pg-preview-btn"
                  data-size={size}
                  data-card-shape={cardShape}
                  data-density={density}
                  data-appearance={appearance}
                  data-content-align={contentAlign}
                  data-hover={hover ? "on" : "off"}
                >
                  {iconMark}
                  <span className="org-ehg-qa-glass__label">{displayLabel}</span>
                </button>
              )}
            </div>
          </div>

          <UiStandardsCopyCommand
            standard="Org Button Glass"
            standardStatus="pilot"
            command={btnGlassCommand(
              tone,
              appearance,
              iconAppearance,
              size,
              cardShape,
              density,
              contentAlign,
              showIconOnly,
              asFab,
              fabPosition,
              asSpeedDial,
              speedDialIcons,
              speedDialLabels,
              split,
              shadow,
              iconShadow,
              hover,
            )}
            testId="ui-standard-org-ehg-qa-btn-pg-copy"
          />
        </PlaygroundSection>
      </div>
    </section>
  );
}

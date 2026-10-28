import { useState } from "react";
import {
  AlertTriangle,
  Check,
  ChevronDown,
  FilePlus,
  Heart,
  Info,
  Loader2,
  Moon,
  Plus,
  RotateCcw,
  Settings,
  ShoppingCart,
  Sparkles,
  Sun,
  UserPlus,
  type LucideIcon,
} from "lucide-react";
import { Button } from "@/components/ui/button";
import { CardTitle } from "@/components/ui/card";
import { Switch } from "@/components/ui/switch";
import { DropdownMenu, MenuItem, useDismissibleOpen } from "@/components/ui/dropdown-menu";
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
  ORG_EHG_SPEED_DIAL_LABEL_DEFAULT,
  ORG_EHG_SPEED_DIAL_LABEL_TYPES,
  ORG_EHG_SPEED_DIAL_SHOW_DEFAULT,
  ORG_EHG_SPEED_DIAL_SHOW_OPTIONS,
  ORG_EHG_SPEED_DIAL_TRANSITION_DEFAULT,
  ORG_EHG_SPEED_DIAL_TRANSITIONS,
  type OrgEhgSpeedDialLabelType,
  type OrgEhgSpeedDialShow,
  type OrgEhgSpeedDialTransition,
} from "@/features/ui-standards/org-enterprise-hybrid-glass/orgEhgSpeedDial";
import { useUiStandardsShellAppearance } from "@/features/ui-standards/UiStandardsShellAppearance";
import {
  INTENT_GLOBAL_AMBER,
  INTENT_GLOBAL_BLACK,
  INTENT_GLOBAL_DANGER,
  INTENT_GLOBAL_HELP,
  INTENT_GLOBAL_INFO,
  INTENT_GLOBAL_PRIMARY,
  INTENT_GLOBAL_SECONDARY,
  INTENT_GLOBAL_STANDARD,
  INTENT_GLOBAL_SUCCESS,
  INTENT_GLOBAL_WARN,
} from "@/features/ui-standards/intent-color-global-default";

const DROPDOWN_ACTIONS = [
  { label: "Save draft", value: "save-draft" },
  { label: "Save & continue", value: "save-continue" },
  { label: "Duplicate", value: "duplicate" },
  { label: "Archive", value: "archive" },
] as const;

const SPEED_DIAL_ACTIONS = [
  { label: "New person", value: "person", Icon: UserPlus, tone: "info" as const },
  { label: "New item", value: "item", Icon: FilePlus, tone: "success" as const },
  { label: "Cart", value: "cart", Icon: ShoppingCart, tone: "secondary" as const },
] as const;

type GlassIntensity = "solid" | "soft" | "medium" | "strong" | "ultra";
/** Button playground intents plus Org EHG extended color intents. */
type GlassIntent =
  | "standard"
  | "primary"
  | "secondary"
  | "success"
  | "info"
  | "amber"
  | "danger"
  | "warn"
  | "help"
  | "black"
  | "neutral"
  | "charcoal"
  | "orange"
  | "teal"
  | "cyan"
  | "sky"
  | "indigo"
  | "violet"
  | "fuchsia"
  | "rose"
  | "lime"
  | "emerald"
  | "people"
  | "inventory"
  | "sales";
/** Same appearance set as the normal Button playground. */
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
type GlassSize = "xs" | "sm" | "md" | "lg" | "xl";
type GlassState = "default" | "hover" | "focused" | "pressed" | "disabled" | "loading";
type GlassIconId = "none" | "check" | "plus" | "heart" | "settings" | "cart" | "info" | "alert" | "sparkles";
type GlassIconPlace = "left" | "right" | "top";
type GlassFocusAnimation = "none" | "ring-pulse" | "glow" | "breathe";

const INTENSITIES: ReadonlyArray<{ label: string; value: GlassIntensity }> = [
  { label: "Solid", value: "solid" },
  { label: "Soft Glass", value: "soft" },
  { label: "Medium Glass", value: "medium" },
  { label: "Strong Glass", value: "strong" },
  { label: "Ultra Glass", value: "ultra" },
];

const INTENTS: ReadonlyArray<{ label: string; value: GlassIntent; color: string }> = [
  { label: "Amber", value: "amber", color: INTENT_GLOBAL_AMBER },
  { label: "Black", value: "black", color: INTENT_GLOBAL_BLACK },
  { label: "Charcoal", value: "charcoal", color: "#0f172a" },
  { label: "Cyan", value: "cyan", color: "#06b6d4" },
  { label: "Danger", value: "danger", color: INTENT_GLOBAL_DANGER },
  { label: "Emerald", value: "emerald", color: "#059669" },
  { label: "Fuchsia", value: "fuchsia", color: "#d946ef" },
  { label: "Help", value: "help", color: INTENT_GLOBAL_HELP },
  { label: "Indigo", value: "indigo", color: "#6366f1" },
  { label: "Info", value: "info", color: INTENT_GLOBAL_INFO },
  { label: "Inventory", value: "inventory", color: "#f59e0b" },
  { label: "Lime", value: "lime", color: "#84cc16" },
  { label: "Neutral", value: "neutral", color: "#475569" },
  { label: "Orange", value: "orange", color: "#fb923c" },
  { label: "People", value: "people", color: "#8b5cf6" },
  { label: "Primary", value: "primary", color: INTENT_GLOBAL_PRIMARY },
  { label: "Rose", value: "rose", color: "#f43f5e" },
  { label: "Sales / POS", value: "sales", color: "#ec4899" },
  { label: "Secondary", value: "secondary", color: INTENT_GLOBAL_SECONDARY },
  { label: "Sky", value: "sky", color: "#38bdf8" },
  { label: "Standard", value: "standard", color: INTENT_GLOBAL_STANDARD },
  { label: "Success", value: "success", color: INTENT_GLOBAL_SUCCESS },
  { label: "Teal", value: "teal", color: "#26a69a" },
  { label: "Violet", value: "violet", color: "#7c3aed" },
  { label: "Warn", value: "warn", color: INTENT_GLOBAL_WARN },
];

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

const SIZES: ReadonlyArray<{ label: string; value: GlassSize }> = [
  { label: "XS", value: "xs" },
  { label: "SM", value: "sm" },
  { label: "MD", value: "md" },
  { label: "LG", value: "lg" },
  { label: "XL", value: "xl" },
];

const STATES: ReadonlyArray<{ label: string; value: GlassState }> = [
  { label: "Default", value: "default" },
  { label: "Hover", value: "hover" },
  { label: "Focused", value: "focused" },
  { label: "Pressed", value: "pressed" },
  { label: "Disabled", value: "disabled" },
  { label: "Loading", value: "loading" },
];

const ICONS: ReadonlyArray<{ label: string; value: GlassIconId; Icon: LucideIcon | null }> = [
  { label: "None", value: "none", Icon: null },
  { label: "Check", value: "check", Icon: Check },
  { label: "Plus", value: "plus", Icon: Plus },
  { label: "Heart", value: "heart", Icon: Heart },
  { label: "Settings", value: "settings", Icon: Settings },
  { label: "Cart", value: "cart", Icon: ShoppingCart },
  { label: "Info", value: "info", Icon: Info },
  { label: "Alert", value: "alert", Icon: AlertTriangle },
  { label: "Sparkles", value: "sparkles", Icon: Sparkles },
];

const ICON_PLACES: ReadonlyArray<{ label: string; value: GlassIconPlace }> = [
  { label: "Left", value: "left" },
  { label: "Right", value: "right" },
  { label: "Top", value: "top" },
];

const FOCUS_ANIMATIONS: ReadonlyArray<{ label: string; value: GlassFocusAnimation }> = [
  { label: "None", value: "none" },
  { label: "Ring pulse", value: "ring-pulse" },
  { label: "Glow", value: "glow" },
  { label: "Breathe", value: "breathe" },
];

const DEFAULTS = {
  intensity: "medium" as GlassIntensity,
  intent: "primary" as GlassIntent,
  appearance: "standard" as GlassAppearance,
  size: "md" as GlassSize,
  state: "default" as GlassState,
  iconId: "check" as GlassIconId,
  iconPlace: "left" as GlassIconPlace,
  iconOnly: false,
  fab: false,
  speedDial: false,
  speedDialLabelType: ORG_EHG_SPEED_DIAL_LABEL_DEFAULT,
  speedDialTransition: ORG_EHG_SPEED_DIAL_TRANSITION_DEFAULT,
  speedDialShow: ORG_EHG_SPEED_DIAL_SHOW_DEFAULT,
  dropdown: false,
  badge: false,
  shadow: true,
  label: "Glass button",
  focusAnimation: "none" as GlassFocusAnimation,
};

function glassCommand(
  intensity: GlassIntensity,
  intent: GlassIntent,
  appearance: GlassAppearance,
  size: GlassSize,
  state: GlassState,
  iconPlace: GlassIconPlace,
  iconOnly: boolean,
  fab: boolean,
  speedDial: boolean,
  speedDialLabelType: OrgEhgSpeedDialLabelType,
  speedDialTransition: OrgEhgSpeedDialTransition,
  speedDialShow: OrgEhgSpeedDialShow,
  dropdown: boolean,
  badge: boolean,
  shadow: boolean,
  focusAnimation: GlassFocusAnimation,
): string {
  const parts = ["ext", "org-ehg", "btn-glass", intensity, intent, appearance, size];
  if (state !== "default") parts.push(state);
  if (fab) parts.push("fab");
  else if (iconOnly) parts.push("icon-only");
  else if (iconPlace !== "left") parts.push(`icon-${iconPlace}`);
  if (speedDial && fab) {
    parts.push("speed-dial");
    if (speedDialLabelType !== ORG_EHG_SPEED_DIAL_LABEL_DEFAULT) {
      parts.push(speedDialLabelType);
    }
    if (speedDialShow !== ORG_EHG_SPEED_DIAL_SHOW_DEFAULT) {
      parts.push(`show-${speedDialShow}`);
    }
    if (speedDialTransition !== ORG_EHG_SPEED_DIAL_TRANSITION_DEFAULT) {
      parts.push(`transition-${speedDialTransition}`);
    }
  }
  if (dropdown) parts.push("dropdown");
  if (badge) parts.push("badge");
  if (!shadow) parts.push("no-shadow");
  if (focusAnimation !== "none") parts.push(`focus-${focusAnimation}`);
  return parts.join(" ");
}

/**
 * Interactive Org Enterprise Hybrid Glass button playground (UI Standards / DEV only).
 * Pass `defaultFab` when hosting under Floating Action glass.
 */
export function OrgEhgButtonGlassPlayground({ defaultFab = false }: { defaultFab?: boolean }) {
  const fabDefaults = defaultFab
    ? {
        size: "lg" as GlassSize,
        iconId: "plus" as GlassIconId,
        iconOnly: true,
        fab: true,
        label: "Add",
      }
    : null;
  const { background, setBackground, theme, toggleTheme } = useUiStandardsShellAppearance();
  const [intensity, setIntensity] = useState(DEFAULTS.intensity);
  const [intent, setIntent] = useState(DEFAULTS.intent);
  const [appearance, setAppearance] = useState(DEFAULTS.appearance);
  const [size, setSize] = useState(fabDefaults?.size ?? DEFAULTS.size);
  const [state, setState] = useState(DEFAULTS.state);
  const [iconId, setIconId] = useState(fabDefaults?.iconId ?? DEFAULTS.iconId);
  const [iconPlace, setIconPlace] = useState(DEFAULTS.iconPlace);
  const [iconOnly, setIconOnly] = useState(fabDefaults?.iconOnly ?? DEFAULTS.iconOnly);
  const [fab, setFab] = useState(fabDefaults?.fab ?? DEFAULTS.fab);
  const [speedDial, setSpeedDial] = useState(DEFAULTS.speedDial);
  const [speedDialLabelType, setSpeedDialLabelType] = useState(DEFAULTS.speedDialLabelType);
  const [speedDialTransition, setSpeedDialTransition] = useState(DEFAULTS.speedDialTransition);
  const [speedDialShow, setSpeedDialShow] = useState(DEFAULTS.speedDialShow);
  const [speedDialOpen, setSpeedDialOpen] = useState(false);
  const [dropdown, setDropdown] = useState(DEFAULTS.dropdown);
  const [badge, setBadge] = useState(DEFAULTS.badge);
  const [shadow, setShadow] = useState(DEFAULTS.shadow);
  const [label, setLabel] = useState(fabDefaults?.label ?? DEFAULTS.label);
  const [focusAnimation, setFocusAnimation] = useState(DEFAULTS.focusAnimation);
  const [menuChoice, setMenuChoice] = useState<string>("");
  const menu = useDismissibleOpen(false);

  const Icon = ICONS.find((item) => item.value === iconId)?.Icon ?? null;
  const showIconOnly = iconOnly || fab;
  const PreviewIcon = showIconOnly ? Icon ?? Plus : Icon;
  const iconOnTop = !showIconOnly && iconPlace === "top" && PreviewIcon != null;
  const iconOnRight = !showIconOnly && iconPlace === "right" && PreviewIcon != null;
  const iconOnLeft = !showIconOnly && iconPlace === "left" && PreviewIcon != null;
  const disabled = state === "disabled" || state === "loading";
  const className = [
    "org-ehg-btn-glass",
    `org-ehg-btn-glass--${intensity}`,
    `org-ehg-btn-glass--intent-${intent}`,
    `org-ehg-btn-glass--appearance-${appearance}`,
    `org-ehg-btn-glass--size-${size}`,
    state !== "default" ? `org-ehg-btn-glass--state-${state}` : "",
    showIconOnly ? "org-ehg-btn-glass--icon-only" : "",
    fab ? "org-ehg-btn-glass--fab" : "",
    iconOnTop ? "org-ehg-btn-glass--icon-top" : "",
    dropdown && !fab ? "org-ehg-btn-glass--with-dropdown" : "",
    shadow ? "" : "org-ehg-btn-glass--no-shadow",
    focusAnimation !== "none" ? `org-ehg-btn-glass--focus-${focusAnimation}` : "",
  ]
    .filter(Boolean)
    .join(" ");

  function resetPreview() {
    setIntensity(DEFAULTS.intensity);
    setIntent(DEFAULTS.intent);
    setAppearance(DEFAULTS.appearance);
    setSize(fabDefaults?.size ?? DEFAULTS.size);
    setState(DEFAULTS.state);
    setIconId(fabDefaults?.iconId ?? DEFAULTS.iconId);
    setIconPlace(DEFAULTS.iconPlace);
    setIconOnly(fabDefaults?.iconOnly ?? DEFAULTS.iconOnly);
    setFab(fabDefaults?.fab ?? DEFAULTS.fab);
    setSpeedDial(DEFAULTS.speedDial);
    setSpeedDialLabelType(DEFAULTS.speedDialLabelType);
    setSpeedDialTransition(DEFAULTS.speedDialTransition);
    setSpeedDialShow(DEFAULTS.speedDialShow);
    setSpeedDialOpen(false);
    setDropdown(DEFAULTS.dropdown);
    setBadge(DEFAULTS.badge);
    setShadow(DEFAULTS.shadow);
    setLabel(fabDefaults?.label ?? DEFAULTS.label);
    setBackground(ORG_EHG_PLAYGROUND_BG_DEFAULT);
    setFocusAnimation(DEFAULTS.focusAnimation);
    setMenuChoice("");
    menu.close();
  }

  function onIconPlaceChange(next: GlassIconPlace) {
    setIconPlace(next);
    if (next === "top") {
      setIconOnly(false);
      setFab(false);
      setSpeedDial(false);
      setSpeedDialOpen(false);
    }
  }

  function onIconOnlyChange(next: boolean) {
    setIconOnly(next);
    if (next) {
      setIconPlace("left");
      setFab(false);
      setSpeedDial(false);
      setSpeedDialOpen(false);
    }
  }

  function onFabChange(next: boolean) {
    setFab(next);
    if (next) {
      setIconOnly(true);
      setIconPlace("left");
      setDropdown(false);
      if (iconId === "none") setIconId("plus");
      if (size === "xs" || size === "md") setSize("lg");
      if (!label.trim() || label === DEFAULTS.label) setLabel("Add");
    } else {
      setSpeedDial(false);
      setSpeedDialOpen(false);
    }
  }

  function onSpeedDialChange(next: boolean) {
    setSpeedDial(next);
    if (next) {
      onFabChange(true);
      setSpeedDialOpen(true);
    } else {
      setSpeedDialOpen(false);
    }
  }

  const childSize: GlassSize = size === "xl" ? "lg" : size === "lg" ? "md" : size === "md" ? "sm" : "xs";

  return (
    <section
      className="org-ehg org-ehg-pg-bg org-ehg-glass-playground-card flex min-w-0 flex-col"
      data-testid="ui-standard-card-org-ehg-btn-glass-playground"
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
          <CardTitle>Glass button playground</CardTitle>
          <div className="flex items-center gap-2">
            <Button
              type="button"
              intent={theme === "dark" ? "primary" : "neutral"}
              appearance={theme === "dark" ? "solid" : "outline"}
              shape="pill"
              size="icon"
              aria-label={theme === "dark" ? "Light mode" : "Dark mode"}
              aria-pressed={theme === "dark"}
              data-testid="ui-standard-org-ehg-btn-pg-theme"
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
              data-testid="ui-standard-org-ehg-btn-pg-reset"
              onClick={resetPreview}
            >
              <RotateCcw className="size-4" aria-hidden />
            </Button>
          </div>
        </div>
      <PlaygroundSection testId="ui-standard-org-ehg-btn-glass-playground" className="">
        <div className="grid min-w-0 gap-2 sm:grid-cols-2 lg:grid-cols-3">
          <CompactSelect
            label="Intensity"
            value={intensity}
            options={[...INTENSITIES]}
            onChange={setIntensity}
            testId="ui-standard-org-ehg-btn-pg-intensity"
          />
          <CompactSelect
            label="Intent"
            value={intent}
            options={INTENTS.map((item) => ({
              label: item.label,
              value: item.value,
              color: item.color === INTENT_GLOBAL_STANDARD ? "#64748b" : item.color,
            }))}
            onChange={setIntent}
            testId="ui-standard-org-ehg-btn-pg-intent"
          />
          <CompactSelect
            label="Appearance"
            value={appearance}
            options={[...APPEARANCES]}
            onChange={setAppearance}
            testId="ui-standard-org-ehg-btn-pg-appearance"
          />
          <CompactSelect
            label="Size"
            value={size}
            options={[...SIZES]}
            onChange={setSize}
            testId="ui-standard-org-ehg-btn-pg-size"
          />
          <CompactSelect
            label="State"
            value={state}
            options={[...STATES]}
            onChange={setState}
            testId="ui-standard-org-ehg-btn-pg-state"
          />
          <CompactSelect
            label="Icon"
            value={iconId}
            options={ICONS.map((item) => ({ label: item.label, value: item.value }))}
            onChange={setIconId}
            testId="ui-standard-org-ehg-btn-pg-icon"
          />
          <CompactSelect
            label="Icon place"
            value={iconPlace}
            options={[...ICON_PLACES]}
            onChange={onIconPlaceChange}
            testId="ui-standard-org-ehg-btn-pg-icon-place"
            disabled={fab || iconOnly}
          />
          <PlaygroundTextField
            label="Label"
            value={label}
            onChange={setLabel}
            testId="ui-standard-org-ehg-btn-pg-label"
          />
          <CompactSelect
            label="Background"
            value={background}
            options={[...ORG_EHG_PLAYGROUND_BG_OPTIONS]}
            onChange={setBackground}
            testId="ui-standard-org-ehg-btn-pg-background"
          />
          <CompactSelect
            label="Focus animation"
            value={focusAnimation}
            options={[...FOCUS_ANIMATIONS]}
            onChange={setFocusAnimation}
            testId="ui-standard-org-ehg-btn-pg-focus-animation"
          />
          <CompactSelect
            label="Speed dial type"
            value={speedDialLabelType}
            options={[...ORG_EHG_SPEED_DIAL_LABEL_TYPES]}
            onChange={setSpeedDialLabelType}
            testId="ui-standard-org-ehg-btn-pg-speed-dial-type"
            disabled={!speedDial}
          />
          <CompactSelect
            label="Speed dial show"
            value={speedDialShow}
            options={[...ORG_EHG_SPEED_DIAL_SHOW_OPTIONS]}
            onChange={setSpeedDialShow}
            testId="ui-standard-org-ehg-btn-pg-speed-dial-show"
            disabled={!speedDial}
          />
          <CompactSelect
            label="Speed dial transition"
            value={speedDialTransition}
            options={[...ORG_EHG_SPEED_DIAL_TRANSITIONS]}
            onChange={setSpeedDialTransition}
            testId="ui-standard-org-ehg-btn-pg-speed-dial-transition"
            disabled={!speedDial}
          />
        </div>

        <div className="col-span-full flex flex-row flex-wrap items-center gap-6">
          <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
            <Switch
              checked={fab}
              onCheckedChange={onFabChange}
              aria-label="Floating action"
              data-testid="ui-standard-org-ehg-btn-pg-fab"
            />
            Floating action
          </label>
          <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
            <Switch
              checked={speedDial}
              onCheckedChange={onSpeedDialChange}
              aria-label="Speed dial"
              data-testid="ui-standard-org-ehg-btn-pg-speed-dial"
            />
            Speed dial
          </label>
          <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
            <Switch
              checked={iconOnly}
              onCheckedChange={onIconOnlyChange}
              aria-label="Icon only"
              disabled={fab}
              data-testid="ui-standard-org-ehg-btn-pg-icon-only"
            />
            Icon only
          </label>
          <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
            <Switch
              checked={dropdown}
              onCheckedChange={setDropdown}
              aria-label="Dropdown"
              disabled={fab}
              data-testid="ui-standard-org-ehg-btn-pg-dropdown"
            />
            Dropdown
          </label>
          <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
            <Switch
              checked={badge}
              onCheckedChange={setBadge}
              aria-label="Badge"
              data-testid="ui-standard-org-ehg-btn-pg-badge"
            />
            Badge
          </label>
          <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
            <Switch
              checked={shadow}
              onCheckedChange={setShadow}
              aria-label="Shadow"
              data-testid="ui-standard-org-ehg-btn-pg-shadow"
            />
            Shadow
          </label>
        </div>

        <div
          className="flex min-w-0 flex-col gap-3"
          data-testid="ui-standard-org-ehg-btn-pg-preview-card"
        >
          <PlaygroundLabel>Preview</PlaygroundLabel>
          <div
            className={
              "org-ehg-btn-glass-panel org-ehg-btn-glass-panel--playground" +
              (fab ? " org-ehg-btn-glass-panel--playground-fab" : "")
            }
            data-org-ehg-theme={theme}
            data-bg-option={background}
            data-testid="ui-standard-org-ehg-btn-pg-preview"
            data-preview-theme={theme}
          >
            {fab ? (
              <div
                className={
                  "org-ehg-btn-glass-fab-scene org-ehg-btn-glass-fab-scene--playground" +
                  (speedDial ? " org-ehg-btn-glass-fab-scene--speed-dial" : "")
                }
                data-testid="ui-standard-org-ehg-btn-pg-preview-fab-scene"
              >
                {speedDial ? (
                  <div
                    className="org-ehg-btn-glass-speed-dial"
                    data-open={speedDialOpen ? "true" : "false"}
                    data-label-type={speedDialLabelType}
                    data-transition={speedDialTransition}
                    data-show={speedDialShow}
                    data-testid="ui-standard-org-ehg-btn-pg-preview-speed-dial"
                  >
                    <div
                      className="org-ehg-btn-glass-speed-dial__actions"
                      role="menu"
                      aria-hidden={!speedDialOpen}
                      data-testid="ui-standard-org-ehg-btn-pg-preview-speed-dial-actions"
                    >
                      {SPEED_DIAL_ACTIONS.map((action) => {
                        const ActionIcon = action.Icon;
                        const showSideLabel =
                          speedDialOpen &&
                          speedDialLabelType !== "icon-only" &&
                          speedDialLabelType !== "extended";
                        const isExtended = speedDialLabelType === "extended";
                        return (
                          <div key={action.value} className="org-ehg-btn-glass-speed-dial__action-wrap">
                            {showSideLabel ? (
                              <span className="org-ehg-btn-glass-speed-dial__label" aria-hidden>
                                {action.label}
                                {speedDialLabelType === "label-tooltip" ? (
                                  <span className="org-ehg-btn-glass-speed-dial__label-caret" />
                                ) : null}
                              </span>
                            ) : null}
                            <button
                              type="button"
                              role="menuitem"
                              className={[
                                "org-ehg-btn-glass",
                                `org-ehg-btn-glass--${intensity}`,
                                `org-ehg-btn-glass--tone-${action.tone}`,
                                `org-ehg-btn-glass--size-${childSize}`,
                                isExtended ? "org-ehg-btn-glass--fab-extended" : "org-ehg-btn-glass--icon-only",
                                "org-ehg-btn-glass--fab",
                              ].join(" ")}
                              aria-label={action.label}
                              tabIndex={speedDialOpen ? 0 : -1}
                              data-testid={`ui-standard-org-ehg-btn-pg-preview-speed-dial-${action.value}`}
                              onClick={() => setSpeedDialOpen(false)}
                            >
                              <ActionIcon
                                className="org-ehg-btn-glass__icon"
                                aria-hidden
                                strokeWidth={2.25}
                              />
                              {isExtended ? (
                                <span className="org-ehg-btn-glass-speed-dial__extended-text">
                                  {action.label}
                                </span>
                              ) : null}
                            </button>
                          </div>
                        );
                      })}
                    </div>
                    <button
                      type="button"
                      className={`${className} org-ehg-btn-glass-speed-dial__trigger`}
                      data-testid="ui-standard-org-ehg-btn-pg-preview-btn"
                      disabled={disabled}
                      aria-busy={state === "loading" || undefined}
                      aria-label={label.trim() || "Add"}
                      aria-expanded={speedDialOpen}
                      aria-haspopup="menu"
                      onClick={() => {
                        if (!disabled) setSpeedDialOpen((open) => !open);
                      }}
                    >
                      {state === "loading" ? (
                        <Loader2
                          className="org-ehg-btn-glass__icon org-ehg-btn-glass__spin"
                          aria-hidden
                          strokeWidth={2.25}
                        />
                      ) : PreviewIcon ? (
                        <PreviewIcon
                          className="org-ehg-btn-glass__icon"
                          aria-hidden
                          strokeWidth={2.25}
                          data-testid="ui-standard-org-ehg-btn-pg-preview-icon"
                        />
                      ) : null}
                      {badge ? (
                        <span className="org-ehg-btn-glass__badge" aria-hidden>
                          3
                        </span>
                      ) : null}
                    </button>
                  </div>
                ) : (
                  <button
                    type="button"
                    className={className}
                    data-testid="ui-standard-org-ehg-btn-pg-preview-btn"
                    disabled={disabled}
                    aria-busy={state === "loading" || undefined}
                    aria-label={label.trim() || "Add"}
                  >
                    {state === "loading" ? (
                      <Loader2
                        className="org-ehg-btn-glass__icon org-ehg-btn-glass__spin"
                        aria-hidden
                        strokeWidth={2.25}
                      />
                    ) : PreviewIcon ? (
                      <PreviewIcon
                        className="org-ehg-btn-glass__icon"
                        aria-hidden
                        strokeWidth={2.25}
                        data-testid="ui-standard-org-ehg-btn-pg-preview-icon"
                      />
                    ) : null}
                    {badge ? (
                      <span className="org-ehg-btn-glass__badge" aria-hidden>
                        3
                      </span>
                    ) : null}
                  </button>
                )}
              </div>
            ) : dropdown ? (
              <DropdownMenu
                open={menu.open && !disabled}
                onOpenChange={(next) => {
                  if (!disabled) menu.setOpen(next);
                }}
                align="start"
                menuClassName={
                  "org-ehg-glass-menu" + (theme === "dark" ? " org-ehg-glass-menu--dark" : "")
                }
                menuLabel={label.trim() || "Actions"}
                trigger={({ id, expanded, controls, onClick, onKeyDown }) => (
                  <button
                    type="button"
                    id={id}
                    className={className}
                    data-testid="ui-standard-org-ehg-btn-pg-preview-btn"
                    disabled={disabled}
                    aria-busy={state === "loading" || undefined}
                    aria-label={showIconOnly ? label.trim() || "Glass button" : undefined}
                    aria-haspopup="menu"
                    aria-expanded={expanded}
                    aria-controls={controls}
                    onClick={onClick}
                    onKeyDown={onKeyDown}
                  >
                    {state === "loading" ? null : iconOnLeft || showIconOnly || iconOnTop ? (
                      PreviewIcon ? (
                        <PreviewIcon
                          className="org-ehg-btn-glass__icon"
                          aria-hidden
                          strokeWidth={2.25}
                          data-testid="ui-standard-org-ehg-btn-pg-preview-icon"
                        />
                      ) : null
                    ) : null}
                    {showIconOnly ? null : (
                      <span>
                        {menuChoice
                          ? DROPDOWN_ACTIONS.find((item) => item.value === menuChoice)?.label ??
                            (label.trim() || "Glass button")
                          : label.trim() || "Glass button"}
                      </span>
                    )}
                    {state === "loading" ? null : iconOnRight ? (
                      PreviewIcon ? (
                        <PreviewIcon
                          className="org-ehg-btn-glass__icon"
                          aria-hidden
                          strokeWidth={2.25}
                          data-testid="ui-standard-org-ehg-btn-pg-preview-icon"
                        />
                      ) : null
                    ) : null}
                    {state === "loading" ? (
                      <Loader2
                        className="org-ehg-btn-glass__icon org-ehg-btn-glass__spin"
                        aria-hidden
                        strokeWidth={2.25}
                      />
                    ) : (
                      <ChevronDown
                        className="org-ehg-btn-glass__icon org-ehg-btn-glass__caret"
                        aria-hidden
                        strokeWidth={2.25}
                        data-testid="ui-standard-org-ehg-btn-pg-preview-caret"
                      />
                    )}
                    {badge ? (
                      <span className="org-ehg-btn-glass__badge" aria-hidden>
                        3
                      </span>
                    ) : null}
                  </button>
                )}
              >
                <div
                  className="org-ehg-glass-menu__list"
                  role="group"
                  data-testid="ui-standard-org-ehg-btn-pg-preview-menu"
                >
                  {DROPDOWN_ACTIONS.map((action) => (
                    <MenuItem
                      key={action.value}
                      className={
                        "org-ehg-glass-menu__option" +
                        (menuChoice === action.value ? " org-ehg-glass-menu__option--selected" : "")
                      }
                      data-selected={menuChoice === action.value ? "true" : "false"}
                      data-testid={`ui-standard-org-ehg-btn-pg-preview-option-${action.value}`}
                      onSelect={() => {
                        setMenuChoice(action.value);
                        menu.close();
                      }}
                    >
                      {action.label}
                    </MenuItem>
                  ))}
                </div>
              </DropdownMenu>
            ) : (
              <button
                type="button"
                className={className}
                data-testid="ui-standard-org-ehg-btn-pg-preview-btn"
                disabled={disabled}
                aria-busy={state === "loading" || undefined}
                aria-label={showIconOnly ? label.trim() || "Glass button" : undefined}
              >
                {state === "loading" ? null : iconOnLeft || showIconOnly || iconOnTop ? (
                  PreviewIcon ? (
                    <PreviewIcon
                      className="org-ehg-btn-glass__icon"
                      aria-hidden
                      strokeWidth={2.25}
                      data-testid="ui-standard-org-ehg-btn-pg-preview-icon"
                    />
                  ) : null
                ) : null}
                {showIconOnly ? null : <span>{label.trim() || "Glass button"}</span>}
                {state === "loading" ? null : iconOnRight ? (
                  PreviewIcon ? (
                    <PreviewIcon
                      className="org-ehg-btn-glass__icon"
                      aria-hidden
                      strokeWidth={2.25}
                      data-testid="ui-standard-org-ehg-btn-pg-preview-icon"
                    />
                  ) : null
                ) : null}
                {state === "loading" ? (
                  <Loader2
                    className="org-ehg-btn-glass__icon org-ehg-btn-glass__spin"
                    aria-hidden
                    strokeWidth={2.25}
                  />
                ) : null}
                {badge ? (
                  <span className="org-ehg-btn-glass__badge" aria-hidden>
                    3
                  </span>
                ) : null}
              </button>
            )}
          </div>
        </div>

        <UiStandardsCopyCommand
          standard="Org Button Glass"
          standardStatus="pilot"
          command={glassCommand(
            intensity,
            intent,
            appearance,
            size,
            state,
            iconPlace,
            iconOnly,
            fab,
            speedDial,
            speedDialLabelType,
            speedDialTransition,
            speedDialShow,
            dropdown,
            badge,
            shadow,
            focusAnimation,
          )}
          testId="ui-standard-org-ehg-btn-pg-copy"
        />
      </PlaygroundSection>
      </div>
    </section>
  );
}

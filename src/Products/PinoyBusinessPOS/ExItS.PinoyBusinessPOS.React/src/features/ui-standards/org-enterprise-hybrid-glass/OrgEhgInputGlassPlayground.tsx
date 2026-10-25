import { useState } from "react";
import {
  Heart,
  Mail,
  Moon,
  RotateCcw,
  Search,
  Settings,
  Sparkles,
  Sun,
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
  OrgEhgInputGlassField,
  type OrgEhgInputGlassFocusAnimation,
  type OrgEhgInputGlassIconPlace,
  type OrgEhgInputGlassIntensity,
  type OrgEhgInputGlassKind,
  type OrgEhgInputGlassLabelPlace,
  type OrgEhgInputGlassSize,
  type OrgEhgInputGlassState,
} from "@/features/ui-standards/org-enterprise-hybrid-glass/OrgEhgInputGlassField";
import {
  ORG_EHG_PLAYGROUND_BG_DEFAULT,
  ORG_EHG_PLAYGROUND_BG_DEFAULT_INTENSITY,
  ORG_EHG_PLAYGROUND_BG_OPTIONS,
} from "@/features/ui-standards/org-enterprise-hybrid-glass/orgEhgPlaygroundBackground";
import { useUiStandardsShellAppearance } from "@/features/ui-standards/UiStandardsShellAppearance";

type IconId = "none" | "mail" | "search" | "settings" | "heart" | "sparkles";

const INTENSITIES: ReadonlyArray<{ label: string; value: OrgEhgInputGlassIntensity }> = [
  { label: "Solid", value: "solid" },
  { label: "Soft Glass", value: "soft" },
  { label: "Medium Glass", value: "medium" },
  { label: "Strong Glass", value: "strong" },
  { label: "Ultra Glass", value: "ultra" },
];

const KINDS: ReadonlyArray<{ label: string; value: OrgEhgInputGlassKind }> = [
  { label: "Text", value: "text" },
  { label: "Email", value: "email" },
  { label: "Password", value: "password" },
  { label: "Search", value: "search" },
  { label: "Textarea", value: "textarea" },
  { label: "Select", value: "select" },
];

const SIZES: ReadonlyArray<{ label: string; value: OrgEhgInputGlassSize }> = [
  { label: "SM", value: "sm" },
  { label: "MD", value: "md" },
  { label: "LG", value: "lg" },
];

const STATES: ReadonlyArray<{ label: string; value: OrgEhgInputGlassState }> = [
  { label: "Default", value: "default" },
  { label: "Hover", value: "hover" },
  { label: "Focused", value: "focused" },
  { label: "Error", value: "error" },
  { label: "Success", value: "success" },
  { label: "Disabled", value: "disabled" },
  { label: "Readonly", value: "readonly" },
];

const LABEL_PLACES: ReadonlyArray<{ label: string; value: OrgEhgInputGlassLabelPlace }> = [
  { label: "Above", value: "above" },
  { label: "None", value: "none" },
];

const ICON_PLACES: ReadonlyArray<{ label: string; value: OrgEhgInputGlassIconPlace }> = [
  { label: "None", value: "none" },
  { label: "Left", value: "left" },
  { label: "Right", value: "right" },
  { label: "Top", value: "top" },
];

const FOCUS_ANIMATIONS: ReadonlyArray<{ label: string; value: OrgEhgInputGlassFocusAnimation }> = [
  { label: "None", value: "none" },
  { label: "Ring pulse", value: "ring-pulse" },
  { label: "Glow", value: "glow" },
  { label: "Breathe", value: "breathe" },
  { label: "Thick below", value: "thick-below" },
];

const ICONS: ReadonlyArray<{ label: string; value: IconId; Icon: LucideIcon | null }> = [
  { label: "None", value: "none", Icon: null },
  { label: "Mail", value: "mail", Icon: Mail },
  { label: "Search", value: "search", Icon: Search },
  { label: "Settings", value: "settings", Icon: Settings },
  { label: "Heart", value: "heart", Icon: Heart },
  { label: "Sparkles", value: "sparkles", Icon: Sparkles },
];

const DEFAULTS = {
  intensity: "medium" as OrgEhgInputGlassIntensity,
  kind: "text" as OrgEhgInputGlassKind,
  size: "md" as OrgEhgInputGlassSize,
  state: "default" as OrgEhgInputGlassState,
  labelPlace: "above" as OrgEhgInputGlassLabelPlace,
  iconPlace: "left" as OrgEhgInputGlassIconPlace,
  iconId: "mail" as IconId,
  clearable: true,
  dropdown: false,
  label: "Email",
  value: "",
  focusAnimation: "none" as OrgEhgInputGlassFocusAnimation,
};

function inputGlassCommand(
  intensity: OrgEhgInputGlassIntensity,
  kind: OrgEhgInputGlassKind,
  size: OrgEhgInputGlassSize,
  state: OrgEhgInputGlassState,
  iconPlace: OrgEhgInputGlassIconPlace,
  dropdown: boolean,
  clearable: boolean,
  focusAnimation: OrgEhgInputGlassFocusAnimation,
): string {
  const parts = ["ext", "org-ehg", "input-glass", intensity, kind, size];
  if (state !== "default") parts.push(state);
  if (iconPlace !== "none") parts.push(`icon-${iconPlace}`);
  if (dropdown || kind === "select") parts.push("dropdown");
  if (clearable) parts.push("clearable");
  if (focusAnimation !== "none") parts.push(`focus-${focusAnimation}`);
  return parts.join(" ");
}

/**
 * Interactive Org Enterprise Hybrid Glass input playground (UI Standards / DEV only).
 * Background dropdown updates this card and the shared UI Standards shell.
 * Light/dark follows Preferences (no separate playground theme control).
 */
export function OrgEhgInputGlassPlayground() {
  const { background, setBackground, theme, toggleTheme } = useUiStandardsShellAppearance();
  const [intensity, setIntensity] = useState(DEFAULTS.intensity);
  const [kind, setKind] = useState(DEFAULTS.kind);
  const [size, setSize] = useState(DEFAULTS.size);
  const [state, setState] = useState(DEFAULTS.state);
  const [labelPlace, setLabelPlace] = useState(DEFAULTS.labelPlace);
  const [iconPlace, setIconPlace] = useState(DEFAULTS.iconPlace);
  const [iconId, setIconId] = useState(DEFAULTS.iconId);
  const [clearable, setClearable] = useState(DEFAULTS.clearable);
  const [dropdown, setDropdown] = useState(DEFAULTS.dropdown);
  const [label, setLabel] = useState(DEFAULTS.label);
  const [value, setValue] = useState(DEFAULTS.value);
  const [focusAnimation, setFocusAnimation] = useState(DEFAULTS.focusAnimation);

  const Icon = ICONS.find((item) => item.value === iconId)?.Icon ?? null;

  function resetPreview() {
    setIntensity(DEFAULTS.intensity);
    setKind(DEFAULTS.kind);
    setSize(DEFAULTS.size);
    setState(DEFAULTS.state);
    setLabelPlace(DEFAULTS.labelPlace);
    setIconPlace(DEFAULTS.iconPlace);
    setIconId(DEFAULTS.iconId);
    setClearable(DEFAULTS.clearable);
    setDropdown(DEFAULTS.dropdown);
    setLabel(DEFAULTS.label);
    setValue(DEFAULTS.value);
    setBackground(ORG_EHG_PLAYGROUND_BG_DEFAULT);
    setFocusAnimation(DEFAULTS.focusAnimation);
  }

  return (
    <section
      className="org-ehg org-ehg-pg-bg org-ehg-glass-playground-card flex min-w-0 flex-col"
      data-testid="ui-standard-card-org-ehg-input-glass-playground"
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
          <CardTitle>Glass input playground</CardTitle>
          <div className="flex items-center gap-2">
            <Button
              type="button"
              intent={theme === "dark" ? "primary" : "neutral"}
              appearance={theme === "dark" ? "solid" : "outline"}
              shape="pill"
              size="icon"
              aria-label={theme === "dark" ? "Light mode" : "Dark mode"}
              aria-pressed={theme === "dark"}
              data-testid="ui-standard-org-ehg-input-pg-theme"
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
              data-testid="ui-standard-org-ehg-input-pg-reset"
              onClick={resetPreview}
            >
              <RotateCcw className="size-4" aria-hidden />
            </Button>
          </div>
        </div>
        <PlaygroundSection testId="ui-standard-org-ehg-input-glass-playground" className="">
          <div className="grid min-w-0 gap-2 sm:grid-cols-2 lg:grid-cols-3">
            <CompactSelect
              label="Intensity"
              value={intensity}
              options={[...INTENSITIES]}
              onChange={setIntensity}
              testId="ui-standard-org-ehg-input-pg-intensity"
            />
            <CompactSelect
              label="Type"
              value={kind}
              options={[...KINDS]}
              onChange={setKind}
              testId="ui-standard-org-ehg-input-pg-kind"
            />
            <CompactSelect
              label="Size"
              value={size}
              options={[...SIZES]}
              onChange={setSize}
              testId="ui-standard-org-ehg-input-pg-size"
            />
            <CompactSelect
              label="State"
              value={state}
              options={[...STATES]}
              onChange={setState}
              testId="ui-standard-org-ehg-input-pg-state"
            />
            <CompactSelect
              label="Label place"
              value={labelPlace}
              options={[...LABEL_PLACES]}
              onChange={setLabelPlace}
              testId="ui-standard-org-ehg-input-pg-label-place"
            />
            <CompactSelect
              label="Icon place"
              value={iconPlace}
              options={[...ICON_PLACES]}
              onChange={setIconPlace}
              testId="ui-standard-org-ehg-input-pg-icon-place"
            />
            <CompactSelect
              label="Icon"
              value={iconId}
              options={ICONS.map((item) => ({ label: item.label, value: item.value }))}
              onChange={setIconId}
              testId="ui-standard-org-ehg-input-pg-icon"
            />
            <PlaygroundTextField
              label="Label"
              value={label}
              onChange={setLabel}
              testId="ui-standard-org-ehg-input-pg-label"
            />
            <CompactSelect
              label="Background"
              value={background}
              options={[...ORG_EHG_PLAYGROUND_BG_OPTIONS]}
              onChange={setBackground}
              testId="ui-standard-org-ehg-input-pg-background"
            />
            <CompactSelect
              label="Focus animation"
              value={focusAnimation}
              options={[...FOCUS_ANIMATIONS]}
              onChange={setFocusAnimation}
              testId="ui-standard-org-ehg-input-pg-focus-animation"
            />
          </div>

          <div className="col-span-full flex flex-row flex-wrap items-center gap-6">
            <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
              <Switch
                checked={dropdown}
                onCheckedChange={setDropdown}
                aria-label="Dropdown"
                data-testid="ui-standard-org-ehg-input-pg-dropdown"
              />
              Dropdown
            </label>
            <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
              <Switch
                checked={clearable}
                onCheckedChange={setClearable}
                aria-label="Clearable"
                data-testid="ui-standard-org-ehg-input-pg-clearable"
              />
              Clearable
            </label>
          </div>

          <div
            className="flex min-w-0 flex-col gap-3"
            data-testid="ui-standard-org-ehg-input-pg-preview-card"
          >
            <PlaygroundLabel>Preview</PlaygroundLabel>
            <div
              className="org-ehg-input-glass-panel org-ehg-input-glass-panel--playground"
              data-testid="ui-standard-org-ehg-input-pg-preview"
              data-bg-option={background}
              data-org-ehg-theme={theme}
              data-preview-theme={theme}
            >
              <OrgEhgInputGlassField
                intensity={intensity}
                kind={dropdown ? "select" : kind}
                size={size}
                state={state}
                label={label}
                labelPlace={labelPlace}
                iconPlace={iconPlace}
                icon={Icon ?? undefined}
                clearable={clearable}
                dropdown={dropdown || kind === "select"}
                value={value}
                onChange={setValue}
                placeholder={dropdown || kind === "select" ? "Select option…" : "Enter text…"}
                hint={
                  state === "error"
                    ? "Please correct this field"
                    : state === "success"
                      ? "Looks good"
                      : undefined
                }
                testId="ui-standard-org-ehg-input-pg-preview-field"
                menuTheme={theme}
                focusAnimation={focusAnimation}
              />
            </div>
          </div>

          <UiStandardsCopyCommand
            standard="Org Input Glass"
            standardStatus="pilot"
            command={inputGlassCommand(
              intensity,
              kind,
              size,
              state,
              iconPlace,
              dropdown,
              clearable,
              focusAnimation,
            )}
            testId="ui-standard-org-ehg-input-pg-copy"
          />
        </PlaygroundSection>
      </div>
    </section>
  );
}

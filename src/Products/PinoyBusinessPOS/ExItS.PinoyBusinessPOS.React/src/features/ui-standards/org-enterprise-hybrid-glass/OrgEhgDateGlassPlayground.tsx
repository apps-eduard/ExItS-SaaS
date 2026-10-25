import { useState } from "react";
import { Moon, RotateCcw, Sun } from "lucide-react";
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
  ORG_EHG_DATE_GLASS_COMMAND,
  ORG_EHG_DATE_GLASS_DEFAULT_FOCUS,
  ORG_EHG_DATE_GLASS_DEFAULT_INTENSITY,
  ORG_EHG_DATE_GLASS_DEFAULT_KIND,
  ORG_EHG_DATE_GLASS_DEFAULT_SIZE,
  ORG_EHG_DATE_GLASS_DEFAULT_SURFACE,
  ORG_EHG_DATE_GLASS_DEFAULT_VARIETY,
} from "@/features/ui-standards/org-enterprise-hybrid-glass/org-ehg-date-glass-global-default";
import {
  OrgEhgDateGlassField,
  type OrgEhgDateGlassFocusAnimation,
  type OrgEhgDateGlassIntensity,
  type OrgEhgDateGlassKind,
  type OrgEhgDateGlassSize,
  type OrgEhgDateGlassState,
  type OrgEhgDateGlassSurface,
  type OrgEhgDateGlassVariety,
} from "@/features/ui-standards/org-enterprise-hybrid-glass/OrgEhgDateGlassField";
import {
  ORG_EHG_PLAYGROUND_BG_DEFAULT,
  ORG_EHG_PLAYGROUND_BG_DEFAULT_INTENSITY,
  ORG_EHG_PLAYGROUND_BG_OPTIONS,
} from "@/features/ui-standards/org-enterprise-hybrid-glass/orgEhgPlaygroundBackground";
import { useUiStandardsShellAppearance } from "@/features/ui-standards/UiStandardsShellAppearance";

const INTENSITIES: ReadonlyArray<{ label: string; value: OrgEhgDateGlassIntensity }> = [
  { label: "Solid", value: "solid" },
  { label: "Soft Glass", value: "soft" },
  { label: "Medium Glass", value: "medium" },
  { label: "Strong Glass", value: "strong" },
  { label: "Ultra Glass", value: "ultra" },
];

const KINDS: ReadonlyArray<{ label: string; value: OrgEhgDateGlassKind }> = [
  { label: "Date", value: "date" },
  { label: "Date and time glass", value: "datetime" },
  { label: "Time only", value: "time" },
  { label: "Date range", value: "range" },
  { label: "Month", value: "month" },
];

const SIZES: ReadonlyArray<{ label: string; value: OrgEhgDateGlassSize }> = [
  { label: "Compact", value: "sm" },
  { label: "Standard", value: "md" },
  { label: "Large", value: "lg" },
];

const STATES: ReadonlyArray<{ label: string; value: OrgEhgDateGlassState }> = [
  { label: "Default", value: "default" },
  { label: "Hover", value: "hover" },
  { label: "Focused", value: "focused" },
  { label: "Error", value: "error" },
  { label: "Success", value: "success" },
  { label: "Disabled", value: "disabled" },
];

const FOCUS_ANIMATIONS: ReadonlyArray<{ label: string; value: OrgEhgDateGlassFocusAnimation }> = [
  { label: "Thick below", value: "thick-below" },
  { label: "Glow", value: "glow" },
  { label: "None", value: "none" },
];

const VARIETIES: ReadonlyArray<{ label: string; value: OrgEhgDateGlassVariety }> = [
  { label: "Quasar (default)", value: "quasar" },
  { label: "Android", value: "android" },
  { label: "Sidebar", value: "sidebar" },
];

const SURFACES: ReadonlyArray<{ label: string; value: OrgEhgDateGlassSurface }> = [
  { label: "Popover", value: "popover" },
  { label: "Dialog", value: "dialog" },
];

const DEFAULTS = {
  intensity: ORG_EHG_DATE_GLASS_DEFAULT_INTENSITY,
  kind: ORG_EHG_DATE_GLASS_DEFAULT_KIND,
  variety: ORG_EHG_DATE_GLASS_DEFAULT_VARIETY,
  surface: ORG_EHG_DATE_GLASS_DEFAULT_SURFACE,
  size: ORG_EHG_DATE_GLASS_DEFAULT_SIZE,
  state: "default" as OrgEhgDateGlassState,
  focusAnimation: ORG_EHG_DATE_GLASS_DEFAULT_FOCUS,
  label: "Date and time",
  value: "02/20/2025  10:30 AM",
  showPicker: true,
};

function dateGlassCommand(
  intensity: OrgEhgDateGlassIntensity,
  kind: OrgEhgDateGlassKind,
  size: OrgEhgDateGlassSize,
  state: OrgEhgDateGlassState,
  focusAnimation: OrgEhgDateGlassFocusAnimation,
  variety: OrgEhgDateGlassVariety,
  surface: OrgEhgDateGlassSurface,
): string {
  const parts = ["ext", "org-ehg", "date-glass", variety, intensity, kind, size, surface];
  if (state !== "default") parts.push(state);
  if (focusAnimation !== "none") parts.push(`focus-${focusAnimation}`);
  return parts.join(" ");
}

/** Locked playground reset command (must match ORG_EHG_DATE_GLASS_COMMAND). */
export const ORG_EHG_DATE_GLASS_PLAYGROUND_DEFAULT_COMMAND = ORG_EHG_DATE_GLASS_COMMAND;

/**
 * Interactive Org Enterprise Hybrid Glass date playground (UI Standards / DEV only).
 */
export function OrgEhgDateGlassPlayground() {
  const { background, setBackground, theme, toggleTheme } = useUiStandardsShellAppearance();
  const [intensity, setIntensity] = useState(DEFAULTS.intensity);
  const [kind, setKind] = useState(DEFAULTS.kind);
  const [variety, setVariety] = useState(DEFAULTS.variety);
  const [size, setSize] = useState(DEFAULTS.size);
  const [state, setState] = useState(DEFAULTS.state);
  const [focusAnimation, setFocusAnimation] = useState(DEFAULTS.focusAnimation);
  const [label, setLabel] = useState(DEFAULTS.label);
  const [value, setValue] = useState(DEFAULTS.value);
  const [surface, setSurface] = useState(DEFAULTS.surface);
  const [showPicker, setShowPicker] = useState(DEFAULTS.showPicker);
  const [open, setOpen] = useState(false);

  function onKindChange(next: OrgEhgDateGlassKind) {
    setKind(next);
    setOpen(false);
    if (next === "datetime") {
      setValue("02/20/2025  10:30 AM");
      setLabel("Date and time");
      setIntensity(ORG_EHG_DATE_GLASS_DEFAULT_INTENSITY);
      setVariety(ORG_EHG_DATE_GLASS_DEFAULT_VARIETY);
    } else if (next === "time") {
      setValue("9:00 PM");
      setLabel("Time");
    } else if (next === "range") {
      setValue("Feb 20, 2025 → Feb 27, 2025");
      setLabel("Date range");
    } else if (next === "month") {
      setValue("Feb 2025");
      setLabel("Month");
    } else {
      setValue("02/20/2025");
      setLabel("Date");
    }
  }

  function resetPreview() {
    setIntensity(DEFAULTS.intensity);
    setKind(DEFAULTS.kind);
    setVariety(DEFAULTS.variety);
    setSize(DEFAULTS.size);
    setState(DEFAULTS.state);
    setFocusAnimation(DEFAULTS.focusAnimation);
    setLabel(DEFAULTS.label);
    setValue(DEFAULTS.value);
    setSurface(DEFAULTS.surface);
    setShowPicker(DEFAULTS.showPicker);
    setOpen(false);
    setBackground(ORG_EHG_PLAYGROUND_BG_DEFAULT);
  }

  return (
    <section
      className="org-ehg org-ehg-pg-bg org-ehg-glass-playground-card org-ehg-date-glass-playground flex min-w-0 flex-col"
      data-testid="ui-standard-card-org-ehg-date-glass-playground"
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
          <CardTitle>Date glass playground</CardTitle>
          <div className="flex items-center gap-2">
            <Button
              type="button"
              intent={theme === "dark" ? "primary" : "neutral"}
              appearance={theme === "dark" ? "solid" : "outline"}
              shape="pill"
              size="icon"
              aria-label={theme === "dark" ? "Light mode" : "Dark mode"}
              aria-pressed={theme === "dark"}
              data-testid="ui-standard-org-ehg-date-pg-theme"
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
              data-testid="ui-standard-org-ehg-date-pg-reset"
              onClick={resetPreview}
            >
              <RotateCcw className="size-4" aria-hidden />
            </Button>
          </div>
        </div>

        <PlaygroundSection testId="ui-standard-org-ehg-date-glass-playground" className="">
          <div className="flex min-w-0 flex-col gap-3" data-testid="ui-standard-org-ehg-date-pg-preview-card">
            <PlaygroundLabel>Preview</PlaygroundLabel>
            <div
              className="org-ehg-date-glass-panel org-ehg-date-glass-panel--playground"
              data-testid="ui-standard-org-ehg-date-pg-preview"
              data-org-ehg-theme={theme}
            >
              <OrgEhgDateGlassField
                intensity={intensity}
                kind={kind}
                variety={variety}
                size={size}
                state={state}
                focusAnimation={focusAnimation}
                label={label}
                value={value}
                surface={surface}
                open={showPicker ? open : false}
                onOpenChange={setOpen}
                onChange={setValue}
                showPopover={
                  showPicker
                    ? kind === "datetime"
                      ? "datetime"
                      : kind === "time"
                        ? "time"
                        : kind === "range"
                          ? "range"
                          : kind === "month"
                            ? "none"
                            : "date"
                    : "none"
                }
                hint={
                  state === "error"
                    ? "Choose a valid date"
                    : state === "success"
                      ? "Date confirmed"
                      : undefined
                }
                testId="ui-standard-org-ehg-date-pg-preview-field"
              />
            </div>
          </div>

          <div
            className="org-ehg-date-glass-playground__controls grid min-w-0 gap-2 sm:grid-cols-2 lg:grid-cols-3"
            aria-hidden={open ? true : undefined}
          >
            <CompactSelect
              label="Variety"
              value={variety}
              options={[...VARIETIES]}
              onChange={setVariety}
              testId="ui-standard-org-ehg-date-pg-variety"
            />
            <CompactSelect
              label="Kind"
              value={kind}
              options={[...KINDS]}
              onChange={onKindChange}
              testId="ui-standard-org-ehg-date-pg-kind"
            />
            <CompactSelect
              label="Intensity"
              value={intensity}
              options={[...INTENSITIES]}
              onChange={setIntensity}
              testId="ui-standard-org-ehg-date-pg-intensity"
            />
            <CompactSelect
              label="Size"
              value={size}
              options={[...SIZES]}
              onChange={setSize}
              testId="ui-standard-org-ehg-date-pg-size"
            />
            <CompactSelect
              label="State"
              value={state}
              options={[...STATES]}
              onChange={setState}
              testId="ui-standard-org-ehg-date-pg-state"
            />
            <CompactSelect
              label="Focus animation"
              value={focusAnimation}
              options={[...FOCUS_ANIMATIONS]}
              onChange={setFocusAnimation}
              testId="ui-standard-org-ehg-date-pg-focus-animation"
            />
            <CompactSelect
              label="Background"
              value={background}
              options={[...ORG_EHG_PLAYGROUND_BG_OPTIONS]}
              onChange={setBackground}
              testId="ui-standard-org-ehg-date-pg-background"
            />
            <PlaygroundTextField
              label="Label"
              value={label}
              onChange={setLabel}
              testId="ui-standard-org-ehg-date-pg-label"
            />
            <PlaygroundTextField
              label="Value"
              value={value}
              onChange={setValue}
              testId="ui-standard-org-ehg-date-pg-value"
            />
          </div>

          <div className="col-span-full grid min-w-0 gap-2 sm:grid-cols-2 lg:grid-cols-3">
            <CompactSelect
              label="Show as"
              value={surface}
              options={[...SURFACES]}
              onChange={(next) => {
                setSurface(next);
                setOpen(false);
              }}
              testId="ui-standard-org-ehg-date-pg-surface"
            />
            <label className="inline-flex items-center gap-2 self-end pb-1 text-[length:var(--exits-text-sm)]">
              <Switch
                checked={showPicker}
                onCheckedChange={(next) => {
                  setShowPicker(next);
                  if (!next) setOpen(false);
                }}
                aria-label="Enable picker"
                data-testid="ui-standard-org-ehg-date-pg-popover"
              />
              Enable picker
            </label>
          </div>

          <UiStandardsCopyCommand
            standard="Org Date Glass"
            standardStatus="pilot"
            command={dateGlassCommand(intensity, kind, size, state, focusAnimation, variety, surface)}
            testId="ui-standard-org-ehg-date-pg-copy"
          />
        </PlaygroundSection>
      </div>
    </section>
  );
}

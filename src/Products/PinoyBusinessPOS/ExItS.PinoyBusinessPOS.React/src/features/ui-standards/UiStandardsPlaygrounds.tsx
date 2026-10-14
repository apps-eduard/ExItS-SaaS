import { useMemo, useState, type CSSProperties, type ReactNode } from "react";
import { ExitsSelect } from "@/components/exits/ExitsSelect";
import { StatusChip } from "@/components/exits/StatusChip";
import type { StatusChipAppearance, StatusChipShape, StatusChipTone } from "@/components/exits/StatusChip";
import {
  Bell,
  Bookmark,
  Check,
  Heart,
  Info,
  Moon,
  Plus,
  Star,
  Sun,
  Trash2,
  RotateCcw,
  TriangleAlert,
  type LucideIcon,
} from "lucide-react";
import { Button } from "@/components/ui/button";
import { usePreferences } from "@/hooks/usePreferences";
import { isDocumentThemeDark } from "@/lib/primary-palette-accents";
import { Input } from "@/components/ui/input";
import { Switch } from "@/components/ui/switch";
import { formatJsxProps } from "@/features/ui-standards/copyUiSnippet";
import {
  PlaygroundLabel,
  PlaygroundSection,
  UiStandardsSnippetBlock,
} from "@/features/ui-standards/UiStandardsSnippetBlock";
import { useQbtnRipple } from "@/features/ui-standards/UiStandardsQbtnRipple";
import { UiStandardsCopyCommand } from "@/features/ui-standards/UiStandardsCopyCommand";
import {
  INPUT_SAMPLES,
  UiStandardsQinput,
  inputAccent,
  type InputAppearance,
  type InputShape,
} from "@/features/ui-standards/UiStandardsInputGallery";
import { radioAccent, UiStandardsQradio } from "@/features/ui-standards/UiStandardsRadioGallery";
import { UiStandardsQcheckbox } from "@/features/ui-standards/UiStandardsCheckboxGallery";
import { toggleAccent, UiStandardsPillToggle, UiStandardsQtoggle } from "@/features/ui-standards/UiStandardsToggleGallery";
import {
  DROPDOWN_OPTIONS,
  dropdownAccent,
  UiStandardsQdropdown,
  type DropdownAppearance,
  type DropdownTransition,
} from "@/features/ui-standards/UiStandardsDropdownGallery";
import { UiStandardsQslider, sliderAccent } from "@/features/ui-standards/UiStandardsSliderGallery";
import { UiStandardsQrange, type SliderRangeValue } from "@/features/ui-standards/UiStandardsRangeGallery";
import { DATE_PICKER_SAMPLE, DATE_RANGE_SAMPLE, UiStandardsQdate, type DateAppearance, type DatePickerValue, type DateVariety } from "@/features/ui-standards/UiStandardsDateGallery";

const PLAYGROUND_INTENTS = [
  { id: "standard", label: "Standard", bg: "#ffffff", fg: "#000000", border: true },
  { id: "primary", label: "Primary", bg: "#1976d2", fg: "#ffffff" },
  { id: "secondary", label: "Secondary", bg: "#26a69a", fg: "#ffffff" },
  { id: "success", label: "Success", bg: "#22c55e", fg: "#ffffff" },
  { id: "info", label: "Info", bg: "#0ea5e9", fg: "#ffffff" },
  { id: "amber", label: "Amber", bg: "#ffc107", fg: "#000000" },
  { id: "danger", label: "Danger", bg: "#ef4444", fg: "#ffffff" },
  { id: "warn", label: "Warn", bg: "#ff5722", fg: "#ffffff" },
  { id: "help", label: "Help", bg: "#a855f7", fg: "#ffffff" },
  { id: "black", label: "Black", bg: "#000000", fg: "#ffffff" },
] as const;

const PLAYGROUND_APPEARANCES = [
  { id: "standard", label: "Standard" },
  { id: "soft", label: "Soft" },
  { id: "soft-outline", label: "Soft outline" },
  { id: "outline", label: "Outline" },
  { id: "raised", label: "Raised" },
  { id: "ghost", label: "Ghost" },
  { id: "glossy", label: "Glossy" },
  { id: "gradient", label: "Gradient" },
  { id: "rounded", label: "Rounded" },
  { id: "flat", label: "Flat" },
] as const;

const BUTTON_SHAPES = [
  { label: "Square", value: "square" },
  { label: "Standard", value: "standard" },
  { label: "Soft", value: "soft" },
  { label: "Pill", value: "pill" },
] as const;

const BUTTON_DENSITIES = [
  { label: "Tight", value: "tight" },
  { label: "Compact", value: "compact" },
  { label: "Standard", value: "balance" },
  { label: "Comfort", value: "comfort" },
  { label: "Relaxed", value: "relaxed" },
] as const;

const BUTTON_PLAYGROUND_DEFAULTS = {
  intent: "standard" as (typeof PLAYGROUND_INTENTS)[number]["id"],
  appearance: "standard" as (typeof PLAYGROUND_APPEARANCES)[number]["id"],
  shape: "standard" as (typeof BUTTON_SHAPES)[number]["value"],
  density: "balance" as (typeof BUTTON_DENSITIES)[number]["value"],
  iconId: "none",
  iconOnly: false,
  ripple: false,
  disabled: false,
  loading: false,
  label: "Standard",
};

const BUTTON_ICONS: ReadonlyArray<{ id: string; label: string; Icon: LucideIcon | null }> = [
  { id: "none", label: "None", Icon: null },
  { id: "check", label: "Check", Icon: Check },
  { id: "plus", label: "Plus", Icon: Plus },
  { id: "star", label: "Star", Icon: Star },
  { id: "bookmark", label: "Bookmark", Icon: Bookmark },
  { id: "bell", label: "Bell", Icon: Bell },
  { id: "info", label: "Info", Icon: Info },
  { id: "heart", label: "Heart", Icon: Heart },
  { id: "trash", label: "Trash", Icon: Trash2 },
  { id: "alert", label: "Alert", Icon: TriangleAlert },
  { id: "moon", label: "Moon", Icon: Moon },
];

const STATUS_TONES: ReadonlyArray<{ label: string; value: StatusChipTone }> = [
  { label: "Primary", value: "primary" },
  { label: "Secondary", value: "secondary" },
  { label: "Success", value: "success" },
  { label: "Info", value: "info" },
  { label: "Warn", value: "warning" },
  { label: "Help", value: "help" },
  { label: "Danger", value: "danger" },
  { label: "Contrast", value: "contrast" },
  { label: "Neutral", value: "neutral" },
];

const STATUS_APPEARANCES: ReadonlyArray<{ label: string; value: StatusChipAppearance }> = [
  { label: "Soft", value: "soft" },
  { label: "Emphasis", value: "emphasis" },
  { label: "Outline", value: "outline" },
  { label: "Solid", value: "solid" },
];

const STATUS_SHAPES: ReadonlyArray<{ label: string; value: StatusChipShape }> = [
  { label: "Auto", value: "auto" },
  { label: "Standard", value: "standard" },
  { label: "Soft (radius)", value: "soft" },
  { label: "Pill", value: "pill" },
  { label: "Square", value: "square" },
];

const INPUT_APPEARANCES = [
  { id: "standard", label: "Standard" },
  { id: "soft", label: "Soft" },
  { id: "soft-outline", label: "Soft outline" },
  { id: "filled", label: "Filled" },
  { id: "outlined", label: "Outlined" },
  { id: "standout", label: "Standout" },
] as const;

const INPUT_SHAPES = [
  { label: "Square", value: "square" },
  { label: "Standard", value: "standard" },
  { label: "Rounded", value: "rounded" },
] as const;

const INPUT_PLAYGROUND_DEFAULTS = {
  intent: "standard" as (typeof INPUT_SAMPLES)[number]["id"],
  appearance: "standard" as InputAppearance,
  shape: "standard" as InputShape,
  density: "balance" as (typeof BUTTON_DENSITIES)[number]["value"],
  iconId: "none",
  disabled: false,
  clearable: false,
  required: false,
  error: false,
  label: "Standard",
};

function appearanceOptions<T extends string>(
  appearances: ReadonlyArray<{ id: T; label: string }>,
  shape: string,
) {
  return appearances.map((item) => ({
    label: item.label,
    value: item.id,
    disabled: shape === "rounded" && item.id === "standard",
  }));
}

function inputCursorCommand(
  intent: string,
  appearance: string,
  shape: string,
  density: string,
  iconId: string,
  clearable: boolean,
  required: boolean,
  error: boolean,
  disabled: boolean,
): string {
  const parts = ["ext input"];
  if (intent !== "standard") parts.push(intent);
  if (appearance !== "standard") parts.push(appearance);
  if (shape !== "standard") parts.push(shape);
  if (density !== "balance") parts.push(density);
  if (iconId !== "none") parts.push(iconId);
  if (clearable) parts.push("clearable");
  if (required) parts.push("required");
  if (error) parts.push("error");
  if (disabled) parts.push("disabled");
  return parts.join(" ");
}

function buttonCursorCommand(
  intent: string,
  appearance: string,
  shape: string,
  density: string,
  iconId: string,
  iconOnly: boolean,
  ripple: boolean,
  disabled: boolean,
): string {
  const parts = ["ext btn"];
  if (iconOnly) parts.push("icon");
  if (intent !== "standard") parts.push(intent);
  if (appearance !== "standard") parts.push(appearance);
  if (shape !== "standard") parts.push(shape);
  if (density !== "balance") parts.push(density);
  if (iconId !== "none") parts.push(iconId);
  if (ripple) parts.push("ripple");
  if (disabled) parts.push("disabled");
  return parts.join(" ");
}

function CompactSelect<T extends string>({
  label,
  value,
  options,
  onChange,
  testId,
}: {
  label: string;
  value: T;
  options: ReadonlyArray<{ label: string; value: T; disabled?: boolean }>;
  onChange: (value: T) => void;
  testId: string;
}) {
  return (
    <label className="flex min-w-0 flex-col gap-1 text-[length:var(--exits-text-xs)]">
      <span className="text-muted">{label}</span>
      <select
        className="exits-input h-8 min-w-0 rounded-[var(--exits-field-radius)] px-2 text-[length:var(--exits-text-sm)]"
        value={value}
        onChange={(e) => onChange(e.target.value as T)}
        data-testid={testId}
        aria-label={label}
      >
        {options.map((opt) => (
          <option key={opt.value} value={opt.value} disabled={opt.disabled}>
            {opt.label}
          </option>
        ))}
      </select>
    </label>
  );
}

function PlaygroundPreviewCard({
  previewTestId,
  themeTestId,
  resetTestId,
  onReset,
  previewClassName,
  previewProps,
  children,
}: {
  previewTestId: string;
  themeTestId: string;
  resetTestId: string;
  onReset: () => void;
  previewClassName: string;
  previewProps?: Record<string, string>;
  children: ReactNode;
}) {
  const { preferences, setTheme } = usePreferences();
  const dark =
    preferences.theme === "dark" || (preferences.theme !== "light" && isDocumentThemeDark());
  return (
    <div className="flex min-w-0 flex-col gap-3 rounded-[var(--exits-radius-lg)] border border-border bg-surface p-4 shadow-[var(--exits-shadow-sm)]">
      <div className="flex items-center justify-between gap-3">
        <PlaygroundLabel>Preview</PlaygroundLabel>
        <div className="flex items-center gap-2">
          <Button
            type="button"
            intent={dark ? "primary" : "neutral"}
            appearance={dark ? "solid" : "outline"}
            shape="pill"
            size="icon"
            aria-label={dark ? "Light mode" : "Dark mode"}
            aria-pressed={dark}
            data-testid={themeTestId}
            onClick={() => setTheme(dark ? "light" : "dark")}
          >
            {dark ? <Moon className="size-4" aria-hidden /> : <Sun className="size-4" aria-hidden />}
          </Button>
          <Button
            type="button"
            intent="neutral"
            appearance="outline"
            shape="pill"
            size="icon"
            aria-label="Reset"
            data-testid={resetTestId}
            onClick={onReset}
          >
            <RotateCcw className="size-4" aria-hidden />
          </Button>
        </div>
      </div>
      <div
        className={previewClassName}
        data-testid={previewTestId}
        data-preview-theme={dark ? "dark" : "light"}
        {...previewProps}
      >
        {children}
      </div>
    </div>
  );
}

export function UiStandardsButtonPlayground() {
  const [intent, setIntent] = useState(BUTTON_PLAYGROUND_DEFAULTS.intent);
  const [appearance, setAppearance] = useState(BUTTON_PLAYGROUND_DEFAULTS.appearance);
  const [shape, setShape] = useState(BUTTON_PLAYGROUND_DEFAULTS.shape);
  const [density, setDensity] = useState(BUTTON_PLAYGROUND_DEFAULTS.density);
  const [iconId, setIconId] = useState(BUTTON_PLAYGROUND_DEFAULTS.iconId);
  const [iconOnly, setIconOnly] = useState(BUTTON_PLAYGROUND_DEFAULTS.iconOnly);
  const [ripple, setRipple] = useState(BUTTON_PLAYGROUND_DEFAULTS.ripple);
  const [disabled, setDisabled] = useState(BUTTON_PLAYGROUND_DEFAULTS.disabled);
  const [loading, setLoading] = useState(BUTTON_PLAYGROUND_DEFAULTS.loading);
  const [label, setLabel] = useState(BUTTON_PLAYGROUND_DEFAULTS.label);
  const chosenIcon = BUTTON_ICONS.find((icon) => icon.id === iconId)?.Icon ?? null;
  const PreviewIcon = iconId === "none" ? (iconOnly ? Heart : null) : chosenIcon;
  const sample = PLAYGROUND_INTENTS.find((item) => item.id === intent) ?? PLAYGROUND_INTENTS[1];
  const accent = sample.bg.toLowerCase() === "#ffffff" ? "#000000" : sample.bg;
  const transparent = appearance === "outline" || appearance === "ghost";
  const soft = appearance === "soft";
  const softOutline = appearance === "soft-outline";
  const darkInk = (sample.id === "standard" || sample.id === "black") && (transparent || soft || softOutline);
  const previewStyle: CSSProperties & { "--qbtn-bg"?: string } = transparent
    ? {
        color: accent,
        borderColor: appearance === "outline" ? accent : "transparent",
        backgroundColor: "transparent",
      }
    : soft || softOutline
      ? { color: accent, "--qbtn-bg": accent, ...(softOutline ? { borderColor: accent } : {}) }
      : { backgroundColor: sample.bg, color: sample.fg };
  if (appearance === "gradient" && !transparent) previewStyle["--qbtn-bg"] = sample.bg;
  const previewClass = [
    "ui-standards-qbtn",
    "ui-standards-qbtn--playground",
    appearance === "soft" ? "ui-standards-qbtn--soft" : "",
    appearance === "soft-outline" ? "ui-standards-qbtn--soft-outline" : "",
    appearance === "outline" ? "ui-standards-qbtn--outline" : "",
    appearance === "ghost" ? "ui-standards-qbtn--ghost" : "",
    appearance === "raised" ? "ui-standards-qbtn--raised" : "",
    appearance === "glossy" ? "ui-standards-qbtn--glossy" : "",
    appearance === "gradient" ? "ui-standards-qbtn--gradient" : "",
    appearance === "flat" ? "ui-standards-qbtn--flat" : "",
    darkInk ? "ui-standards-qbtn--dark-ink" : "",
    iconOnly ? "ui-standards-qbtn--icon-only" : "",
    ripple ? "ui-standards-qbtn--ripple" : "",
    !transparent && !soft && !softOutline && "border" in sample && sample.border ? "ui-standards-qbtn--border" : "",
  ]
    .filter(Boolean)
    .join(" ");

  const { showRipple, layer: rippleLayer } = useQbtnRipple(
    ripple && !disabled && !loading,
    "ui-standard-btn-pg-ripple-wave",
  );

  function resetPreview() {
    setIntent(BUTTON_PLAYGROUND_DEFAULTS.intent);
    setAppearance(BUTTON_PLAYGROUND_DEFAULTS.appearance);
    setShape(BUTTON_PLAYGROUND_DEFAULTS.shape);
    setDensity(BUTTON_PLAYGROUND_DEFAULTS.density);
    setIconId(BUTTON_PLAYGROUND_DEFAULTS.iconId);
    setIconOnly(BUTTON_PLAYGROUND_DEFAULTS.iconOnly);
    setRipple(BUTTON_PLAYGROUND_DEFAULTS.ripple);
    setDisabled(BUTTON_PLAYGROUND_DEFAULTS.disabled);
    setLoading(BUTTON_PLAYGROUND_DEFAULTS.loading);
    setLabel(BUTTON_PLAYGROUND_DEFAULTS.label);
  }

  return (
    <PlaygroundSection testId="ui-standard-button-playground" className="">
      <div className="grid min-w-0 gap-2 sm:grid-cols-2 lg:grid-cols-3">
        <CompactSelect
          label="Intent"
          value={intent}
          options={PLAYGROUND_INTENTS.map((item) => ({ label: item.label, value: item.id }))}
          onChange={setIntent}
          testId="ui-standard-btn-pg-intent"
        />
        <CompactSelect
          label="Appearance"
          value={appearance}
          options={PLAYGROUND_APPEARANCES.map((item) => ({ label: item.label, value: item.id }))}
          onChange={setAppearance}
          testId="ui-standard-btn-pg-appearance"
        />
        <CompactSelect
          label="Shape"
          value={shape}
          options={[...BUTTON_SHAPES]}
          onChange={setShape}
          testId="ui-standard-btn-pg-shape"
        />
        <CompactSelect
          label="Icon"
          value={iconId}
          options={BUTTON_ICONS.map((icon) => ({ label: icon.label, value: icon.id }))}
          onChange={setIconId}
          testId="ui-standard-btn-pg-icon"
        />
        <CompactSelect
          label="Density"
          value={density}
          options={[...BUTTON_DENSITIES]}
          onChange={setDensity}
          testId="ui-standard-btn-pg-density"
        />
        <label className="flex min-w-0 flex-col gap-1 text-[length:var(--exits-text-xs)]">
          <span className="text-muted">Label</span>
          <Input
            name="ui-std-btn-label"
            value={label}
            onChange={(e) => setLabel(e.target.value)}
            data-testid="ui-standard-btn-pg-label"
            aria-label="Button label"
          />
        </label>
      </div>
      <div className="col-span-full flex flex-row flex-nowrap items-center gap-6">
          <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
            <Switch
              checked={disabled}
              onCheckedChange={setDisabled}
              aria-label="Disabled"
              data-testid="ui-standard-btn-pg-disabled"
            />
            Disabled
          </label>
          <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
            <Switch
              checked={loading}
              onCheckedChange={setLoading}
              aria-label="Loading"
              data-testid="ui-standard-btn-pg-loading"
            />
            Loading
          </label>
          <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
            <Switch
              checked={iconOnly}
              onCheckedChange={setIconOnly}
              aria-label="Icon only"
              data-testid="ui-standard-btn-pg-icon-only"
            />
            Icon only
          </label>
          <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
            <Switch
              checked={ripple}
              onCheckedChange={setRipple}
              aria-label="Ripple"
              data-testid="ui-standard-btn-pg-ripple"
            />
            {ripple ? "Ripple" : "No ripple"}
          </label>
        </div>
      <PlaygroundPreviewCard
        previewTestId="ui-standard-btn-pg-preview"
        themeTestId="ui-standard-btn-pg-theme"
        resetTestId="ui-standard-btn-pg-reset"
        onReset={resetPreview}
        previewClassName="ui-standards-btn-pg-preview flex min-h-16 items-center"
        previewProps={{ "data-density": density }}
      >
        <button
          type="button"
          className={previewClass}
          data-shape={shape}
          style={{ ...previewStyle, textTransform: "none" }}
          disabled={disabled || loading}
          aria-busy={loading || undefined}
          aria-label={iconOnly ? label.trim() || "Standard" : undefined}
          data-ripple={ripple ? "ripple" : "no-ripple"}
          onClick={showRipple}
        >
          <span className="ui-standards-qbtn-ripple-content">
            {PreviewIcon ? (
              <PreviewIcon className="size-4" aria-hidden data-testid="ui-standard-btn-pg-preview-icon" />
            ) : null}
            {iconOnly ? null : label.trim() || "Standard"}
          </span>
          {rippleLayer}
        </button>
      </PlaygroundPreviewCard>
      <UiStandardsCopyCommand
        standard="Button"
        command={buttonCursorCommand(intent, appearance, shape, density, iconId, iconOnly, ripple, disabled)}
      />
    </PlaygroundSection>
  );
}

export function UiStandardsInputPlayground() {
  const [intent, setIntent] = useState(INPUT_PLAYGROUND_DEFAULTS.intent);
  const [appearance, setAppearance] = useState(INPUT_PLAYGROUND_DEFAULTS.appearance);
  const [shape, setShape] = useState(INPUT_PLAYGROUND_DEFAULTS.shape);
  const [density, setDensity] = useState(INPUT_PLAYGROUND_DEFAULTS.density);
  const [iconId, setIconId] = useState(INPUT_PLAYGROUND_DEFAULTS.iconId);
  const [disabled, setDisabled] = useState(INPUT_PLAYGROUND_DEFAULTS.disabled);
  const [clearable, setClearable] = useState(INPUT_PLAYGROUND_DEFAULTS.clearable);
  const [required, setRequired] = useState(INPUT_PLAYGROUND_DEFAULTS.required);
  const [error, setError] = useState(INPUT_PLAYGROUND_DEFAULTS.error);
  const [label, setLabel] = useState(INPUT_PLAYGROUND_DEFAULTS.label);
  const [text, setText] = useState("");
  const sample = INPUT_SAMPLES.find((item) => item.id === intent) ?? INPUT_SAMPLES[0];
  const PreviewIcon = BUTTON_ICONS.find((icon) => icon.id === iconId)?.Icon ?? null;

  function resetPreview() {
    setIntent(INPUT_PLAYGROUND_DEFAULTS.intent);
    setAppearance(INPUT_PLAYGROUND_DEFAULTS.appearance);
    setShape(INPUT_PLAYGROUND_DEFAULTS.shape);
    setDensity(INPUT_PLAYGROUND_DEFAULTS.density);
    setIconId(INPUT_PLAYGROUND_DEFAULTS.iconId);
    setDisabled(INPUT_PLAYGROUND_DEFAULTS.disabled);
    setClearable(INPUT_PLAYGROUND_DEFAULTS.clearable);
    setRequired(INPUT_PLAYGROUND_DEFAULTS.required);
    setError(INPUT_PLAYGROUND_DEFAULTS.error);
    setLabel(INPUT_PLAYGROUND_DEFAULTS.label);
    setText("");
  }

  return (
    <PlaygroundSection testId="ui-standard-input-playground" className="">
      <div className="grid min-w-0 gap-2 sm:grid-cols-2 lg:grid-cols-3">
        <CompactSelect
          label="Intent"
          value={intent}
          options={INPUT_SAMPLES.map((item) => ({ label: item.label, value: item.id }))}
          onChange={setIntent}
          testId="ui-standard-input-pg-intent"
        />
        <CompactSelect
          label="Appearance"
          value={appearance}
          options={appearanceOptions(INPUT_APPEARANCES, shape)}
          onChange={(next) => {
            if (shape === "rounded" && next === "standard") return;
            setAppearance(next);
          }}
          testId="ui-standard-input-pg-appearance"
        />
        <CompactSelect
          label="Shape"
          value={shape}
          options={[...INPUT_SHAPES]}
          onChange={(next) => {
            setShape(next);
            if (next === "rounded" && appearance === "standard") setAppearance("outlined");
          }}
          testId="ui-standard-input-pg-shape"
        />
        <CompactSelect
          label="Icon"
          value={iconId}
          options={BUTTON_ICONS.map((icon) => ({ label: icon.label, value: icon.id }))}
          onChange={setIconId}
          testId="ui-standard-input-pg-icon"
        />
        <CompactSelect
          label="Density"
          value={density}
          options={[...BUTTON_DENSITIES]}
          onChange={setDensity}
          testId="ui-standard-input-pg-density"
        />
        <label className="flex min-w-0 flex-col gap-1 text-[length:var(--exits-text-xs)]">
          <span className="text-muted">Label</span>
          <Input
            name="ui-std-input-label"
            value={label}
            onChange={(e) => setLabel(e.target.value)}
            data-testid="ui-standard-input-pg-label"
            aria-label="Input label"
          />
        </label>
      </div>
      <div className="col-span-full flex flex-row flex-nowrap items-center gap-6">
        <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
          <Switch
            checked={disabled}
            onCheckedChange={setDisabled}
            aria-label="Disabled"
            data-testid="ui-standard-input-pg-disabled"
          />
          Disabled
        </label>
        <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
          <Switch
            checked={clearable}
            onCheckedChange={setClearable}
            aria-label="Clearable"
            data-testid="ui-standard-input-pg-clearable"
          />
          Clearable
        </label>
        <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
          <Switch
            checked={required}
            onCheckedChange={setRequired}
            aria-label="Required"
            data-testid="ui-standard-input-pg-required"
          />
          Required
        </label>
        <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
          <Switch
            checked={error}
            onCheckedChange={setError}
            aria-label="Error"
            data-testid="ui-standard-input-pg-error"
          />
          Error
        </label>
      </div>
      <PlaygroundPreviewCard
        previewTestId="ui-standard-input-pg-preview"
        themeTestId="ui-standard-input-pg-theme"
        resetTestId="ui-standard-input-pg-reset"
        onReset={resetPreview}
        previewClassName="ui-standards-input-pg-preview flex min-h-16 items-center"
        previewProps={{ "data-density": density }}
      >
        <UiStandardsQinput
          sampleId={sample.id}
          label={label.trim() || "Standard"}
          value={text}
          onChange={setText}
          accent={inputAccent(sample)}
          fill={sample.bg}
          ink={sample.fg}
          appearance={appearance}
          shape={shape}
          disabled={disabled}
          invalid={error}
          clearable={clearable}
          required={required}
          onClear={() => setText("")}
          icon={PreviewIcon}
          testId="ui-standard-input-pg-field"
        />
      </PlaygroundPreviewCard>
      <UiStandardsCopyCommand
        standard="Input"
        command={inputCursorCommand(
          intent,
          appearance,
          shape,
          density,
          iconId,
          clearable,
          required,
          error,
          disabled,
        )}
      />
    </PlaygroundSection>
  );
}

export function UiStandardsStatusPlayground() {
  const [tone, setTone] = useState<StatusChipTone>("success");
  const [appearance, setAppearance] = useState<StatusChipAppearance>("soft");
  const [shape, setShape] = useState<StatusChipShape>("auto");
  const [label, setLabel] = useState("Active");

  const snippet = useMemo(() => {
    const props = formatJsxProps({
      tone,
      appearance,
      shape,
    });
    return `<StatusChip\n${props}\n>\n  ${label.trim() || "Active"}\n</StatusChip>`;
  }, [tone, appearance, shape, label]);

  return (
    <PlaygroundSection testId="ui-standard-status-playground">
      <PlaygroundLabel>Playground</PlaygroundLabel>
      <div className="grid min-w-0 gap-2 sm:grid-cols-2 lg:grid-cols-4">
        <CompactSelect
          label="Tone"
          value={tone}
          options={STATUS_TONES}
          onChange={setTone}
          testId="ui-standard-status-pg-tone"
        />
        <CompactSelect
          label="Appearance"
          value={appearance}
          options={STATUS_APPEARANCES}
          onChange={setAppearance}
          testId="ui-standard-status-pg-appearance"
        />
        <CompactSelect
          label="Shape"
          value={shape}
          options={STATUS_SHAPES}
          onChange={setShape}
          testId="ui-standard-status-pg-shape"
        />
        <label className="flex min-w-0 flex-col gap-1 text-[length:var(--exits-text-xs)]">
          <span className="text-muted">Label</span>
          <input
            name="ui-std-status-label"
            className="exits-input h-8 min-w-0 rounded-[var(--exits-field-radius)] px-2 text-[length:var(--exits-text-sm)]"
            value={label}
            onChange={(e) => setLabel(e.target.value)}
            data-testid="ui-standard-status-pg-label"
            aria-label="Status chip label"
          />
        </label>
      </div>
      <PlaygroundLabel>Preview</PlaygroundLabel>
      <div data-testid="ui-standard-status-pg-preview">
        <StatusChip tone={tone} appearance={appearance} shape={shape}>
          {label.trim() || "Active"}
        </StatusChip>
      </div>
      <PlaygroundLabel>Code</PlaygroundLabel>
      <UiStandardsSnippetBlock snippet={snippet} testIdPrefix="ui-standard-status-pg" />
    </PlaygroundSection>
  );
}

/** ExitsSelect playground — real API only (no invented shape props). */
export function UiStandardsSelectPlayground() {
  const [value, setValue] = useState("Cash");
  const [disabled, setDisabled] = useState(false);
  const [searchable, setSearchable] = useState(false);

  const options = useMemo(
    () => [
      { value: "Cash", label: "Cash" },
      { value: "ManualGCash", label: "Manual GCash" },
      { value: "Check", label: "Check" },
    ],
    [],
  );

  const snippet = useMemo(() => {
    const props = formatJsxProps({
      value,
      disabled: disabled || undefined,
      searchable: searchable || undefined,
      menuLabel: "Payment method",
    });
    return `<ExitsSelect\n${props}\n  options={[/* … */]}\n  onChange={setValue}\n/>`;
  }, [value, disabled, searchable]);

  return (
    <PlaygroundSection testId="ui-standard-select-playground">
      <PlaygroundLabel>Playground — ExitsSelect</PlaygroundLabel>
      <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
        Canonical single-select API. Shape follows field radius (not Control Shape). No fabricated
        props.
      </p>
      <div className="grid min-w-0 gap-2 sm:grid-cols-2">
        <div className="flex flex-wrap items-center gap-4">
          <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
            <Switch
              checked={disabled}
              onCheckedChange={setDisabled}
              aria-label="Disabled"
              data-testid="ui-standard-select-pg-disabled"
            />
            Disabled
          </label>
          <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
            <Switch
              checked={searchable}
              onCheckedChange={setSearchable}
              aria-label="Searchable"
              data-testid="ui-standard-select-pg-searchable"
            />
            Searchable
          </label>
        </div>
      </div>
      <PlaygroundLabel>Preview</PlaygroundLabel>
      <div className="max-w-sm" data-testid="ui-standard-select-pg-preview">
        <ExitsSelect
          value={value}
          options={options}
          onChange={setValue}
          disabled={disabled}
          searchable={searchable}
          menuLabel="Payment method"
          testId="ui-standard-select-pg-control"
        />
      </div>
      <PlaygroundLabel>Code</PlaygroundLabel>
      <UiStandardsSnippetBlock snippet={snippet} testIdPrefix="ui-standard-select-pg" />
    </PlaygroundSection>
  );
}

const RADIO_TYPES = [
  { label: "Standard", value: "standard" },
  { label: "With icons", value: "icons" },
] as const;

const RADIO_PLAYGROUND_DEFAULTS = {
  intent: "standard" as (typeof INPUT_SAMPLES)[number]["id"],
  type: "standard" as (typeof RADIO_TYPES)[number]["value"],
  density: "balance" as (typeof BUTTON_DENSITIES)[number]["value"],
  dense: false,
  disabled: false,
  label: "Standard",
};

function radioCursorCommand(
  intent: string,
  type: string,
  density: string,
  dense: boolean,
  disabled: boolean,
): string {
  const parts = ["ext radio"];
  if (intent !== "standard") parts.push(intent);
  if (type === "icons") parts.push("icons");
  if (density !== "balance") parts.push(density);
  if (dense) parts.push("dense");
  if (disabled) parts.push("disabled");
  return parts.join(" ");
}

export function UiStandardsRadioPlayground() {
  const [intent, setIntent] = useState(RADIO_PLAYGROUND_DEFAULTS.intent);
  const [type, setType] = useState(RADIO_PLAYGROUND_DEFAULTS.type);
  const [density, setDensity] = useState(RADIO_PLAYGROUND_DEFAULTS.density);
  const [dense, setDense] = useState(RADIO_PLAYGROUND_DEFAULTS.dense);
  const [disabled, setDisabled] = useState(RADIO_PLAYGROUND_DEFAULTS.disabled);
  const [label, setLabel] = useState(RADIO_PLAYGROUND_DEFAULTS.label);
  const sample = INPUT_SAMPLES.find((item) => item.id === intent) ?? INPUT_SAMPLES[0];

  function resetPreview() {
    setIntent(RADIO_PLAYGROUND_DEFAULTS.intent);
    setType(RADIO_PLAYGROUND_DEFAULTS.type);
    setDensity(RADIO_PLAYGROUND_DEFAULTS.density);
    setDense(RADIO_PLAYGROUND_DEFAULTS.dense);
    setDisabled(RADIO_PLAYGROUND_DEFAULTS.disabled);
    setLabel(RADIO_PLAYGROUND_DEFAULTS.label);
  }

  return (
    <PlaygroundSection testId="ui-standard-radio-playground" className="">
      <div className="grid min-w-0 gap-2 sm:grid-cols-2 lg:grid-cols-3">
        <CompactSelect
          label="Intent"
          value={intent}
          options={INPUT_SAMPLES.map((item) => ({ label: item.label, value: item.id }))}
          onChange={setIntent}
          testId="ui-standard-radio-pg-intent"
        />
        <CompactSelect
          label="Type"
          value={type}
          options={[...RADIO_TYPES]}
          onChange={setType}
          testId="ui-standard-radio-pg-type"
        />
        <CompactSelect
          label="Density"
          value={density}
          options={[...BUTTON_DENSITIES]}
          onChange={setDensity}
          testId="ui-standard-radio-pg-density"
        />
        <label className="flex min-w-0 flex-col gap-1 text-[length:var(--exits-text-xs)]">
          <span className="text-muted">Label</span>
          <Input
            name="ui-std-radio-label"
            value={label}
            onChange={(event) => setLabel(event.target.value)}
            data-testid="ui-standard-radio-pg-label"
            aria-label="Radio label"
          />
        </label>
      </div>
      <div className="flex flex-wrap gap-4">
        <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
          <Switch
            checked={dense}
            onCheckedChange={setDense}
            aria-label="Dense"
            data-testid="ui-standard-radio-pg-dense"
          />
          Dense
        </label>
        <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
          <Switch
            checked={disabled}
            onCheckedChange={setDisabled}
            aria-label="Disabled"
            data-testid="ui-standard-radio-pg-disabled"
          />
          Disabled
        </label>
      </div>
      <PlaygroundLabel>Preview</PlaygroundLabel>
      <div className="flex flex-wrap items-center gap-3">
        <div className="ui-standards-radio-pg-preview" data-density={density} data-testid="ui-standard-radio-pg-preview">
          <UiStandardsQradio
            label={label.trim() || "Standard"}
            color={radioAccent(sample)}
            checked
            dense={dense}
            icons={type === "icons"}
            disabled={disabled}
            darkInk={sample.id === "black"}
            testId="ui-standard-radio-pg-field"
          />
        </div>
        <button
          type="button"
          className="inline-flex h-9 items-center rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 text-[length:var(--exits-text-sm)]"
          data-testid="ui-standard-radio-pg-reset"
          onClick={resetPreview}
        >
          Reset
        </button>
      </div>
      <UiStandardsCopyCommand
        standard="Radio"
        standardStatus="pilot"
        command={radioCursorCommand(intent, type, density, dense, disabled)}
      />
    </PlaygroundSection>
  );
}

const CHECKBOX_PLAYGROUND_DEFAULTS = {
  intent: "standard" as (typeof INPUT_SAMPLES)[number]["id"],
  density: "balance" as (typeof BUTTON_DENSITIES)[number]["value"],
  dense: false,
  disabled: false,
  label: "Standard",
};

function checkboxCursorCommand(intent: string, density: string, dense: boolean, disabled: boolean): string {
  const parts = ["ext checkbox"];
  if (intent !== "standard") parts.push(intent);
  if (density !== "balance") parts.push(density);
  if (dense) parts.push("dense");
  if (disabled) parts.push("disabled");
  return parts.join(" ");
}

export function UiStandardsCheckboxPlayground() {
  const [intent, setIntent] = useState(CHECKBOX_PLAYGROUND_DEFAULTS.intent);
  const [density, setDensity] = useState(CHECKBOX_PLAYGROUND_DEFAULTS.density);
  const [dense, setDense] = useState(CHECKBOX_PLAYGROUND_DEFAULTS.dense);
  const [disabled, setDisabled] = useState(CHECKBOX_PLAYGROUND_DEFAULTS.disabled);
  const [label, setLabel] = useState(CHECKBOX_PLAYGROUND_DEFAULTS.label);
  const sample = INPUT_SAMPLES.find((item) => item.id === intent) ?? INPUT_SAMPLES[0];

  function resetPreview() {
    setIntent(CHECKBOX_PLAYGROUND_DEFAULTS.intent);
    setDensity(CHECKBOX_PLAYGROUND_DEFAULTS.density);
    setDense(CHECKBOX_PLAYGROUND_DEFAULTS.dense);
    setDisabled(CHECKBOX_PLAYGROUND_DEFAULTS.disabled);
    setLabel(CHECKBOX_PLAYGROUND_DEFAULTS.label);
  }

  return (
    <PlaygroundSection testId="ui-standard-checkbox-playground" className="">
      <div className="grid min-w-0 gap-2 sm:grid-cols-2 lg:grid-cols-3">
        <CompactSelect
          label="Intent"
          value={intent}
          options={INPUT_SAMPLES.map((item) => ({ label: item.label, value: item.id }))}
          onChange={setIntent}
          testId="ui-standard-checkbox-pg-intent"
        />
        <CompactSelect
          label="Density"
          value={density}
          options={[...BUTTON_DENSITIES]}
          onChange={setDensity}
          testId="ui-standard-checkbox-pg-density"
        />
        <label className="flex min-w-0 flex-col gap-1 text-[length:var(--exits-text-xs)]">
          <span className="text-muted">Label</span>
          <Input
            name="ui-std-checkbox-label"
            value={label}
            onChange={(event) => setLabel(event.target.value)}
            data-testid="ui-standard-checkbox-pg-label"
            aria-label="Checkbox label"
          />
        </label>
      </div>
      <div className="flex flex-wrap gap-4">
        <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
          <Switch
            checked={dense}
            onCheckedChange={setDense}
            aria-label="Dense"
            data-testid="ui-standard-checkbox-pg-dense"
          />
          Dense
        </label>
        <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
          <Switch
            checked={disabled}
            onCheckedChange={setDisabled}
            aria-label="Disabled"
            data-testid="ui-standard-checkbox-pg-disabled"
          />
          Disabled
        </label>
      </div>
      <PlaygroundLabel>Preview</PlaygroundLabel>
      <div className="flex flex-wrap items-center gap-3">
        <div
          className="ui-standards-checkbox-pg-preview"
          data-density={density}
          data-testid="ui-standard-checkbox-pg-preview"
        >
          <UiStandardsQcheckbox
            label={label.trim() || "Standard"}
            color={radioAccent(sample)}
            checked
            dense={dense}
            disabled={disabled}
            darkInk={sample.id === "black"}
            testId="ui-standard-checkbox-pg-field"
          />
        </div>
        <button
          type="button"
          className="inline-flex h-9 items-center rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 text-[length:var(--exits-text-sm)]"
          data-testid="ui-standard-checkbox-pg-reset"
          onClick={resetPreview}
        >
          Reset
        </button>
      </div>
      <UiStandardsCopyCommand
        standard="Checkbox"
        standardStatus="pilot"
        command={checkboxCursorCommand(intent, density, dense, disabled)}
      />
    </PlaygroundSection>
  );
}

const TOGGLE_VARIETIES = [
  { label: "Standard", value: "standard" },
  { label: "Solid", value: "solid" },
  { label: "Outline", value: "outline" },
] as const;

const TOGGLE_APPEARANCES = [
  { id: "standard", label: "Standard" },
  { id: "soft", label: "Soft" },
  { id: "soft-outline", label: "Soft outline" },
  { id: "outline", label: "Outline" },
  { id: "raised", label: "Raised" },
  { id: "glossy", label: "Glossy" },
  { id: "gradient", label: "Gradient" },
  { id: "flat", label: "Flat" },
] as const;

const TOGGLE_PLAYGROUND_DEFAULTS = {
  intent: "standard" as (typeof INPUT_SAMPLES)[number]["id"],
  variety: "standard" as (typeof TOGGLE_VARIETIES)[number]["value"],
  appearance: "standard" as (typeof TOGGLE_APPEARANCES)[number]["id"],
  density: "balance" as (typeof BUTTON_DENSITIES)[number]["value"],
  iconId: "none",
  disabled: false,
  label: "Standard",
};

function toggleCursorCommand(
  intent: string,
  variety: string,
  appearance: string,
  density: string,
  iconId: string,
  disabled: boolean,
): string {
  const parts = ["ext toggle"];
  if (intent !== "standard") parts.push(intent);
  if (variety !== "standard") parts.push(variety);
  if (appearance !== "standard") parts.push(appearance);
  if (density !== "balance") parts.push(density);
  if (iconId !== "none") parts.push(iconId);
  if (disabled) parts.push("disabled");
  return parts.join(" ");
}

export function UiStandardsTogglePlayground() {
  const [intent, setIntent] = useState(TOGGLE_PLAYGROUND_DEFAULTS.intent);
  const [variety, setVariety] = useState(TOGGLE_PLAYGROUND_DEFAULTS.variety);
  const [appearance, setAppearance] = useState(TOGGLE_PLAYGROUND_DEFAULTS.appearance);
  const [density, setDensity] = useState(TOGGLE_PLAYGROUND_DEFAULTS.density);
  const [iconId, setIconId] = useState(TOGGLE_PLAYGROUND_DEFAULTS.iconId);
  const [disabled, setDisabled] = useState(TOGGLE_PLAYGROUND_DEFAULTS.disabled);
  const [checked, setChecked] = useState(true);
  const [label, setLabel] = useState(TOGGLE_PLAYGROUND_DEFAULTS.label);
  const sample = INPUT_SAMPLES.find((item) => item.id === intent) ?? INPUT_SAMPLES[0];
  const PreviewIcon = BUTTON_ICONS.find((icon) => icon.id === iconId)?.Icon ?? null;
  const glyph = PreviewIcon ? <PreviewIcon aria-hidden /> : undefined;
  const outlined = variety === "outline" || appearance === "outline";

  function resetPreview() {
    setIntent(TOGGLE_PLAYGROUND_DEFAULTS.intent);
    setVariety(TOGGLE_PLAYGROUND_DEFAULTS.variety);
    setAppearance(TOGGLE_PLAYGROUND_DEFAULTS.appearance);
    setDensity(TOGGLE_PLAYGROUND_DEFAULTS.density);
    setIconId(TOGGLE_PLAYGROUND_DEFAULTS.iconId);
    setDisabled(TOGGLE_PLAYGROUND_DEFAULTS.disabled);
    setChecked(true);
    setLabel(TOGGLE_PLAYGROUND_DEFAULTS.label);
  }

  return (
    <PlaygroundSection testId="ui-standard-toggle-playground" className="">
      <div className="grid min-w-0 gap-2 sm:grid-cols-2 lg:grid-cols-3">
        <CompactSelect
          label="Intent"
          value={intent}
          options={INPUT_SAMPLES.map((item) => ({ label: item.label, value: item.id }))}
          onChange={setIntent}
          testId="ui-standard-toggle-pg-intent"
        />
        <CompactSelect
          label="Variety"
          value={variety}
          options={[...TOGGLE_VARIETIES]}
          onChange={setVariety}
          testId="ui-standard-toggle-pg-variety"
        />
        <CompactSelect
          label="Appearance"
          value={appearance}
          options={TOGGLE_APPEARANCES.map((item) => ({ label: item.label, value: item.id }))}
          onChange={setAppearance}
          testId="ui-standard-toggle-pg-appearance"
        />
        <CompactSelect
          label="Icon"
          value={iconId}
          options={BUTTON_ICONS.map((icon) => ({ label: icon.label, value: icon.id }))}
          onChange={setIconId}
          testId="ui-standard-toggle-pg-icon"
        />
        <CompactSelect
          label="Density"
          value={density}
          options={[...BUTTON_DENSITIES]}
          onChange={setDensity}
          testId="ui-standard-toggle-pg-density"
        />
        <label className="flex min-w-0 flex-col gap-1 text-[length:var(--exits-text-xs)]">
          <span className="text-muted">Label</span>
          <Input
            name="ui-std-toggle-label"
            value={label}
            onChange={(event) => setLabel(event.target.value)}
            data-testid="ui-standard-toggle-pg-label"
            aria-label="Toggle label"
          />
        </label>
      </div>
      <div className="flex flex-wrap gap-4">
        <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
          <Switch
            checked={disabled}
            onCheckedChange={setDisabled}
            aria-label="Disabled"
            data-testid="ui-standard-toggle-pg-disabled"
          />
          Disabled
        </label>
      </div>
      <PlaygroundPreviewCard
        previewTestId="ui-standard-toggle-pg-preview"
        themeTestId="ui-standard-toggle-pg-theme"
        resetTestId="ui-standard-toggle-pg-reset"
        onReset={resetPreview}
        previewClassName="ui-standards-toggle-pg-preview flex min-h-16 items-center"
        previewProps={{ "data-density": density, "data-variety": variety, "data-appearance": appearance }}
      >
          {variety === "standard" ? (
            <UiStandardsQtoggle
              label={label.trim() || "Standard"}
              color={toggleAccent(sample)}
              checked={checked}
              disabled={disabled}
              standard={sample.id === "standard"}
              darkInk={sample.id === "black"}
              appearance={appearance}
              glyph={glyph}
              onSelect={() => setChecked((value) => !value)}
              testId="ui-standard-toggle-pg-field"
            />
          ) : (
            <UiStandardsPillToggle
              label={label.trim() || "Standard"}
              color={toggleAccent(sample)}
              ink={sample.fg}
              outline={outlined}
              standard={sample.id === "standard"}
              disabled={disabled}
              checked={checked}
              appearance={appearance === "outline" ? "standard" : appearance}
              glyph={glyph}
              onSelect={() => setChecked((value) => !value)}
              testId="ui-standard-toggle-pg-field"
            />
          )}
      </PlaygroundPreviewCard>
      <UiStandardsCopyCommand
        standard="Toggle"
        command={toggleCursorCommand(intent, variety, appearance, density, iconId, disabled)}
      />
    </PlaygroundSection>
  );
}

const DROPDOWN_APPEARANCES = [
  { id: "standard", label: "Standard" },
  { id: "soft", label: "Soft" },
  { id: "soft-outline", label: "Soft outline" },
  { id: "filled", label: "Filled" },
  { id: "outlined", label: "Outlined" },
  { id: "standout", label: "Standout" },
] as const;

const DROPDOWN_TRANSITIONS = [
  { id: "expand", label: "Expand" },
  { id: "flip", label: "Flip" },
  { id: "scale", label: "Scale" },
  { id: "slide", label: "Slide" },
] as const;

const DROPDOWN_PLAYGROUND_OPTIONS = ["Option 1", "Option 2", "Option 3", "Option 4", "Option 5", "Option 6"] as const;

const DROPDOWN_PLAYGROUND_DEFAULTS = {
  intent: "standard" as (typeof INPUT_SAMPLES)[number]["id"],
  appearance: "standard" as DropdownAppearance,
  shape: "standard" as (typeof INPUT_SHAPES)[number]["value"],
  density: "balance" as (typeof BUTTON_DENSITIES)[number]["value"],
  transition: "expand" as DropdownTransition,
  multi: false,
  disabled: false,
  label: "Standard",
  value: DROPDOWN_OPTIONS[0],
};

function dropdownCursorCommand(
  intent: string,
  appearance: string,
  shape: string,
  density: string,
  transition: string,
  multi: boolean,
  disabled: boolean,
): string {
  const parts = ["ext dropdown"];
  if (intent !== "standard") parts.push(intent);
  if (appearance !== "standard") parts.push(appearance);
  if (shape !== "standard") parts.push(shape);
  if (density !== "balance") parts.push(density);
  if (transition !== "expand") parts.push(transition);
  if (multi) parts.push("multi");
  if (disabled) parts.push("disabled");
  return parts.join(" ");
}

export function UiStandardsDropdownPlayground() {
  const [intent, setIntent] = useState(DROPDOWN_PLAYGROUND_DEFAULTS.intent);
  const [appearance, setAppearance] = useState(DROPDOWN_PLAYGROUND_DEFAULTS.appearance);
  const [shape, setShape] = useState(DROPDOWN_PLAYGROUND_DEFAULTS.shape);
  const [density, setDensity] = useState(DROPDOWN_PLAYGROUND_DEFAULTS.density);
  const [disabled, setDisabled] = useState(DROPDOWN_PLAYGROUND_DEFAULTS.disabled);
  const [multi, setMulti] = useState(DROPDOWN_PLAYGROUND_DEFAULTS.multi);
  const [transition, setTransition] = useState(DROPDOWN_PLAYGROUND_DEFAULTS.transition);
  const [label, setLabel] = useState(DROPDOWN_PLAYGROUND_DEFAULTS.label);
  const [value, setValue] = useState<string>(DROPDOWN_PLAYGROUND_DEFAULTS.value);
  const [values, setValues] = useState<string[]>([DROPDOWN_PLAYGROUND_DEFAULTS.value]);
  const [previewKey, setPreviewKey] = useState(0);
  const sample = INPUT_SAMPLES.find((item) => item.id === intent) ?? INPUT_SAMPLES[0];

  function resetPreview() {
    setIntent(DROPDOWN_PLAYGROUND_DEFAULTS.intent);
    setAppearance(DROPDOWN_PLAYGROUND_DEFAULTS.appearance);
    setShape(DROPDOWN_PLAYGROUND_DEFAULTS.shape);
    setDensity(DROPDOWN_PLAYGROUND_DEFAULTS.density);
    setTransition(DROPDOWN_PLAYGROUND_DEFAULTS.transition);
    setMulti(DROPDOWN_PLAYGROUND_DEFAULTS.multi);
    setDisabled(DROPDOWN_PLAYGROUND_DEFAULTS.disabled);
    setLabel(DROPDOWN_PLAYGROUND_DEFAULTS.label);
    setValue(DROPDOWN_PLAYGROUND_DEFAULTS.value);
    setValues([DROPDOWN_PLAYGROUND_DEFAULTS.value]);
    setPreviewKey((key) => key + 1);
  }

  return (
    <PlaygroundSection testId="ui-standard-dropdown-playground" className="">
      <div className="grid min-w-0 gap-2 sm:grid-cols-2 lg:grid-cols-3">
        <CompactSelect
          label="Intent"
          value={intent}
          options={INPUT_SAMPLES.map((item) => ({ label: item.label, value: item.id }))}
          onChange={setIntent}
          testId="ui-standard-dropdown-pg-intent"
        />
        <CompactSelect
          label="Appearance"
          value={appearance}
          options={appearanceOptions(DROPDOWN_APPEARANCES, shape)}
          onChange={(next) => {
            if (shape === "rounded" && next === "standard") return;
            setAppearance(next);
          }}
          testId="ui-standard-dropdown-pg-appearance"
        />
        <CompactSelect
          label="Shape"
          value={shape}
          options={[...INPUT_SHAPES]}
          onChange={(next) => {
            setShape(next);
            if (next === "rounded" && appearance === "standard") setAppearance("outlined");
          }}
          testId="ui-standard-dropdown-pg-shape"
        />
        <CompactSelect
          label="Density"
          value={density}
          options={[...BUTTON_DENSITIES]}
          onChange={setDensity}
          testId="ui-standard-dropdown-pg-density"
        />
        <CompactSelect
          label="Transition"
          value={transition}
          options={DROPDOWN_TRANSITIONS.map((item) => ({ label: item.label, value: item.id }))}
          onChange={setTransition}
          testId="ui-standard-dropdown-pg-transition"
        />
        <label className="flex min-w-0 flex-col gap-1 text-[length:var(--exits-text-xs)]">
          <span className="text-muted">Label</span>
          <Input
            name="ui-std-dropdown-label"
            value={label}
            onChange={(event) => setLabel(event.target.value)}
            data-testid="ui-standard-dropdown-pg-label"
            aria-label="Dropdown label"
          />
        </label>
      </div>
      <div className="flex flex-wrap gap-4">
        <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
          <Switch
            checked={multi}
            onCheckedChange={(checked) => {
              setMulti(checked);
              if (checked) setValues(value ? [value] : []);
            }}
            aria-label="Multi select"
            data-testid="ui-standard-dropdown-pg-multi"
          />
          Multi select
        </label>
        <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
          <Switch
            checked={disabled}
            onCheckedChange={setDisabled}
            aria-label="Disabled"
            data-testid="ui-standard-dropdown-pg-disabled"
          />
          Disabled
        </label>
      </div>
      <PlaygroundPreviewCard
        previewTestId="ui-standard-dropdown-pg-preview"
        themeTestId="ui-standard-dropdown-pg-theme"
        resetTestId="ui-standard-dropdown-pg-reset"
        onReset={resetPreview}
        previewClassName="ui-standards-dropdown-pg-preview flex min-h-16 items-center"
        previewProps={{ "data-density": density, "data-appearance": appearance }}
      >
          <UiStandardsQdropdown
            key={previewKey}
            label={label.trim() || "Standard"}
            color={dropdownAccent(sample)}
            value={value}
            values={values}
            options={DROPDOWN_PLAYGROUND_OPTIONS}
            appearance={appearance}
            shape={shape}
            disabled={disabled}
            multiple={multi}
            transition={transition}
            standard={sample.id === "standard"}
            darkInk={sample.id === "black"}
            onSelect={setValue}
            onChangeValues={setValues}
            testId="ui-standard-dropdown-pg-field"
          />
      </PlaygroundPreviewCard>
      <UiStandardsCopyCommand
        standard="Dropdown"
        standardStatus="pilot"
        command={dropdownCursorCommand(intent, appearance, shape, density, transition, multi, disabled)}
      />
    </PlaygroundSection>
  );
}

const SLIDER_STEPS = [
  { label: "1", value: "1" },
  { label: "5", value: "5" },
  { label: "10", value: "10" },
  { label: "25", value: "25" },
] as const;

function sliderCursorCommand(
  intent: string,
  step: number,
  showLabel: boolean,
  labelAlways: boolean,
  markers: boolean,
  snap: boolean,
  vertical: boolean,
  disabled: boolean,
): string {
  const parts = ["ext slider"];
  if (intent !== "standard") parts.push(intent);
  if (step !== 1) parts.push("step", String(step));
  if (showLabel) parts.push("label");
  if (labelAlways) parts.push("label-always");
  if (markers) parts.push("markers");
  if (snap) parts.push("snap");
  if (vertical) parts.push("vertical");
  if (disabled) parts.push("disabled");
  return parts.join(" ");
}

export function UiStandardsSliderPlayground() {
  const [intent, setIntent] = useState("standard");
  const [step, setStep] = useState("1");
  const [showLabel, setShowLabel] = useState(false);
  const [labelAlways, setLabelAlways] = useState(false);
  const [markers, setMarkers] = useState(false);
  const [snap, setSnap] = useState(false);
  const [vertical, setVertical] = useState(false);
  const [disabled, setDisabled] = useState(false);
  const [value, setValue] = useState(40);
  const sample = INPUT_SAMPLES.find((item) => item.id === intent) ?? INPUT_SAMPLES[0];
  const stepNumber = Number(step);

  function resetPreview() {
    setIntent("standard");
    setStep("1");
    setShowLabel(false);
    setLabelAlways(false);
    setMarkers(false);
    setSnap(false);
    setVertical(false);
    setDisabled(false);
    setValue(40);
  }

  return (
    <PlaygroundSection testId="ui-standard-slider-playground" className="">
      <div className="grid min-w-0 gap-2 sm:grid-cols-2 lg:grid-cols-3">
        <CompactSelect
          label="Intent"
          value={intent}
          options={INPUT_SAMPLES.map((item) => ({ label: item.label, value: item.id }))}
          onChange={setIntent}
          testId="ui-standard-slider-pg-intent"
        />
        <CompactSelect
          label="Step"
          value={step}
          options={[...SLIDER_STEPS]}
          onChange={setStep}
          testId="ui-standard-slider-pg-step"
        />
      </div>
      <div className="flex flex-wrap gap-4">
        <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
          <Switch checked={showLabel || labelAlways} onCheckedChange={setShowLabel} aria-label="Label" data-testid="ui-standard-slider-pg-label" />
          Label
        </label>
        <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
          <Switch
            checked={labelAlways}
            onCheckedChange={(checked) => {
              setLabelAlways(checked);
              if (checked) setShowLabel(true);
            }}
            aria-label="Label always"
            data-testid="ui-standard-slider-pg-label-always"
          />
          Label always
        </label>
        <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
          <Switch checked={markers} onCheckedChange={setMarkers} aria-label="Markers" data-testid="ui-standard-slider-pg-markers" />
          Markers
        </label>
        <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
          <Switch checked={snap} onCheckedChange={setSnap} aria-label="Snap" data-testid="ui-standard-slider-pg-snap" />
          Snap
        </label>
        <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
          <Switch checked={vertical} onCheckedChange={setVertical} aria-label="Vertical" data-testid="ui-standard-slider-pg-vertical" />
          Vertical
        </label>
        <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
          <Switch checked={disabled} onCheckedChange={setDisabled} aria-label="Disabled" data-testid="ui-standard-slider-pg-disabled" />
          Disabled
        </label>
      </div>
      <PlaygroundPreviewCard
        previewTestId="ui-standard-slider-pg-preview"
        themeTestId="ui-standard-slider-pg-theme"
        resetTestId="ui-standard-slider-pg-reset"
        onReset={resetPreview}
        previewClassName="ui-standards-slider-pg-preview flex min-h-16 items-center"
        previewProps={{ "data-vertical": vertical ? "true" : "false" }}
      >
        <UiStandardsQslider
          color={sliderAccent(sample)}
          ink={sample.fg}
          value={value}
          step={stepNumber}
          snap={snap}
          showLabel={showLabel || labelAlways}
          labelAlways={labelAlways}
          markers={markers}
          vertical={vertical}
          disabled={disabled}
          onChange={setValue}
          testId="ui-standard-slider-pg-field"
        />
      </PlaygroundPreviewCard>
      <UiStandardsCopyCommand
        standard="Slider"
        standardStatus="pilot"
        command={sliderCursorCommand(intent, stepNumber, showLabel || labelAlways, labelAlways, markers, snap, vertical, disabled)}
      />
    </PlaygroundSection>
  );
}

function rangeCursorCommand(
  intent: string,
  step: number,
  showLabel: boolean,
  labelAlways: boolean,
  markers: boolean,
  dragRange: boolean,
  disabled: boolean,
): string {
  const parts = ["ext range"];
  if (intent !== "standard") parts.push(intent);
  if (step !== 1) parts.push("step", String(step));
  if (showLabel) parts.push("label");
  if (labelAlways) parts.push("label-always");
  if (markers) parts.push("markers");
  if (dragRange) parts.push("drag");
  if (disabled) parts.push("disabled");
  return parts.join(" ");
}

export function UiStandardsRangePlayground() {
  const [intent, setIntent] = useState("standard");
  const [step, setStep] = useState("1");
  const [showLabel, setShowLabel] = useState(false);
  const [labelAlways, setLabelAlways] = useState(false);
  const [markers, setMarkers] = useState(false);
  const [dragRange, setDragRange] = useState(false);
  const [disabled, setDisabled] = useState(false);
  const [value, setValue] = useState<SliderRangeValue>({ min: 20, max: 70 });
  const sample = INPUT_SAMPLES.find((item) => item.id === intent) ?? INPUT_SAMPLES[0];
  const stepNumber = Number(step);

  function resetPreview() {
    setIntent("standard");
    setStep("1");
    setShowLabel(false);
    setLabelAlways(false);
    setMarkers(false);
    setDragRange(false);
    setDisabled(false);
    setValue({ min: 20, max: 70 });
  }

  return (
    <PlaygroundSection testId="ui-standard-range-playground" className="">
      <div className="grid min-w-0 gap-2 sm:grid-cols-2 lg:grid-cols-3">
        <CompactSelect
          label="Intent"
          value={intent}
          options={INPUT_SAMPLES.map((item) => ({ label: item.label, value: item.id }))}
          onChange={setIntent}
          testId="ui-standard-range-pg-intent"
        />
        <CompactSelect
          label="Step"
          value={step}
          options={[...SLIDER_STEPS]}
          onChange={setStep}
          testId="ui-standard-range-pg-step"
        />
      </div>
      <div className="flex flex-wrap gap-4">
        <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
          <Switch checked={showLabel || labelAlways} onCheckedChange={setShowLabel} aria-label="Label" data-testid="ui-standard-range-pg-label" />
          Label
        </label>
        <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
          <Switch
            checked={labelAlways}
            onCheckedChange={(checked) => {
              setLabelAlways(checked);
              if (checked) setShowLabel(true);
            }}
            aria-label="Label always"
            data-testid="ui-standard-range-pg-label-always"
          />
          Label always
        </label>
        <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
          <Switch checked={markers} onCheckedChange={setMarkers} aria-label="Markers" data-testid="ui-standard-range-pg-markers" />
          Markers
        </label>
        <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
          <Switch checked={dragRange} onCheckedChange={setDragRange} aria-label="Drag range" data-testid="ui-standard-range-pg-drag" />
          Drag range
        </label>
        <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
          <Switch checked={disabled} onCheckedChange={setDisabled} aria-label="Disabled" data-testid="ui-standard-range-pg-disabled" />
          Disabled
        </label>
      </div>
      <PlaygroundPreviewCard
        previewTestId="ui-standard-range-pg-preview"
        themeTestId="ui-standard-range-pg-theme"
        resetTestId="ui-standard-range-pg-reset"
        onReset={resetPreview}
        previewClassName="flex min-h-16 items-center"
      >
        <UiStandardsQrange
          color={sliderAccent(sample)}
          ink={sample.fg}
          value={value}
          step={stepNumber}
          snap={false}
          showLabel={showLabel || labelAlways}
          labelAlways={labelAlways}
          markers={markers}
          dragRange={dragRange}
          disabled={disabled}
          onChange={setValue}
          testId="ui-standard-range-pg-field"
        />
      </PlaygroundPreviewCard>
      <UiStandardsCopyCommand
        standard="Range"
        standardStatus="pilot"
        command={rangeCursorCommand(intent, stepNumber, showLabel || labelAlways, labelAlways, markers, dragRange, disabled)}
      />
    </PlaygroundSection>
  );
}

function dateCursorCommand(
  intent: string,
  appearance: string,
  variety: string,
  showToday: boolean,
  range: boolean,
  disabled: boolean,
): string {
  const parts = ["ext date"];
  if (intent !== "standard") parts.push(intent);
  if (appearance !== "standard") parts.push(appearance);
  if (variety === "roll") parts.push("roll");
  if (showToday) parts.push("today");
  if (range) parts.push("range");
  if (disabled) parts.push("disabled");
  return parts.join(" ");
}

export function UiStandardsDatePlayground() {
  const [intent, setIntent] = useState("standard");
  const [appearance, setAppearance] = useState("standard");
  const [variety, setVariety] = useState("calendar");
  const [showToday, setShowToday] = useState(false);
  const [range, setRange] = useState(false);
  const [disabled, setDisabled] = useState(false);
  const [value, setValue] = useState<DatePickerValue>(DATE_PICKER_SAMPLE);
  const [rangeValue, setRangeValue] = useState<DatePickerValue>(DATE_RANGE_SAMPLE);
  const [previewKey, setPreviewKey] = useState(0);
  const sample = INPUT_SAMPLES.find((item) => item.id === intent) ?? INPUT_SAMPLES[0];

  function resetPreview() {
    setIntent("standard");
    setAppearance("standard");
    setVariety("calendar");
    setShowToday(false);
    setRange(false);
    setDisabled(false);
    setValue(DATE_PICKER_SAMPLE);
    setRangeValue(DATE_RANGE_SAMPLE);
    setPreviewKey((key) => key + 1);
  }

  return (
    <PlaygroundSection testId="ui-standard-date-playground" className="">
      <div className="grid min-w-0 gap-2 sm:grid-cols-2 lg:grid-cols-3">
        <CompactSelect
          label="Intent"
          value={intent}
          options={INPUT_SAMPLES.map((item) => ({ label: item.label, value: item.id }))}
          onChange={setIntent}
          testId="ui-standard-date-pg-intent"
        />
        <CompactSelect
          label="Appearance"
          value={appearance}
          options={[
            { label: "Standard", value: "standard" },
            { label: "Raised", value: "raised" },
            { label: "Gradient", value: "gradient" },
            { label: "Flat", value: "flat" },
          ]}
          onChange={setAppearance}
          testId="ui-standard-date-pg-appearance"
        />
        <CompactSelect
          label="Variety"
          value={variety}
          options={[
            { label: "Calendar", value: "calendar" },
            { label: "Roll", value: "roll" },
          ]}
          onChange={setVariety}
          testId="ui-standard-date-pg-variety"
        />
      </div>
      <div className="flex flex-wrap gap-4">
        <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
          <Switch checked={showToday} onCheckedChange={setShowToday} aria-label="Today" data-testid="ui-standard-date-pg-today" />
          Today
        </label>
        <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
          <Switch checked={range} onCheckedChange={setRange} aria-label="Range" data-testid="ui-standard-date-pg-range" />
          Range
        </label>
        <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
          <Switch checked={disabled} onCheckedChange={setDisabled} aria-label="Disabled" data-testid="ui-standard-date-pg-disabled" />
          Disabled
        </label>
      </div>
      <PlaygroundPreviewCard
        previewTestId="ui-standard-date-pg-preview"
        themeTestId="ui-standard-date-pg-theme"
        resetTestId="ui-standard-date-pg-reset"
        onReset={resetPreview}
        previewClassName="flex min-h-16 items-center"
      >
        <UiStandardsQdate
          key={previewKey}
          color={radioAccent(sample)}
          ink={sample.fg}
          value={range ? rangeValue : value}
          range={range}
          showToday={showToday}
          disabled={disabled}
          appearance={appearance as DateAppearance}
          variety={variety as DateVariety}
          onChange={range ? setRangeValue : setValue}
          testId="ui-standard-date-pg-field"
        />
      </PlaygroundPreviewCard>
      <UiStandardsCopyCommand
        standard="Date"
        standardStatus="pilot"
        command={dateCursorCommand(intent, appearance, variety, showToday, range, disabled)}
      />
    </PlaygroundSection>
  );
}

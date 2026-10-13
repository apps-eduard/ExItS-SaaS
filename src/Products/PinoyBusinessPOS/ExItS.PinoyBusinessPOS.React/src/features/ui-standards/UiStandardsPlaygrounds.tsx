import { useMemo, useState, type CSSProperties } from "react";
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
} from "@/features/ui-standards/UiStandardsDropdownGallery";

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
  options: ReadonlyArray<{ label: string; value: T }>;
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
          <option key={opt.value} value={opt.value}>
            {opt.label}
          </option>
        ))}
      </select>
    </label>
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
  const darkInk = (sample.id === "standard" || sample.id === "black") && transparent;
  const previewStyle: CSSProperties & { "--qbtn-bg"?: string } = transparent
    ? {
        color: accent,
        borderColor: appearance === "outline" ? accent : "transparent",
        backgroundColor: "transparent",
      }
    : { backgroundColor: sample.bg, color: sample.fg };
  if (appearance === "gradient" && !transparent) previewStyle["--qbtn-bg"] = sample.bg;
  const previewClass = [
    "ui-standards-qbtn",
    "ui-standards-qbtn--playground",
    appearance === "outline" ? "ui-standards-qbtn--outline" : "",
    appearance === "ghost" ? "ui-standards-qbtn--ghost" : "",
    appearance === "raised" ? "ui-standards-qbtn--raised" : "",
    appearance === "glossy" ? "ui-standards-qbtn--glossy" : "",
    appearance === "gradient" ? "ui-standards-qbtn--gradient" : "",
    appearance === "flat" ? "ui-standards-qbtn--flat" : "",
    darkInk ? "ui-standards-qbtn--dark-ink" : "",
    iconOnly ? "ui-standards-qbtn--icon-only" : "",
    ripple ? "ui-standards-qbtn--ripple" : "",
    !transparent && "border" in sample && sample.border ? "ui-standards-qbtn--border" : "",
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
      <PlaygroundLabel>Preview</PlaygroundLabel>
      <div className="flex flex-wrap items-center gap-3">
      <div className="ui-standards-btn-pg-preview" data-testid="ui-standard-btn-pg-preview" data-density={density}>
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
      </div>
      <button
        type="button"
        className="inline-flex h-9 items-center rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 text-[length:var(--exits-text-sm)]"
        data-testid="ui-standard-btn-pg-reset"
        onClick={resetPreview}
      >
        Reset
      </button>
      </div>
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
          options={INPUT_APPEARANCES.map((item) => ({ label: item.label, value: item.id }))}
          onChange={setAppearance}
          testId="ui-standard-input-pg-appearance"
        />
        <CompactSelect
          label="Shape"
          value={shape}
          options={[...INPUT_SHAPES]}
          onChange={setShape}
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
      <PlaygroundLabel>Preview</PlaygroundLabel>
      <div className="flex flex-wrap items-end gap-3">
        <div
          className="ui-standards-input-pg-preview"
          data-testid="ui-standard-input-pg-preview"
          data-density={density}
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
        </div>
        <button
          type="button"
          className="inline-flex h-9 items-center rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 text-[length:var(--exits-text-sm)]"
          data-testid="ui-standard-input-pg-reset"
          onClick={resetPreview}
        >
          Reset
        </button>
      </div>
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
      <div className="flex min-w-0 flex-col gap-3 rounded-[var(--exits-radius-lg)] border border-border bg-surface p-4 shadow-[var(--exits-shadow-sm)]">
        <div className="flex items-center justify-between gap-3">
          <PlaygroundLabel>Preview</PlaygroundLabel>
          <Button
            type="button"
            intent="neutral"
            appearance="outline"
            shape="pill"
            size="icon"
            aria-label="Reset"
            data-testid="ui-standard-toggle-pg-reset"
            onClick={resetPreview}
          >
            <RotateCcw className="size-4" aria-hidden />
          </Button>
        </div>
        <div
          className="ui-standards-toggle-pg-preview flex min-h-16 items-center"
          data-density={density}
          data-variety={variety}
          data-appearance={appearance}
          data-testid="ui-standard-toggle-pg-preview"
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
        </div>
      </div>
      <UiStandardsCopyCommand
        standard="Toggle"
        command={toggleCursorCommand(intent, variety, appearance, density, iconId, disabled)}
      />
    </PlaygroundSection>
  );
}

const DROPDOWN_APPEARANCES = [
  { id: "standard", label: "Standard" },
  { id: "filled", label: "Filled" },
  { id: "outlined", label: "Outlined" },
  { id: "standout", label: "Standout" },
] as const;

const DROPDOWN_PLAYGROUND_DEFAULTS = {
  intent: "standard" as (typeof INPUT_SAMPLES)[number]["id"],
  appearance: "standard" as DropdownAppearance,
  shape: "standard" as (typeof INPUT_SHAPES)[number]["value"],
  density: "balance" as (typeof BUTTON_DENSITIES)[number]["value"],
  disabled: false,
  label: "Standard",
  value: DROPDOWN_OPTIONS[0],
};

function dropdownCursorCommand(
  intent: string,
  appearance: string,
  shape: string,
  density: string,
  disabled: boolean,
): string {
  const parts = ["ext dropdown"];
  if (intent !== "standard") parts.push(intent);
  if (appearance !== "standard") parts.push(appearance);
  if (shape !== "standard") parts.push(shape);
  if (density !== "balance") parts.push(density);
  if (disabled) parts.push("disabled");
  return parts.join(" ");
}

export function UiStandardsDropdownPlayground() {
  const [intent, setIntent] = useState(DROPDOWN_PLAYGROUND_DEFAULTS.intent);
  const [appearance, setAppearance] = useState(DROPDOWN_PLAYGROUND_DEFAULTS.appearance);
  const [shape, setShape] = useState(DROPDOWN_PLAYGROUND_DEFAULTS.shape);
  const [density, setDensity] = useState(DROPDOWN_PLAYGROUND_DEFAULTS.density);
  const [disabled, setDisabled] = useState(DROPDOWN_PLAYGROUND_DEFAULTS.disabled);
  const [label, setLabel] = useState(DROPDOWN_PLAYGROUND_DEFAULTS.label);
  const [value, setValue] = useState<string>(DROPDOWN_PLAYGROUND_DEFAULTS.value);
  const [previewDark, setPreviewDark] = useState(false);
  const sample = INPUT_SAMPLES.find((item) => item.id === intent) ?? INPUT_SAMPLES[0];

  function resetPreview() {
    setIntent(DROPDOWN_PLAYGROUND_DEFAULTS.intent);
    setAppearance(DROPDOWN_PLAYGROUND_DEFAULTS.appearance);
    setShape(DROPDOWN_PLAYGROUND_DEFAULTS.shape);
    setDensity(DROPDOWN_PLAYGROUND_DEFAULTS.density);
    setDisabled(DROPDOWN_PLAYGROUND_DEFAULTS.disabled);
    setLabel(DROPDOWN_PLAYGROUND_DEFAULTS.label);
    setValue(DROPDOWN_PLAYGROUND_DEFAULTS.value);
    setPreviewDark(false);
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
          options={DROPDOWN_APPEARANCES.map((item) => ({ label: item.label, value: item.id }))}
          onChange={setAppearance}
          testId="ui-standard-dropdown-pg-appearance"
        />
        <CompactSelect
          label="Shape"
          value={shape}
          options={[...INPUT_SHAPES]}
          onChange={setShape}
          testId="ui-standard-dropdown-pg-shape"
        />
        <CompactSelect
          label="Density"
          value={density}
          options={[...BUTTON_DENSITIES]}
          onChange={setDensity}
          testId="ui-standard-dropdown-pg-density"
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
            checked={disabled}
            onCheckedChange={setDisabled}
            aria-label="Disabled"
            data-testid="ui-standard-dropdown-pg-disabled"
          />
          Disabled
        </label>
      </div>
      <div className="flex min-w-0 flex-col gap-3 rounded-[var(--exits-radius-lg)] border border-border bg-surface p-4 shadow-[var(--exits-shadow-sm)]">
        <div className="flex items-center justify-between gap-3">
          <PlaygroundLabel>Preview</PlaygroundLabel>
          <div className="flex items-center gap-2">
            <Button
              type="button"
              intent={previewDark ? "neutral" : "primary"}
              appearance={previewDark ? "outline" : "solid"}
              shape="pill"
              size="icon"
              aria-label="Light mode"
              aria-pressed={!previewDark}
              data-testid="ui-standard-dropdown-pg-light"
              onClick={() => setPreviewDark(false)}
            >
              <Sun className="size-4" aria-hidden />
            </Button>
            <Button
              type="button"
              intent={previewDark ? "primary" : "neutral"}
              appearance={previewDark ? "solid" : "outline"}
              shape="pill"
              size="icon"
              aria-label="Dark mode"
              aria-pressed={previewDark}
              data-testid="ui-standard-dropdown-pg-dark"
              onClick={() => setPreviewDark(true)}
            >
              <Moon className="size-4" aria-hidden />
            </Button>
            <Button
              type="button"
              intent="neutral"
              appearance="outline"
              shape="pill"
              size="icon"
              aria-label="Reset"
              data-testid="ui-standard-dropdown-pg-reset"
              onClick={resetPreview}
            >
              <RotateCcw className="size-4" aria-hidden />
            </Button>
          </div>
        </div>
        <div
          className={
            "ui-standards-dropdown-pg-preview flex min-h-16 items-center" +
            (previewDark ? " ui-standards-qdropdown-dark" : "")
          }
          data-density={density}
          data-appearance={appearance}
          data-preview-theme={previewDark ? "dark" : "light"}
          data-testid="ui-standard-dropdown-pg-preview"
        >
          <UiStandardsQdropdown
            label={label.trim() || "Standard"}
            color={dropdownAccent(sample)}
            value={value}
            appearance={appearance}
            shape={shape}
            disabled={disabled}
            onDark={previewDark}
            standard={sample.id === "standard"}
            darkInk={sample.id === "black"}
            onSelect={setValue}
            testId="ui-standard-dropdown-pg-field"
          />
        </div>
      </div>
      <UiStandardsCopyCommand
        standard="Dropdown"
        standardStatus="pilot"
        command={dropdownCursorCommand(intent, appearance, shape, density, disabled)}
      />
    </PlaygroundSection>
  );
}

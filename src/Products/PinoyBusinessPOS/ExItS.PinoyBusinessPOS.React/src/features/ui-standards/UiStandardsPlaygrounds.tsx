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
  Trash2,
  TriangleAlert,
  type LucideIcon,
} from "lucide-react";
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

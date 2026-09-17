import { useMemo, useState } from "react";
import { ExitsSelect } from "@/components/exits/ExitsSelect";
import { StatusChip } from "@/components/exits/StatusChip";
import type { StatusChipAppearance, StatusChipShape, StatusChipTone } from "@/components/exits/StatusChip";
import {
  Button,
  type ButtonAppearance,
  type ButtonIntentTone,
} from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Switch } from "@/components/ui/switch";
import { formatJsxProps } from "@/features/ui-standards/copyUiSnippet";
import {
  PlaygroundLabel,
  PlaygroundSection,
  UiStandardsSnippetBlock,
} from "@/features/ui-standards/UiStandardsSnippetBlock";

const BUTTON_INTENTS: ReadonlyArray<{ label: string; value: ButtonIntentTone }> = [
  { label: "Primary", value: "primary" },
  { label: "Neutral", value: "neutral" },
  { label: "Success", value: "success" },
  { label: "Info", value: "info" },
  { label: "Warning", value: "warning" },
  { label: "Danger", value: "danger" },
];

const BUTTON_APPEARANCES: ReadonlyArray<{ label: string; value: ButtonAppearance }> = [
  { label: "Solid", value: "solid" },
  { label: "Outline", value: "outline" },
  { label: "Ghost", value: "ghost" },
  { label: "Elevated", value: "elevated" },
  { label: "Gradient", value: "gradient" },
];

const BUTTON_SHAPES = [
  { label: "Auto", value: "auto" },
  { label: "Standard", value: "standard" },
  { label: "Soft", value: "soft" },
  { label: "Pill", value: "pill" },
  { label: "Round", value: "round" },
] as const;

const BUTTON_SIZES = [
  { label: "Default", value: "default" },
  { label: "Large", value: "large" },
  { label: "Icon", value: "icon" },
] as const;

const STATUS_TONES: ReadonlyArray<{ label: string; value: StatusChipTone }> = [
  { label: "Neutral", value: "neutral" },
  { label: "Info", value: "info" },
  { label: "Success", value: "success" },
  { label: "Warning", value: "warning" },
  { label: "Danger", value: "danger" },
  { label: "Preferred", value: "primary" },
];

const STATUS_APPEARANCES: ReadonlyArray<{ label: string; value: StatusChipAppearance }> = [
  { label: "Soft", value: "soft" },
  { label: "Outline", value: "outline" },
  { label: "Solid", value: "solid" },
];

const STATUS_SHAPES: ReadonlyArray<{ label: string; value: StatusChipShape }> = [
  { label: "Auto", value: "auto" },
  { label: "Standard", value: "standard" },
  { label: "Soft", value: "soft" },
  { label: "Pill", value: "pill" },
];

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
  const [intent, setIntent] = useState<ButtonIntentTone>("primary");
  const [appearance, setAppearance] = useState<ButtonAppearance>("solid");
  const [shape, setShape] = useState<(typeof BUTTON_SHAPES)[number]["value"]>("auto");
  const [size, setSize] = useState<(typeof BUTTON_SIZES)[number]["value"]>("default");
  const [disabled, setDisabled] = useState(false);
  const [loading, setLoading] = useState(false);
  const [label, setLabel] = useState("Save");

  const snippet = useMemo(() => {
    const props = formatJsxProps({
      type: "button",
      intent,
      appearance,
      shape,
      size: size === "default" ? undefined : size,
      disabled: disabled || undefined,
      "aria-busy": loading || undefined,
    });
    const children =
      size === "icon" ? `{/* icon */}` : label.trim() || "Save";
    return `<Button\n${props}\n>\n  ${children}\n</Button>`;
  }, [intent, appearance, shape, size, disabled, loading, label]);

  return (
    <PlaygroundSection testId="ui-standard-button-playground">
      <PlaygroundLabel>Playground</PlaygroundLabel>
      <div className="grid min-w-0 gap-2 sm:grid-cols-2 lg:grid-cols-3">
        <CompactSelect
          label="Intent"
          value={intent}
          options={BUTTON_INTENTS}
          onChange={setIntent}
          testId="ui-standard-btn-pg-intent"
        />
        <CompactSelect
          label="Appearance"
          value={appearance}
          options={BUTTON_APPEARANCES}
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
          label="Size"
          value={size}
          options={[...BUTTON_SIZES]}
          onChange={setSize}
          testId="ui-standard-btn-pg-size"
        />
        <label className="flex min-w-0 flex-col gap-1 text-[length:var(--exits-text-xs)] sm:col-span-2">
          <span className="text-muted">Label</span>
          <Input
            name="ui-std-btn-label"
            value={label}
            onChange={(e) => setLabel(e.target.value)}
            disabled={size === "icon"}
            data-testid="ui-standard-btn-pg-label"
            aria-label="Button label"
          />
        </label>
        <div className="flex flex-wrap items-center gap-4">
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
        </div>
      </div>
      <PlaygroundLabel>Preview</PlaygroundLabel>
      <div data-testid="ui-standard-btn-pg-preview">
        <Button
          type="button"
          intent={intent}
          appearance={appearance}
          shape={shape}
          size={size}
          disabled={disabled || loading}
          aria-busy={loading || undefined}
        >
          {size === "icon" ? "✓" : label.trim() || "Save"}
        </Button>
      </div>
      <PlaygroundLabel>Code</PlaygroundLabel>
      <UiStandardsSnippetBlock snippet={snippet} testIdPrefix="ui-standard-btn-pg" />
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
          <Input
            name="ui-std-status-label"
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

import { useEffect, useId, useRef, useState, type CSSProperties, type ReactNode } from "react";
import { ChevronDown } from "lucide-react";
import { INPUT_SAMPLES, type InputSample } from "@/features/ui-standards/UiStandardsInputGallery";
import { radioAccent } from "@/features/ui-standards/UiStandardsRadioGallery";

/**
 * Quasar-style dropdown samples for the Form Components pilot.
 * Showcase only — not the product ExitsSelect.
 */
export const DROPDOWN_OPTIONS = ["Option 1", "Option 2", "Option 3"] as const;

export type DropdownAppearance = "standard" | "filled" | "outlined" | "standout";

/** Button Standard is white. A dark edge keeps it visible on a light page. */
export const DROPDOWN_STANDARD_COLOR = "#ffffff";

export function dropdownAccent(sample: Pick<InputSample, "id" | "bg">): string {
  return sample.id === "standard" ? DROPDOWN_STANDARD_COLOR : radioAccent(sample);
}

export function UiStandardsQdropdown({
  label,
  color,
  value,
  options = DROPDOWN_OPTIONS,
  appearance = "standard",
  shape = "standard",
  disabled = false,
  darkInk = false,
  onDark = false,
  standard = false,
  onSelect,
  testId,
}: {
  label: string;
  color: string;
  value: string;
  options?: readonly string[];
  appearance?: DropdownAppearance;
  shape?: "square" | "standard" | "rounded";
  disabled?: boolean;
  darkInk?: boolean;
  onDark?: boolean;
  standard?: boolean;
  onSelect?: (value: string) => void;
  testId?: string;
}) {
  const [open, setOpen] = useState(false);
  const rootRef = useRef<HTMLDivElement>(null);
  const menuId = useId();
  const style = { "--qdropdown-color": color } as CSSProperties;

  useEffect(() => {
    if (!open) return;
    function onPointerDown(event: PointerEvent) {
      if (!rootRef.current?.contains(event.target as Node)) setOpen(false);
    }
    function onKey(event: KeyboardEvent) {
      if (event.key === "Escape") setOpen(false);
    }
    document.addEventListener("pointerdown", onPointerDown);
    document.addEventListener("keydown", onKey);
    return () => {
      document.removeEventListener("pointerdown", onPointerDown);
      document.removeEventListener("keydown", onKey);
    };
  }, [open]);

  return (
    <div
      ref={rootRef}
      className={
        "ui-standards-qdropdown" +
        (appearance !== "standard" ? ` ui-standards-qdropdown--${appearance}` : "") +
        (disabled ? " ui-standards-qdropdown--disabled" : "") +
        (darkInk ? " ui-standards-qdropdown--dark-ink" : "") +
        (onDark ? " ui-standards-qdropdown--on-dark" : "") +
        (standard ? " ui-standards-qdropdown--standard" : "") +
        (open ? " ui-standards-qdropdown--open" : "")
      }
      style={style}
      data-shape={shape}
      data-testid={testId}
    >
      <span className="ui-standards-qdropdown__label">{label}</span>
      <button
        type="button"
        className="ui-standards-qdropdown__field"
        aria-haspopup="listbox"
        aria-expanded={open}
        aria-controls={menuId}
        disabled={disabled}
        data-testid={testId ? `${testId}-field` : undefined}
        onClick={() => {
          if (!disabled) setOpen((current) => !current);
        }}
      >
        <span className="ui-standards-qdropdown__value">{value}</span>
        <ChevronDown className="ui-standards-qdropdown__chevron" aria-hidden />
      </button>
      {open ? (
        <ul className="ui-standards-qdropdown__menu" id={menuId} role="listbox" data-testid={testId ? `${testId}-menu` : undefined}>
          {options.map((option) => (
            <li key={option} role="presentation">
              <button
                type="button"
                role="option"
                aria-selected={option === value}
                className={
                  "ui-standards-qdropdown__option" +
                  (option === value ? " ui-standards-qdropdown__option--selected" : "")
                }
                data-testid={testId ? `${testId}-option-${option.replace(/\s+/g, "-").toLowerCase()}` : undefined}
                onClick={() => {
                  onSelect?.(option);
                  setOpen(false);
                }}
              >
                {option}
              </button>
            </li>
          ))}
        </ul>
      ) : null}
    </div>
  );
}

function DropdownColor({
  sample,
  testId,
  appearance = "standard",
  onDark = false,
}: {
  sample: InputSample;
  testId: string;
  appearance?: DropdownAppearance;
  onDark?: boolean;
}) {
  const [value, setValue] = useState<string>(DROPDOWN_OPTIONS[0]);
  return (
    <UiStandardsQdropdown
      label={sample.label}
      color={dropdownAccent(sample)}
      value={value}
      appearance={appearance}
      onDark={onDark}
      standard={sample.id === "standard"}
      darkInk={sample.id === "black"}
      onSelect={setValue}
      testId={`${testId}-${sample.id}`}
    />
  );
}

function DropdownRow({
  title,
  testId,
  appearance = "standard",
  onDark = false,
}: {
  title: string;
  testId: string;
  appearance?: DropdownAppearance;
  onDark?: boolean;
}) {
  return (
    <div className="ui-standards-q-example" data-testid={testId}>
      <div className="ui-standards-q-example__label">{title}</div>
      {onDark ? (
        <div className="ui-standards-qdropdown-dark" data-testid={`${testId}-panel`}>
          <div className="ui-standards-qdropdown-dark__row">
            {INPUT_SAMPLES.map((sample) => (
              <DropdownColor key={sample.id} sample={sample} testId={testId} appearance={appearance} onDark />
            ))}
          </div>
        </div>
      ) : (
        <div className="ui-standards-q-example__row">
          {INPUT_SAMPLES.map((sample) => (
            <DropdownColor key={sample.id} sample={sample} testId={testId} appearance={appearance} />
          ))}
        </div>
      )}
    </div>
  );
}

export function UiStandardsDropdownGallery(): ReactNode {
  return (
    <div className="flex flex-col gap-4" data-testid="ui-standard-dropdown-gallery">
      <DropdownRow title="Standard" testId="ui-standard-dropdown" />
      <DropdownRow title="Filled" testId="ui-standard-dropdown-filled" appearance="filled" />
      <DropdownRow title="Outlined" testId="ui-standard-dropdown-outlined" appearance="outlined" />
      <DropdownRow title="Standout" testId="ui-standard-dropdown-standout" appearance="standout" />
      <DropdownRow title="Force dark mode" testId="ui-standard-dropdown-dark" onDark />
    </div>
  );
}

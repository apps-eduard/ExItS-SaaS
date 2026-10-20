import { useEffect, useId, useRef, useState, type CSSProperties, type ReactNode } from "react";
import { Check, ChevronDown, X } from "lucide-react";
import { SEARCH_CLEAR_GLOBAL_STROKE } from "@/features/ui-standards/search-clear-global-default";
import { INPUT_SAMPLES, type InputSample } from "@/features/ui-standards/UiStandardsInputGallery";
import { radioAccent } from "@/features/ui-standards/UiStandardsRadioGallery";

/**
 * Quasar-style dropdown samples for the locked Dropdown standard.
 * Showcase only — not the product ExitsSelect.
 */
export const DROPDOWN_OPTIONS = ["Option 1", "Option 2", "Option 3"] as const;

export type DropdownAppearance =
  | "standard"
  | "soft"
  | "soft-outline"
  | "filled"
  | "outlined"
  | "standout"
  | "stacked";
export type DropdownTransition = "flip-up" | "flip-down" | "scale" | "jump-up";
export type DropdownValueDisplay = "standard" | "short" | "chips";

function dropdownFieldValue(
  multiple: boolean,
  selected: readonly string[],
  value: string,
  valueDisplay: DropdownValueDisplay,
  placeholderOnly: boolean,
  label: string,
  emptyText = "Select",
) {
  const chosen = multiple ? selected : value ? [value] : [];
  if (chosen.length > 0 && valueDisplay === "chips") {
    return { text: chosen.join(", "), chips: chosen, placeholder: false };
  }
  if (chosen.length >= 2 && valueDisplay === "short") {
    return { text: `${chosen.length} Selected`, chips: null as string[] | null, placeholder: false };
  }
  if (chosen.length === 0) {
    const text = placeholderOnly ? label.trim() || "Select" : emptyText;
    return {
      text,
      chips: null as string[] | null,
      placeholder: text.length > 0,
    };
  }
  return { text: multiple ? chosen.join(", ") : value, chips: null as string[] | null, placeholder: false };
}

function ClosedDropdownValue({
  field,
  testId,
}: {
  field: { text: string; chips: readonly string[] | null; placeholder: boolean };
  testId?: string;
}) {
  if (field.chips) {
    return (
      <span className="ui-standards-qdropdown__chips" data-testid={testId ? `${testId}-chips` : undefined}>
        {field.chips.map((chip) => (
          <span key={chip} className="ui-standards-qdropdown__chip">
            {chip}
          </span>
        ))}
      </span>
    );
  }
  return (
    <span className={"ui-standards-qdropdown__value" + (field.placeholder ? " ui-standards-qdropdown__value--placeholder" : "")}>
      {field.text}
    </span>
  );
}

/** Button Standard is white. A dark edge keeps it visible on a light page. */
export const DROPDOWN_STANDARD_COLOR = "#ffffff";

export function dropdownAccent(sample: Pick<InputSample, "id" | "bg">): string {
  return sample.id === "standard" ? DROPDOWN_STANDARD_COLOR : radioAccent(sample);
}

export function UiStandardsQdropdown({
  label,
  color,
  value,
  values = [],
  options = DROPDOWN_OPTIONS,
  appearance = "standard",
  shape = "standard",
  disabled = false,
  darkInk = false,
  onDark = false,
  standard = false,
  searchable = true,
  multiple = false,
  transition = "scale",
  valueDisplay = "standard",
  placeholderOnly = false,
  emptyText = "Select",
  disabledOptions = [],
  optionColors,
  onSelect,
  onChangeValues,
  testId,
}: {
  label: string;
  color: string;
  value: string;
  values?: readonly string[];
  options?: readonly string[];
  appearance?: DropdownAppearance;
  shape?: "square" | "standard" | "soft" | "pill" | "rounded";
  disabled?: boolean;
  darkInk?: boolean;
  onDark?: boolean;
  standard?: boolean;
  searchable?: boolean;
  multiple?: boolean;
  transition?: DropdownTransition;
  valueDisplay?: DropdownValueDisplay;
  placeholderOnly?: boolean;
  emptyText?: string;
  disabledOptions?: readonly string[];
  optionColors?: Readonly<Record<string, string>>;
  onSelect?: (value: string) => void;
  onChangeValues?: (values: string[]) => void;
  testId?: string;
}) {
  const [open, setOpen] = useState(false);
  const [mounted, setMounted] = useState(false);
  const [query, setQuery] = useState("");
  const rootRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLInputElement>(null);
  const menuId = useId();
  const style = { "--qdropdown-color": color } as CSSProperties;
  const selected = multiple ? values : value ? [value] : [];
  const allSelected = options.length > 0 && options.every((option) => selected.includes(option));
  const fieldValue = dropdownFieldValue(multiple, selected, value, valueDisplay, placeholderOnly, label, emptyText);
  const shown = fieldValue.text;
  const needle = query.trim().toLowerCase();
  const visible = searchable && needle ? options.filter((option) => option.toLowerCase().includes(needle)) : options;

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

  useEffect(() => {
    if (open && searchable) inputRef.current?.focus();
  }, [open, searchable]);

  useEffect(() => {
    if (open) {
      setMounted(true);
      return;
    }
    if (!mounted) return;
    const reduce = typeof window.matchMedia === "function" && window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    if (reduce) {
      setMounted(false);
      setQuery("");
      return;
    }
    const timeout = window.setTimeout(() => {
      setMounted(false);
      setQuery("");
    }, 180);
    return () => window.clearTimeout(timeout);
  }, [open, mounted]);

  function choose(option: string) {
    if (multiple) {
      const next = selected.includes(option) ? selected.filter((item) => item !== option) : [...selected, option];
      onChangeValues?.(options.filter((item) => next.includes(item)));
      return;
    }
    onSelect?.(option);
    setOpen(false);
  }

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
        (open ? " ui-standards-qdropdown--open" : "") +
        (placeholderOnly ? " ui-standards-qdropdown--placeholder" : "") +
        (valueDisplay === "chips" ? " ui-standards-qdropdown--chips" : "")
      }
      style={style}
      data-shape={shape}
      data-transition={transition}
      data-value-display={valueDisplay}
      data-placeholder-only={placeholderOnly ? "true" : "false"}
      data-testid={testId}
    >
      {searchable ? (
        <div
          className="ui-standards-qdropdown__field"
          data-testid={testId ? `${testId}-field` : undefined}
          onClick={() => {
            if (!disabled && !open) setOpen(true);
          }}
        >
          {placeholderOnly ? null : <span className="ui-standards-qdropdown__label">{label}</span>}
          {open ? (
            <input
              ref={inputRef}
              className="ui-standards-qdropdown__value ui-standards-qdropdown__query"
              type="text"
              value={query}
              placeholder={shown}
              disabled={disabled}
              role="combobox"
              aria-autocomplete="list"
              aria-expanded={open}
              aria-controls={menuId}
              aria-label={label}
              data-testid={testId ? `${testId}-search` : undefined}
              onChange={(event) => setQuery(event.target.value)}
              onClick={(event) => event.stopPropagation()}
            />
          ) : (
            <ClosedDropdownValue field={fieldValue} testId={testId} />
          )}
          {open && query ? (
            <button
              type="button"
              className="ui-standards-qdropdown__field-clear"
              aria-label="Clear search"
              data-testid={testId ? `${testId}-search-clear` : undefined}
              onMouseDown={(event) => event.preventDefault()}
              onClick={(event) => {
                event.stopPropagation();
                setQuery("");
                inputRef.current?.focus();
              }}
            >
              <X
                className="ui-standards-qdropdown__field-clear-icon"
                strokeWidth={SEARCH_CLEAR_GLOBAL_STROKE}
                aria-hidden
              />
            </button>
          ) : null}
          <ChevronDown
            className="ui-standards-qdropdown__chevron"
            aria-hidden
            onClick={(event) => {
              event.stopPropagation();
              if (!disabled) setOpen((current) => !current);
            }}
          />
        </div>
      ) : (
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
          {placeholderOnly ? null : <span className="ui-standards-qdropdown__label">{label}</span>}
          <ClosedDropdownValue field={fieldValue} testId={testId} />
          <ChevronDown className="ui-standards-qdropdown__chevron" aria-hidden />
        </button>
      )}
      {mounted ? (
        <div
          className="ui-standards-qdropdown__menu"
          id={menuId}
          role="listbox"
          aria-multiselectable={multiple || undefined}
          data-transition={transition}
          data-state={open ? "open" : "closed"}
          data-testid={testId ? `${testId}-menu` : undefined}
        >
          {multiple ? (
            <div className="ui-standards-qdropdown__search">
              <button
                type="button"
                className="ui-standards-qdropdown__search-toggle"
                aria-label="All"
                aria-pressed={allSelected}
                data-testid={testId ? `${testId}-select-all` : undefined}
                onClick={() => onChangeValues?.(allSelected ? [] : [...options])}
              >
                <span
                  className={"ui-standards-qdropdown__check" + (allSelected ? " ui-standards-qdropdown__check--on" : "")}
                  aria-hidden
                >
                  {allSelected ? (
                    <svg viewBox="0 0 24 24">
                      <path d="M5 12.5 9.2 17 19 7" />
                    </svg>
                  ) : null}
                </span>
                <span className="ui-standards-qdropdown__all-label">All</span>
              </button>
            </div>
          ) : null}
          {visible.map((option) => {
            const blocked = disabledOptions.includes(option);
            const optionColor = optionColors?.[option];
            return (
            <button
              key={option}
              type="button"
              role="option"
              aria-selected={selected.includes(option)}
              disabled={blocked}
              className={
                "ui-standards-qdropdown__option" +
                (selected.includes(option) ? " ui-standards-qdropdown__option--selected" : "")
              }
              data-option-color={optionColor ? "" : undefined}
              style={optionColor ? ({ "--qoption-color": optionColor } as CSSProperties) : undefined}
              data-testid={testId ? `${testId}-option-${option.replace(/\s+/g, "-").toLowerCase()}` : undefined}
              onClick={() => {
                if (!blocked) choose(option);
              }}
            >
              {multiple ? (
                <span
                  className={
                    "ui-standards-qdropdown__check" +
                    (selected.includes(option) ? " ui-standards-qdropdown__check--on" : "")
                  }
                  aria-hidden
                >
                  {selected.includes(option) ? (
                    <svg viewBox="0 0 24 24">
                      <path d="M5 12.5 9.2 17 19 7" />
                    </svg>
                  ) : null}
                </span>
              ) : null}
              <span className="ui-standards-qdropdown__option-label">{option}</span>
              {!multiple ? (
                selected.includes(option) ? (
                  <Check
                    className="ui-standards-qdropdown__selected-check"
                    strokeWidth={2.5}
                    aria-hidden
                    data-testid={testId ? `${testId}-selected-check` : undefined}
                  />
                ) : (
                  <span className="ui-standards-qdropdown__selected-check" aria-hidden />
                )
              ) : null}
            </button>
            );
          })}
          {searchable && visible.length === 0 ? <p className="ui-standards-qdropdown__empty">No matches</p> : null}
        </div>
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
      <DropdownRow title="Soft" testId="ui-standard-dropdown-soft" appearance="soft" />
      <DropdownRow title="Soft outline" testId="ui-standard-dropdown-soft-outline" appearance="soft-outline" />
      <DropdownRow title="Filled" testId="ui-standard-dropdown-filled" appearance="filled" />
      <DropdownRow title="Outlined" testId="ui-standard-dropdown-outlined" appearance="outlined" />
      <DropdownRow title="Standout" testId="ui-standard-dropdown-standout" appearance="standout" />
      <DropdownRow title="Label above" testId="ui-standard-dropdown-stacked" appearance="stacked" />
    </div>
  );
}

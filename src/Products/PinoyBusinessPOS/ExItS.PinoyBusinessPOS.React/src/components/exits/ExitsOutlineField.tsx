import { useEffect, useId, useState, type CSSProperties, type InputHTMLAttributes, type TextareaHTMLAttributes } from "react";
import { Check, ChevronDown, X } from "lucide-react";
import { INPUT_GLOBAL_DEFAULT_APPEARANCE } from "@/features/ui-standards/input-global-default";
import { SELECT_GLOBAL_DEFAULT_APPEARANCE } from "@/features/ui-standards/select-global-default";
import "@/features/ui-standards/exits-qfield.css";

const FIELD_ACCENT = "var(--exits-primary)";

type OutlineOption<T extends string> = {
  value: T;
  label: string;
  disabled?: boolean;
};

export function ExitsOutlineSelect<T extends string>({
  label,
  value,
  options,
  onChange,
  disabled = false,
  testId,
}: {
  label: string;
  value: T;
  options: ReadonlyArray<OutlineOption<T>>;
  onChange: (value: T) => void;
  disabled?: boolean;
  testId?: string;
}) {
  const [open, setOpen] = useState(false);
  const [mounted, setMounted] = useState(false);
  const rootId = useId();
  const menuId = `${rootId}-menu`;
  const selected = options.find((option) => option.value === value);
  const appearance: string = SELECT_GLOBAL_DEFAULT_APPEARANCE;
  const style = { "--qdropdown-color": FIELD_ACCENT } as CSSProperties;

  useEffect(() => {
    if (!open) return;
    function onPointerDown(event: PointerEvent) {
      const root = document.getElementById(rootId);
      if (!root?.contains(event.target as Node)) setOpen(false);
    }
    function onKey(event: KeyboardEvent) {
      if (event.key !== "Escape") return;
      event.preventDefault();
      event.stopPropagation();
      setOpen(false);
    }
    document.addEventListener("pointerdown", onPointerDown);
    document.addEventListener("keydown", onKey, true);
    return () => {
      document.removeEventListener("pointerdown", onPointerDown);
      document.removeEventListener("keydown", onKey, true);
    };
  }, [open, rootId]);

  useEffect(() => {
    if (open) {
      setMounted(true);
      return;
    }
    if (!mounted) return;
    const reduce =
      typeof window.matchMedia === "function" && window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    if (reduce) {
      setMounted(false);
      return;
    }
    const timeout = window.setTimeout(() => setMounted(false), 180);
    return () => window.clearTimeout(timeout);
  }, [open, mounted]);

  return (
    <div
      id={rootId}
      className={
        "ui-standards-qdropdown exits-locked-field" +
        (appearance !== "standard" ? ` ui-standards-qdropdown--${appearance}` : "") +
        (open ? " ui-standards-qdropdown--open" : "")
      }
      style={style}
      data-shape="standard"
      data-appearance={appearance}
    >
      <button
        type="button"
        className="ui-standards-qdropdown__field"
        aria-haspopup="listbox"
        aria-expanded={open}
        aria-controls={menuId}
        aria-label={label}
        disabled={disabled}
        data-testid={testId}
        onClick={() => {
          if (!disabled) setOpen((current) => !current);
        }}
      >
        <span className="ui-standards-qdropdown__label">{label}</span>
        <span className="ui-standards-qdropdown__value">{selected?.label ?? ""}</span>
        <ChevronDown className="ui-standards-qdropdown__chevron" aria-hidden />
      </button>
      {mounted ? (
        <div
          className="ui-standards-qdropdown__menu"
          id={menuId}
          role="listbox"
          data-transition="scale"
          data-state={open ? "open" : "closed"}
          data-testid={testId ? `${testId}-menu` : undefined}
        >
          {options.map((option) => {
            const chosen = option.value === value;
            return (
              <button
                key={option.value}
                type="button"
                role="option"
                aria-selected={chosen}
                disabled={option.disabled}
                className={
                  "ui-standards-qdropdown__option" +
                  (chosen ? " ui-standards-qdropdown__option--selected" : "")
                }
                data-testid={testId ? `${testId}-option-${option.value}` : undefined}
                onClick={() => {
                  if (option.disabled) return;
                  onChange(option.value);
                  setOpen(false);
                }}
              >
                <span className="ui-standards-qdropdown__option-label">{option.label}</span>
                {chosen ? (
                  <Check className="ui-standards-qdropdown__selected-check" strokeWidth={2.5} aria-hidden />
                ) : (
                  <span className="ui-standards-qdropdown__selected-check" aria-hidden />
                )}
              </button>
            );
          })}
        </div>
      ) : null}
    </div>
  );
}

export function ExitsOutlineInput({
  label,
  value,
  onChange,
  onFocus,
  onBlur,
  required = false,
  disabled = false,
  clearable = false,
  id,
  testId,
  autoFocus,
  prefix,
  ...props
}: Omit<InputHTMLAttributes<HTMLInputElement>, "onChange" | "value"> & {
  label: string;
  value: string;
  onChange: (value: string) => void;
  testId?: string;
  clearable?: boolean;
  /** Shown inside the field, before the typed value. */
  prefix?: string;
}) {
  const appearance = INPUT_GLOBAL_DEFAULT_APPEARANCE;
  const style = {
    "--qinput-accent": FIELD_ACCENT,
    "--qinput-fill": FIELD_ACCENT,
    "--qinput-ink": "#ffffff",
  } as CSSProperties;
  return (
    <div
      className={
        "ui-standards-qinput exits-locked-field" +
        ` ui-standards-qinput--${appearance}` +
        (prefix ? " ui-standards-qinput--prefix" : "") +
        (required ? " ui-standards-qinput--required" : "") +
        (clearable ? " ui-standards-qinput--clearable" : "") +
        (disabled ? " ui-standards-qinput--disabled" : "")
      }
      style={style}
      data-appearance={appearance}
      data-shape="standard"
    >
      <span className="ui-standards-qinput__box">
        {prefix ? (
          <span className="ui-standards-qinput__prefix" aria-hidden>
            {prefix}
          </span>
        ) : null}
        <input
          {...props}
          id={id}
          className="ui-standards-qinput__control"
          value={value}
          placeholder=" "
          disabled={disabled}
          required={required}
          aria-required={required || undefined}
          aria-label={label}
          data-testid={testId}
          autoFocus={autoFocus}
          onChange={(event) => onChange(event.target.value)}
          onFocus={onFocus}
          onBlur={onBlur}
        />
        <span className="ui-standards-qinput__label">
          {label}
          {required ? (
            <span className="ui-standards-qinput__required" aria-hidden>
              *
            </span>
          ) : null}
        </span>
        {clearable && value.trim() && !disabled ? (
          <button
            type="button"
            className="ui-standards-qinput__clear"
            aria-label={`Clear ${label}`}
            data-testid={testId ? `${testId}-clear` : undefined}
            onMouseDown={(event) => event.preventDefault()}
            onClick={() => onChange("")}
          >
            <X className="size-4" aria-hidden />
          </button>
        ) : null}
      </span>
    </div>
  );
}

export function ExitsOutlineTextarea({
  label,
  value,
  onChange,
  required = false,
  disabled = false,
  id,
  testId,
  rows = 3,
  ...props
}: Omit<TextareaHTMLAttributes<HTMLTextAreaElement>, "onChange" | "value"> & {
  label: string;
  value: string;
  onChange: (value: string) => void;
  testId?: string;
}) {
  const appearance = INPUT_GLOBAL_DEFAULT_APPEARANCE;
  const style = {
    "--qinput-accent": FIELD_ACCENT,
    "--qinput-fill": FIELD_ACCENT,
    "--qinput-ink": "#ffffff",
  } as CSSProperties;
  return (
    <div
      className={
        "ui-standards-qinput ui-standards-qinput--area exits-locked-field" +
        ` ui-standards-qinput--${appearance}` +
        (required ? " ui-standards-qinput--required" : "") +
        (disabled ? " ui-standards-qinput--disabled" : "")
      }
      style={style}
      data-appearance={appearance}
      data-shape="standard"
    >
      <span className="ui-standards-qinput__box">
        <textarea
          {...props}
          id={id}
          className="ui-standards-qinput__control ui-standards-qinput__control--area"
          value={value}
          placeholder=" "
          disabled={disabled}
          required={required}
          aria-required={required || undefined}
          aria-label={label}
          data-testid={testId}
          rows={rows}
          onChange={(event) => onChange(event.target.value)}
        />
        <span className="ui-standards-qinput__label">
          {label}
          {required ? (
            <span className="ui-standards-qinput__required" aria-hidden>
              *
            </span>
          ) : null}
        </span>
      </span>
    </div>
  );
}

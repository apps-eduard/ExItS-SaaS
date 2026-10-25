import {
  ChevronDown,
  Mail,
  Search,
  X,
  type LucideIcon,
} from "lucide-react";
import type { ReactNode } from "react";
import { DropdownMenu, MenuItem, useDismissibleOpen } from "@/components/ui/dropdown-menu";

export type OrgEhgInputGlassIntensity = "solid" | "soft" | "medium" | "strong" | "ultra";
export type OrgEhgInputGlassSize = "sm" | "md" | "lg";
export type OrgEhgInputGlassState =
  | "default"
  | "hover"
  | "focused"
  | "error"
  | "success"
  | "disabled"
  | "readonly";
export type OrgEhgInputGlassLabelPlace = "above" | "none";
export type OrgEhgInputGlassIconPlace = "none" | "left" | "right" | "top";
export type OrgEhgInputGlassKind = "text" | "email" | "password" | "search" | "textarea" | "select";
export type OrgEhgInputGlassBgAnimation = "none" | "shimmer" | "pulse" | "drift";
export type OrgEhgInputGlassFocusAnimation =
  | "none"
  | "ring-pulse"
  | "glow"
  | "breathe"
  | "thick-below";

export type OrgEhgInputGlassOption = {
  label: string;
  value: string;
};

export type OrgEhgInputGlassFieldProps = {
  intensity?: OrgEhgInputGlassIntensity;
  size?: OrgEhgInputGlassSize;
  state?: OrgEhgInputGlassState;
  kind?: OrgEhgInputGlassKind;
  label?: string;
  labelPlace?: OrgEhgInputGlassLabelPlace;
  iconPlace?: OrgEhgInputGlassIconPlace;
  icon?: LucideIcon;
  prefix?: string;
  suffix?: string;
  clearable?: boolean;
  dropdown?: boolean;
  options?: ReadonlyArray<OrgEhgInputGlassOption>;
  hint?: string;
  value?: string;
  placeholder?: string;
  onChange?: (value: string) => void;
  onClear?: () => void;
  testId?: string;
  className?: string;
  /** Portaled menu theme — menus leave the panel, so pass light/dark explicitly. */
  menuTheme?: "light" | "dark";
  bgAnimation?: OrgEhgInputGlassBgAnimation;
  focusAnimation?: OrgEhgInputGlassFocusAnimation;
};

const DEFAULT_OPTIONS: ReadonlyArray<OrgEhgInputGlassOption> = [
  { label: "Credit Card", value: "credit-card" },
  { label: "Cash", value: "cash" },
  { label: "Bank transfer", value: "bank-transfer" },
  { label: "E-wallet", value: "e-wallet" },
];

export function OrgEhgInputGlassField({
  intensity = "medium",
  size = "md",
  state = "default",
  kind = "text",
  label = "Label",
  labelPlace = "above",
  iconPlace = "none",
  icon: Icon,
  prefix,
  suffix,
  clearable = false,
  dropdown = false,
  options = DEFAULT_OPTIONS,
  hint,
  value = "",
  placeholder = "Enter text…",
  onChange,
  onClear,
  testId,
  className,
  menuTheme = "light",
  bgAnimation = "none",
  focusAnimation = "none",
}: OrgEhgInputGlassFieldProps) {
  const menu = useDismissibleOpen(false);
  const isSelect = kind === "select" || dropdown;
  const isTextarea = kind === "textarea";
  const disabled = state === "disabled";
  const readOnly = state === "readonly";
  const Mark = Icon ?? (kind === "email" ? Mail : kind === "search" ? Search : null);
  const showTopIcon = iconPlace === "top" && Mark;
  const showLeftIcon = iconPlace === "left" && Mark;
  const showRightIcon = iconPlace === "right" && Mark && !isSelect && !clearable;
  const showCaret = isSelect;
  const inputType =
    kind === "password" ? "password" : kind === "email" ? "email" : kind === "search" ? "search" : "text";
  const selected = options.find((option) => option.value === value || option.label === value);
  const displayValue = isSelect ? (selected?.label ?? value) : value;

  const rootClass = [
    "org-ehg-input-glass-field",
    `org-ehg-input-glass-field--${intensity}`,
    `org-ehg-input-glass-field--size-${size}`,
    state !== "default" ? `org-ehg-input-glass-field--state-${state}` : "",
    isSelect ? "org-ehg-input-glass-field--dropdown" : "",
    isTextarea ? "org-ehg-input-glass-field--textarea" : "",
    menu.open ? "org-ehg-input-glass-field--open" : "",
    bgAnimation !== "none" ? `org-ehg-input-glass-field--bg-${bgAnimation}` : "",
    focusAnimation !== "none" ? `org-ehg-input-glass-field--focus-${focusAnimation}` : "",
    className ?? "",
  ]
    .filter(Boolean)
    .join(" ");

  function handleChange(next: string) {
    onChange?.(next);
  }

  const focusBar =
    focusAnimation === "thick-below" ? (
      <span className="org-ehg-input-glass-field__focus-bar" aria-hidden />
    ) : null;

  function renderMarks(trailing: boolean) {
    if (!trailing) {
      return (
        <>
          {prefix ? <span className="org-ehg-input-glass-field__affix">{prefix}</span> : null}
          {showLeftIcon ? (
            <Mark className="org-ehg-input-glass-field__mark" aria-hidden strokeWidth={2.25} />
          ) : null}
        </>
      );
    }
    return (
      <>
        {clearable && displayValue && !disabled && !isSelect ? (
          <button
            type="button"
            className="org-ehg-input-glass-field__mark"
            aria-label="Clear"
            onClick={(event) => {
              event.stopPropagation();
              onClear?.();
              onChange?.("");
            }}
          >
            <X strokeWidth={2.5} aria-hidden />
          </button>
        ) : null}
        {showRightIcon ? (
          <Mark className="org-ehg-input-glass-field__mark" aria-hidden strokeWidth={2.25} />
        ) : null}
        {suffix ? <span className="org-ehg-input-glass-field__affix">{suffix}</span> : null}
        {showCaret ? (
          <ChevronDown
            className="org-ehg-input-glass-field__mark"
            aria-hidden
            strokeWidth={2.25}
            data-testid={testId ? `${testId}-caret` : undefined}
          />
        ) : null}
      </>
    );
  }

  let control: ReactNode;
  if (isSelect) {
    control = (
      <DropdownMenu
        open={menu.open && !disabled}
        onOpenChange={(next) => {
          if (!disabled) menu.setOpen(next);
        }}
        align="start"
        matchTriggerWidth
        menuClassName={
          "org-ehg-glass-menu" + (menuTheme === "dark" ? " org-ehg-glass-menu--dark" : "")
        }
        menuLabel={label || "Options"}
        className="org-ehg-input-glass-field__menu-host"
        trigger={({ id, expanded, controls, onClick, onKeyDown }) => (
          <button
            type="button"
            id={id}
            className="org-ehg-input-glass-field__control org-ehg-input-glass-field__control--trigger"
            aria-haspopup="menu"
            aria-expanded={expanded}
            aria-controls={controls}
            aria-invalid={state === "error" || undefined}
            disabled={disabled}
            onClick={onClick}
            onKeyDown={onKeyDown}
            data-testid={testId ? `${testId}-trigger` : undefined}
          >
            {focusBar}
            {renderMarks(false)}
            <span className="org-ehg-input-glass-field__input org-ehg-input-glass-field__value">
              {displayValue || <span className="org-ehg-input-glass-field__placeholder">{placeholder}</span>}
            </span>
            {renderMarks(true)}
          </button>
        )}
      >
        <div className="org-ehg-glass-menu__list" role="group" data-testid={testId ? `${testId}-menu` : undefined}>
          {options.map((option) => {
            const selectedOption =
              option.value === value || option.label === value || selected?.value === option.value;
            return (
              <MenuItem
                key={option.value}
                className={
                  "org-ehg-glass-menu__option" +
                  (selectedOption ? " org-ehg-glass-menu__option--selected" : "")
                }
                data-selected={selectedOption ? "true" : "false"}
                data-testid={testId ? `${testId}-option-${option.value}` : undefined}
                onSelect={() => {
                  handleChange(option.value);
                  menu.close();
                }}
              >
                {option.label}
              </MenuItem>
            );
          })}
        </div>
      </DropdownMenu>
    );
  } else if (isTextarea) {
    control = (
      <div className="org-ehg-input-glass-field__control">
        {focusBar}
        {renderMarks(false)}
        <textarea
          className="org-ehg-input-glass-field__input"
          value={value}
          placeholder={placeholder}
          disabled={disabled}
          readOnly={readOnly}
          rows={3}
          aria-invalid={state === "error" || undefined}
          onChange={(event) => handleChange(event.currentTarget.value)}
        />
        {renderMarks(true)}
      </div>
    );
  } else {
    control = (
      <div className="org-ehg-input-glass-field__control">
        {focusBar}
        {renderMarks(false)}
        <input
          className="org-ehg-input-glass-field__input"
          type={inputType}
          value={value}
          placeholder={placeholder}
          disabled={disabled}
          readOnly={readOnly}
          aria-invalid={state === "error" || undefined}
          onChange={(event) => handleChange(event.currentTarget.value)}
        />
        {renderMarks(true)}
      </div>
    );
  }

  return (
    <div className={rootClass} data-testid={testId}>
      {showTopIcon ? (
        <span className="org-ehg-input-glass-field__icon-top" aria-hidden>
          <Mark className="org-ehg-input-glass-field__mark" strokeWidth={2.25} />
        </span>
      ) : null}
      {labelPlace === "above" && label ? (
        <label className="org-ehg-input-glass-field__label">{label}</label>
      ) : null}
      {control}
      {hint ? <p className="org-ehg-input-glass-field__hint">{hint}</p> : null}
    </div>
  );
}
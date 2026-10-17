import type { CSSProperties } from "react";
import type { LucideIcon } from "lucide-react";

import { cn } from "@/lib/cn";

const TRUTHY_PATH = "M1.73,12.91 8.1,19.28 22.79,4.59";

export type ExitsCheckboxProps = {
  label: string;
  checked: boolean;
  onCheckedChange: (checked: boolean) => void;
  disabled?: boolean;
  /** Icon shown before the label (checked state). */
  icon?: LucideIcon;
  /** Icon shown before the label when unchecked. Defaults to `icon`. */
  uncheckedIcon?: LucideIcon;
  color?: string;
  dense?: boolean;
  className?: string;
  testId?: string;
};

export function ExitsCheckbox({
  label,
  checked,
  onCheckedChange,
  disabled = false,
  icon,
  uncheckedIcon,
  color = "var(--exits-primary)",
  dense = false,
  className,
  testId,
}: ExitsCheckboxProps) {
  const LabelIcon = checked ? icon : (uncheckedIcon ?? icon);
  const style = { "--qcheckbox-color": color } as CSSProperties;

  function toggle() {
    if (!disabled) onCheckedChange(!checked);
  }

  return (
    <div
      role="checkbox"
      aria-checked={checked}
      aria-disabled={disabled || undefined}
      aria-label={label}
      tabIndex={disabled ? -1 : 0}
      className={cn(
        "ui-standards-qcheckbox exits-checkbox",
        dense && "ui-standards-qcheckbox--dense",
        disabled && "ui-standards-qcheckbox--disabled",
        LabelIcon && "exits-checkbox--icon",
        className,
      )}
      style={style}
      data-testid={testId}
      onClick={toggle}
      onKeyDown={(event) => {
        if (event.key === "Enter" || event.key === " ") {
          event.preventDefault();
          toggle();
        }
      }}
    >
      <span
        className={cn(
          "ui-standards-qcheckbox__inner",
          checked && "ui-standards-qcheckbox__inner--truthy",
        )}
        aria-hidden
      >
        <span className="ui-standards-qcheckbox__bg">
          <svg className="ui-standards-qcheckbox__svg" viewBox="0 0 24 24">
            <path className="ui-standards-qcheckbox__truthy" d={TRUTHY_PATH} />
          </svg>
        </span>
      </span>
      <span className="ui-standards-qcheckbox__label exits-checkbox__label">
        {LabelIcon ? (
          <LabelIcon
            className={cn("exits-checkbox__icon", checked && "exits-checkbox__icon--on")}
            aria-hidden
          />
        ) : null}
        {label}
      </span>
    </div>
  );
}

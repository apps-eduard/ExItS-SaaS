import type { ButtonHTMLAttributes } from "react";
import { cn } from "@/lib/cn";

export type SwitchProps = Omit<
  ButtonHTMLAttributes<HTMLButtonElement>,
  "onChange" | "role" | "aria-checked" | "children"
> & {
  checked: boolean;
  onCheckedChange: (checked: boolean) => void;
};

/**
 * Compact accessible settings switch (theme + density aware).
 * Not an oversized iOS-style control.
 */
export function Switch({
  checked,
  onCheckedChange,
  className,
  disabled,
  id,
  ...props
}: SwitchProps) {
  return (
    <button
      {...props}
      id={id}
      type="button"
      role="switch"
      aria-checked={checked}
      disabled={disabled}
      className={cn("exits-switch", checked && "exits-switch--on", className)}
      onClick={() => {
        if (disabled) {
          return;
        }
        onCheckedChange(!checked);
      }}
    >
      <span className="exits-switch__thumb" aria-hidden />
    </button>
  );
}

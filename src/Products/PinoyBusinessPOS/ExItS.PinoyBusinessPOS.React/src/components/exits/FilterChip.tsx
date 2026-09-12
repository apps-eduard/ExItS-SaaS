import type { ButtonHTMLAttributes, ReactNode } from "react";
import { Check } from "lucide-react";
import { cn } from "@/lib/cn";
import { filterChipVariants, type FilterChipShape } from "@/components/exits/chip-variants";

export type FilterChipProps = Omit<ButtonHTMLAttributes<HTMLButtonElement>, "children"> & {
  children: ReactNode;
  selected?: boolean;
  /**
   * Default `auto` follows Preferences Control Shape via `--exits-control-radius`.
   * Explicit `pill` / `soft` / `square` win over the global preference.
   */
  shape?: FilterChipShape;
  /** Show a check when selected (multi-select clarity). */
  showCheck?: boolean;
  icon?: ReactNode;
};

/**
 * Interactive filter / selection chip (APPROVED / LOCKED).
 * Button semantics — not a StatusChip. Default shape: auto (global Control Shape).
 */
export function FilterChip({
  children,
  className,
  selected = false,
  shape = "auto",
  showCheck = false,
  icon,
  type = "button",
  disabled,
  ...props
}: FilterChipProps) {
  return (
    <button
      type={type}
      disabled={disabled}
      aria-pressed={selected}
      className={cn(filterChipVariants({ selected, shape }), className)}
      data-selected={selected ? "true" : "false"}
      data-shape={shape}
      {...props}
    >
      {showCheck && selected ? (
        <Check
          className="size-[0.875rem] shrink-0 transition-[opacity,transform] duration-[var(--exits-motion-fast)] motion-reduce:transition-none"
          aria-hidden
        />
      ) : null}
      {icon ? (
        <span className="inline-flex size-[0.875rem] shrink-0 items-center justify-center [&_svg]:size-full" aria-hidden>
          {icon}
        </span>
      ) : null}
      <span className="min-w-0 truncate">{children}</span>
    </button>
  );
}

import { Check, ChevronDown } from "lucide-react";
import type { ReactNode } from "react";
import { DropdownMenu, MenuItem } from "@/components/ui/dropdown-menu";
import { cn } from "@/lib/cn";

export type SettingsOption<T extends string> = {
  value: T;
  label: string;
  icon?: ReactNode;
};

type SettingsSelectProps<T extends string> = {
  label: string;
  value: T;
  options: SettingsOption<T>[];
  onChange: (value: T) => void;
  open: boolean;
  onOpenChange: (open: boolean) => void;
};

/** Compact settings-row select (not segmented buttons). */
export function SettingsSelect<T extends string>({
  label,
  value,
  options,
  onChange,
  open,
  onOpenChange,
}: SettingsSelectProps<T>) {
  const selected = options.find((option) => option.value === value) ?? options[0];

  return (
    <div className="flex min-w-0 items-center justify-between gap-4 py-3">
      <span className="shrink-0 text-[length:var(--exits-text-sm)] font-semibold text-foreground">
        {label}
      </span>
      <DropdownMenu
        align="end"
        open={open}
        onOpenChange={onOpenChange}
        menuLabel={label}
        trigger={({ id, expanded, controls, onClick, onKeyDown }) => (
          <button
            id={id}
            type="button"
            className={cn(
              "inline-flex min-h-[2.5rem] max-w-[12rem] items-center justify-between gap-2 rounded-[var(--exits-radius-md)] border border-border bg-background px-3 text-[length:var(--exits-text-sm)] font-semibold text-foreground transition-colors duration-[var(--exits-motion-fast)] hover:bg-[var(--exits-surface-muted)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
              expanded && "bg-[var(--exits-surface-muted)]",
            )}
            aria-haspopup="menu"
            aria-expanded={expanded}
            aria-controls={controls}
            aria-label={`${label}: ${selected?.label ?? ""}`}
            onClick={onClick}
            onKeyDown={onKeyDown}
          >
            <span className="flex min-w-0 items-center gap-1.5 truncate">
              {selected?.icon}
              <span className="truncate">{selected?.label}</span>
            </span>
            <ChevronDown className="size-3.5 shrink-0 text-muted" aria-hidden="true" />
          </button>
        )}
      >
        {options.map((option) => {
          const selectedOption = option.value === value;
          return (
            <MenuItem
              key={option.value}
              onSelect={() => {
                onChange(option.value);
                onOpenChange(false);
              }}
            >
              <span className="flex min-w-0 flex-1 items-center gap-2 truncate">
                {option.icon}
                <span className="truncate">{option.label}</span>
              </span>
              {selectedOption ? (
                <Check className="size-4 shrink-0 text-primary" aria-hidden="true" />
              ) : (
                <span className="size-4 shrink-0" aria-hidden="true" />
              )}
              {selectedOption ? <span className="sr-only">selected</span> : null}
            </MenuItem>
          );
        })}
      </DropdownMenu>
    </div>
  );
}

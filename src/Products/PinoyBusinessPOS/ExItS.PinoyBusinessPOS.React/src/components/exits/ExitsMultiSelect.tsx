import { useMemo, useState } from "react";
import { Check, ChevronDown } from "lucide-react";
import {
  EXITS_SELECT_TRIGGER_CLASS,
  type ExitsSelectOption,
} from "@/components/exits/ExitsSelect";
import { DropdownMenu, useDismissibleOpen } from "@/components/ui/dropdown-menu";
import { cn } from "@/lib/cn";

export type ExitsMultiSelectProps<T extends string = string> = {
  value: ReadonlyArray<T>;
  options: ReadonlyArray<ExitsSelectOption<T>>;
  onChange: (value: T[]) => void;
  disabled?: boolean;
  invalid?: boolean;
  id?: string;
  className?: string;
  triggerClassName?: string;
  menuLabel?: string;
  placeholder?: string;
  searchable?: boolean;
  searchPlaceholder?: string;
  selectAllLabel?: string;
  clearAllLabel?: string;
  selectedCountLabel?: (count: number) => string;
  testId?: string;
  "aria-label"?: string;
  "aria-labelledby"?: string;
};

/**
 * Themed multi-select listbox — same closed field as ExitsSelect.
 * Reuses locked receive-stock menu chrome (no new globals.css).
 */
export function ExitsMultiSelect<T extends string>({
  value,
  options,
  onChange,
  disabled = false,
  invalid = false,
  id,
  className,
  triggerClassName,
  menuLabel,
  placeholder = "Select…",
  searchable = false,
  searchPlaceholder = "Search…",
  selectAllLabel = "Select all",
  clearAllLabel = "Clear",
  selectedCountLabel = (count) => `${count} selected`,
  testId,
  "aria-label": ariaLabel,
  "aria-labelledby": ariaLabelledBy,
}: ExitsMultiSelectProps<T>) {
  const menu = useDismissibleOpen(false);
  const [query, setQuery] = useState("");
  const selected = useMemo(() => new Set(value), [value]);

  const filtered = useMemo(() => {
    const q = query.trim().toLocaleLowerCase();
    if (!q) {
      return options;
    }
    return options.filter((option) => option.label.toLocaleLowerCase().includes(q));
  }, [options, query]);

  const allSelected =
    options.length > 0 && options.every((option) => selected.has(option.value));
  const noneSelected = value.length === 0;

  const triggerText =
    value.length === 0
      ? placeholder
      : value.length === 1
        ? (options.find((o) => o.value === value[0])?.label ?? value[0])
        : selectedCountLabel(value.length);

  function toggle(next: T) {
    if (selected.has(next)) {
      onChange(value.filter((item) => item !== next));
      return;
    }
    onChange([...value, next]);
  }

  return (
    <DropdownMenu
      open={menu.open && !disabled}
      onOpenChange={(next) => {
        if (!disabled) {
          menu.setOpen(next);
          if (!next) {
            setQuery("");
          }
        }
      }}
      align="start"
      matchTriggerWidth
      menuZIndex={90}
      menuLabel={menuLabel ?? ariaLabel}
      className={cn("!block w-full", className)}
      menuClassName="receive-stock-category-menu"
      trigger={({ id: triggerId, expanded, controls, onClick, onKeyDown }) => (
        <button
          type="button"
          id={id ?? triggerId}
          className={cn(
            EXITS_SELECT_TRIGGER_CLASS,
            invalid && "border-destructive",
            triggerClassName,
          )}
          aria-haspopup="menu"
          aria-expanded={expanded}
          aria-controls={controls}
          aria-invalid={invalid || undefined}
          aria-label={ariaLabel}
          aria-labelledby={ariaLabelledBy}
          disabled={disabled}
          onClick={onClick}
          onKeyDown={onKeyDown}
          data-testid={testId}
        >
          <span
            className={cn(
              "min-w-0 flex-1 truncate text-start",
              noneSelected && "text-muted",
            )}
          >
            {triggerText}
          </span>
          <ChevronDown className="size-4 shrink-0 opacity-70" aria-hidden />
        </button>
      )}
    >
      {searchable ? (
        <div className="border-b border-border p-1.5">
          <input
            type="search"
            className="exits-input w-full"
            style={{ paddingInlineStart: "0.75rem" }}
            value={query}
            placeholder={searchPlaceholder}
            aria-label={searchPlaceholder}
            data-testid={testId ? `${testId}-search` : undefined}
            onChange={(event) => setQuery(event.target.value)}
            onClick={(event) => event.stopPropagation()}
            onKeyDown={(event) => event.stopPropagation()}
          />
        </div>
      ) : null}
      <div className="receive-stock-category-menu__bulk" role="group">
        <button
          type="button"
          role="menuitem"
          className="receive-stock-category-menu__bulk-action"
          disabled={allSelected}
          data-testid={testId ? `${testId}-select-all` : undefined}
          onClick={(event) => {
            event.preventDefault();
            event.stopPropagation();
            onChange(options.filter((o) => !o.disabled).map((o) => o.value));
          }}
        >
          {selectAllLabel}
        </button>
        <button
          type="button"
          role="menuitem"
          className="receive-stock-category-menu__bulk-action"
          disabled={noneSelected}
          data-testid={testId ? `${testId}-clear` : undefined}
          onClick={(event) => {
            event.preventDefault();
            event.stopPropagation();
            onChange([]);
          }}
        >
          {clearAllLabel}
        </button>
      </div>
      <div className="receive-stock-category-menu__list" role="group">
        {filtered.length === 0 ? (
          <p className="m-0 px-2 py-2 text-center text-[length:var(--exits-text-sm)] text-muted">
            No matches
          </p>
        ) : (
          filtered.map((option) => {
            const checked = selected.has(option.value);
            return (
              <button
                key={option.value}
                type="button"
                role="menuitem"
                aria-checked={checked}
                disabled={option.disabled}
                className={cn(
                  "receive-stock-category-menu__item",
                  checked && "receive-stock-category-menu__item--checked",
                )}
                data-testid={testId ? `${testId}-option-${option.value}` : undefined}
                onClick={(event) => {
                  event.preventDefault();
                  event.stopPropagation();
                  if (!option.disabled) {
                    toggle(option.value);
                  }
                }}
              >
                <span
                  className={cn(
                    "receive-stock-category-menu__check",
                    checked && "receive-stock-category-menu__check--on",
                  )}
                  aria-hidden
                >
                  {checked ? <Check className="size-3.5" strokeWidth={2.5} /> : null}
                </span>
                <span className="min-w-0 flex-1 truncate text-start">{option.label}</span>
              </button>
            );
          })
        )}
      </div>
    </DropdownMenu>
  );
}

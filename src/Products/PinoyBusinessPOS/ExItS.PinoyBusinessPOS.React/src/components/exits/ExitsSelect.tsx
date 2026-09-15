import { useMemo, useState } from "react";
import { Check, ChevronDown } from "lucide-react";
import { DropdownMenu, useDismissibleOpen } from "@/components/ui/dropdown-menu";
import { cn } from "@/lib/cn";

export type ExitsSelectOption<T extends string = string> = {
  value: T;
  label: string;
  disabled?: boolean;
  /** Optional secondary count (e.g. category product totals). `0` renders; omit when unknown. */
  count?: number | null;
};

export type ExitsSelectProps<T extends string = string> = {
  value: T;
  options: ReadonlyArray<ExitsSelectOption<T>>;
  onChange: (value: T) => void;
  disabled?: boolean;
  invalid?: boolean;
  id?: string;
  className?: string;
  triggerClassName?: string;
  menuLabel?: string;
  /** Show in-menu search for longer option lists. */
  searchable?: boolean;
  searchPlaceholder?: string;
  emptyLabel?: string;
  testId?: string;
  "aria-label"?: string;
  "aria-labelledby"?: string;
};

/** Shared closed-field classes — reuses locked receive-stock trigger (kills native caret). */
export const EXITS_SELECT_TRIGGER_CLASS =
  "exits-select receive-stock-category-trigger w-full";

/**
 * Themed single-select listbox (replaces native &lt;select&gt; popup OS chrome).
 * Closed field matches `.exits-select`; open menu reuses receive-stock menu chrome.
 */
export function ExitsSelect<T extends string>({
  value,
  options,
  onChange,
  disabled = false,
  invalid = false,
  id,
  className,
  triggerClassName,
  menuLabel,
  searchable = false,
  searchPlaceholder = "Search…",
  emptyLabel = "No matches",
  testId,
  "aria-label": ariaLabel,
  "aria-labelledby": ariaLabelledBy,
}: ExitsSelectProps<T>) {
  const menu = useDismissibleOpen(false);
  const [query, setQuery] = useState("");
  const selected = options.find((option) => option.value === value);
  const label = selected?.label ?? value;

  const filtered = useMemo(() => {
    const q = query.trim().toLocaleLowerCase();
    if (!q) {
      return options;
    }
    return options.filter((option) => option.label.toLocaleLowerCase().includes(q));
  }, [options, query]);

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
          <span className="min-w-0 flex-1 truncate text-start">{label}</span>
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
      <div className="receive-stock-category-menu__list" role="group">
        {filtered.length === 0 ? (
          <p className="m-0 px-2 py-2 text-center text-[length:var(--exits-text-sm)] text-muted">
            {emptyLabel}
          </p>
        ) : (
          filtered.map((option) => {
            const checked = option.value === value;
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
                  if (option.disabled) {
                    return;
                  }
                  onChange(option.value);
                  menu.setOpen(false);
                }}
              >
                <span className="min-w-0 flex-1 truncate text-start">{option.label}</span>
                {checked ? (
                  <Check className="size-4 shrink-0 text-[var(--exits-primary)]" strokeWidth={2.5} aria-hidden />
                ) : (
                  <span className="size-4 shrink-0" aria-hidden />
                )}
              </button>
            );
          })
        )}
      </div>
    </DropdownMenu>
  );
}

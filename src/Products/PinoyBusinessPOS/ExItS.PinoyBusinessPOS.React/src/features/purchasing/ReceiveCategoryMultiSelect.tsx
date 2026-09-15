import { useEffect, useMemo, useState } from "react";
import { Check, ChevronDown } from "lucide-react";
import { CountBadge } from "@/components/exits/CountChip";
import { DropdownMenu, useDismissibleOpen } from "@/components/ui/dropdown-menu";
import { cn } from "@/lib/cn";

export type CategoryOption = {
  categoryId: string;
  name: string;
  /** Product count for this category. `0` still renders normally. */
  count?: number;
};

type ReceiveCategoryMultiSelectProps = {
  categories: ReadonlyArray<CategoryOption>;
  selectedIds: ReadonlyArray<string>;
  onChange: (nextIds: string[]) => void;
  label: string;
  placeholder: string;
  selectedCountLabel: (count: number) => string;
  selectAllLabel: string;
  deselectAllLabel: string;
  searchPlaceholder?: string;
  emptySearchLabel?: string;
  testId?: string;
};

/**
 * Compact multi-select for Receive Stock categories.
 * Uses existing DropdownMenu — no new library.
 */
export function ReceiveCategoryMultiSelect({
  categories,
  selectedIds,
  onChange,
  label,
  placeholder,
  selectedCountLabel,
  selectAllLabel,
  deselectAllLabel,
  searchPlaceholder = "Search categories",
  emptySearchLabel = "No matches",
  testId = "direct-category-multiselect",
}: ReceiveCategoryMultiSelectProps) {
  const menu = useDismissibleOpen(false);
  const [query, setQuery] = useState("");
  const selected = new Set(selectedIds);
  const allSelected =
    categories.length > 0 && categories.every((category) => selected.has(category.categoryId));
  const noneSelected = selectedIds.length === 0;

  useEffect(() => {
    if (!menu.open) {
      setQuery("");
    }
  }, [menu.open]);

  const filtered = useMemo(() => {
    const q = query.trim().toLocaleLowerCase();
    if (!q) {
      return categories;
    }
    return categories.filter((category) => category.name.toLocaleLowerCase().includes(q));
  }, [categories, query]);

  function toggle(categoryId: string) {
    if (selected.has(categoryId)) {
      onChange(selectedIds.filter((id) => id !== categoryId));
      return;
    }
    onChange([...selectedIds, categoryId]);
  }

  function selectAll() {
    onChange(categories.map((category) => category.categoryId));
  }

  function deselectAll() {
    onChange([]);
  }

  const triggerText =
    selectedIds.length === 0 ? placeholder : selectedCountLabel(selectedIds.length);

  return (
    <label className="receive-stock-field" data-testid={testId}>
      <span className="receive-stock-field__label">{label}</span>
      <DropdownMenu
        open={menu.open}
        onOpenChange={menu.setOpen}
        align="start"
        menuLabel={label}
        className="w-full"
        menuClassName="receive-stock-category-menu"
        trigger={({ id, expanded, controls, onClick, onKeyDown }) => (
          <button
            type="button"
            id={id}
            className={cn(
              "exits-select catalog-form-select receive-stock-category-trigger w-full",
            )}
            aria-haspopup="menu"
            aria-expanded={expanded}
            aria-controls={controls}
            onClick={onClick}
            onKeyDown={onKeyDown}
            data-testid={`${testId}-trigger`}
          >
            <span className="min-w-0 truncate">{triggerText}</span>
            <ChevronDown className="size-4 shrink-0 opacity-70" aria-hidden />
          </button>
        )}
      >
        <div className="receive-stock-category-menu__search border-b border-border p-1.5">
          <input
            type="search"
            className="exits-input w-full"
            style={{ paddingInlineStart: "0.75rem" }}
            value={query}
            placeholder={searchPlaceholder}
            aria-label={searchPlaceholder}
            data-testid={`${testId}-search`}
            onChange={(event) => setQuery(event.target.value)}
            onClick={(event) => event.stopPropagation()}
            onKeyDown={(event) => event.stopPropagation()}
          />
        </div>
        <div className="receive-stock-category-menu__bulk" role="group">
          <button
            type="button"
            role="menuitem"
            className="receive-stock-category-menu__bulk-action"
            disabled={allSelected}
            onClick={(e) => {
              e.preventDefault();
              e.stopPropagation();
              selectAll();
            }}
            data-testid={`${testId}-select-all`}
          >
            {selectAllLabel}
          </button>
          <button
            type="button"
            role="menuitem"
            className="receive-stock-category-menu__bulk-action"
            disabled={noneSelected}
            onClick={(e) => {
              e.preventDefault();
              e.stopPropagation();
              deselectAll();
            }}
            data-testid={`${testId}-deselect-all`}
          >
            {deselectAllLabel}
          </button>
        </div>
        <div className="receive-stock-category-menu__list" role="group">
          {filtered.length === 0 ? (
            <p className="m-0 px-2 py-2 text-center text-[length:var(--exits-text-sm)] text-muted">
              {emptySearchLabel}
            </p>
          ) : (
            filtered.map((category) => {
              const checked = selected.has(category.categoryId);
              return (
                <button
                  key={category.categoryId}
                  type="button"
                  role="menuitem"
                  aria-checked={checked}
                  className={cn(
                    "receive-stock-category-menu__item",
                    checked && "receive-stock-category-menu__item--checked",
                  )}
                  onClick={(e) => {
                    e.preventDefault();
                    e.stopPropagation();
                    toggle(category.categoryId);
                  }}
                  data-testid={`direct-category-option-${category.categoryId}`}
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
                  <span className="min-w-0 flex-1 truncate text-start">{category.name}</span>
                  {category.count != null ? (
                    <CountBadge
                      count={category.count}
                      tone={checked ? "primary" : "neutral"}
                    />
                  ) : null}
                </button>
              );
            })
          )}
        </div>
      </DropdownMenu>
    </label>
  );
}

import { Check, ChevronDown } from "lucide-react";
import { DropdownMenu, useDismissibleOpen } from "@/components/ui/dropdown-menu";
import { cn } from "@/lib/cn";

export type CategoryOption = {
  categoryId: string;
  name: string;
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
  testId = "direct-category-multiselect",
}: ReceiveCategoryMultiSelectProps) {
  const menu = useDismissibleOpen(false);
  const selected = new Set(selectedIds);
  const allSelected =
    categories.length > 0 && categories.every((category) => selected.has(category.categoryId));
  const noneSelected = selectedIds.length === 0;

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
          {categories.map((category) => {
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
                <span className="min-w-0 truncate">{category.name}</span>
              </button>
            );
          })}
        </div>
      </DropdownMenu>
    </label>
  );
}

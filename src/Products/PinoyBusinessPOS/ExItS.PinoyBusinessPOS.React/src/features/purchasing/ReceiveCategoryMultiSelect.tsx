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
  clearLabel: string;
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
  clearLabel,
  testId = "direct-category-multiselect",
}: ReceiveCategoryMultiSelectProps) {
  const menu = useDismissibleOpen(false);
  const selected = new Set(selectedIds);

  function toggle(categoryId: string) {
    if (selected.has(categoryId)) {
      onChange(selectedIds.filter((id) => id !== categoryId));
      return;
    }
    onChange([...selectedIds, categoryId]);
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
        {selectedIds.length > 0 ? (
          <button
            type="button"
            role="menuitem"
            className="receive-stock-category-menu__clear"
            onClick={() => {
              onChange([]);
              menu.close();
            }}
            data-testid={`${testId}-clear`}
          >
            {clearLabel}
          </button>
        ) : null}
      </DropdownMenu>
    </label>
  );
}

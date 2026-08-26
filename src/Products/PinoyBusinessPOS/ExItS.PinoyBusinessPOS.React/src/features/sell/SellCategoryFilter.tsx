import { cn } from "@/lib/cn";
import { resolveSellCategoryIcon } from "@/features/sell/sell-category-icon";

export type SellCategoryOption = {
  categoryId: string;
  name: string;
  /** Optional count when the caller already knows it — never invent from incomplete pages. */
  productCount?: number;
};

type SellCategoryFilterProps = {
  categories: SellCategoryOption[];
  activeCategoryId: string;
  allLabel: string;
  listLabel: string;
  onSelect: (categoryId: string) => void;
  /** Optional total for the All tile when known. */
  allProductCount?: number;
};

export function SellCategoryFilter({
  categories,
  activeCategoryId,
  allLabel,
  listLabel,
  onSelect,
}: SellCategoryFilterProps) {
  const activeName =
    activeCategoryId === "all"
      ? allLabel
      : (categories.find((category) => category.categoryId === activeCategoryId)?.name ?? allLabel);

  return (
    <nav className="sell-categories sell-categories--chips min-w-0" aria-label={listLabel}>
      <p className="sr-only" data-testid="sell-category-active" aria-live="polite">
        {activeName}
      </p>
      <div
        data-testid="sell-categories"
        className="sell-categories-track flex gap-1.5 overflow-x-auto overscroll-x-contain pb-0.5"
        role="list"
      >
        <CategoryChip
          pressed={activeCategoryId === "all"}
          onClick={() => onSelect("all")}
          label={allLabel}
          testId="sell-category-all"
          isAll
        />
        {categories.map((category) => (
          <CategoryChip
            key={category.categoryId}
            testId={`sell-category-${category.categoryId}`}
            pressed={activeCategoryId === category.categoryId}
            onClick={() => onSelect(category.categoryId)}
            label={category.name}
          />
        ))}
      </div>
    </nav>
  );
}

function CategoryChip({
  label,
  pressed,
  onClick,
  testId,
  isAll = false,
}: {
  label: string;
  pressed: boolean;
  onClick: () => void;
  testId?: string;
  isAll?: boolean;
}) {
  const Icon = resolveSellCategoryIcon(isAll ? "all" : label);

  return (
    <button
      type="button"
      role="listitem"
      data-testid={testId}
      className={cn("sell-category-chip", pressed && "sell-category-chip--active")}
      aria-pressed={pressed}
      onClick={onClick}
    >
      <Icon className="sell-category-chip__icon" aria-hidden strokeWidth={1.75} />
      <span className="sell-category-chip__label">{label}</span>
    </button>
  );
}

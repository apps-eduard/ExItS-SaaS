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
  allProductCount,
}: SellCategoryFilterProps) {
  const activeName =
    activeCategoryId === "all"
      ? allLabel
      : (categories.find((category) => category.categoryId === activeCategoryId)?.name ?? allLabel);

  return (
    <div className="flex min-w-0 flex-col gap-1.5">
      <div className="flex min-w-0 items-baseline justify-between gap-2 px-0.5">
        <p className="m-0 text-[length:var(--exits-text-xs)] font-semibold tracking-wide uppercase text-muted">
          {listLabel}
        </p>
        <p
          className="m-0 min-w-0 truncate text-[length:var(--exits-text-xs)] text-muted"
          data-testid="sell-category-active"
          aria-live="polite"
        >
          {activeName}
        </p>
      </div>

      <div className="sell-categories relative min-w-0">
        <div
          data-testid="sell-categories"
          className="sell-categories-track flex gap-2 overflow-x-auto overscroll-x-contain pb-1"
          role="list"
          aria-label={listLabel}
        >
          <CategoryTile
            pressed={activeCategoryId === "all"}
            onClick={() => onSelect("all")}
            label={allLabel}
            productCount={allProductCount}
            isAll
          />
          {categories.map((category) => (
            <CategoryTile
              key={category.categoryId}
              testId={`sell-category-${category.categoryId}`}
              pressed={activeCategoryId === category.categoryId}
              onClick={() => onSelect(category.categoryId)}
              label={category.name}
              productCount={category.productCount}
            />
          ))}
        </div>
      </div>
    </div>
  );
}

function CategoryTile({
  label,
  pressed,
  onClick,
  testId,
  productCount,
  isAll = false,
}: {
  label: string;
  pressed: boolean;
  onClick: () => void;
  testId?: string;
  productCount?: number;
  isAll?: boolean;
}) {
  const Icon = resolveSellCategoryIcon(isAll ? "all" : label);
  const countLabel =
    typeof productCount === "number" && Number.isFinite(productCount)
      ? productCount === 1
        ? "1 item"
        : `${productCount} items`
      : null;

  return (
    <button
      type="button"
      role="listitem"
      data-testid={testId}
      className={cn("sell-category-tile", pressed && "sell-category-tile--active")}
      aria-pressed={pressed}
      onClick={onClick}
    >
      <span className="sell-category-tile__icon" aria-hidden>
        <Icon className="size-6" strokeWidth={1.75} />
      </span>
      <span className="sell-category-tile__name">{label}</span>
      {countLabel ? <span className="sell-category-tile__count">{countLabel}</span> : null}
    </button>
  );
}

import { cn } from "@/lib/cn";

export type SellCategoryOption = {
  categoryId: string;
  name: string;
};

type SellCategoryFilterProps = {
  categories: SellCategoryOption[];
  activeCategoryId: string;
  allLabel: string;
  listLabel: string;
  onSelect: (categoryId: string) => void;
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
          <CategoryChip
            pressed={activeCategoryId === "all"}
            onClick={() => onSelect("all")}
            label={allLabel}
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
      </div>
    </div>
  );
}

function CategoryChip({
  label,
  pressed,
  onClick,
  testId,
}: {
  label: string;
  pressed: boolean;
  onClick: () => void;
  testId?: string;
}) {
  return (
    <button
      type="button"
      role="listitem"
      data-testid={testId}
      className={cn(
        "sell-category-chip shrink-0 snap-start whitespace-nowrap rounded-[var(--exits-radius-md)] border px-3 py-2 text-[length:var(--exits-text-sm)] font-semibold transition-[background-color,border-color,color] duration-[var(--exits-motion-fast)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
        pressed
          ? "border-primary bg-primary text-primary-foreground"
          : "border-border bg-surface text-foreground hover:bg-[var(--exits-surface-muted)]",
      )}
      aria-pressed={pressed}
      onClick={onClick}
    >
      {label}
    </button>
  );
}

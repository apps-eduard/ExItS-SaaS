import { Filter, ArrowUpDown } from "lucide-react";
import type { ButtonHTMLAttributes, ReactNode } from "react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/cn";

/** Shared pill classes for list-toolbar filter controls (Filters button, Sort, active chips). */
export const exitsFilterPillClassName = "exits-filter-pill";

export function FilterButton({
  activeCount = 0,
  className,
  children = "Filters",
  ...props
}: ButtonHTMLAttributes<HTMLButtonElement> & { activeCount?: number }) {
  return (
    <Button
      type="button"
      variant="ghost"
      className={cn(exitsFilterPillClassName, "exits-filter-pill--button", className)}
      {...props}
    >
      <Filter className="size-4 shrink-0" aria-hidden />
      <span>{children}</span>
      {activeCount > 0 ? (
        <span className="exits-filter-pill__badge" aria-hidden>
          {activeCount}
        </span>
      ) : null}
    </Button>
  );
}

export type FilterChipItem = {
  id: string;
  label: string;
};

export function FilterChips({
  items,
  onRemove,
  listLabel = "Active filters",
}: {
  items: FilterChipItem[];
  onRemove: (id: string) => void;
  listLabel?: string;
}) {
  if (items.length === 0) {
    return null;
  }

  return (
    <div className="exits-active-filters min-w-0" aria-label={listLabel}>
      <div
        className="exits-active-filters__track"
        role="list"
        aria-label={listLabel}
        data-testid="active-filter-chips"
      >
        {items.map((item) => (
          <button
            key={item.id}
            type="button"
            role="listitem"
            className={exitsFilterPillClassName}
            onClick={() => onRemove(item.id)}
            aria-label={`Remove filter ${item.label}`}
          >
            <span className="whitespace-nowrap">{item.label}</span>
            <span aria-hidden>×</span>
          </button>
        ))}
      </div>
    </div>
  );
}

export function SortButton({
  className,
  children = "Sort",
  ...props
}: ButtonHTMLAttributes<HTMLButtonElement>) {
  return (
    <Button
      type="button"
      variant="ghost"
      className={cn(exitsFilterPillClassName, "exits-filter-pill--button", className)}
      {...props}
    >
      <ArrowUpDown className="size-4 shrink-0" aria-hidden />
      <span>{children}</span>
    </Button>
  );
}

export function ListToolbar({
  search,
  filters,
  sort,
  primaryAction,
  chips,
  className,
}: {
  search: ReactNode;
  filters?: ReactNode;
  sort?: ReactNode;
  primaryAction?: ReactNode;
  chips?: ReactNode;
  className?: string;
}) {
  return (
    <div className={cn("flex min-w-0 flex-col gap-2", className)} data-testid="list-toolbar">
      <div className="flex min-w-0 flex-col gap-2 md:flex-row md:items-center">
        <div className="min-w-0 flex-1">{search}</div>
        <div className="flex min-w-0 flex-wrap items-center gap-2">
          {filters}
          {sort}
          {primaryAction}
        </div>
      </div>
      {chips}
    </div>
  );
}

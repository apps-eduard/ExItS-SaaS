import { Filter, ArrowUpDown } from "lucide-react";
import type { ButtonHTMLAttributes, ReactNode } from "react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/cn";

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
      className={cn("rounded-full border border-border px-3", className)}
      {...props}
    >
      <Filter className="size-4 shrink-0" aria-hidden />
      <span>{children}</span>
      {activeCount > 0 ? (
        <span className="inline-flex min-h-6 min-w-6 items-center justify-center rounded-full bg-primary px-1.5 text-[length:var(--exits-text-xs)] text-primary-foreground">
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
    <div
      className="flex gap-2 overflow-x-auto overscroll-x-contain pb-1"
      role="list"
      aria-label={listLabel}
    >
      {items.map((item) => (
        <button
          key={item.id}
          type="button"
          role="listitem"
          className="inline-flex min-h-[var(--exits-touch-target-min)] shrink-0 items-center gap-1.5 rounded-full border border-border bg-surface px-3 text-[length:var(--exits-text-sm)] font-semibold focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
          onClick={() => onRemove(item.id)}
          aria-label={`Remove filter ${item.label}`}
        >
          <span>{item.label}</span>
          <span aria-hidden>×</span>
        </button>
      ))}
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
      className={cn("rounded-full border border-border px-3", className)}
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

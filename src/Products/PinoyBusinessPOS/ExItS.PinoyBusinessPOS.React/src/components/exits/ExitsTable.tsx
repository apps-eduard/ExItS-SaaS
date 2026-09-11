import {
  forwardRef,
  type ButtonHTMLAttributes,
  type HTMLAttributes,
  type InputHTMLAttributes,
  type ReactNode,
  type TdHTMLAttributes,
  type ThHTMLAttributes,
} from "react";
import { ArrowDown, ArrowUp, ArrowUpDown, ChevronLeft, ChevronRight } from "lucide-react";
import { cn } from "@/lib/cn";

/** Semantic cell alignment for ExitsTable (pages should prefer this over raw Tailwind). */
export type ExitsTableAlign = "text" | "numeric" | "money" | "actions" | "center";

export type ExitsTableSortDirection = "asc" | "desc" | null;

const alignClass: Record<ExitsTableAlign, string> = {
  text: "exits-table__cell--text",
  numeric: "exits-table__cell--numeric",
  money: "exits-table__cell--money",
  actions: "exits-table__cell--actions",
  center: "exits-table__cell--center",
};

export type ExitsTableContainerProps = HTMLAttributes<HTMLDivElement>;

/** Bordered rounded shell around desktop table + optional mobile list. */
export const ExitsTableContainer = forwardRef<HTMLDivElement, ExitsTableContainerProps>(
  function ExitsTableContainer({ className, ...props }, ref) {
    return <div ref={ref} className={cn("exits-table-container", className)} {...props} />;
  },
);

export type ExitsTableToolbarProps = HTMLAttributes<HTMLDivElement> & {
  search?: ReactNode;
  filter?: ReactNode;
  selection?: ReactNode;
};

/** Optional toolbar above the table (search / filter / selection summary). */
export function ExitsTableToolbar({
  className,
  search,
  filter,
  selection,
  children,
  ...props
}: ExitsTableToolbarProps) {
  return (
    <div className={cn("exits-table-toolbar", className)} data-testid="exits-table-toolbar" {...props}>
      <div className="exits-table-toolbar__controls">
        {search ? <div className="exits-table-toolbar__search">{search}</div> : null}
        {filter ? <div className="exits-table-toolbar__filter">{filter}</div> : null}
        {children}
      </div>
      {selection ? <div className="exits-table-toolbar__selection">{selection}</div> : null}
    </div>
  );
}

export type ExitsTableProps = HTMLAttributes<HTMLTableElement>;

export const ExitsTable = forwardRef<HTMLTableElement, ExitsTableProps>(function ExitsTable(
  { className, ...props },
  ref,
) {
  return (
    <div className="exits-table-scroll">
      <table ref={ref} className={cn("exits-table", className)} {...props} />
    </div>
  );
});

export type ExitsTableHeaderProps = HTMLAttributes<HTMLTableSectionElement>;

export const ExitsTableHeader = forwardRef<HTMLTableSectionElement, ExitsTableHeaderProps>(
  function ExitsTableHeader({ className, ...props }, ref) {
    return <thead ref={ref} className={cn("exits-table__header", className)} {...props} />;
  },
);

export type ExitsTableBodyProps = HTMLAttributes<HTMLTableSectionElement>;

export const ExitsTableBody = forwardRef<HTMLTableSectionElement, ExitsTableBodyProps>(
  function ExitsTableBody({ className, ...props }, ref) {
    return <tbody ref={ref} className={cn("exits-table__body", className)} {...props} />;
  },
);

export type ExitsTableFooterProps = HTMLAttributes<HTMLTableSectionElement>;

export const ExitsTableFooter = forwardRef<HTMLTableSectionElement, ExitsTableFooterProps>(
  function ExitsTableFooter({ className, ...props }, ref) {
    return <tfoot ref={ref} className={cn("exits-table__footer", className)} {...props} />;
  },
);

export type ExitsTableRowProps = HTMLAttributes<HTMLTableRowElement> & {
  /** Soft hover for future clickable/selectable tables. Off by default. */
  interactive?: boolean;
  selected?: boolean;
  editing?: boolean;
};

export const ExitsTableRow = forwardRef<HTMLTableRowElement, ExitsTableRowProps>(function ExitsTableRow(
  { className, interactive = false, selected = false, editing = false, ...props },
  ref,
) {
  return (
    <tr
      ref={ref}
      className={cn(
        "exits-table__row",
        interactive && "exits-table__row--interactive",
        selected && "exits-table__row--selected",
        editing && "exits-table__row--editing",
        className,
      )}
      data-selected={selected ? "true" : undefined}
      data-editing={editing ? "true" : undefined}
      {...props}
    />
  );
});

export type ExitsTableHeadProps = ThHTMLAttributes<HTMLTableCellElement> & {
  cellAlign?: ExitsTableAlign;
  sortable?: boolean;
  sortDirection?: ExitsTableSortDirection;
  onSort?: () => void;
};

export const ExitsTableHead = forwardRef<HTMLTableCellElement, ExitsTableHeadProps>(
  function ExitsTableHead(
    {
      className,
      cellAlign = "text",
      sortable = false,
      sortDirection = null,
      onSort,
      children,
      ...props
    },
    ref,
  ) {
    const SortIcon =
      sortDirection === "asc" ? ArrowUp : sortDirection === "desc" ? ArrowDown : ArrowUpDown;
    const testId =
      "data-testid" in props && typeof props["data-testid"] === "string"
        ? props["data-testid"]
        : undefined;

    return (
      <th
        ref={ref}
        className={cn(
          "exits-table__head",
          alignClass[cellAlign],
          sortable && "exits-table__head--sortable",
          sortDirection && "exits-table__head--sorted",
          className,
        )}
        data-align={cellAlign}
        aria-sort={
          sortable
            ? sortDirection === "asc"
              ? "ascending"
              : sortDirection === "desc"
                ? "descending"
                : "none"
            : undefined
        }
        {...props}
      >
        {sortable ? (
          <button
            type="button"
            className="exits-table__sort-btn"
            onClick={onSort}
            data-testid={testId ? `${testId}-sort` : undefined}
          >
            <span>{children}</span>
            <SortIcon
              className={cn(
                "exits-table__sort-icon",
                !sortDirection && "exits-table__sort-icon--idle",
              )}
              aria-hidden
            />
          </button>
        ) : (
          children
        )}
      </th>
    );
  },
);

export type ExitsTableCellProps = TdHTMLAttributes<HTMLTableCellElement> & {
  cellAlign?: ExitsTableAlign;
  /** Slightly stronger weight for primary numeric columns (e.g. line total). */
  emphasis?: "normal" | "semibold" | "bold";
};

export const ExitsTableCell = forwardRef<HTMLTableCellElement, ExitsTableCellProps>(
  function ExitsTableCell({ className, cellAlign = "text", emphasis = "normal", ...props }, ref) {
    return (
      <td
        ref={ref}
        className={cn(
          "exits-table__cell",
          alignClass[cellAlign],
          emphasis === "semibold" && "exits-table__cell--semibold",
          emphasis === "bold" && "exits-table__cell--bold",
          className,
        )}
        data-align={cellAlign}
        {...props}
      />
    );
  },
);

export type ExitsTableCheckboxProps = Omit<InputHTMLAttributes<HTMLInputElement>, "type"> & {
  indeterminate?: boolean;
};

/** Presentation-only checkbox for selection columns. */
export const ExitsTableCheckbox = forwardRef<HTMLInputElement, ExitsTableCheckboxProps>(
  function ExitsTableCheckbox({ className, indeterminate = false, ...props }, ref) {
    return (
      <input
        ref={(node) => {
          if (typeof ref === "function") {
            ref(node);
          } else if (ref) {
            ref.current = node;
          }
          if (node) {
            node.indeterminate = indeterminate;
          }
        }}
        type="checkbox"
        className={cn("exits-table__checkbox", className)}
        {...props}
      />
    );
  },
);

export type ExitsTablePaginationProps = HTMLAttributes<HTMLDivElement> & {
  page: number;
  pageSize: number;
  total: number;
  pageSizeOptions?: ReadonlyArray<number>;
  onPageChange: (page: number) => void;
  onPageSizeChange: (pageSize: number) => void;
  rowsPerPageLabel: string;
  previousLabel: string;
  nextLabel: string;
  rangeLabel: string;
};

/** Compact pagination footer; page is 1-based. */
export function ExitsTablePagination({
  className,
  page,
  pageSize,
  total,
  pageSizeOptions = [10, 25, 50],
  onPageChange,
  onPageSizeChange,
  rowsPerPageLabel,
  previousLabel,
  nextLabel,
  rangeLabel,
  ...props
}: ExitsTablePaginationProps) {
  const pageCount = Math.max(1, Math.ceil(total / pageSize) || 1);
  const safePage = Math.min(Math.max(page, 1), pageCount);
  const from = total === 0 ? 0 : (safePage - 1) * pageSize + 1;
  const to = Math.min(safePage * pageSize, total);
  const canPrev = safePage > 1;
  const canNext = safePage < pageCount;

  return (
    <div
      className={cn("exits-table-pagination", className)}
      data-testid="exits-table-pagination"
      {...props}
    >
      <p className="exits-table-pagination__range" data-testid="exits-table-pagination-range">
        {rangeLabel.replace("{from}", String(from)).replace("{to}", String(to)).replace("{total}", String(total))}
      </p>
      <label className="exits-table-pagination__size">
        <span>{rowsPerPageLabel}</span>
        <select
          className="exits-select exits-table-pagination__size-select"
          value={pageSize}
          onChange={(e) => onPageSizeChange(Number(e.target.value))}
          data-testid="exits-table-page-size"
        >
          {pageSizeOptions.map((size) => (
            <option key={size} value={size}>
              {size}
            </option>
          ))}
        </select>
      </label>
      <div className="exits-table-pagination__nav">
        <PaginationNavButton
          disabled={!canPrev}
          aria-label={previousLabel}
          data-testid="exits-table-prev"
          onClick={() => onPageChange(safePage - 1)}
        >
          <ChevronLeft className="size-4" aria-hidden />
          <span>{previousLabel}</span>
        </PaginationNavButton>
        <span className="exits-table-pagination__page" data-testid="exits-table-page">
          {safePage}
        </span>
        <PaginationNavButton
          disabled={!canNext}
          aria-label={nextLabel}
          data-testid="exits-table-next"
          onClick={() => onPageChange(safePage + 1)}
        >
          <span>{nextLabel}</span>
          <ChevronRight className="size-4" aria-hidden />
        </PaginationNavButton>
      </div>
    </div>
  );
}

function PaginationNavButton({
  className,
  ...props
}: ButtonHTMLAttributes<HTMLButtonElement>) {
  return (
    <button type="button" className={cn("exits-table-pagination__btn", className)} {...props} />
  );
}

export type ExitsTableMobileProps = HTMLAttributes<HTMLUListElement>;

/**
 * Same visual container tokens as the desktop table; shown below the md breakpoint.
 * Page supplies row content — no second data model.
 */
export const ExitsTableMobile = forwardRef<HTMLUListElement, ExitsTableMobileProps>(
  function ExitsTableMobile({ className, ...props }, ref) {
    return <ul ref={ref} className={cn("exits-table-mobile", className)} {...props} />;
  },
);

export type ExitsTableMobileRowProps = HTMLAttributes<HTMLLIElement> & {
  selected?: boolean;
};

export const ExitsTableMobileRow = forwardRef<HTMLLIElement, ExitsTableMobileRowProps>(
  function ExitsTableMobileRow({ className, selected = false, ...props }, ref) {
    return (
      <li
        ref={ref}
        className={cn(
          "exits-table-mobile__row",
          selected && "exits-table-mobile__row--selected",
          className,
        )}
        data-selected={selected ? "true" : undefined}
        {...props}
      />
    );
  },
);

/** Cycle none → asc → desc → none for page-owned sort state. */
export function cycleExitsTableSort(
  currentKey: string | null,
  currentDirection: ExitsTableSortDirection,
  nextKey: string,
): { key: string | null; direction: ExitsTableSortDirection } {
  if (currentKey !== nextKey) {
    return { key: nextKey, direction: "asc" };
  }
  if (currentDirection === "asc") {
    return { key: nextKey, direction: "desc" };
  }
  if (currentDirection === "desc") {
    return { key: null, direction: null };
  }
  return { key: nextKey, direction: "asc" };
}

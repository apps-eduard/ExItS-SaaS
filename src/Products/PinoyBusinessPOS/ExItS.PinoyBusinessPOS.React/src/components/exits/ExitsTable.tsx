import {
  forwardRef,
  type HTMLAttributes,
  type TdHTMLAttributes,
  type ThHTMLAttributes,
} from "react";
import { cn } from "@/lib/cn";

/** Semantic cell alignment for ExitsTable (pages should prefer this over raw Tailwind). */
export type ExitsTableAlign = "text" | "numeric" | "money" | "actions" | "center";

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
};

export const ExitsTableHead = forwardRef<HTMLTableCellElement, ExitsTableHeadProps>(
  function ExitsTableHead({ className, cellAlign = "text", ...props }, ref) {
    return (
      <th
        ref={ref}
        className={cn("exits-table__head", alignClass[cellAlign], className)}
        data-align={cellAlign}
        {...props}
      />
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

export type ExitsTableMobileRowProps = HTMLAttributes<HTMLLIElement>;

export const ExitsTableMobileRow = forwardRef<HTMLLIElement, ExitsTableMobileRowProps>(
  function ExitsTableMobileRow({ className, ...props }, ref) {
    return <li ref={ref} className={cn("exits-table-mobile__row", className)} {...props} />;
  },
);

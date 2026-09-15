import { Fragment, type ReactNode } from "react";
import { ExitsDataRecordCard, type ExitsDataRecordField } from "@/components/exits/ExitsDataRecordCard";
import { ExitsResponsiveDataView } from "@/components/exits/ExitsResponsiveDataView";
import {
  ExitsTable,
  ExitsTableActions,
  ExitsTableBody,
  ExitsTableCell,
  ExitsTableContainer,
  ExitsTableHead,
  ExitsTableHeader,
  ExitsTableRow,
  type ExitsTableAlign,
  type ExitsTableColSize,
} from "@/components/exits/ExitsTable";
import type { ResponsiveDataLayout } from "@/components/exits/responsive-data-view";
import { cn } from "@/lib/cn";

export type ProductSelectionColumn = {
  id: string;
  header: ReactNode;
  cellAlign?: ExitsTableAlign;
  colSize?: ExitsTableColSize;
  className?: string;
};

/**
 * Presentation row — feature adapters map domain DTOs into this shape.
 * No PO / Receive / Transfer mode branching lives here.
 */
export type ProductSelectionRow = {
  id: string;
  testId: string;
  title: ReactNode;
  subtitle?: ReactNode;
  status?: ReactNode;
  /** Table body cells in the same order as `columns` (action column is separate). */
  cells: readonly ReactNode[];
  /** List/card key fields (category, stock, price, etc.). */
  fields?: readonly ExitsDataRecordField[];
  primaryAction: ReactNode;
  moreActions?: ReactNode;
  /** Secondary controls (e.g. lot select) — list card details + table sub-row. */
  details?: ReactNode;
  selected?: boolean;
};

export type ProductSelectionViewProps = {
  layout: ResponsiveDataLayout;
  columns: readonly ProductSelectionColumn[];
  rows: readonly ProductSelectionRow[];
  /** Action column header (Add / Qty / Action). */
  actionHeader: ReactNode;
  actionColClassName?: string;
  toolbar?: ReactNode;
  pagination?: ReactNode;
  testId?: string;
  className?: string;
  tableClassName?: string;
  listClassName?: string;
};

/**
 * Domain-neutral product selection presentation shell.
 * Desktop TABLE / narrow LIST via ExitsResponsiveDataView — same rows in both modes.
 */
export function ProductSelectionView({
  layout,
  columns,
  rows,
  actionHeader,
  actionColClassName,
  toolbar,
  pagination,
  testId = "product-selection",
  className,
  tableClassName,
  listClassName,
}: ProductSelectionViewProps) {
  const colSpan = columns.length + 1;

  const tableBody = (
    <ExitsTableContainer className={cn("product-selection__table", tableClassName)}>
      <ExitsTable>
        <ExitsTableHeader>
          <ExitsTableRow>
            {columns.map((column) => (
              <ExitsTableHead
                key={column.id}
                cellAlign={column.cellAlign ?? "text"}
                colSize={column.colSize}
                className={column.className}
              >
                {column.header}
              </ExitsTableHead>
            ))}
            <ExitsTableHead
              cellAlign="center"
              colSize="actions"
              className={cn("product-selection__action-col", actionColClassName)}
            >
              {actionHeader}
            </ExitsTableHead>
          </ExitsTableRow>
        </ExitsTableHeader>
        <ExitsTableBody>
          {rows.map((row) => (
            <Fragment key={row.id}>
              <ExitsTableRow data-testid={row.testId} data-selected={row.selected || undefined}>
                {columns.map((column, index) => (
                  <ExitsTableCell
                    key={column.id}
                    cellAlign={column.cellAlign ?? "text"}
                    colSize={column.colSize}
                    className={column.className}
                  >
                    {row.cells[index]}
                  </ExitsTableCell>
                ))}
                <ExitsTableCell
                  cellAlign="center"
                  colSize="actions"
                  className={cn("product-selection__action-col", actionColClassName)}
                >
                  <ExitsTableActions className="justify-center">
                    {row.primaryAction}
                    {row.moreActions}
                  </ExitsTableActions>
                </ExitsTableCell>
              </ExitsTableRow>
              {row.details ? (
                <ExitsTableRow className="product-selection__details-row">
                  <ExitsTableCell colSpan={colSpan} className="product-selection__details-cell">
                    {row.details}
                  </ExitsTableCell>
                </ExitsTableRow>
              ) : null}
            </Fragment>
          ))}
        </ExitsTableBody>
      </ExitsTable>
    </ExitsTableContainer>
  );

  const listBody = (
    <ul className={cn("exits-data-record-list product-selection__list", listClassName)}>
      {rows.map((row) => (
        <ExitsDataRecordCard
          key={row.id}
          as="li"
          data-testid={row.testId}
          title={row.title}
          subtitle={row.subtitle}
          status={row.status}
          fields={row.fields}
          primaryAction={row.primaryAction}
          moreActions={row.moreActions}
          details={row.details}
          selected={row.selected}
        />
      ))}
    </ul>
  );

  return (
    <ExitsResponsiveDataView
      layout={layout}
      testId={testId}
      className={cn("product-selection", className)}
      toolbar={toolbar}
      table={layout === "table" ? tableBody : null}
      list={layout === "list" ? listBody : null}
      pagination={pagination}
    />
  );
}

export type ProductSelectionToolbarProps = {
  children: ReactNode;
  className?: string;
  testId?: string;
};

/** Shared filter toolbar chrome — wraps/reflows on tablet/mobile. */
export function ProductSelectionToolbar({
  children,
  className,
  testId = "product-selection-toolbar",
}: ProductSelectionToolbarProps) {
  return (
    <div className={cn("product-selection__toolbar", className)} data-testid={testId}>
      {children}
    </div>
  );
}

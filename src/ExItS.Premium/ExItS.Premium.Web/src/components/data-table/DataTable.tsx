import { useId, type CSSProperties, type KeyboardEvent as ReactKeyboardEvent, type PointerEvent as ReactPointerEvent, type ReactNode } from "react";
import { Button } from "../button/Button";
import { Checkbox } from "../checkbox/Checkbox";
import { EmptyState } from "../empty/EmptyState";
import { ActionMenu } from "../menu/DropdownMenu";
import { SearchField } from "../search/SearchField";
import { Select } from "../select/Select";
import { Skeleton } from "../skeleton/Skeleton";
import { ExIcon } from "../../icons/ExIcon";
import { pageItems } from "./query";
import { DataTableExport } from "./DataTableExport";
import type { DataTableColumn, DataTableDensity, DataTableFilter, DataTableRowAction, DataTableSelectionMode } from "./types";
import { useDataTable } from "./useDataTable";
import styles from "./DataTable.module.scss";

const SELECT_WIDTH = 52;

export type DataTableProps<T extends { id: string }> = {
  title: string;
  columns: readonly DataTableColumn<T>[];
  rows: readonly T[];
  getRowId?: (row: T) => string;
  selectionLabel?: (row: T) => string;
  density?: DataTableDensity;
  selectionMode?: DataTableSelectionMode;
  searchable?: boolean;
  searchLabel?: string;
  searchPlaceholder?: string;
  filters?: readonly DataTableFilter[];
  primaryAction?: ReactNode;
  renderBulkActions?: (selectedIds: readonly string[]) => ReactNode;
  rowActions?: DataTableRowAction<T>;
  onRowAction?: (row: T, actionId: string) => void;
  onSelectionChange?: (selectedIds: readonly string[]) => void;
  loading?: boolean;
  error?: string | null;
  onRetry?: () => void;
  emptyTitle?: string;
  emptyDescription?: string;
  pageSizeOptions?: readonly number[];
  initialPageSize?: number;
  enableColumnResize?: boolean;
  enableColumnVisibility?: boolean;
  enableColumnFilters?: boolean;
};

function sortIcon(direction: "asc" | "desc" | undefined) {
  if (direction === "asc") return "arrow_upward" as const;
  if (direction === "desc") return "arrow_downward" as const;
  return "unfold_more" as const;
}

function ariaSort(direction: "asc" | "desc" | undefined): "ascending" | "descending" | "none" {
  if (direction === "asc") return "ascending";
  if (direction === "desc") return "descending";
  return "none";
}

export function DataTable<T extends { id: string }>({
  title,
  columns,
  rows,
  getRowId,
  selectionLabel,
  density,
  selectionMode = "multiple",
  searchable = true,
  searchLabel = "Search",
  searchPlaceholder = "Search records",
  filters = [],
  primaryAction,
  renderBulkActions,
  rowActions,
  onRowAction,
  onSelectionChange,
  loading = false,
  error = null,
  onRetry,
  emptyTitle = "No records yet",
  emptyDescription = "Records you add will show up here.",
  pageSizeOptions,
  initialPageSize,
  enableColumnResize = true,
  enableColumnVisibility = true,
  enableColumnFilters = true,
}: DataTableProps<T>) {
  const titleId = useId();
  const table = useDataTable({
    rows,
    columns,
    getRowId,
    selectionMode,
    pageSizeOptions,
    initialPageSize,
  });
  const showSelection = selectionMode !== "none";
  const showActions = Boolean(rowActions);
  const columnCount = table.visibleColumns.length + (showSelection ? 1 : 0) + (showActions ? 1 : 0);
  const filterable = enableColumnFilters && table.visibleColumns.some((column) => column.filterable !== false);
  const hideable = columns.filter((column) => column.hideable !== false);

  let pinLeft = showSelection ? SELECT_WIDTH : 0;
  const pinOffsets = new Map<string, number>();
  const pinnedIds: string[] = [];
  for (const column of table.visibleColumns) {
    if (column.pin !== "start") continue;
    pinOffsets.set(column.id, pinLeft);
    pinnedIds.push(column.id);
    pinLeft += table.columnWidth(column);
  }
  const lastPinned = pinnedIds[pinnedIds.length - 1];

  const countLabel = loading
    ? "Loading records"
    : error
      ? "Unable to load records"
      : table.filteredCount === table.totalCount
        ? `${table.totalCount.toLocaleString("en-US")} records`
        : `${table.filteredCount.toLocaleString("en-US")} of ${table.totalCount.toLocaleString("en-US")} records`;

  function notifySelection(next: readonly string[]) {
    onSelectionChange?.(next);
  }

  function onSortClick(columnId: string, additive: boolean) {
    table.sortBy(columnId, additive);
  }

  function onResizeKey(event: ReactKeyboardEvent<HTMLSpanElement>, column: DataTableColumn<T>) {
    const current = table.columnWidth(column);
    if (event.key === "ArrowRight") {
      event.preventDefault();
      table.resizeColumn(column.id, current + 16);
    } else if (event.key === "ArrowLeft") {
      event.preventDefault();
      table.resizeColumn(column.id, current - 16);
    } else if (event.key === "Home") {
      event.preventDefault();
      table.resizeColumn(column.id, column.width ?? 160);
    }
  }

  function onResizePointer(event: ReactPointerEvent<HTMLSpanElement>, column: DataTableColumn<T>) {
    if (event.button !== 0) return;
    event.preventDefault();
    const handle = event.currentTarget;
    const startX = event.clientX;
    const startWidth = table.columnWidth(column);
    handle.setPointerCapture(event.pointerId);
    const move = (pointer: PointerEvent) => {
      table.resizeColumn(column.id, startWidth + (pointer.clientX - startX));
    };
    const stop = () => {
      handle.removeEventListener("pointermove", move);
      handle.removeEventListener("pointerup", stop);
      handle.removeEventListener("pointercancel", stop);
    };
    handle.addEventListener("pointermove", move);
    handle.addEventListener("pointerup", stop);
    handle.addEventListener("pointercancel", stop);
  }

  const allOnPage = table.pageIds.length > 0 && table.selectedOnPage === table.pageIds.length;
  const someOnPage = table.selectedOnPage > 0 && !allOnPage;
  const pages = pageItems(table.page, table.pageCount);

  return (
    <section className={styles.root} data-density={density} aria-labelledby={titleId}>
      <div className={styles.frame}>
        <div className={styles.heading}>
          <h2 id={titleId} className={styles.title}>
            {title}
          </h2>
          <p className={styles.count} aria-live="polite">
            {countLabel}
            {table.filteredCount > 0 && !loading && !error
              ? ` · ${table.rangeStart.toLocaleString("en-US")}–${table.rangeEnd.toLocaleString("en-US")}`
              : ""}
          </p>
        </div>

        <div className={styles.toolbar}>
          {searchable ? (
            <div className={styles.search}>
              <SearchField label={searchLabel} value={table.query} placeholder={searchPlaceholder} onChange={table.updateQuery} />
            </div>
          ) : null}
          {filters.map((filter) => (
            <div key={filter.id} className={styles.filter}>
              <Select
                label={filter.label}
                value={table.optionFilters[filter.columnId] ?? ""}
                onChange={(event) => table.updateOptionFilter(filter.columnId, event.target.value)}
              >
                <option value="">All</option>
                {filter.options.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </Select>
            </div>
          ))}
          {enableColumnVisibility ? (
            <details className={styles.columnMenu}>
              <summary>Columns</summary>
              <div className={styles.columnPanel}>
                {hideable.map((column) => (
                  <Checkbox
                    key={column.id}
                    label={column.header}
                    checked={!table.hidden.includes(column.id)}
                    onChange={() => table.toggleColumn(column.id)}
                  />
                ))}
                <Button type="button" variant="secondary" onClick={table.restoreColumns}>
                  Restore defaults
                </Button>
              </div>
            </details>
          ) : null}
          {table.filtersActive ? (
            <Button type="button" variant="ghost" onClick={table.clearFilters}>
              Clear all
            </Button>
          ) : null}
          <div className={styles.actions}>
            <DataTableExport
              title={title}
              columns={table.visibleColumns}
              sortedRows={table.sortedRows}
              pageRows={table.pageRows}
              selectedIds={table.selected}
              getRowId={table.getRowId}
              disabled={loading}
              blockedReason={error}
            />
            {primaryAction}
          </div>
        </div>

        {selectionMode !== "none" && table.selected.length > 0 ? (
          <div className={styles.bulk} role="status">
            <span className={styles.bulkCount}>{table.selected.length.toLocaleString("en-US")} selected</span>
            {renderBulkActions?.(table.selected)}
            <Button
              type="button"
              variant="ghost"
              onClick={() => {
                table.clearSelection();
                notifySelection([]);
              }}
            >
              Clear selection
            </Button>
          </div>
        ) : null}

        <div className={styles.scroll}>
          <table className={styles.table} aria-labelledby={titleId} aria-busy={loading || undefined}>
            <caption className={styles.srOnly}>{title}</caption>
            <thead>
              <tr>
                {showSelection ? (
                  <th scope="col" className={`${styles.check} ${styles.pin}`} style={{ "--pin-left": "0px", width: SELECT_WIDTH } as CSSProperties}>
                    {selectionMode === "multiple" ? (
                      <Checkbox
                        label="Select all rows on this page"
                        checked={allOnPage}
                        indeterminate={someOnPage}
                        disabled={loading || Boolean(error) || table.pageIds.length === 0}
                        onChange={() => {
                          const pageIds = table.pageIds;
                          const next = allOnPage
                            ? table.selected.filter((id) => !pageIds.includes(id))
                            : [...table.selected, ...pageIds.filter((id) => !table.selected.includes(id))];
                          table.toggleCurrentPage();
                          notifySelection(next);
                        }}
                      />
                    ) : (
                      <span className={styles.srOnly}>Select</span>
                    )}
                  </th>
                ) : null}
                {table.visibleColumns.map((column) => {
                  const sort = table.sorts.find((item) => item.id === column.id);
                  const rank = table.sorts.findIndex((item) => item.id === column.id);
                  const pinned = column.pin === "start";
                  const width = table.columnWidth(column);
                  return (
                    <th
                      key={column.id}
                      scope="col"
                      aria-sort={column.sortable === false ? undefined : ariaSort(sort?.direction)}
                      className={[pinned ? styles.pin : "", column.id === lastPinned ? styles.pinEdge : "", column.align === "end" ? styles.end : ""]
                        .filter(Boolean)
                        .join(" ")}
                      style={
                        {
                          width,
                          minWidth: column.minWidth ?? 72,
                          "--pin-left": pinned ? `${pinOffsets.get(column.id) ?? 0}px` : undefined,
                        } as CSSProperties
                      }
                    >
                      <div className={styles.headerCell}>
                        {column.sortable === false ? (
                          <span className={styles.sortStatic}>{column.header}</span>
                        ) : (
                          <button type="button" className={styles.sort} onClick={(event) => onSortClick(column.id, event.shiftKey)}>
                            <span>{column.header}</span>
                            {rank > 0 ? <span className={styles.rank}>{rank + 1}</span> : null}
                            <ExIcon name={sortIcon(sort?.direction)} />
                            <span className={styles.srOnly}>
                              {sort?.direction === "asc" ? "sorted ascending" : sort?.direction === "desc" ? "sorted descending" : "not sorted"}
                            </span>
                          </button>
                        )}
                        {enableColumnResize ? (
                          <span
                            className={styles.resize}
                            role="separator"
                            tabIndex={0}
                            aria-orientation="vertical"
                            aria-label={`Resize ${column.header}`}
                            aria-valuemin={column.minWidth ?? 72}
                            aria-valuenow={width}
                            aria-valuemax={640}
                            onPointerDown={(event) => onResizePointer(event, column)}
                            onKeyDown={(event) => onResizeKey(event, column)}
                          />
                        ) : null}
                      </div>
                    </th>
                  );
                })}
                {showActions ? (
                  <th scope="col" className={styles.end} style={{ width: 72 }}>
                    <span className={styles.sortStatic}>Actions</span>
                  </th>
                ) : null}
              </tr>
              {filterable ? (
                <tr className={styles.filterRow}>
                  {showSelection ? <td aria-hidden="true" /> : null}
                  {table.visibleColumns.map((column) => (
                    <td key={column.id}>
                      {column.filterable === false ? null : (
                        <input
                          className={styles.filterInput}
                          type="search"
                          value={table.columnFilters[column.id] ?? ""}
                          aria-label={`Filter ${column.header}`}
                          onChange={(event) => table.updateColumnFilter(column.id, event.target.value)}
                        />
                      )}
                    </td>
                  ))}
                  {showActions ? <td aria-hidden="true" /> : null}
                </tr>
              ) : null}
            </thead>
            <tbody>
              {error ? (
                <tr>
                  <td colSpan={columnCount}>
                    <div className={styles.state} role="alert">
                      <EmptyState
                        icon="error"
                        title="Couldn't load records"
                        description={error}
                        action={
                          onRetry ? (
                            <Button type="button" onClick={onRetry}>
                              Retry
                            </Button>
                          ) : null
                        }
                      />
                    </div>
                  </td>
                </tr>
              ) : loading ? (
                Array.from({ length: 6 }, (_, index) => (
                  <tr key={`skeleton-${index}`} className={styles.skeletonRow}>
                    <td colSpan={columnCount}>
                      <Skeleton height="0.875rem" label={index === 0 ? "Loading records" : ""} />
                    </td>
                  </tr>
                ))
              ) : rows.length === 0 ? (
                <tr>
                  <td colSpan={columnCount}>
                    <div className={styles.state}>
                      <EmptyState icon="inbox" title={emptyTitle} description={emptyDescription} />
                    </div>
                  </td>
                </tr>
              ) : table.filteredCount === 0 ? (
                <tr>
                  <td colSpan={columnCount}>
                    <div className={styles.state}>
                      <EmptyState
                        icon="search"
                        title="No matching records"
                        description="Try a different search or clear the filters."
                        action={
                          <Button type="button" variant="secondary" onClick={table.clearFilters}>
                            Clear filters
                          </Button>
                        }
                      />
                    </div>
                  </td>
                </tr>
              ) : (
                table.pageRows.map((row) => {
                  const id = table.getRowId(row);
                  const isSelected = table.selected.includes(id);
                  return (
                    <tr key={id} className={styles.bodyRow} data-selected={isSelected ? "true" : undefined}>
                      {showSelection ? (
                        <td className={`${styles.check} ${styles.pin}`} style={{ "--pin-left": "0px" } as CSSProperties}>
                          <Checkbox
                            label={selectionLabel ? `Select ${selectionLabel(row)}` : `Select ${id}`}
                            checked={isSelected}
                            onChange={() => {
                              const next =
                                selectionMode === "single"
                                  ? isSelected
                                    ? []
                                    : [id]
                                  : isSelected
                                    ? table.selected.filter((item) => item !== id)
                                    : [...table.selected, id];
                              table.toggleRow(id);
                              notifySelection(next);
                            }}
                          />
                        </td>
                      ) : null}
                      {table.visibleColumns.map((column) => {
                        const pinned = column.pin === "start";
                        return (
                          <td
                            key={column.id}
                            className={[
                              pinned ? styles.pin : "",
                              column.id === lastPinned ? styles.pinEdge : "",
                              column.align === "end" ? styles.end : "",
                              column.wrap ? styles.wrap : "",
                            ]
                              .filter(Boolean)
                              .join(" ")}
                            style={pinned ? ({ "--pin-left": `${pinOffsets.get(column.id) ?? 0}px` } as CSSProperties) : undefined}
                          >
                            {column.cell ? column.cell(row) : cellFallback(column, row)}
                          </td>
                        );
                      })}
                      {showActions && rowActions ? (
                        <td className={styles.end}>
                          <ActionMenu
                            label={`Actions for ${selectionLabel?.(row) ?? id}`}
                            items={rowActions(row)}
                            onSelect={(actionId) => onRowAction?.(row, actionId)}
                          />
                        </td>
                      ) : null}
                    </tr>
                  );
                })
              )}
            </tbody>
          </table>
        </div>

        <div className={styles.footer}>
          <div className={styles.pageSize}>
            <Select
              label="Rows per page"
              value={String(table.pageSize)}
              disabled={loading || Boolean(error)}
              onChange={(event) => table.setPageSize(Number(event.target.value))}
            >
              {table.pageSizeOptions.map((size) => (
                <option key={size} value={size}>
                  {size}
                </option>
              ))}
            </Select>
          </div>
          <div className={styles.pager} role="navigation" aria-label={`${title} pages`}>
            <Button type="button" variant="ghost" disabled={table.page <= 1} onClick={() => table.setPage(table.page - 1)}>
              Previous
            </Button>
            {pages.map((item, index) =>
              item === "gap" ? (
                <span key={`gap-${index}`} className={styles.gap} aria-hidden="true">
                  …
                </span>
              ) : (
                <Button
                  key={item}
                  type="button"
                  variant={item === table.page ? "primary" : "ghost"}
                  aria-current={item === table.page ? "page" : undefined}
                  aria-label={`Page ${item}`}
                  onClick={() => table.setPage(item)}
                >
                  {item}
                </Button>
              ),
            )}
            <Button type="button" variant="ghost" disabled={table.page >= table.pageCount} onClick={() => table.setPage(table.page + 1)}>
              Next
            </Button>
          </div>
        </div>
      </div>
    </section>
  );
}

function cellFallback<T>(column: DataTableColumn<T>, row: T) {
  const value = column.accessor(row);
  if (value instanceof Date) return value.toLocaleDateString("en-US");
  if (value == null || value === "") return "—";
  return String(value);
}

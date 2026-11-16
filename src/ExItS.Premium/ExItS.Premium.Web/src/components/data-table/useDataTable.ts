import { useMemo, useState } from "react";
import { filterRows, nextSort, paginate, sortRows, toggleId, togglePage } from "./query";
import type { ColumnSort, DataTableColumn, DataTableSelectionMode } from "./types";

const DEFAULT_PAGE_SIZES = [10, 25, 50] as const;

type UseDataTableOptions<T extends { id: string }> = {
  rows: readonly T[];
  columns: readonly DataTableColumn<T>[];
  getRowId?: (row: T) => string;
  selectionMode?: DataTableSelectionMode;
  pageSizeOptions?: readonly number[];
  initialPageSize?: number;
};

function defaultRowId<T extends { id: string }>(row: T) {
  return row.id;
}

export function useDataTable<T extends { id: string }>({
  rows,
  columns,
  getRowId = defaultRowId,
  selectionMode = "multiple",
  pageSizeOptions = DEFAULT_PAGE_SIZES,
  initialPageSize = 10,
}: UseDataTableOptions<T>) {
  const [sorts, setSorts] = useState<ColumnSort[]>([]);
  const [query, setQuery] = useState("");
  const [columnFilters, setColumnFilters] = useState<Record<string, string>>({});
  const [optionFilters, setOptionFilters] = useState<Record<string, string>>({});
  const [page, setPage] = useState(1);
  const [pageSize, setPageSizeState] = useState(initialPageSize);
  const [selected, setSelected] = useState<string[]>([]);
  const [hidden, setHidden] = useState<string[]>([]);
  const [widths, setWidths] = useState<Record<string, number>>({});

  const rowIds = useMemo(() => new Set(rows.map((row) => getRowId(row))), [rows, getRowId]);
  const selectedIds = useMemo(() => selected.filter((id) => rowIds.has(id)), [selected, rowIds]);

  const visibleColumns = useMemo(
    () => columns.filter((column) => !hidden.includes(column.id)),
    [columns, hidden],
  );

  const filtered = useMemo(
    () => filterRows(rows, columns, query, columnFilters, optionFilters),
    [rows, columns, query, columnFilters, optionFilters],
  );

  const sorted = useMemo(() => sortRows(filtered, sorts, columns), [filtered, sorts, columns]);
  const paged = paginate(sorted, page, pageSize);

  const pageIds = paged.rows.map((row) => getRowId(row));
  const selectedOnPage = pageIds.filter((id) => selectedIds.includes(id)).length;
  const filtersActive =
    query.trim().length > 0 ||
    Object.values(columnFilters).some((value) => value.trim().length > 0) ||
    Object.values(optionFilters).some((value) => value.length > 0);

  function sortBy(columnId: string, additive: boolean) {
    setSorts((current) => nextSort(current, columnId, additive));
  }

  function updateQuery(value: string) {
    setQuery(value);
    setPage(1);
  }

  function updateColumnFilter(columnId: string, value: string) {
    setColumnFilters((current) => ({ ...current, [columnId]: value }));
    setPage(1);
  }

  function updateOptionFilter(columnId: string, value: string) {
    setOptionFilters((current) => ({ ...current, [columnId]: value }));
    setPage(1);
  }

  function clearFilters() {
    setQuery("");
    setColumnFilters({});
    setOptionFilters({});
    setPage(1);
  }

  function setPageSize(size: number) {
    setPageSizeState(size);
    setPage(1);
  }

  function toggleRow(id: string) {
    if (selectionMode === "none") return;
    setSelected(toggleId(selectedIds, id, selectionMode));
  }

  function toggleCurrentPage() {
    if (selectionMode !== "multiple") return;
    setSelected(togglePage(selectedIds, pageIds));
  }

  function clearSelection() {
    setSelected([]);
  }

  function toggleColumn(columnId: string) {
    setHidden((current) => {
      const visible = columns.filter((column) => !current.includes(column.id));
      if (!current.includes(columnId) && visible.length <= 1) return current;
      return current.includes(columnId) ? current.filter((id) => id !== columnId) : [...current, columnId];
    });
  }

  function resizeColumn(columnId: string, width: number) {
    const column = columns.find((item) => item.id === columnId);
    const min = column?.minWidth ?? 72;
    const next = Math.max(min, Math.min(640, Math.round(width)));
    setWidths((current) => ({ ...current, [columnId]: next }));
  }

  function restoreColumns() {
    setHidden([]);
    setWidths({});
  }

  function columnWidth(column: DataTableColumn<T>) {
    return widths[column.id] ?? column.width ?? 160;
  }

  return {
    sorts,
    query,
    columnFilters,
    optionFilters,
    page: paged.page,
    pageCount: paged.pageCount,
    pageSize,
    pageSizeOptions,
    pageRows: paged.rows,
    sortedRows: sorted,
    rangeStart: sorted.length === 0 ? 0 : paged.start + 1,
    rangeEnd: paged.start + paged.rows.length,
    filteredCount: filtered.length,
    totalCount: rows.length,
    visibleColumns,
    selected: selectedIds,
    selectedOnPage,
    pageIds,
    filtersActive,
    hidden,
    getRowId,
    sortBy,
    updateQuery,
    updateColumnFilter,
    updateOptionFilter,
    clearFilters,
    setPage,
    setPageSize,
    toggleRow,
    toggleCurrentPage,
    clearSelection,
    toggleColumn,
    resizeColumn,
    restoreColumns,
    columnWidth,
    selectionMode,
  };
}

export type DataTableState<T extends { id: string }> = ReturnType<typeof useDataTable<T>>;
